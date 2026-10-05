using System;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.InputSystem;

public class ball : MonoBehaviour
{
    public bool isTouchDown;

    public int power;

    public bool isLaunched = false;
    Vector2 startMousePos;
    Vector3 startBallPos;

    public bool isTransitioning = false;

    Rigidbody2D rigid;
    public Transform panel;
    public float offsetY = 0.5f;

    //전환 상태 저장 변수
    Vector3 savedPosition;
    Vector2 savedVelocity;
    float savedSpeed;
    bool savedIsLaunched;

    GameManager gameManager;

    // 발사 방향 표시용 LineRenderer
    public LineRenderer aimLine;

    // 공의 고정 발사 속도
    public float launchSpeed = 10f;
    float currentSpeed;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.simulated = false; // 발사 전에는 물리 영향을 받지 않게 함
        startBallPos = transform.position; // 현재 공의 위치를 저장
        gameManager = FindFirstObjectByType<GameManager>(); //GameManager 찾기
        aimLine.enabled = false; // 처음에는 방향 표시 끄기
        currentSpeed = launchSpeed;
    }

    void Update()
    {
        // 스테이지 전환 중에는
        // 공 위치를 패널 위치로 강제로 이동시키지 않는다.
        if (isTransitioning)
        {
            return;
        }

        // 발사 전
        if (!isLaunched)
        {
            transform.position = new Vector3(
                panel.position.x,
                panel.position.y + offsetY,
                transform.position.z
            );

            // 좌클릭 하는 순간
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                startMousePos = Mouse.current.position.ReadValue();
                startBallPos = transform.position;
                aimLine.enabled = true;
            }

            // 좌클릭 하는 동안
            if (Mouse.current.leftButton.isPressed)
            {
                ShowAimDirection();
            }

            // 마우스 뗐을 때
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                aimLine.enabled = false;
                Launch();
            }
        }
        else
        {
            ballmove();
        }
    }

    void ShowAimDirection()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue(); // 현재 마우스 위치
        // 마우스를 움직인 거리
        float mouseX = mousePos.x - startMousePos.x;
        float mouseY = startMousePos.y - mousePos.y;

        float directionX = mouseX * 0.01f; // 좌우 방향
        directionX = Mathf.Clamp(directionX, -1.8f, 1.8f); // 너무 많이 꺾이지 않도록 제한
        float directionY = 1f; // 위쪽 방향
        Vector2 direction = new Vector2(directionX, directionY).normalized; // 발사 방향
        aimLine.SetPosition(0, transform.position); // LineRenderer 시작점
        float lineLength = 2.5f; // 화살표 길이
        Vector3 endPosition = transform.position + (Vector3)(direction * lineLength); // LineRenderer 끝점
        aimLine.SetPosition(1, endPosition); //선 긋기
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
        else if (collision.gameObject.tag == "panel")
        {
            float speed = rigid.linearVelocity.magnitude; // 현재 공의 속도

            // 패널의 가운데 X 좌표
            float panelCenter = collision.transform.position.x;

            // 공이 패널 가운데에서 얼마나 떨어져 맞았는지
            float hitPoint = transform.position.x - panelCenter;

            // 패널의 절반 너비
            float panelWidth = collision.bounds.size.x / 2;

            // -1 ~ 1 사이의 값으로 변환
            float h = hitPoint / panelWidth;

            Vector2 direction = new Vector2(h, 1f).normalized; // 패널에 맞는 위치에 따라 방향 결정
            rigid.linearVelocity = direction * speed; // 속도 크기는 그대로 유지
        }

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
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
        rigid.simulated = true;

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

    //공 크기 감소
    public void ReduceSize()
    {
        transform.localScale *= 0.7f;
    }

    // 공 속도를 70%로 줄인다.
    public void ReduceSpeed()
    {
        currentSpeed *= 0.7f;
    }

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

        // 패널 가운데 위로 이동
        transform.position = new Vector3(panel.position.x,panel.position.y + offsetY,transform.position.z);
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

}
