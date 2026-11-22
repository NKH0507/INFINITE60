using UnityEngine;

public static class PlayerStats  // 플레이어 스탯 증가 (공격력, 놓쳤을 때 시간, 라운드 시간, 코인 지급량)
{
    public static float GetAttackMultiplier(int level)  // 시작 공격력 배율
    {
        int attackLevel = Mathf.Clamp(level, 0, 10);

        return 1f + 0.05f * attackLevel;
    }

    public static int GetMissPenaltySeconds(int level) // 놓쳤을 때 시간 감소
    {
        int penaltyLevel = Mathf.Clamp(level, 0, 10); // 최대 레벨 10로 제한
        return 20 - penaltyLevel;                     // 레벨 증가 마다 1초 감소
    }
    
    public static int GetCoinReward(int baseAmount, int level) // 코인 지급량 증가
    {
        int percent = 100 + 10 * ClampLevel(level);            // 레벨마다 10% 증가
        return (int)((long)baseAmount * percent / 100);
    }

    private static int ClampLevel(int level) // 레벨 범위 10까지
    {
        return Mathf.Clamp(level, 0, 10);
    }
    public static float GetPaddleScaleX(int level) // 패들의 가로 크기
    {
        int paddleLevel = Mathf.Clamp(level, 0, 10);

        return 1.5f + 0.1f * paddleLevel;
    }

    public static float GetClearBonusSeconds(int level) //클리어 시 추가 시간
    {
        int clearBonusLevel = Mathf.Clamp(level, 0, 10);

        return 0.5f * clearBonusLevel;
    }

    public static float GetAbsorptionRate(int level)
    {
        int absorptionLevel = Mathf.Clamp(level, 0, 10);

        return 0.1f + 0.005f * absorptionLevel;
    }
}