using System;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.InputSystem;

public class ball : MonoBehaviour
{
    public bool isTouchLeft;
    public bool isTouchRight;
    public bool isTouchUp;
    public bool isTouchDown;

    public int power;

    bool isLaunched = false;
    Vector2 startMousePos;
    Vector3 startBallPos;

    Rigidbody2D rigid;
    public Transform panel;
    public float offsetY = 0.5f;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.simulated = false; // 발사 전에는 물리 영향을 받지 않게 함
        startBallPos = transform.position; // 현재 공의 위치를 저장
    }

    void Update()
    {
        //발사전
        if (!isLaunched)
        {
            transform.position = new Vector3(panel.position.x, panel.position.y + offsetY, transform.position.z); //공 위치 고정

            //좌클릭 하는 순간
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                startMousePos = Mouse.current.position.ReadValue(); //클릭위치 저장
                startBallPos = transform.position; //공의 현재 위치 저장

            }

            //좌클릭 하는 동안
            if(Mouse.current.leftButton.isPressed)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue(); //현재 마우스 위치 가져옴
                float mouseX = mousePos.x - startMousePos.x; //x방향 이동량
                float mouseY = startMousePos.y - mousePos.y; //y방향 이동량
                float moveX = mouseX * 0.01f; //좌우로 움직인 정도
                float moveY = mouseY * 0.01f; //아래로 당긴 정도
                //속도 제한
                moveX = Mathf.Clamp(moveX, -2f, 2f); 
                moveY = Mathf.Clamp(moveY, 0f, 3f);

                //transform.position = startBallPos + new Vector3(moveX, -moveY, 0); //처음 공의 위치를 기준으로 당겨진 위치를 계산
            }

            //마우스 뗏을때
            if(Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Launch();
            }
        }
        else
        {
            ballmove();
        }

    }

    void Launch()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue(); //마우스 위치
        //마우스 이동거리 계산
        float mouseX = mousePos.x - startMousePos.x;
        float mouseY = startMousePos.y - mousePos.y;
        float directionX = mouseX * 0.01f; //좌우 방향 계산
        directionX = Mathf.Clamp(directionX, -2f, 2f); //크게 꺾이지 않도록 제한
        float strong = mouseY * 0.05f; //힘 계산

        //최소 발사 힘
        if (strong < 2f)
        { 
            strong = 2f; 
        }

        //발사 방향 정하기
        float directionY = 1f; 
        Vector2 direction = new Vector2(directionX, directionY);
        //발사기능
        direction = direction.normalized; //방향 크기 1로
        rigid.simulated = true; //물리 기능 활성화
        rigid.linearVelocity = direction * strong; // 계산된 방향과 힘으로 발사
        isLaunched = true;
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
            switch (collision.gameObject.name)
            {
                // 왼쪽 벽에 부딪힘
                case "leftwall":
                    isTouchLeft = true;
                    // X 방향 반전
                    rigid.linearVelocity = new Vector2(-rigid.linearVelocity.x, rigid.linearVelocity.y);
                    break;
                // 오른쪽 벽에 부딪힘
                case "rightwall":
                    isTouchRight = true;
                    // X 방향 반전
                    rigid.linearVelocity = new Vector2(-rigid.linearVelocity.x, rigid.linearVelocity.y);
                    break;
                // 위쪽 벽에 부딪힘
                case "upwall":
                    isTouchUp = true;
                    // Y 방향 반전
                    rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, -rigid.linearVelocity.y);
                    break;
            }

        }
        else if (collision.gameObject.tag == "panel")
        {
            // 패널에 맞으면 중력을 약하게 함
            rigid.gravityScale = 0.2f;

            // 패널의 가운데 X 좌표
            float panelCenter = collision.transform.position.x;

            // 공이 패널 가운데에서 얼마나 떨어져 맞았는지
            float hitPoint = transform.position.x - panelCenter;

            // 패널의 절반 너비
            float panelWidth = collision.bounds.size.x / 2;

            // -1 ~ 1 사이의 값으로 변환
            float h = hitPoint / panelWidth;

            // 현재 공의 속력
            float speed = rigid.linearVelocity.magnitude;

            // 방향을 결정
            float x = h; 
            float y = 1f;

            //방향은 그대로 유지하면서 크기만 1로
            Vector2 direction = new Vector2(x, y).normalized;

            // 기존 속력 유지
            rigid.linearVelocity = direction * speed;

        }
        else if (collision.gameObject.tag == "brick")
        {
            // 공의 현재 위치
            Vector2 ballPos = transform.position;
            // 벽돌의 가운데 위치
            Vector2 brickPos = collision.transform.position;
            //공과 벽돌의 위치 차이
            float dx = ballPos.x - brickPos.x; float dy = ballPos.y - brickPos.y;
            //벽돌의 절반 크기
            float brickWidth = collision.bounds.size.x / 2;
            float brickHeight = collision.bounds.size.y / 2;

            //위,아래 면에 맞았을 때
            if (Mathf.Abs(dx) < brickWidth)
            {
                //Y 방향 반전
                rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, -rigid.linearVelocity.y);
            }
            // 왼쪽,오른쪽 면에 맞았을 때
            else
            {
                // X 방향 반전
                rigid.linearVelocity = new Vector2(-rigid.linearVelocity.x, rigid.linearVelocity.y);
            }

        }

    }

    //공 상태 초기화
    public void ResetBall()
    {
        // 발사 전 상태로 변경
        isLaunched = false;

        // 물리 기능 끄기
        rigid.simulated = false;

        // 현재 속도 제거
        rigid.linearVelocity = Vector2.zero;

        // 회전 속도 제거
        rigid.angularVelocity = 0f;

        // 중력 원래대로
        rigid.gravityScale = 1f;

        // 패널 가운데 위로 이동
        transform.position = new Vector3(
            panel.position.x,
            panel.position.y + offsetY,
            transform.position.z
        );

    }

    // 플래그 지우기
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "wall")
        {
            switch (collision.gameObject.name)
            {
                case "leftwall":
                    isTouchLeft = false;
                    break;

                case "rightwall":
                    isTouchRight = false;
                    break;

                case "upwall":
                    isTouchUp = false;
                    break;
            }

        }

    }

}
