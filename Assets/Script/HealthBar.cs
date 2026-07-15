using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("캐릭터 종류 설정")]
    public bool isZombie = false;

    [Header("체력 설정")]
    public float maxHealth = 50f; // 인간은 50, 좀비는 각자 설정
    private float _currentHealth;

    [Header("UI 컴포넌트 연결")]
    public Slider hpSlider;
    public Image backgroundImage;
    public Image fillImage;
    
    [Header("선택 사항 (필요한 경우에만 연결)")]
    [Tooltip("체력 수치 텍스트를 표시하고 싶은 체력바에만 이 칸을 연결하세요.")]
    public TMP_Text hpText; 

    void Start()
    {
        _currentHealth = maxHealth;
        InitHealthBarUI();
    }

    private void InitHealthBarUI()
    {
        if (hpSlider == null) return;

        hpSlider.minValue = 0f;
        hpSlider.maxValue = maxHealth;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }

        if (fillImage != null)
        {
            if (isZombie)
            {
                fillImage.color = new Color32(235, 40, 40, 255); // 좀비: 빨간색
            }
            else
            {
                fillImage.color = new Color32(40, 215, 70, 255); // 인간: 초록색
            }
        }

        UpdateHealthUI();
    }

    // 슬라이더 바와 체력 텍스트 수치를 실시간 동기화
    public void UpdateHealthUI()
    {
        _currentHealth = Mathf.Max(_currentHealth, 0f);

        if (hpSlider != null)
        {
            hpSlider.value = _currentHealth;
        }

        if (!isZombie && hpText != null)
        {
            int currentInt = Mathf.RoundToInt(_currentHealth);
            int maxInt = Mathf.RoundToInt(maxHealth);
            hpText.text = $"{currentInt} / {maxInt}";
        }
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);

        UpdateHealthUI(); // 피해를 입을 때 실시간 텍스트/바 즉시 동기화

        if (_currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);

        UpdateHealthUI(); // 회복할 때 즉시 동기화
    }

    private void HandleDeath()
    {
        if (isZombie)
        {
            Debug.Log($"[사망] {gameObject.name} 좀비가 죽었습니다.");

            ZombieAI2_0 zombieAI = GetComponent<ZombieAI2_0>();
            if (zombieAI != null && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(zombieAI.defeatScore);
            }

            Destroy(gameObject); 
        }
        else
        {
            Debug.Log("인간(플레이어)이 사망했습니다.");
        }
    }
}