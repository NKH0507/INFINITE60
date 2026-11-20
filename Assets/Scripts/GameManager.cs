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

    // 패널
    public GameObject panel;

    // 스테이지를 관리하는 StageManager
    public stagemanager stageManager;

    // 현재 스테이지 번호
    public int currentStage = 1;

    // 스테이지 데이터 목록
    public stage[] stages;

    // 70% 이상 깨졌는지 확인
    bool isChangingStage = false;

    // 현재 스테이지의 처음 벽돌 개수
    int startBrickCount;

    // 시간 관리
    public TimeManager timeManager;

    public bossmanager bossManager;

    // 일시정지 상태인지 확인
    public bool isPaused = false;

    // 일시정지 메뉴
    public GameObject pauseMenu;

    // 결과 화면
    public GameObject resultPanel;

    // 결과 화면 텍스트
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

    void Awake()
    {
        zoomPowers = new float[stages.Length + 1];
        zoomPowers[0] = firstStagePower;

        float totalHp = 0f;
        for (int i = 0; i < stages.Length; i++)
        {
            // 스테이지 프리팹 안 벽돌의 HP를 읽어서 누적
            foreach (Brick b in stages[i].stagePrefab.GetComponentsInChildren<Brick>(true))
                totalHp += b.hp;

            zoomPowers[i + 1] = zoomRate * (firstStagePower + BaseAbsorbRate * totalHp);
        }
    }

    // 이번 스테이지가 시작된 기준과 줌아웃 기준
    public float StageStartPower => zoomPowers[Mathf.Clamp(currentStage - 1, 0, stages.Length)];
    public float StageZoomPower => zoomPowers[Mathf.Clamp(currentStage, 0, stages.Length)];

    // 벽돌을 흡수할 때마다 호출. 기준을 넘으면 줌아웃
    public void CheckZoom()
    {
        if (isChangingStage) return;
        if (ball.GetComponent<ball>().power >= StageZoomPower)
            StartStageChange();
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

        // 다음 스테이지가 존재하면 위쪽에 미리 생성
        if (currentStage < stages.Length)
        {
            stageManager.CreateWaitingStage(stages[currentStage]);
        }

        // 이번 게임에서 얻은 코인 초기화
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.ResetEarnedCoin();
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
        // 이미 실행 중이면 중복 실행하지 않음
        if (isChangingStage)
        {
            return;
        }

        StartCoroutine(ChangeStage());
    }

    IEnumerator ChangeStage()
    {
        isChangingStage = true; // 스테이지 전환 중이라는 것을 표시
        currentStage++; // 다음 스테이지 번호로 변경

        // 다음 스테이지가 존재하는지 확인
        if (currentStage <= stages.Length)
        {
            yield return StartCoroutine(stageManager.StageTransition(stages[currentStage - 1],ChangeStageFinished)); // StageManager에게 스테이지 전환을 맡긴다.
        }

    }

    // 스테이지 전환이 끝난 후 실행되는 함수
    void ChangeStageFinished()
    {
        // 보스 스테이지
        if (currentStage % 2 == 0)
        {
            int chapter = currentStage / 2;
            bossManager.StartBoss(chapter);
        }

        startBrickCount = stageManager.GetBrickCount(); // 새로 생성된 스테이지의 전체 벽돌 개수를 저장한다.
        isChangingStage = false; // 스테이지 전환이 끝났다고 표시
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
        ball.GetComponent<ball>().ResetBall();
    }

    // 게임 일시정지
    public void PauseGame()
    {
        isPaused = true; // 일시정지 상태
        Time.timeScale = 0f; // 게임 시간 정지
        // 일시정지 UI 표시
        pauseMenu.SetActive(true);
    }

    // 게임 재개
    public void ResumeGame()
    {
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

    // 게임 종료 결과 화면 표시
    public void ShowResult()
    {
        // 게임 멈춤
        Time.timeScale = 0f;
        isPaused = true;

        // 결과 텍스트 표시
        resultStageText.text = "stage : " + currentStage;

        // 이번 게임에서 획득한 코인 표시
        if (CoinManager.Instance != null)
        {
            resultCoinText.text = "coin : " + CoinManager.Instance.earnedCoin;
        }

        // 결과창 표시
        resultPanel.SetActive(true);
    }
}