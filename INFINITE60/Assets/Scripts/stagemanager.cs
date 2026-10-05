using System;
using System.Collections;
using UnityEngine;

public class stagemanager : MonoBehaviour
{
    // 현재 생성되어 있는 스테이지
    GameObject currentStage;
    public GameManager gameManager;
    public ball Ball;
    public panelmove Panel;
    public float transitionTime = 2f;

    // 현재 스테이지에 남아있는 일반 벽돌을 가져온다.
    // 이미 장애물이 된 벽돌은 제외한다.
    public Brick[] GetRemainBricks()
    {
        if (currentStage == null)
        {
            return new Brick[0];
        }

        Brick[] allBricks = currentStage.GetComponentsInChildren<Brick>();
        // 남아있는 일반 벽돌 개수 세기
        int count = 0;

        foreach (Brick b in allBricks)
        {
            if (!b.isObstacle)
            {
                count++;
            }
        }

        // 실제 배열 크기를 맞춰서 생성
        Brick[] remainBricks = new Brick[count];

        int index = 0;

        foreach (Brick b in allBricks)
        {
            if (!b.isObstacle)
            {
                remainBricks[index] = b;
                index++;
            }
        }

        return remainBricks;
    }

    // StageData에 저장된 스테이지 프리팹을 생성한다.
    public void CreateStage(stage stageData)
    {

        // 기존 스테이지가 남아있으면 삭제
        if (currentStage != null)
        {
            Destroy(currentStage);
        }

        // StageData에 저장된 프리팹을 생성
        currentStage = Instantiate(stageData.stagePrefab,Vector3.zero,Quaternion.identity);
    }

    // 스테이지 전환을 실행하는 함수
    public IEnumerator StageTransition(stage nextStage, Action onFinished)
    {
        // 1. 공의 현재 상태 저장
        Vector3 ballStartPosition = Ball.transform.position; // 전환 시작 순간의 공 위치를 저장
        Ball.SaveBallState(); // 공의 이동 방향과 속도 저장
        Ball.isTransitioning = true; // 스테이지 전환 중이라고 알려줌
        Ball.StopBall(); // 공 일시정지
        Panel.enabled = false; // 패널 일시정지

        // 시간 일시정지 + 새 시간으로 초기화
        gameManager.timeManager.StopTimer();
        gameManager.timeManager.ResetTime();

        // 2. 현재 스테이지의 남은 벽돌 가져오기
        Brick[] remainBricks = GetRemainBricks();
        // 기존 스테이지 정보 저장
        Vector3 oldStagePosition = currentStage.transform.position; // 기존 스테이지의 위치
        Vector3 oldStageScale = currentStage.transform.localScale; // 기존 스테이지의 크기
        // 최종 스테이지 위치와 크기
        Vector3 targetStagePosition = new Vector3(0f, -2.5f, 0f);
        float targetStageScale = 0.5f;

        // 3. 남은 벽돌을 현재 스테이지에서 분리
        foreach (Brick b in remainBricks)
        {
            if (b != null)
            {
                b.transform.SetParent(null); // 스테이지 부모에서 분리한다.
                b.isObstacle = true; // 다음부터 이 벽돌은 장애물로 취급한다.
            }
        }

        // 4. 다음 스테이지 생성
        CreateStage(nextStage);

        // 5. 남은 벽돌의 원래 위치와 크기 저장
        Vector3[] startPositions = new Vector3[remainBricks.Length];
        Vector3[] startScales = new Vector3[remainBricks.Length];
        Vector3[] targetPositions = new Vector3[remainBricks.Length];

        // 각 벽돌의 최종 위치 계산
        for (int i = 0; i < remainBricks.Length; i++)
        {
            if (remainBricks[i] != null)
            {
                startPositions[i] = remainBricks[i].transform.position; // 현재 위치
                startScales[i] = remainBricks[i].transform.localScale; // 현재 크기
                Vector3 offset = startPositions[i] - oldStagePosition; // 기존 스테이지 중심에서 벽돌까지의 거리

                // 기존 스테이지 Scale 보정
                if (oldStageScale.x != 0f)
                {
                    offset.x /= oldStageScale.x;
                }
                if (oldStageScale.y != 0f)
                {
                    offset.y /= oldStageScale.y;
                }

                offset *= targetStageScale; // 전체 패턴을 50% 축소
                targetPositions[i] = targetStagePosition + offset; // 최종 위치
            }
        }

        // 6. 공 + 벽돌 이동 및 크기 계산

        // 공의 최종 위치 계산
        float ballTargetX = ballStartPosition.x / 2f;
        float ballTargetY = ((ballStartPosition.y + 5f) / 2f) - 5f;
        Vector3 ballTargetPosition = new Vector3(ballTargetX,ballTargetY,ballStartPosition.z); // 공의 최종 위치

        // 7. 공의 시작 크기와 최종 크기
        Vector3 ballStartScale = Ball.transform.localScale;
        Vector3 ballTargetScale = ballStartScale * 0.7f;

        // 8. 공 + 벽돌 동시에 이동
        float time = 0f;

        while (time < transitionTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / transitionTime);

            // 벽돌 이동
            for (int i = 0; i < remainBricks.Length; i++)
            {
                if (remainBricks[i] != null)
                {
                    remainBricks[i].transform.position = Vector3.Lerp(startPositions[i],targetPositions[i],t); // 벽돌 위치 이동
                    remainBricks[i].transform.localScale = Vector3.Lerp(startScales[i],startScales[i] * 0.5f,t); // 벽돌 크기 50%까지 감소
                }
            }

            Ball.transform.position = Vector3.Lerp(ballStartPosition,ballTargetPosition,t); // 공 위치 이동
            Ball.transform.localScale = Vector3.Lerp(ballStartScale,ballTargetScale,t); // 공 크기 70%까지 감소
            yield return null; // 한 프레임 기다림
        }

        // 최종 위치 정확하게 적용
        Ball.transform.position = ballTargetPosition;
        Ball.transform.localScale =  ballTargetScale;

        // 9. 연출이 끝난 후 공 크기와 속도 감소 및 패널 활성화
        //Ball.ReduceSize();
        Ball.ReduceSpeed();
        Ball.RestoreBallState();
        Panel.enabled = true;

        // 10. 시간 다시 시작
        gameManager.timeManager.StartTimer();

        // 11. 게임매니저에게 전환 완료 알림
        if (onFinished != null)
        {
            onFinished();
        }
    }

    // 현재 스테이지의 벽돌 개수를 반환한다.
    public int GetBrickCount()
    {
        if (currentStage == null)
        {
            return 0;
        }

        Brick[] bricks = currentStage.GetComponentsInChildren<Brick>();

        int count = 0;
        //장해물은 카운트 안되게
        foreach (Brick b in bricks)
        {
            if (!b.isObstacle)
            {
                count++;
            }
        }

        return count;
    }

    // 현재 스테이지를 삭제한다.
    public void ClearStage()
    {

        // 현재 생성된 스테이지가 있는 경우
        if (currentStage != null)
        {
            Destroy(currentStage);

            // 삭제했으므로 null로 설정
            currentStage = null;
        }

    }

}