using System;
using UnityEngine;

public interface IBossChapter
{
    void StartBoss(bossmanager manager);
    void OnBrickDestroyed(Brick brick);
    void EndBoss();
}

public class bossmanager : MonoBehaviour
{
    public MonoBehaviour[] chapterBosses; // 챕터별 보스 스크립트
    IBossChapter currentBoss; // 현재 실행 중인 챕터 보스
    bool isCleared = false;
    public event Action OnBossCleared; // GameManager에 클리어를 알리는 이벤트

    // 보스 시작
    public void StartBoss(int chapter)
    {
        EndBoss(); // 이전 보스 종료
        int index = chapter - 1;

        // 챕터 번호 확인
        if (chapterBosses == null || index < 0 || index >= chapterBosses.Length)
        {
            Debug.LogError("보스 챕터가 등록되지 않았습니다.");
            return;
        }

        // 해당 챕터 보스 가져오기
        currentBoss = chapterBosses[index] as IBossChapter;

        if (currentBoss == null)
        {
            Debug.LogError("IBossChapter를 구현한 스크립트가 아닙니다.");
            return;
        }

        isCleared = false;
        currentBoss.StartBoss(this); // 해당 챕터 보스 시작
    }

    // 벽돌이 파괴되었을 때
    public void BrickDestroyed(Brick brick)
    {
        if (currentBoss == null || isCleared)
        {
            return;
        }

        // 이전 스테이지 장애물은 제외
        if (brick == null || brick.isObstacle)
        {
            return;
        }

        currentBoss.OnBrickDestroyed(brick); // 현재 챕터 보스에게 전달
    }

    // 보스 클리어
    public void BossClear()
    {
        if (currentBoss == null || isCleared)
        {
            return;
        }

        isCleared = true;

        Debug.Log("보스 스테이지 클리어!");
        OnBossCleared?.Invoke(); // GameManager에 알림
    }

    // 보스 종료
    public void EndBoss()
    {
        if (currentBoss != null)
        {
            currentBoss.EndBoss();
            currentBoss = null;
        }

        isCleared = false;
    }

    // 현재 보스 스테이지인지 확인
    public bool IsBossStage()
    {
        return currentBoss != null;
    }
}