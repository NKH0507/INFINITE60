using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    // �ð� ǥ�� UI
    public TMP_Text timeText;

    // �������� ���ѽð�
    public float stageTime = 60f;

    // ���� �������� �� �����ϴ� �ð�
    public float fallTimePenalty = 20f;

    // ���� ���� �ð�
    public float remainTime;

    // ���� �ð��� �帣�� �ִ���
    bool isRunning = false;

    // ���� ���� ����
    bool isGameOver = false;

    void Start()
    {
        // ó������ �ð� �ʱ�ȭ�� �ϰ�
        // �ð��� �帣�� �ʰ� �Ѵ�.
        ResetTime();
    }

    void Update()
    {
        // �ð��� �帣�� ���°� �ƴϸ� �ƹ��͵� ���� ����
        if (!isRunning || isGameOver)
        {
            return;
        }

        // �ð� ����
        remainTime -= Time.deltaTime;

        // �ð��� 0 ���ϰ� �Ǹ� ���� ����
        if (remainTime <= 0)
        {
            remainTime = 0;
            UpdateTimeUI();

            GameOver();
            return;
        }

        // �ð� UI ����
        UpdateTimeUI();
    }

    // �ð��� �ʱ�ȭ
    public void ResetTime()
    {
        remainTime = stageTime;

        // �ʱ�ȭ�� ���� �ð� ����
        isRunning = false;

        isGameOver = false;

        UpdateTimeUI();
    }

    // ���� �߻���� �� �ð� ����
    public void StartTimer()
    {
        if (isGameOver)
        {
            return;
        }

        isRunning = true;
    }

    // ���� �߻�Ǳ� �� / ������ �� �ð� ����
    public void StopTimer()
    {
        isRunning = false;
    }

    // ���� �������� ��
    public void BallFell()
    {
        if (isGameOver)
        {
            return;
        }

        // �ð� 20�� ����
        remainTime -= fallTimePenalty;

        // �ð��� 0 �������� Ȯ��
        if (remainTime <= 0)
        {
            remainTime = 0;
            UpdateTimeUI();

            GameOver();
            return;
        }

        // ���� �ٽ� �߻�Ǳ� ���̹Ƿ� �ð� ����
        isRunning = false;

        UpdateTimeUI();
    }

    // �ð� UI ������Ʈ
    void UpdateTimeUI()
    {
        if (timeText == null)
        {
            return;
        }

        timeText.text = "제한시간 : " + Mathf.Ceil(remainTime);
    }

    /// ���ӿ��� ������ �̵����� ���濹��
    // ���� ����
    void GameOver()
    {
        isGameOver = true;
        isRunning = false;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}