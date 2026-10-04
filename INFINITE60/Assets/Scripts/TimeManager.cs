using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    // 시간 표시 UI
    public TMP_Text timeText;

    // 스테이지 제한시간
    public float stageTime = 60f;

    // 공이 떨어졌을 때 감소하는 시간
    public float fallTimePenalty = 20f;

    // 현재 남은 시간
    public float remainTime;

    // 현재 시간이 흐르고 있는지
    bool isRunning = false;

    // 게임 오버 여부
    bool isGameOver = false;

    void Start()
    {
        // 처음에는 시간 초기화만 하고
        // 시간은 흐르지 않게 한다.
        ResetTime();
    }

    void Update()
    {
        // 시간이 흐르는 상태가 아니면 아무것도 하지 않음
        if (!isRunning || isGameOver)
        {
            return;
        }

        // 시간 감소
        remainTime -= Time.deltaTime;

        // 시간이 0 이하가 되면 게임 오버
        if (remainTime <= 0)
        {
            remainTime = 0;
            UpdateTimeUI();

            GameOver();
            return;
        }

        // 시간 UI 갱신
        UpdateTimeUI();
    }

    // 시간을 초기화
    public void ResetTime()
    {
        remainTime = stageTime;

        // 초기화할 때는 시간 정지
        isRunning = false;

        isGameOver = false;

        UpdateTimeUI();
    }

    // 공이 발사됐을 때 시간 시작
    public void StartTimer()
    {
        if (isGameOver)
        {
            return;
        }

        isRunning = true;
    }

    // 공이 발사되기 전 / 떨어진 후 시간 정지
    public void StopTimer()
    {
        isRunning = false;
    }

    // 공이 떨어졌을 때
    public void BallFell()
    {
        if (isGameOver)
        {
            return;
        }

        // 시간 20초 감소
        remainTime -= fallTimePenalty;

        // 시간이 0 이하인지 확인
        if (remainTime <= 0)
        {
            remainTime = 0;
            UpdateTimeUI();

            GameOver();
            return;
        }

        // 공이 다시 발사되기 전이므로 시간 정지
        isRunning = false;

        UpdateTimeUI();
    }

    // 시간 UI 업데이트
    void UpdateTimeUI()
    {
        if (timeText == null)
        {
            return;
        }

        timeText.text = "TIME : " + Mathf.Ceil(remainTime);
    }

    /// 게임오버 씬으로 이동으로 변경예정
    // 게임 오버
    void GameOver()
    {
        isGameOver = true;
        isRunning = false;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}