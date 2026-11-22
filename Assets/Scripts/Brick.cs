using UnityEngine;

public class Brick : MonoBehaviour
{
    // 현재 체력과 최초 체력은 피해 계산 / 관통 판정 / 흡수량 계산에 각각 사용한다.
    public int hp;
    public int maxHp;

    [SerializeField] private int coinReward = 1;

    // 이전 스테이지에서 넘어온 장애물은 보스 진행 알림 대상에서 제외한다.
    public bool isObstacle = false;

    // Destroy는 프레임 끝에 적용되므로 그전에 들어온 중복 타격의 보상을 막는다.
    bool isDestroyed;
    // SpriteRenderer를 연결
    [SerializeField] private SpriteRenderer pierceOutline;

    // 금 간 모습: 벽돌 위에 겹쳐 그리는 렌더러와 단계별 이미지 (1단계, 2단계, 3단계 순서)
    [SerializeField] private SpriteRenderer crackRenderer;
    [SerializeField] private Sprite[] crackSprites;

    // 공의 현재 공격력을 확인하기 위한 참조.
    private Ball playerBall;

    void Start()
    {
        // 처음 체력을 최대 체력으로 저장
        maxHp = hp;

        // 씬에 있는 공을 찾아 저장한다.
        playerBall = FindFirstObjectByType<Ball>();

        // 생성 직후에는 테두리를 숨긴다.
        // 실제 표시 여부는 LateUpdate에서 공의 초기 공격력 설정이 끝난 뒤 판단한다.
        if (pierceOutline != null)
        {
            pierceOutline.enabled = false;
        }

        UpdateCrack();
    }

    // 남은 체력 비율에 맞춰 금 간 단계를 바꾼다.
    // 75% 이하 1단계, 50% 이하 2단계, 25% 이하 3단계
    void UpdateCrack()
    {
        if (crackRenderer == null || crackSprites == null || crackSprites.Length == 0 || maxHp <= 0)
        {
            return;
        }

        float ratio = (float)hp / maxHp;
        int stage = ratio <= 0.25f ? 3 : ratio <= 0.5f ? 2 : ratio <= 0.75f ? 1 : 0;
        stage = Mathf.Min(stage, crackSprites.Length);

        crackRenderer.sprite = stage == 0 ? null : crackSprites[stage - 1];
        crackRenderer.enabled = stage > 0;
    }

    // 벽돌 파괴로 공의 공격력이 증가하면 그 결과를 테두리에 반영.
    void LateUpdate()
    {
        // Inspector에서 테두리를 연결하지 않은 벽돌은 표시 작업을 건너뛴다.
        if (pierceOutline == null)
        {
            return;
        }

        // 벽돌에 테두리를 표시할지 판단.
        bool canPierce = playerBall != null && !isDestroyed && hp > 0 && maxHp > 0 && playerBall.power >= maxHp * 3f;

        // 표시 상태가 달라졌을 때만 Renderer 설정을 변경.
        if (pierceOutline.enabled != canPierce)
        {
            pierceOutline.enabled = canPierce;
        }

    }

    // 일반 충돌과 ball의 사전 관통 검사가 같은 피해/보상 처리를 사용하도록 공개한다.
    public void OnHit(int dmg)
    {
        // 이미 파괴된 벽돌이라면 무시
        if (isDestroyed)
        {
            return;
        }

        // 호출 순간에 확정한 피해량을 사용한다. 흡수 후의 공격력을 다시 읽지 않는다.
        int damage = Mathf.Max(1, dmg);

        // 최소 피해 보정은 이 공통 함수에서만 수행한다.
        hp -= damage;

        // 살아 있으면 종료하여 파괴/보상 처리를 실행하지 않는다.
        if (hp > 0)
        {
            UpdateCrack();
            return;
        }

        // 다른 시스템에 알리기 전에 중복 처리를 차단한다.
        isDestroyed = true;
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        Ball currentBall = FindFirstObjectByType<Ball>();
        if (currentBall != null && gameManager != null)
        {
            // 최대 체력 기준으로 공격력을 흡수한 뒤 스테이지 전환 조건을 확인한다.
            float absorbAmount = maxHp * gameManager.absorbRate;
            currentBall.AddPower(absorbAmount);
            // 삼각형 조각이 흩어졌다가 공으로 빨려 들어가는 연출. 공은 조각이 닿을 때 커진다.
            AbsorbFx.Spawn(this, GetComponent<SpriteRenderer>(), currentBall, absorbAmount);
            gameManager.CheckZoom();
        }

        // 장애물을 포함해 파괴된 벽돌 하나당 코인 1개를 지급한다.
        if (CoinManager.instance != null)
        {
            CoinManager.instance.AddCoin(coinReward);
        }

        // 현재 스테이지의 벽돌만 일반/보스 진행에 반영한다.
        if (!isObstacle && gameManager != null)
        {
            gameManager.OnBrickDestroyed(this);
        }

        Destroy(gameObject);
    }

    // 일반 충돌도 관통과 동일한 피해 함수를 사용한다.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("ball"))
        {
            return;
        }

        // 태그만 있고 Ball 컴포넌트가 없는 오브젝트는 안전하게 무시한다.
        if (collision.gameObject.TryGetComponent<Ball>(out var hittingBall))
        {
            // 소수점은 기존처럼 버리고, 최소 피해 1 보정은 OnHit에 맡긴다.
            OnHit(Mathf.FloorToInt(hittingBall.power));
        }
    }
}
