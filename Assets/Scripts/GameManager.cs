using UnityEngine;
using System.Collections;
using System;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // 공
    public GameObject ball;

    // 패들
    [UnityEngine.Serialization.FormerlySerializedAs("panel")]
    public GameObject paddle;

    // 한 번 표시한 결과가 다른 종료 신호로 덮어써지는 것을 막는다.
    bool hasResult;

    // 스테이지를 관리하는 StageManager
    public StageManager stageManager;

    // 현재 스테이지 번호
    public int currentStage = 1;

    // 스테이지 데이터 목록
    public Stage[] stages;

    // 70% 이상 깨졌는지 확인
    bool isChangingStage = false;

    // 현재 스테이지의 처음 벽돌 개수
    int startBrickCount;

    // 시간 관리
    public TimeManager timeManager;

    public BossManager bossManager;

    // 일시정지 상태인지 확인
    public bool isPaused = false;

    // 일시정지 메뉴
    public GameObject pauseMenu;

    // 결과 화면
    public GameObject resultPanel;

    // 결과 화면 텍스트
    public TMP_Text resultTitleText;
    public TMP_Text resultStageText;
    public TMP_Text resultCoinText;

    // 메인 메뉴 씬 이름
    public string mainMenuSceneName = "StartScene";

    // 코인 매니저
    public CoinManager coinManager;

    public float firstStagePower = 10f;   // 1스테이지 시작 공격력
    public float absorbRate = 0.1f;       // 흡수율 (최대 HP의 10%)   
    public float zoomRate = 0.8f;         // 80%
    // 줌아웃 기준 계산용 흡수율. 강화와 상관없이 항상 10%
    const float BaseAbsorbRate = 0.1f;
    // zoomPowers[0] = 시작 공격력, zoomPowers[n] = n스테이지 줌아웃 기준
    float[] zoomPowers;
    public const int FinalStage = 9; // 9스테이지 = 보스 스테이지

    // 공격력 상한은 스테이지 전환 기준과 분리하여 항상 50,000으로 고정한다.
    public float MaxAttackPower => 50000f;

    public float ClampAttackPower(float attackPower)
    {
        return Mathf.Clamp(attackPower, 0f, MaxAttackPower);
    }

    // 기존 공격력 진행 기준을 크기 1, 2, 4, ... 256배에 연결한다.
    // 이 배율은 스테이지와 무관하며, 화면 크기는 여기에 스테이지별 1/2 배율을 곱한다.
    public float GetPowerSizeMultiplier(float attackPower)
    {
        float clampedPower = ClampAttackPower(attackPower);
        if (clampedPower <= zoomPowers[0]) return clampedPower / zoomPowers[0];
        for (int i = 1; i < FinalStage; i++)
        {
            if (clampedPower <= zoomPowers[i])
            {
                float progress = Mathf.InverseLerp(zoomPowers[i - 1], zoomPowers[i], clampedPower);
                return Mathf.Lerp(Mathf.Pow(2f, i - 1), Mathf.Pow(2f, i), progress);
            }
        }
        // 마지막 스테이지 진입 후에도 마지막 구간의 성장 기울기를 그대로 이어간다.
        // InverseLerp/Lerp는 범위를 제한하므로 여기서는 직접 계산하여 마지막 배율을 넘길 수 있게 한다.
        int lastIndex = FinalStage - 1;
        float lastStartScale = Mathf.Pow(2f, lastIndex - 1);
        float lastProgress = (clampedPower - zoomPowers[lastIndex - 1]) /
            (zoomPowers[lastIndex] - zoomPowers[lastIndex - 1]);
        return lastStartScale * (1f + lastProgress);
    }

    void Awake()
    {
        zoomPowers = new float[Mathf.Max(stages.Length, FinalStage) + 1];
        zoomPowers[0] = Mathf.Max(0.001f, firstStagePower);

        float totalHp = 0f;
        for (int i = 0; i < stages.Length; i++)
        {
            // 스테이지 프리팹 안 벽돌의 HP를 읽어서 누적
            foreach (Brick b in stages[i].stagePrefab.GetComponentsInChildren<Brick>(true))
                totalHp += b.hp;

            zoomPowers[i + 1] = zoomRate * (firstStagePower + BaseAbsorbRate * totalHp);
            // 공격력 구간이 역전되거나 폭이 0이 되는 설정을 방지한다.
            zoomPowers[i + 1] = Mathf.Max(zoomPowers[i + 1], zoomPowers[i] + 0.001f);
        }
        // 개발 중 스테이지 데이터가 8개 미만이어도 상한 계산이 0이 되지 않게 한다.
        for (int i = stages.Length + 1; i < zoomPowers.Length; i++)
            zoomPowers[i] = zoomPowers[i - 1] * 2f;
    }

    // 이번 스테이지가 시작된 기준과 줌아웃 기준
    public float StageStartPower => zoomPowers[Mathf.Clamp(currentStage - 1, 0, stages.Length)];
    public float StageZoomPower => zoomPowers[Mathf.Clamp(currentStage, 0, stages.Length)];

    // 벽돌을 흡수할 때마다 호출. 기준을 넘으면 줌아웃
    public void CheckZoom()
    {
        if (hasResult || isChangingStage || isPaused || currentStage >= FinalStage) return;
        if (NeedsStageChange())
            StartStageChange();
    }

    bool NeedsStageChange()
    {
        if (currentStage >= FinalStage || currentStage >= stages.Length) return false;
        float attackPower = ball.GetComponent<Ball>().power;
        // 마지막 직전 스테이지는 기준에 정확히 도달해도 전환한다.
        return currentStage == FinalStage - 1
            ? attackPower >= zoomPowers[currentStage]
            : attackPower > zoomPowers[currentStage];
    }

    void Update()
    {
        // 시작 공격력이 높거나 다른 경로에서 공격력을 올린 경우도 자동 전환한다.
        CheckZoom();
    }
    void Start()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
        resultPanel.SetActive(false);

        // 첫 번째 스테이지 생성
        stageManager.CreateStage(stages[currentStage - 1]);

        // 생성된 벽돌 개수 저장
        startBrickCount = stageManager.GetBrickCount();

        // 이번 게임에서 얻은 코인 초기화
        if (CoinManager.instance != null)
        {
            CoinManager.instance.ResetEarnedCoin();
        }


    }

    // 벽돌이 하나 깨질 때마다 호출할 함수
    public void BrickDestroyed()
    {
        // 이미 화면 전환 중이면 실행하지 않음
        if (isChangingStage)
        {
            return;
        }
        //크기증가
    }

    public void BallFell()
    {
        // 시간 20초 감소
        timeManager.BallFell();

        // 시간이 남아 있으면 공 초기화
        if (timeManager.remainTime > 0)
        {
            ResetBall();
        }
    }

    // 화면 전환 시작
    public void StartStageChange()
    {
        // 마지막 스테이지에서는 공격력 기준으로 넘어가지 않고,
        // 보스 매니저의 클리어 함수 호출을 기다린다.
        if (hasResult || isChangingStage || currentStage >= FinalStage || currentStage >= stages.Length)
        {
            return;
        }

        StartCoroutine(ChangeStage());
    }

    IEnumerator ChangeStage()
    {
        isChangingStage = true; // 스테이지 전환 중이라는 것을 표시
        do
        {
            // 성장 보간이 끝나지 않았어도 공격력에 맞는 실제 크기에서 축소를 시작한다.
            ball.GetComponent<Ball>().RefreshSize(true);
            currentStage++;
            yield return StartCoroutine(stageManager.StageTransition(stages[currentStage - 1],ChangeStageFinished)); // StageManager에게 스테이지 전환을 맡긴다.
        }
        while (!hasResult && NeedsStageChange());
        isChangingStage = false;
    }

    // 스테이지 전환이 끝난 후 실행되는 함수
    void ChangeStageFinished()
    {
        // 마지막(9) 스테이지 = 보스 스테이지. 챕터 배경은 ChapterBackground가 바꾼다.
        if (currentStage == FinalStage)
        {
            // 인자는 스테이지 번호가 아니라 보스 목록의 순서(1부터 시작)다.
            bossManager.StartBoss(1);
        }

        startBrickCount = stageManager.GetBrickCount(); // 새로 생성된 스테이지의 전체 벽돌 개수를 저장한다.
        // 연속 전환이 모두 끝날 때까지 ChangeStage에서 전환 잠금을 유지한다.
    }

    public void OnBrickDestroyed(Brick brick)
    {
        // 보스 스테이지
        if (bossManager != null && bossManager.IsBossStage())
        {
            bossManager.BrickDestroyed(brick);
        }
        else
        {
            // 일반 스테이지
            BrickDestroyed();
        }
    }

    // 공 초기 상태로 되돌림
    public void ResetBall()
    {
        ball.GetComponent<Ball>().ResetBall();
    }

    // 게임 일시정지
    public void PauseGame()
    {
        if (hasResult) return;
        isPaused = true; // 일시정지 상태
        Time.timeScale = 0f; // 게임 시간 정지
        // 일시정지 UI 표시
        pauseMenu.SetActive(true);
    }

    // 게임 재개
    public void ResumeGame()
    {
        if (hasResult) return;
        isPaused = false; // 일시정지 해제
        Time.timeScale = 1f; // 시간 다시 돌아감
        pauseMenu.SetActive(false);
    }

    // 처음부터 다시 시작
    public void RestartGame()
    {
        // 일시정지 해제
        Time.timeScale = 1f;
        isPaused = false;

        // 현재 게임 씬을 다시 불러옴
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 메인 메뉴로 이동
    public void GoMainMenu()
    {
        // 일시정지 해제
        Time.timeScale = 1f;
        isPaused = false;

        // 메인 메뉴 씬 불러오기
        SceneManager.LoadScene("StartScene");
    }

    // 시간 초과 등 실패 시 TimeManager에서 호출한다.
    public void ShowResult()
    {
        DisplayResult(false);
    }

    // 9스테이지 보스 클리어 시 BossManager에서 직접 호출한다.
    public void ShowGameClear()
    {
        if (currentStage != 8) return;
        DisplayResult(true);
    }

    // 실패와 클리어가 같은 패널, 스테이지/코인 표시를 공유한다.
    void DisplayResult(bool isClear)
    {
        if (hasResult) return;
        hasResult = true;
        timeManager.StopTimer();
        Time.timeScale = 0f;
        isPaused = true;
        pauseMenu.SetActive(false);

        resultTitleText.text = isClear ? "Game Clear!" : "Gameover";
        resultStageText.text = "stage : " + currentStage;
        resultCoinText.text = "coin : " +
            (CoinManager.instance != null ? CoinManager.instance.earnedCoin : 0);
        resultPanel.SetActive(true);
    }
}
