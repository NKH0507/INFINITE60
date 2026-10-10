using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chapter1Boss : MonoBehaviour, IBossChapter
{
    public stagemanager stageManager;
    public stage bossStageData; // 원본 배치 데이터
    public GameObject lockPrefab; // 자물쇠 이미지 프리팹
    public int requiredLockCount = 5; // 클리어에 필요한 자물쇠 파괴 횟수
    int destroyedLockCount = 0; // 현재 자물쇠 파괴 횟수
    Brick lockedBrick;
    HashSet<Brick> remainingBricks = new HashSet<Brick>(); // 남아 있는 벽돌
    bossmanager manager;
    Coroutine reloadCoroutine; // 배치 재생성 코루틴
    // 보스 진행 상태
    bool isActive = false;
    bool isCleared = false;

    // 1챕터 보스 시작
    public void StartBoss(bossmanager bossManager)
    {
        manager = bossManager;
        destroyedLockCount = 0;
        lockedBrick = null;
        isActive = true;
        isCleared = false;
        SetupBricks(); // 벽돌 등록
    }

    // 현재 스테이지 벽돌 등록
    void SetupBricks()
    {
        remainingBricks.Clear();
        lockedBrick = null;
        Brick[] bricks = stageManager.GetRemainBricks(); // 현재 스테이지의 벽돌만 가져오기

        foreach (Brick brick in bricks)
        {
            if (brick != null)
            {
                remainingBricks.Add(brick);
            }
        }

        SetRandomLock(); // 랜덤 자물쇠 지정
    }

    // 남은 벽돌 중 랜덤으로 자물쇠 하나 지정
    void SetRandomLock()
    {
        lockedBrick = null; // 이전 자물쇠 정보 초기화

        if (remainingBricks.Count == 0)
        {
            return;
        }

        List<Brick> bricks = new List<Brick>(); // 남아 있는 벽돌을 리스트로 변환

        foreach (Brick brick in remainingBricks)
        {
            if (brick != null)
            {
                bricks.Add(brick);
            }
        }

        if (bricks.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, bricks.Count); // 랜덤으로 벽돌 하나 선택
        lockedBrick = bricks[randomIndex];

        // 선택한 벽돌에 자물쇠 표시
        if (lockPrefab != null)
        {
            GameObject lockObject = Instantiate(lockPrefab,lockedBrick.transform);
            lockObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            lockObject.transform.localRotation = Quaternion.identity;
        }

    }

    // 벽돌이 파괴되었을 때
    public void OnBrickDestroyed(Brick brick)
    {
        if (!isActive || isCleared)
        {
            return;
        }

        // 2스테이지 벽돌이 아니면 무시
        if (!remainingBricks.Remove(brick))
        {
            return;
        }

        // 자물쇠 벽돌을 파괴했는지 확인
        if (brick == lockedBrick)
        {
            destroyedLockCount++;
            lockedBrick = null; // 기존 자물쇠 정보 삭제

            // 목표 횟수에 도달했다면 클리어
            if (destroyedLockCount >= requiredLockCount)
            {
                BossClear();
                return;
            }

            // 벽돌이 남아 있다면
            if (remainingBricks.Count > 0)
            {
                SetRandomLock(); // 남아 있는 벽돌 중 새로운 자물쇠 지정
            }

        }

        // 현재 배치의 벽돌이 전부 파괴되었다면
        if (remainingBricks.Count == 0 && reloadCoroutine == null)
        {
            reloadCoroutine = StartCoroutine(ReloadBricks());
        }

    }

    // 현재 2스테이지 벽돌 배치 재생성
    IEnumerator ReloadBricks()
    {
        yield return null; // 마지막 벽돌이 실제로 삭제될 때까지 대기

        if (!isActive || isCleared)
        {
            reloadCoroutine = null;
            yield break; //코루틴(일시 정지) 즉시 종료
        }

        if (bossStageData == null)
        {
            reloadCoroutine = null;
            yield break;
        }

        stageManager.CreateStage(bossStageData); // 같은 배치 다시 불러오기
        SetupBricks(); // 벽돌 등록 및 새 자물쇠 지정
        reloadCoroutine = null;
    }

    // 1챕터 보스 클리어
    void BossClear()
    {
        if (isCleared)
        {
            return;
        }

        isCleared = true;
        isActive = false;

        // 남아 있는 2스테이지 벽돌 모두 삭제
        foreach (Brick brick in remainingBricks)
        {
            if (brick != null)
            {
                Destroy(brick.gameObject);
            }
        }

        remainingBricks.Clear();
        lockedBrick = null;
        manager.BossClear(); // 공통 보스매니저에 클리어 전달
    }

    // 보스 종료
    public void EndBoss()
    {
        isActive = false;
        isCleared = false;

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        remainingBricks.Clear();
        lockedBrick = null;
    }
}