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

    GameManager gameManager;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.simulated = false; // �߻� ������ ���� ������ ���� �ʰ� ��
        startBallPos = transform.position; // ���� ���� ��ġ�� ����
        gameManager = FindFirstObjectByType<GameManager>(); //GameManager ã��
    }

    void Update()
    {
        //�߻���
        if (!isLaunched)
        {
            transform.position = new Vector3(panel.position.x, panel.position.y + offsetY, transform.position.z); //�� ��ġ ����

            //��Ŭ�� �ϴ� ����
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                startMousePos = Mouse.current.position.ReadValue(); //Ŭ����ġ ����
                startBallPos = transform.position; //���� ���� ��ġ ����

            }

            //��Ŭ�� �ϴ� ����
            if(Mouse.current.leftButton.isPressed)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue(); //���� ���콺 ��ġ ������
                float mouseX = mousePos.x - startMousePos.x; //x���� �̵���
                float mouseY = startMousePos.y - mousePos.y; //y���� �̵���
                float moveX = mouseX * 0.01f; //�¿�� ������ ����
                float moveY = mouseY * 0.01f; //�Ʒ��� ��� ����
                //�ӵ� ����
                moveX = Mathf.Clamp(moveX, -2f, 2f); 
                moveY = Mathf.Clamp(moveY, 0f, 3f);

                //transform.position = startBallPos + new Vector3(moveX, -moveY, 0); //ó�� ���� ��ġ�� �������� ����� ��ġ�� ���
            }

            //���콺 ������
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
        Vector2 mousePos = Mouse.current.position.ReadValue(); //���콺 ��ġ
        //���콺 �̵��Ÿ� ���
        float mouseX = mousePos.x - startMousePos.x;
        float mouseY = startMousePos.y - mousePos.y;
        float directionX = mouseX * 0.01f; //�¿� ���� ���
        directionX = Mathf.Clamp(directionX, -2f, 2f); //ũ�� ������ �ʵ��� ����
        float strong = mouseY * 0.05f; //�� ���

        //�ּ� �߻� ��
        if (strong < 2f)
        { 
            strong = 2f; 
        }

        //�߻� ���� ���ϱ�
        float directionY = 1f; 
        Vector2 direction = new Vector2(directionX, directionY);
        //�߻���
        direction = direction.normalized; //���� ũ�� 1��
        rigid.simulated = true; //���� ��� Ȱ��ȭ
        rigid.linearVelocity = direction * strong; // ���� ����� ������ �߻�
        isLaunched = true;
        gameManager.timeManager.StartTimer(); //�ð� ����
    }

    void ballmove()
    {
        Vector2 velocity = rigid.linearVelocity; // ���� �� �ӵ�
        float h = velocity.x;
        float v = velocity.y;
    }

    // �浹 �÷��� ���� + �� ƨ���
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == "downwall")
        {
            gameManager.BallFell();

        }
        else if (collision.gameObject.tag == "panel")
        {

            // �г��� ��� X ��ǥ
            float panelCenter = collision.transform.position.x;

            // ���� �г� ������� �󸶳� ������ �¾Ҵ���
            float hitPoint = transform.position.x - panelCenter;

            // �г��� ���� �ʺ�
            float panelWidth = collision.collider.bounds.size.x / 2;

            // -1 ~ 1 ������ ������ ��ȯ
            float h = hitPoint / panelWidth;

            // ���� ���� �ӷ�
            float speed = rigid.linearVelocity.magnitude;

            // ������ ����
            float x = h; 
            float y = 1f;

            //������ �״�� �����ϸ鼭 ũ�⸸ 1��
            Vector2 direction = new Vector2(x, y).normalized;

            // ���� �ӷ� ����
            rigid.linearVelocity = direction * speed;

        }
    }

    //�� ���� �ʱ�ȭ
    public void ResetBall()
    {
        // �߻� �� ���·� ����
        isLaunched = false;

        // �ð� ����
        gameManager.timeManager.StopTimer();

        // ���� ��� ����
        rigid.simulated = false;

        // ���� �ӵ� ����
        rigid.linearVelocity = Vector2.zero;

        // ȸ�� �ӵ� ����
        rigid.angularVelocity = 0f;

        // �г� ��� ���� �̵�
        transform.position = new Vector3(panel.position.x,panel.position.y + offsetY,transform.position.z);
    }
}
