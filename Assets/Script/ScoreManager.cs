using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI 설정")]
    public TMP_Text scoreText;
    public string scorePrefix = "Score : ";
    public bool isScoring = false; 

    private int _currentScore = 0;
    private float _timer = 0f;

    // [중요] 외부에서 접근 가능한 속성으로 선언
    public int FinalScore { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (isScoring)
        {
            _timer += Time.deltaTime;
            if (_timer >= 1.0f) { AddScore(10); _timer = 0f; }
        }
    }

    public void AddScore(int amount) { _currentScore += amount; UpdateScoreUI(); }
    
    public void SaveFinalScore() { FinalScore = _currentScore; StopScoring(); }

    public void StopScoring() => isScoring = false; 

    private void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = scorePrefix + _currentScore.ToString("N0");
    }
}