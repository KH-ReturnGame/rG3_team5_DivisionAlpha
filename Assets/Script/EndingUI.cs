using UnityEngine;
using TMPro;

public class EndingUI : MonoBehaviour
{
    public TMP_Text scoreText;

    void Start()
    {
        if (ScoreManager.Instance != null)
        {
            scoreText.text = "Score: " + ScoreManager.Instance.FinalScore.ToString("N0");
        }
    }
}