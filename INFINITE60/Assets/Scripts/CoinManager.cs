using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance; // 다른 스크립트에서 접근할 수 있도록 설정
    public int coin = 0; // 현재 보유 코인
    TMP_Text coinText; // 코인 표시 텍스트

    void Awake()
    {
        // 코인매니저 중복 생성 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 변경되어도 코인매니저 유지
        coin = PlayerPrefs.GetInt("Coin", 0); // 저장된 코인 불러오기
        SceneManager.sceneLoaded += OnSceneLoaded; // 씬이 변경되었을 때 실행
    }

    void Start()
    {
        FindCoinText();
    }

    void Update()
    {
        // F1을 누르면 코인 초기화
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame)
        {
            ResetCoin();
        }
    }

    // 코인 초기화
    public void ResetCoin()
    {
        coin = 0;

        // 저장된 코인도 0으로 변경
        PlayerPrefs.SetInt("Coin", 0);
        PlayerPrefs.Save();

        // 화면에 표시된 코인 갱신
        UpdateCoinText();
    }

    // 씬 전환 시 새로운 코인 UI 찾기
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindCoinText();
    }

    // 코인 UI 자동 연결
    void FindCoinText()
    {
        GameObject obj = GameObject.Find("CoinText");

        if (obj != null)
        {
            coinText = obj.GetComponent<TMP_Text>();
        }
        else
        {
            coinText = null;
        }

        UpdateCoinText();
    }

    // 코인 획득
    public void AddCoin(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        coin += amount;

        SaveCoin();
        UpdateCoinText();
    }

    // 코인 사용
    public bool UseCoin(int amount)
    {
        // 코인이 부족하거나 잘못된 값이면 실패
        if (amount <= 0 || coin < amount)
        {
            return false;
        }

        coin -= amount;

        SaveCoin();
        UpdateCoinText();

        return true;
    }

    // 코인 저장
    void SaveCoin()
    {
        PlayerPrefs.SetInt("Coin", coin);
        PlayerPrefs.Save();
    }

    // 코인 UI 갱신
    void UpdateCoinText()
    {
        if (coinText != null)
        {
            coinText.text = "Coin : " + coin;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }
}