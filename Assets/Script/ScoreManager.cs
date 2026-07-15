using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI 설정")]
    public TMP_Text scoreText;

    [Header("설정")]
    public string scorePrefix = "Score : ";
    public bool isScoring = false; // 점수 자동 증가 활성화 여부

    private int _currentScore = 0;
    private float _timer = 0f; // 1초를 체크하기 위한 내부 타이머

    public int CurrentScore => _currentScore;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    private void Update()
    {
        if (isScoring)
        {
            _timer += Time.deltaTime;

            // 1초마다 10점 추가
            if (_timer >= 1.0f)
            {
                AddScore(10);
                _timer = 0f; // 1초 체크 초기화
            }
        }
    }

    // 좀비 처치 등 외부 점수 추가
    public void AddScore(int amount)
    {
        _currentScore += amount;
        UpdateScoreUI();
    }

    public void StartScoring() => isScoring = true;
    public void StopScoring() => isScoring = false;

    public void ResetScore()
    {
        _currentScore = 0;
        _timer = 0f;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = scorePrefix + _currentScore.ToString("N0");
        }
    }
}