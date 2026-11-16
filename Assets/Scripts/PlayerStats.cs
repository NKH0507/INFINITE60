using UnityEngine;

public static class PlayerStats  // 플레이어 스탯 증가 (공격력, 놓쳤을 때 시간, 라운드 시간, 코인 지급량)
{
    public static float GetAttackMultiplier(int level) // 공격력 배율
    {
        return (100 + 10 * ClampLevel(level)) / 100f; // 1레벨 100% -> 2레벨 110%
    }

    public static int GetMissPenaltySeconds(int level) // 놓쳤을 때 시간 감소
    {
        int penaltyLevel = Mathf.Clamp(level, 0, 15); // 최대 레벨 15로 제한
        return 20 - penaltyLevel;                     // 레벨 증가 마다 1초 감소
    }



    public static int GetStartingTimeSeconds(int level) // 라운드 시간 증가
    {
        int startingTimeLevel = Mathf.Clamp(level, 0, 15); // 레벨을 15까지 제한 (최대 라운드 시간 2분)
        return 60 + 4 * ClampLevel(level);              // 레벨마다 시간 4초 증가
    }

    
    public static int GetCoinReward(int baseAmount, int level) // 코인 지급량 증가
    {
        int percent = 100 + 10 * ClampLevel(level);            // 레벨마다 10% 증가
        return (int)((long)baseAmount * percent / 100);
    }

    private static int ClampLevel(int level) // 레벨 범위 50까지
    {
        return Mathf.Clamp(level, 0, 50);
    }
}