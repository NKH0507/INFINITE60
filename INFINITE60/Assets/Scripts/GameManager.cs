using UnityEngine;
using System.Collections;
using System;

public class GameManager : MonoBehaviour
{
    // 메인 카메라
    public Camera mainCamera;

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

    // 처음 카메라 크기
    float startCameraSize;

    // 현재 스테이지의 처음 벽돌 개수
    int startBrickCount;

    // 시간 관리
    public TimeManager timeManager;

    void Start()
    {
        // 게임 시작할 때 카메라 크기 저장
        startCameraSize = mainCamera.orthographicSize;

        // 첫 번째 스테이지 생성
        stageManager.CreateStage(stages[currentStage - 1]);

        // 생성된 벽돌 개수 저장
        startBrickCount = stageManager.GetBrickCount();
    }


    // 벽돌이 하나 깨질 때마다 호출할 함수
    public void BrickDestroyed()
    {
        // 이미 화면 전환 중이면 실행하지 않음
        if (isChangingStage)
        {
            return;
        }

        // 현재 남아있는 벽돌 개수
        int remainBrick = stageManager.GetBrickCount();

        // 깨진 벽돌 개수
        int destroyedBrick = startBrickCount - remainBrick;

        // 깨진 비율
        float destroyPercent =
            (float)destroyedBrick / startBrickCount;

        // 70% 이상 깨졌으면 다음 스테이지로 이동
        if (destroyPercent >= 0.7f)
        {
            StartStageChange();
        }
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
        isChangingStage = true;

        //시간 정지
        timeManager.StopTimer();

        // 공과 패널 숨기기
        ball.SetActive(false);
        panel.SetActive(false);

        // 카메라 줌아웃
        float targetSize = 20f;

        while (mainCamera.orthographicSize < targetSize)
        {
            // 카메라 크기를 점점 크게 만듦
            mainCamera.orthographicSize += Time.deltaTime * 5f;

            yield return null;
        }

        // 잠깐 기다림
        yield return new WaitForSeconds(0.5f);

        // 현재 스테이지 삭제
        stageManager.ClearStage();

        // 다음 스테이지 번호
        currentStage++;

        // 다음 스테이지가 존재하는지 확인
        if (currentStage <= stages.Length)
        {
            // 다음 스테이지 생성
            stageManager.CreateStage(stages[currentStage - 1]);
            // 다음 스테이지의 벽돌 개수 저장
            startBrickCount = stageManager.GetBrickCount();
            // 제한시간 초기화
            timeManager.ResetTime();
        }

        // 카메라 원래 크기로 복구
        mainCamera.orthographicSize = startCameraSize;

        // 공과 패널 다시 등장
        panel.SetActive(true);
        ball.SetActive(true);

        // 공 초기화
        ResetBall();

        // 화면 전환 완료
        isChangingStage = false;
    }

    // 공 초기 상태로 되돌림
    public void ResetBall()
    {
        ball.GetComponent<ball>().ResetBall();
    }
}