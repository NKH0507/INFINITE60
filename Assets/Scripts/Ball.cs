using System;
using System.Collections.Generic; // 관통 검사 결과를 재사용할 List에 필요
using UnityEngine;
using UnityEngine.InputSystem;

public class Ball : MonoBehaviour
{
    public bool isTouchDown;
    //공의 공격력
    public float power;
    Vector3 baseScale;
    Vector3 targetScale;
    public float growSpeed = 10f;

    // 화면에 보이는 크기의 기준 공격력.
    // 실제 공격력(power)은 벽돌을 깨는 순간 바로 오르고,
    // 보이는 크기는 흡수 조각이 공에 닿을 때마다 조금씩 따라 오른다.
    float shownPower;
    float lastAbsorbQueuedTime;
    const float ShowCatchUpDelay = 1.5f; // 조각이 사라져도 이 시간이 지나면 크기를 맞춘다.

    // 조각을 먹을 때 공이 통통 커지는 정도와 반짝임
    [SerializeField] float absorbPulse = 0.12f;
    [SerializeField] float pulseDecay = 10f;
    [SerializeField] Color flashColor = new Color(1f, 0.95f, 0.7f, 0.8f);
    float pulse;
    float flash;
    Vector3 smoothScale;
    SpriteRenderer flashRenderer;

    public bool isLaunched = false;
    Vector2 startMousePos;
    Vector3 startBallPos;

    public bool isTransitioning = false;

    Rigidbody2D rigid;
    // 매 물리 프레임마다 배열을 새로 만들지 않고 검사 결과 목록을 재사용한다.
    readonly List<RaycastHit2D> piercingHits = new List<RaycastHit2D>(32);
    [UnityEngine.Serialization.FormerlySerializedAs("panel")]
    public Transform paddle;
    public float offsetY = 0.5f;

    //전환 상태 저장 변수
    Vector3 savedPosition;
    Vector2 savedVelocity;
    float savedSpeed;
    bool savedIsLaunched;

    GameManager gameManager;

    // 발사 방향 표시용 LineRenderer
    public LineRenderer aimLine;

    // 화살촉 왼쪽과 오른쪽 선
    public LineRenderer arrowLeft;
    public LineRenderer arrowRight;

    // 화살표 길이
    public float arrowLength = 2.5f;

    // 화살촉 길이
    public float arrowHeadLength = 0.4f;

    // 화살촉 벌어지는 각도
    public float arrowHeadAngle = 30f;

    // 공의 고정 발사 속도
    public float launchSpeed = 10f;
    float currentSpeed;

    // 벽돌 파괴 시 흡수하는 비율 (10%)
    public float absorptionRate = 0.1f;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.simulated = false; // 발사 전에는 물리 영향을 받지 않게 함
        startBallPos = transform.position; // 현재 공의 위치를 저장
        gameManager = FindFirstObjectByType<GameManager>(); //GameManager 찾기

        // 처음에는 방향 표시 끄기
        aimLine.enabled = false;
        arrowLeft.enabled = false;
        arrowRight.enabled = false;

        currentSpeed = launchSpeed;
        baseScale = transform.localScale;
        // Inspector의 Power를 시작 공격력으로 사용한다. 0 이하이면 기본 공격력을 사용한다.
        power = gameManager.ClampAttackPower(power > 0f ? power : gameManager.firstStagePower);
        RefreshSize(true);
        CreateFlash();
    }

    // 공 뒤에서 반짝이는 빛무리 (공과 같은 모양을 조금 크게)
    void CreateFlash()
    {
        SpriteRenderer ballRenderer = GetComponent<SpriteRenderer>();
        if (ballRenderer == null) return;

        GameObject glow = new GameObject("AbsorbFlash");
        glow.transform.SetParent(transform, false);
        glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        flashRenderer = glow.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = CreateGlowSprite();
        flashRenderer.sharedMaterial = ballRenderer.sharedMaterial;
        flashRenderer.sortingLayerID = ballRenderer.sortingLayerID;
        flashRenderer.sortingOrder = ballRenderer.sortingOrder - 1;
        flashRenderer.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
    }

    // 가운데가 밝고 가장자리로 갈수록 투명해지는 동그란 빛 (공 크기 = 1)
    static Sprite glowSprite;
    static Sprite CreateGlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f; // 0 = 가운데, 1 = 가장자리
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return glowSprite;
    }

    // 벽돌이 깨져서 흡수 조각이 날아오기 시작할 때 호출된다.
    public void NotifyAbsorbQueued()
    {
        lastAbsorbQueuedTime = Time.time;
    }

    // 흡수 조각 하나가 공에 닿았을 때 호출된다. amount만큼 보이는 크기가 커진다.
    public void OnFragmentAbsorbed(float amount, bool withEffect)
    {
        shownPower = Mathf.Min(power, shownPower + amount);
        if (!withEffect) return;
        pulse = Mathf.Max(pulse, absorbPulse);
        flash = 1f;
    }

    void Update()
    {

        // 일시정지 중이면 공 조작 불가능
        if (Time.timeScale == 0f)
        {
            return;
        }


        // 스테이지 전환 중에는
        // 공 위치를 패들 위치로 강제로 이동시키지 않는다.
        if (isTransitioning)
        {
            return;
        }

        // 발사 전
        if (!isLaunched)
        {
            transform.position = new Vector3(
                paddle.position.x,
                paddle.position.y + offsetY,
                transform.position.z
            );

            // 좌클릭 하는 순간
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                startMousePos = Mouse.current.position.ReadValue();
                startBallPos = transform.position;
            }

            // 좌클릭 하는 동안
            if (Mouse.current.leftButton.isPressed)
            {
                ShowAimDirection();

                // 화살표 표시
                aimLine.enabled = true;
                arrowLeft.enabled = true;
                arrowRight.enabled = true;
            }

            // 마우스 뗐을 때
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                // 화살표 숨기기
                aimLine.enabled = false;
                arrowLeft.enabled = false;
                arrowRight.enabled = false;

                Launch();
            }
        }
        else
        {
            ballmove();
        }
    }

    // 물리 엔진이 반사하기 전에 이번 이동 구간의 관통 대상을 처리한다.
    void FixedUpdate()
    {
        // 발사 전, 스테이지 전환 중, 일시정지 중에는 벽돌을 미리 파괴하지 않는다.
        if (!isLaunched || isTransitioning || Time.timeScale == 0f || !rigid.simulated)
            return;

        Vector2 velocity = rigid.linearVelocity;
        // 이번 물리 프레임에 실제로 이동할 거리만 검사한다.
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        if (distance <= 0f) return;

        ContactFilter2D filter = new ContactFilter2D();
        // 공과 충돌하도록 설정된 레이어만 대상으로 삼는다.
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = true; // 패들/바닥 뒤의 벽돌을 먼저 파괴하지 않도록 검사
        // 가느다란 광선 대신 공의 실제 콜라이더 모양으로 이동 경로를 검사한다.
        rigid.Cast(velocity.normalized, filter, piercingHits, distance);
        // 가까운 물체부터 판단해야 앞에 막힌 벽돌 너머를 먼저 파괴하지 않는다.
        piercingHits.Sort((a, b) => a.distance.CompareTo(b.distance));

        // 한 물리 프레임의 판정에는 흡수 전 공격력을 사용한다.
        float hitPower = power;
        int damage = Mathf.Max(1, Mathf.FloorToInt(hitPower));
        // 먼저 공의 진행을 막거나 방향을 바꿀 가장 가까운 위치를 구한다.
        float blockingDistance = float.PositiveInfinity;
        foreach (RaycastHit2D hit in piercingHits)
        {
            Collider2D target = hit.collider;
            if (target == null || !target.enabled) continue;

            bool gameplayTrigger = target.CompareTag("paddle") || target.CompareTag("wall");
            // 장식/감지용 Trigger는 무시하지만 패들과 바닥은 진행을 막는 것으로 취급한다.
            if (target.isTrigger && !gameplayTrigger) continue;

            Brick brick = target.GetComponent<Brick>();
            if (target.isTrigger || !CanPierce(brick, hitPower, damage))
            {
                // 일반 벽돌의 피해와 반사는 기존 물리 충돌 처리에 맡긴다.
                blockingDistance = hit.distance;
                break;
            }
        }

        foreach (RaycastHit2D hit in piercingHits)
        {
            // 같은 거리의 벽/일반 벽돌을 우선하여 뒤쪽을 먼저 파괴하지 않는다.
            if (hit.distance >= blockingDistance) break;
            Collider2D target = hit.collider;
            if (target == null || !target.enabled || target.isTrigger) continue;

            Brick brick = target.GetComponent<Brick>();
            if (!CanPierce(brick, hitPower, damage)) continue;

            // 이번 타격으로 파괴될 벽돌만 비활성화하여 Destroy 대기 중 반사를 막는다.
            foreach (Collider2D brickCollider in brick.GetComponents<Collider2D>())
                brickCollider.enabled = false;
            // 기존 피해 함수를 재사용하여 흡수/코인/파괴 보상이 중복되지 않게 한다.
            brick.OnHit(damage);

            // 흡수로 스테이지 전환이 시작되면 이전 검사 결과를 사용하지 않는다.
            if (isTransitioning || !isLaunched || !rigid.simulated || Time.timeScale == 0f)
                break;
        }
    }

    static bool CanPierce(Brick brick, float hitPower, int damage)
    {
        // 최대 HP가 초기화된 살아 있는 벽돌만 검사한다.
        // 공격력 3배 조건에 더해 실제로 이번 타격에 죽는지 확인해야 안전하게 충돌을 끌 수 있다.
        return brick != null && brick.hp > 0 && brick.maxHp > 0
            && hitPower >= brick.maxHp * 3f && damage >= brick.hp;
    }

    void ShowAimDirection()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue(); // 현재 마우스 위치
        float mouseX = mousePos.x - startMousePos.x; // 마우스 좌우 이동 거리

        // 발사 방향 계산
        float directionX = mouseX * 0.01f;
        directionX = Mathf.Clamp(directionX, -2f, 2f);
        Vector2 direction = new Vector2(directionX, 1f).normalized;

        // 화살표 시작 위치 (공 위치)
        Vector3 startPosition = transform.position; // 화살표 시작 위치 (공 위치)
        Vector3 endPosition = startPosition + (Vector3)(direction * arrowLength); // 화살표 끝 위치

        // 화살표 몸통
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, startPosition);
        aimLine.SetPosition(1, endPosition);

        // 화살촉 방향 계산
        Vector2 leftDirection = Quaternion.Euler(0f, 0f, 180f - arrowHeadAngle) * direction;
        Vector2 rightDirection = Quaternion.Euler(0f, 0f, 180f + arrowHeadAngle) * direction;

        // 왼쪽 화살촉
        arrowLeft.positionCount = 2;
        arrowLeft.SetPosition(0, endPosition);
        arrowLeft.SetPosition(1, endPosition + (Vector3)(leftDirection * arrowHeadLength));

        // 오른쪽 화살촉
        arrowRight.positionCount = 2;
        arrowRight.SetPosition(0, endPosition);
        arrowRight.SetPosition(1, endPosition + (Vector3)(rightDirection * arrowHeadLength));
    }

    void Launch()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        // 마우스를 당긴 방향 계산
        float mouseX = mousePos.x - startMousePos.x;
        float directionX = mouseX * 0.01f; // 좌우 방향
        directionX = Mathf.Clamp(directionX, -2f, 2f); // 너무 많이 꺾이지 않도록 제한
        float directionY = 1f; // 위쪽 방향
        Vector2 direction = new Vector2(directionX, directionY).normalized; // 발사 방향
        rigid.simulated = true; // 물리 기능 켜기
        rigid.linearVelocity = direction * currentSpeed;// 현재 속도로 공 발사
        isLaunched = true; // 발사 상태
        gameManager.timeManager.StartTimer(); // 시간 시작
    }

    void ballmove()
    {
        Vector2 velocity = rigid.linearVelocity; // 현재 공 속도
        float h = velocity.x;
        float v = velocity.y;
    }

    // 충돌 플래그 생성 + 공 튕기기
    void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.gameObject.tag == "wall")
        {
            if (collision.gameObject.name == "downwall")
            {
                isTouchDown = true;
                gameManager.BallFell(); // 공이 떨어졌다고 GameManager에 알림
            }

        }
        else if (collision.gameObject.tag == "paddle")
        {
            float speed = rigid.linearVelocity.magnitude; // 현재 공의 속도

            // 패들의 가운데 X 좌표
            float paddleCenter = collision.transform.position.x;

            // 공이 패들 가운데에서 얼마나 떨어져 맞았는지
            float hitPoint = transform.position.x - paddleCenter;

            // 패들의 절반 너비
            float paddleHalfWidth = collision.bounds.size.x / 2;

            // -1 ~ 1 사이의 값으로 변환
            float h = hitPoint / paddleHalfWidth;

            Vector2 direction = new Vector2(h, 1f).normalized; // 패들에 맞는 위치에 따라 방향 결정
            rigid.linearVelocity = direction * speed; // 속도 크기는 그대로 유지
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 관통은 FixedUpdate에서 충돌 전에 처리한다.
        // 여기서 IgnoreCollision을 호출해도 이미 계산된 반사를 되돌릴 수는 없다.
        if (collision.gameObject.CompareTag("wall"))
        {
            // 물리 엔진이 계산한 반사 방향을 그대로 사용한다.
            Vector2 direction = rigid.linearVelocity.normalized;

            // 속도만 일정하게 맞춘다.
            rigid.linearVelocity = direction * currentSpeed;
        }
    }

    //공 상태 저장 함수
    public void SaveBallState()
    {
        savedPosition = transform.position;
        savedVelocity = rigid.linearVelocity;
        savedSpeed = currentSpeed;
        savedIsLaunched = isLaunched;
    }

    //공 상태 복원 함수 추가(방향유지 속도 30%감소)
    public void RestoreBallState()
    {
        // 물리 기능 다시 켜기
        // 발사 전 자동 전환이라면 물리를 켜지 않고 패들 위 대기 상태를 유지한다.
        rigid.simulated = savedIsLaunched;

        // 저장해 둔 이동 방향으로 다시 움직인다.
        if (savedVelocity != Vector2.zero)
        {
            rigid.linearVelocity =
                savedVelocity.normalized * currentSpeed;
        }

        // 다시 움직이는 상태로 만든다.
        isLaunched = savedIsLaunched;
    }
    //공 일시정지
    public void StopBall()
    {
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f;

        rigid.simulated = false;

        isLaunched = false;
    }

    //// 공 속도를 70%로 줄인다.
    //public void ReduceSpeed()
    //{
    //    currentSpeed *= 0.7f;
    //}

    //공 상태 초기화
    public void ResetBall()
    {
        // 발사 전 상태로 변경
        isLaunched = false;

        // 스테이지 전환 상태도 해제
        // 다시 마우스로 발사할 수 있게 한다.
        isTransitioning = false;

        // 아래 벽에 닿았다는 상태도 초기화
        isTouchDown = false;

        // 시간 정지
        gameManager.timeManager.StopTimer();

        // 물리 기능 끄기
        rigid.simulated = false;

        // 현재 속도 제거
        rigid.linearVelocity = Vector2.zero;

        // 회전 속도 제거
        rigid.angularVelocity = 0f;

        // 패들 가운데 위로 이동
        transform.position = new Vector3(paddle.position.x, paddle.position.y + offsetY, transform.position.z);
    }

    // 플래그 지우기
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "wall")
        {
            if (collision.gameObject.name == "downwall")
            {
                isTouchDown = false;
            }

        }

    }

    public void AddPower(float amount)
    {
        // 추가 흡수를 해도 공격력은 최종 상한인 50,000을 넘지 않는다.
        power = gameManager.ClampAttackPower(power + amount);
        RefreshSize(false);
    }

    public Vector3 SizeForCurrentStage()
    {
        float powerScale = gameManager.GetPowerSizeMultiplier(shownPower);
        float stageScale = Mathf.Pow(0.5f, gameManager.currentStage - 1);
        // 단계별 크기 제한은 없다. Z는 유지하고 화면의 가로/세로만 축소한다.
        float sizeMultiplier = powerScale * stageScale;
        return new Vector3(baseScale.x * sizeMultiplier, baseScale.y * sizeMultiplier, baseScale.z);
    }

    public void RefreshSize(bool instant)
    {
        if (instant) shownPower = power;
        targetScale = SizeForCurrentStage();
        if (instant)
        {
            smoothScale = targetScale;
            transform.localScale = targetScale;
        }
    }

    void LateUpdate()
    {
        // 실행 중 Inspector에서 공격력을 직접 변경한 경우에도 상한과 목표 크기를 갱신한다.
        power = gameManager.ClampAttackPower(power);

        // 보이는 크기가 실제 공격력을 넘지 않게 하고, 조각이 오지 않으면 잠시 뒤 따라잡는다.
        if (shownPower > power || Time.time - lastAbsorbQueuedTime > ShowCatchUpDelay)
        {
            shownPower = power;
        }

        RefreshSize(false);
        UpdateFlash();

        if (isTransitioning)
        {
            // 전환 중에는 StageManager가 크기를 직접 바꾼다.
            smoothScale = transform.localScale;
            pulse = 0f;
            return;
        }

        smoothScale = Vector3.Lerp(smoothScale, targetScale, growSpeed * Time.deltaTime);
        if ((smoothScale - targetScale).sqrMagnitude < 0.00001f)
            smoothScale = targetScale;

        pulse = Mathf.MoveTowards(pulse, 0f, pulseDecay * pulse * Time.deltaTime + 0.001f);
        transform.localScale = new Vector3(smoothScale.x * (1f + pulse), smoothScale.y * (1f + pulse), smoothScale.z);
    }

    void UpdateFlash()
    {
        if (flashRenderer == null) return;
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 6f);
        flashRenderer.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * flash);
    }
}
