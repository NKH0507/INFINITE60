using UnityEngine;
using UnityEngine.UI;
using static StatUpgradeButton;


public class StatUpgradeButton : MonoBehaviour
{
    public enum StatType
    {
        Attack,        // 공격력
        MissPenalty,   // 공 놓침 페널티
        StartingTime,  // 라운드 시작 시간
        CoinGain       // 코인 획득량
    }

    
    [SerializeField] private StatType stat;
   
    [SerializeField] private int upgradeCost = 50;   // 한 번 구매에 50코인

    [SerializeField] private Text statText;

    void OnEnable()
    {
        UpdateStatText();
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
                float attackPercent =
                    PlayerStats.GetAttackMultiplier(level) * 100f;

                statText.text =
                    $"공격력 Lv.{level}\n{attackPercent:0}%";
                break;

            case StatType.MissPenalty:
                int penalty = PlayerStats.GetMissPenaltySeconds(level);

                statText.text =
                    $"공 놓침 페널티 Lv.{level}\n-{penalty}초";
                break;

            case StatType.StartingTime:
                int seconds = PlayerStats.GetStartingTimeSeconds(level);

                statText.text =
                    $"시작 시간 Lv.{level}\n{seconds}초";
                break;

            case StatType.CoinGain:
                int coinPercent = PlayerStats.GetCoinReward(100, level);

                statText.text =
                    $"코인 획득 Lv.{level}\n{coinPercent}%";
                break;
        }
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

    public static void ResetStatLevels()
    {
        PlayerPrefs.SetInt(GetSaveKey(StatType.Attack), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.MissPenalty), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.StartingTime), 0);
        PlayerPrefs.SetInt(GetSaveKey(StatType.CoinGain), 0);
        PlayerPrefs.Save();

        Debug.Log("모든 스탯 레벨을 0으로 초기화했습니다.");
    }


    public void Upgrade()
    {
        int level = GetLevel(stat);

        
        int maxLevel = 50;                       // 기본 최대 레벨 15

      
        if (stat == StatType.MissPenalty ||      // 시간 관련 레벨은 15가 최대
            stat == StatType.StartingTime)
        {
            maxLevel = 15;
        }

        if (level >= maxLevel)
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

  
        int newLevel = level + 1;                       // 레벨 1올리고 저장
        PlayerPrefs.SetInt(GetSaveKey(stat), newLevel);
        PlayerPrefs.Save();

        UpdateStatText();

        Debug.Log(stat + " 업그레이드 완료 현재 레벨 " + newLevel);
    }

                                                       // 다른 코드에서 읽기 가능
    public static int GetLevel(StatType stat)
    {
        return PlayerPrefs.GetInt(GetSaveKey(stat), 0);
    }

    
    private static string GetSaveKey(StatType stat)   // 스탯 마다 다른 이름으로 저장
    {
        return "StatLevel_" + stat;
    }
}