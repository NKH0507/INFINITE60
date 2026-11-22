using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    // 현재 생성되어 있는 스테이지
    GameObject currentStage;
    // 이전 스테이지의 장애물도 다음 전환에서 다시 축소할 수 있도록 보관한다.
    [SerializeField, HideInInspector] List<Brick> obstacleBricks = new List<Brick>();
    const float StageShrinkRatio = 0.5f;
    public GameManager gameManager;
    [UnityEngine.Serialization.FormerlySerializedAs("Ball")]
    public Ball ball;
    [UnityEngine.Serialization.FormerlySerializedAs("Panel")]
    [UnityEngine.Serialization.FormerlySerializedAs("panel")]
    public PaddleMove paddle;
    public float transitionTime = 2f;
    // 챕터 배경 (씬에서 자동으로 찾는다)
    ChapterBackground background;

    // 스테이지가 실제로 플레이되는 위치
    public Vector3 playPosition = new Vector3(0f, 0f, 0f);

    // 기준점과의 X/Y 거리를 배율만큼 바꾼다. 2D 표시 순서에 쓰이는 Z는 유지한다.
    static Vector3 ScalePositionAroundPivot(Vector3 position, Vector3 pivot, float ratio)
    {
        return new Vector3(
            pivot.x + (position.x - pivot.x) * ratio,
            pivot.y + (position.y - pivot.y) * ratio,
            position.z
        );
    }

    static Vector3 ScaleSize2D(Vector3 scale, float ratio)
    {
        return new Vector3(scale.x * ratio, scale.y * ratio, scale.z);
    }

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

    // 스테이지 생성
    public void CreateStage(Stage stageData)
    {
        // 플레이 위치에 생성
        currentStage = Instantiate(stageData.stagePrefab,playPosition,Quaternion.identity);
    }

    // 스테이지 전환을 실행하는 함수
    public IEnumerator StageTransition(Stage nextStage, Action onFinished)
    {
        // 패들의 중심이 아니라 콜라이더의 실제 윗면을 사용한다.
        // X는 패들이 좌우로 움직여도 항상 화면 중앙인 0으로 고정한다.
        Collider2D paddleCollider = paddle.GetComponent<Collider2D>();
        if (paddleCollider == null || !paddleCollider.enabled)
        {
            Debug.LogError("스테이지 축소 기준점을 계산할 패들 Collider2D가 없습니다.", this);
            yield break;
        }
        Vector3 shrinkPivot = new Vector3(0f, paddleCollider.bounds.max.y, 0f);

        // 1. 공의 현재 상태 저장
        Vector3 ballStartPosition = ball.transform.position; // 전환 시작 순간의 공 위치를 저장
        ball.SaveBallState(); // 공의 이동 방향과 속도 저장
        ball.isTransitioning = true; // 스테이지 전환 중이라고 알려줌
        ball.StopBall(); // 공 일시정지
        paddle.enabled = false; // 패들 일시정지

        // 시간 일시정지
        gameManager.timeManager.StopTimer();

        // 2. 현재 스테이지의 남은 벽돌 가져오기
        Brick[] remainBricks = GetRemainBricks();
        GameObject oldStage = currentStage;
        // 파괴된 장애물은 제외하고, 살아 있는 이전 벽돌의 현재 크기부터 다시 절반으로 줄인다.
        obstacleBricks.RemoveAll(brick => brick == null || brick.hp <= 0);

        // 3. 남은 벽돌을 현재 스테이지에서 분리
        foreach (Brick b in remainBricks)
        {
            if (b != null)
            {
                b.transform.SetParent(null); // 스테이지 부모에서 분리한다.
                b.isObstacle = true; // 다음부터 이 벽돌은 장애물로 취급한다.
                if (b.hp > 0 && !obstacleBricks.Contains(b)) obstacleBricks.Add(b);
            }
        }
        // 분리한 벽돌은 유지하고, 더 이상 사용하지 않는 스테이지 부모만 제거한다.
        Destroy(oldStage);
        Brick[] shrinkingBricks = obstacleBricks.ToArray();

        // 4. 다음 스테이지는 전환 시점에 새로 생성한다.
        // 프리팹의 원래 크기를 최종 크기로 저장한다.
        // Vector3.one을 쓰지 않아 프리팹에 설정된 크기도 그대로 유지한다.
        Vector3 nextTargetScale = nextStage.stagePrefab.transform.localScale;

        // 전환을 시작할 때 가로/세로를 2배로 확대한다. 2D 화면의 깊이(Z)는 유지한다.
        // 부모를 확대하므로 벽돌 크기뿐 아니라 벽돌 사이 간격도 함께 커진다.
        Vector3 nextStartScale = ScaleSize2D(nextTargetScale, 1f / StageShrinkRatio);
        // 새 배치도 동일한 기준점에서 2배 확대한 위치에 생성한다.
        // 이 위치와 크기를 함께 절반으로 줄이면 정확히 playPosition과 원래 배치에 도달한다.
        Vector3 nextStartPosition = ScalePositionAroundPivot(playPosition, shrinkPivot, 1f / StageShrinkRatio);
        currentStage = Instantiate(nextStage.stagePrefab,nextStartPosition,Quaternion.identity);
        // 생성 직후 같은 프레임에 2배 크기를 적용하여 원래 크기로 대기하는 과정이 없게 한다.
        currentStage.transform.localScale = nextStartScale;

        // 5. 남은 벽돌의 원래 위치와 크기 저장
        Vector3[] startPositions = new Vector3[shrinkingBricks.Length];
        Vector3[] startScales = new Vector3[shrinkingBricks.Length];
        Vector3[] targetPositions = new Vector3[shrinkingBricks.Length];

        // 각 벽돌의 최종 위치 계산
        for (int i = 0; i < shrinkingBricks.Length; i++)
        {
            if (shrinkingBricks[i] != null)
            {
                startPositions[i] = shrinkingBricks[i].transform.position;
                startScales[i] = shrinkingBricks[i].transform.localScale;
                // 새 위치 = 기준점 + (현재 위치 - 기준점) × 0.5
                // 부모 크기로 나누지 않아 이전에 줄어든 벽돌도 현재 상태에서 다시 절반이 된다.
                targetPositions[i] = ScalePositionAroundPivot(startPositions[i], shrinkPivot, StageShrinkRatio);
            }
        }

        // 6. 공 + 벽돌 이동 및 크기 계산

        // 공의 최종 위치 계산
        // 공도 같은 기준으로 이동해 벽돌과의 상대적인 배치가 어긋나지 않게 한다.
        Vector3 ballTargetPosition = ScalePositionAroundPivot(ballStartPosition, shrinkPivot, StageShrinkRatio);

        // 7. 공의 시작 크기와 최종 크기
        Vector3 ballStartScale = ball.transform.localScale;
        // 새 스테이지 기준으로 다시 맞추는 대신, 시작 크기를 정확히 절반으로 줄인다.
        Vector3 ballTargetScale = ScaleSize2D(ballStartScale, StageShrinkRatio);

        // 배경도 같은 기준점, 같은 시간으로 함께 줌아웃한다.
        if (background == null) background = FindFirstObjectByType<ChapterBackground>();
        if (background != null) background.PlayZoomOut(gameManager.currentStage, shrinkPivot, transitionTime);

        // 8. 공 + 벽돌 동시에 이동
        float time = 0f;

        while (time < transitionTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / transitionTime);
            currentStage.transform.position = Vector3.Lerp(nextStartPosition,playPosition,t); // 다음 스테이지 전체를 위에서 아래로 이동

            // 내려오는 진행률과 동일하게 2배 크기에서 원래 크기(시작 크기의 1/2)로 축소한다.
            // 기존 벽돌이 하단 중앙으로 축소되는 연출과 동시에 진행된다.
            currentStage.transform.localScale = Vector3.Lerp(nextStartScale,nextTargetScale,t);

            // 벽돌 이동
            for (int i = 0; i < shrinkingBricks.Length; i++)
            {
                if (shrinkingBricks[i] != null)
                {
                    shrinkingBricks[i].transform.position = Vector3.Lerp(startPositions[i],targetPositions[i],t);
                    shrinkingBricks[i].transform.localScale = Vector3.Lerp(startScales[i],ScaleSize2D(startScales[i], StageShrinkRatio),t);
                }
            }

            ball.transform.position = Vector3.Lerp(ballStartPosition,ballTargetPosition,t); // 공 위치 이동
            ball.transform.localScale = Vector3.Lerp(ballStartScale,ballTargetScale,t); // 시작 크기의 정확히 1/2까지 축소
            yield return null; // 한 프레임 기다림
        }

        // 최종 위치 정확하게 적용
        // 전환 시간이 0이어도 기존/이전 스테이지 벽돌의 최종 위치와 크기를 적용한다.
        for (int i = 0; i < shrinkingBricks.Length; i++)
        {
            if (shrinkingBricks[i] == null) continue;
            shrinkingBricks[i].transform.position = targetPositions[i];
            shrinkingBricks[i].transform.localScale = ScaleSize2D(startScales[i], StageShrinkRatio);
        }
        ball.transform.position = ballTargetPosition;
        ball.transform.localScale =  ballTargetScale;
        currentStage.transform.position = playPosition;
        // 전환 시간이 0이거나 프레임 간격이 커도 마지막 크기는 정확히 맞춘다.
        currentStage.transform.localScale = nextTargetScale;

        // 9. 연출이 끝난 후 공 크기 감소 및 패들 활성화
        ball.RestoreBallState();
        ball.isTransitioning = false;   // 크기 자동 계산 다시 켜기
        ball.RefreshSize(true);         // 새 스테이지 기준 크기로 맞춤
        paddle.enabled = true;

        // 10. 시간증가 및 다시 시작
        gameManager.timeManager.PlusTimer();
        // 발사 전 고공격력으로 자동 전환된 경우에는 타이머를 시작하지 않는다.
        if (ball.isLaunched) gameManager.timeManager.StartTimer();

        // 11. 게임매니저에게 전환 완료 알림. 다음 배치는 다음 전환 때 생성한다.
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
