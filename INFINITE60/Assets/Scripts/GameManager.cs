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

    // 벽돌들이 들어있는 부모 오브젝트
    public Transform brickParent;

    // 70% 이상 깨졌는지 확인
    bool isChangingStage = false;

    // 처음 카메라 크기
    float startCameraSize;

    public int startBrickCount;

    void Start()
    {
        // 게임 시작할 때 카메라 크기 저장
        startCameraSize = mainCamera.orthographicSize;
    }

    void Update()
    {
        // 이미 화면 전환 중이면 다시 실행하지 않음
        if (isChangingStage)
        {
            return;
        }

        // 현재 존재하는 벽돌 개수 확인
        int totalBrick = brickParent.childCount;

        // 전체 벽돌이 모두 사라진 경우 방지
        if (totalBrick == 0)
        {
            return;
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

        // 현재 남아있는 벽돌 개수
        int remainBrick = brickParent.childCount;
        // 깨진 벽돌 개수
        int destroyedBrick = startBrickCount - remainBrick;
        // 깨진 비율
        float destroyPercent = (float)destroyedBrick / startBrickCount;

        // 70% 이상 깨졌으면 화면 전환
        if (destroyPercent >= 0.7f) 
        { 
            StartStageChange(); 
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

        // 1. 공과 패널 숨기기
        ball.SetActive(false);
        panel.SetActive(false);

        // 2. 카메라 줌아웃
        float targetSize = 20f;

        while (mainCamera.orthographicSize < targetSize)
        {
            // 카메라 크기를 점점 크게 만듦
            mainCamera.orthographicSize += Time.deltaTime * 5f;

            yield return null; //yield return = 함수 일시정지
        }

        // 잠깐 기다림
        yield return new WaitForSeconds(0.5f);

        // 남아있는 벽돌 전부 삭제
        for (int i = brickParent.childCount - 1; i >= 0; i--)
        {
            Destroy(brickParent.GetChild(i).gameObject);
        }

        // 4. 카메라 원래 크기로 즉시 복구
        mainCamera.orthographicSize = startCameraSize;

        // 정확하게 원래 크기로 맞춤
        mainCamera.orthographicSize = startCameraSize;

        // 5. 공과 패널 다시 등장
        panel.SetActive(true);
        ball.SetActive(true);
        ResetBall();

        // 다음 스테이지 준비 완료
        isChangingStage = false;
    }

    //공 초기 상태로 되돌림
    public void ResetBall()
    {
        ball.GetComponent<ball>().ResetBall();
    }

}