using UnityEngine;

public class Brick : MonoBehaviour
{
    public int health = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //�������� �Ծ��� ��
    private void OnHit(int dmg)
    {
        hp -= dmg;

        //�� �� ��� �����
        if (hp <= 0)
        {
            GameManager gamemanager = FindObjectOfType<GameManager>();
            Destroy(gameObject);
            gamemanager.BrickDestroyed();
        }
    }

    // ������ ����
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "ball")
        {
            ball Ball = collision.gameObject.GetComponent<ball>(); //�浹�� ������Ʈ���� ball ��ũ��Ʈ ������
            OnHit(Ball.power); //OnHit�� �������� �־� ������ ��ŭ ü���� ���̰� ��
        }
    }
}
