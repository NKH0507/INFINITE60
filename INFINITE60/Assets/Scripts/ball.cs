using UnityEngine;
using UnityEngine.InputSystem;

public class ball : MonoBehaviour
{
    public bool isTouchLeft;
    public bool isTouchRight;
    public bool isTouchUp;
    public bool isTouchDown;

    Rigidbody2D rigid;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        // 테스트용 공 대각선 이동
        rigid.linearVelocity = new Vector2(3, 0);
    }

    void Update()
    {
        ballmove();
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
                // 아래쪽 벽에 부딪힘
                case "downwall":
                    isTouchDown = true;
                    // Y 방향 반전
                    rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, -rigid.linearVelocity.y);
                    break;
            }

        }

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

                case "downwall":
                    isTouchDown = false;
                    break;
            }

        }

    }

}
