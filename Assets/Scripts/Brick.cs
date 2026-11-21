using UnityEngine;

public class Brick : MonoBehaviour
{
    public int hp;
    public bool isObstacle = false;
    // 벽돌의 처음 최대 체력
    public int maxHp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 처음 체력을 최대 체력으로 저장
        maxHp = hp;
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

        // 공 스크립트 가져오기
        ball ballScript = FindFirstObjectByType<ball>();

        // 공의 공격력 계산
        int damage = 1;

        if (ballScript != null)
        {
            damage = Mathf.Max(1, Mathf.FloorToInt(ballScript.power));
        }

        // 공격력만큼 벽돌 체력 감소
        hp -= damage;

        // 피가 다 닳으면 사라짐
        if (hp <= 0)
        {
            isDestroyed = true;
            ball b = FindFirstObjectByType<ball>();
            GameManager gmForAbsorb = FindFirstObjectByType<GameManager>();
            if (b != null && gmForAbsorb != null)
            {
                b.AddPower(maxHp * gmForAbsorb.absorbRate);
                gmForAbsorb.CheckZoom();   // 추가
            }

            GameManager gamemanager = FindFirstObjectByType<GameManager>();

            // 벽돌 파괴 시 코인 1개 획득
            if (CoinManager.Instance != null)
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
            OnHit(Mathf.CeilToInt(Ball.power)); //OnHit에 데미지를 넣어 데미지 만큼 체력이 깎이게 함
        }
    }

}
