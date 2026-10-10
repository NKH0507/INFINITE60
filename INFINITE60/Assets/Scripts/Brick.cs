using UnityEngine;

public class Brick : MonoBehaviour
{
    public int hp = 3;
    public bool isObstacle = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 벽돌 중복 파괴 방지
    bool isDestroyed = false;

    // 데미지를 입었을 시
    private void OnHit(int dmg)
    {
        // 이미 파괴된 벽돌이라면 무시
        if (isDestroyed)
        {
            return;
        }

        hp -= dmg;

        // 피가 다 닳으면 사라짐
        if (hp <= 0)
        {
            isDestroyed = true;

            GameManager gamemanager = FindFirstObjectByType<GameManager>();

            // 현재 스테이지의 벽돌 파괴 시 코인 1개 획득
            if (!isObstacle && CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoin(1);
            }

            // 기존 스테이지 / 보스 스테이지 처리
            if (!isObstacle && gamemanager != null)
            {
                gamemanager.OnBrickDestroyed(this);
            }

            // 벽돌 삭제
            Destroy(gameObject);
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
