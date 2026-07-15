using UnityEngine;
using TMPro; // TextMeshPro를 사용하므로 꼭 포함해야 합니다.

public class GameTimerManager : MonoBehaviour
{
    public static GameTimerManager instance;

    [Header("UI 연결 (Inspector에서 드래그)")]
    public TextMeshProUGUI timerText; 

    [Header("상태값")]
    public float currentTime = 0f;
    public bool isTimerRunning = false;
    private bool isBossMode = false;

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;

            // 일반 모드에서 5분(300초) 지나면 정지
            if (!isBossMode && currentTime >= 300f)
            {
                currentTime = 300f;
                StopTimer();
            }

            UpdateUI();
        }
    }

    public void StartTimer()
    {
        currentTime = 0f;
        isTimerRunning = true;
        isBossMode = false;
    }

    public void StartBossTimer()
    {
        currentTime = 0f;
        isTimerRunning = true;
        isBossMode = true; // 5분 제한 해제
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    void UpdateUI()
    {
        if (timerText != null)
        {
            // 분:초 형식으로 텍스트 업데이트
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}