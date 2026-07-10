using UnityEngine;
using TMPro; // TextMeshPro를 사용할 경우 필수 (기본 Text라면 이 줄을 지우고 UnityEngine.UI를 사용하세요)

public class ScoreManager : MonoBehaviour
{
    // 어디서나 접근할 수 있도록 싱글톤 인스턴스 생성
    public static ScoreManager Instance { get; private set; }

    [Header("UI 설정")]
    public TMP_Text scoreText; // 화면에 띄울 TextMeshPro 컴포넌트 연결

    [Header("점수 텍스트 접두사")]
    public string scorePrefix = "Score : ";

    private int _currentScore = 0; // 누적 점수 내부 변수

    // 외부에서 현재 점수가 몇 점인지 읽을 수 있는 프로퍼티
    public int CurrentScore => _currentScore;

    private void Awake()
    {
        // 싱글톤 예외 처리 및 초기화
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 점수가 유지되길 원한다면 주석을 해제하세요.
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    // 💡 좀비가 죽을 때 호출할 점수 추가 함수
    public void AddScore(int amount)
    {
        _currentScore += amount;
        UpdateScoreUI();
    }

    // 💡 점수를 초기화할 때 사용할 함수 (게임 오버나 재시작 시 활용)
    public void ResetScore()
    {
        _currentScore = 0;
        UpdateScoreUI();
    }

    // 💡 UI 텍스트를 최신 점수로 갱신하는 함수
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = scorePrefix + _currentScore.ToString("N0"); // N0를 넣으면 1,000 단위 쉼표가 생겨서 보기 좋습니다.
        }
    }
}