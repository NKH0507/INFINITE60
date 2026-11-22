using UnityEngine;
using UnityEngine.UI;

public class StatUpgradeButton : MonoBehaviour
{
    public enum StatType
    {
        Attack = 0,
        MissPenalty = 1,
        CoinGain,
        ClearTimeBonus = 4,
        Absorption = 5,
        PaddleSize = 6
    }

    [SerializeField] private StatType stat;
    [SerializeField] private int upgradeCost = 50;
    [SerializeField] private Text statText;

    void OnEnable()
    {
        UpdateStatText();
    }

    void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        if (keyboard != null && keyboard.f2Key.wasPressedThisFrame)
        {
            ResetStatLevels();
            UpdateStatText();
        }
    }

    void UpdateStatText()
    {
        if (statText == null)
        {
            return;
        }

        int level = GetLevel(stat);

        switch (stat)
        {
            case StatType.Attack:
                float attackPower =
                    10f * PlayerStats.GetAttackMultiplier(level);

                statText.text =
                    $"시작 공격력 Lv.{level}\n{attackPower:0.#}";
                break;

            case StatType.MissPenalty:
                int penalty = PlayerStats.GetMissPenaltySeconds(level);

                statText.text =
                    $"공 놓침 페널티 Lv.{level}\n-{penalty}초";
                break;

            case StatType.CoinGain:
                int coinPercent = PlayerStats.GetCoinReward(100, level);

                statText.text =
                    $"코인 획득 Lv.{level}\n{coinPercent}%";
                break;

            case StatType.ClearTimeBonus:
                float bonusSeconds =
                    PlayerStats.GetClearBonusSeconds(level);

                statText.text =
                    $"클리어 시간 추가 Lv.{level}\n+{bonusSeconds:0.#}초";
                break;

            case StatType.Absorption:
                float absorptionPercent =
                    PlayerStats.GetAbsorptionRate(level) * 100f;

                statText.text =
                    $"흡수율 Lv.{level}\n{absorptionPercent:0.#}%";
                break;

            case StatType.PaddleSize:
                float scaleX = PlayerStats.GetPaddleScaleX(level);

                statText.text =
                    $"패들 크기 Lv.{level}\n{scaleX:0.#}";
                break;
        }
    }

    public void Upgrade()
    {
        int level = GetLevel(stat);

        if (level >= 10)
        {
            Debug.Log("이미 최대 레벨입니다.");
            return;
        }

        if (CoinManager.instance == null)
        {
            Debug.Log("씬에 CoinManager가 필요합니다.");
            return;
        }

        if (!CoinManager.instance.UseCoin(upgradeCost))
        {
            Debug.Log("코인이 부족합니다.");
            return;
        }

        int newLevel = level + 1;
        PlayerPrefs.SetInt(GetSaveKey(stat), newLevel);
        PlayerPrefs.Save();

        UpdateStatText();

        Debug.Log(stat + " 업그레이드 완료 현재 레벨 " + newLevel);
    }

    public static void ResetStatLevels()
    {
        PlayerPrefs.SetInt(GetSaveKey(StatType.Attack), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.MissPenalty), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.CoinGain), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.ClearTimeBonus), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.Absorption), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.PaddleSize), 0);
        PlayerPrefs.Save();

        Debug.Log("모든 스탯 레벨을 0으로 초기화했습니다.");
    }

    public static int GetLevel(StatType stat)
    {
        int savedLevel = PlayerPrefs.GetInt(GetSaveKey(stat), 0);
        return Mathf.Clamp(savedLevel, 0, 10);
    }

    private static string GetSaveKey(StatType stat)
    {
        return "StatLevel_" + stat;
    }
}