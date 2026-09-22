using UnityEngine;
using UnityEngine.InputSystem;

public class panelmove : MonoBehaviour
{
    public float speed;
    public bool isTouchLeft;
    public bool isTouchRight;
    Vector2 mouseStartPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }
    void Move()
    {
        float h = 0;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue(); //현재 마우스 위치
            float mouseMove = mousePos.x - mouseStartPos.x; //클릭한 위치와 현재 위치 차이

            //좌우 방향 결정
            if (mouseMove > 0)
            {
                h = 1;
            }
            else if (mouseMove < 0) 
            {
                h = -1; 
            }

            mouseStartPos = mousePos; //클릭 위치를 현재 위치로 갱신
        }
        //마우스를 누르지 않으면
        else 
        {
            h = 0; 
        }

        //벽 충돌
        if (isTouchLeft && h == -1) 
        {
            h = 0; 
        }
        if (isTouchRight && h == 1) 
        {
            h = 0; 
        }

        Vector3 curPos = transform.position; // 현재 오브젝트의 위치를 가져옴
        Vector3 nextPos = new Vector3(h, 0, 0) * speed * Time.deltaTime; //이동 계산
        transform.position = curPos + nextPos; // 현재 위치에 계산한 이동량을 더해 이동

    }

    //마우스 버튼을 처음 눌렀을 때
    void OnMouseDown() 
    {
        mouseStartPos = Mouse.current.position.ReadValue(); //마우스를 처음 클릭한 순간의 위치 저장
    }

    //충돌 플래그 생성
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "wall")
        {
            switch (collision.gameObject.name)
            {
                case "leftwall":
                    isTouchLeft = true;
                    break;
                case "rightwall":
                    isTouchRight = true;
                    break;
            }

        }

    }

    //플래그 지우기
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
            }

        }
    }
}