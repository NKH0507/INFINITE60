using UnityEngine;

public class Brick : MonoBehaviour
{
    public int hp = 3;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnHit(int dmg)
    {
        hp -= dmg;

        //피 다 닳면 사라짐
        if (hp <= 0)
        {
            GameManager gamemanager = FindObjectOfType<GameManager>();
            Destroy(gameObject);
            gamemanager.BrickDestroyed();
        }
    }

    // 데미지 측정
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "ball")
        {
            ball Ball = collision.gameObject.GetComponent<ball>(); //충돌한 오브젝트에서 ball 스크립트 가져옴
            OnHit(Ball.power); //OnHit에 데미지를 넣어 데미지 만큼 체력이 깎이게 함
        }
    }
}
