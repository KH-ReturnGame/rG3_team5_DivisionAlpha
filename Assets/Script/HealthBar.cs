using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("캐릭터 종류 설정")]
    public bool isZombie = false;

    [Header("체력 설정")]
    public float maxHealth = 50f;
    private float _currentHealth;

    [Header("UI 컴포넌트 연결")]
    public Slider hpSlider;
    public Image backgroundImage;
    public Image fillImage;
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
        
        if (backgroundImage != null) backgroundImage.color = Color.white;
        
        if (fillImage != null)
        {
            fillImage.color = isZombie ? new Color32(235, 40, 40, 255) : new Color32(40, 215, 70, 255);
        }
        UpdateHealthUI();
    }

    // 좀비 전용: 내부 체력 값으로 UI 갱신
    public void UpdateHealthUI()
    {
        _currentHealth = Mathf.Max(_currentHealth, 0f);
        if (hpSlider != null) hpSlider.value = _currentHealth;

        if (!isZombie && hpText != null)
        {
            hpText.text = $"{Mathf.RoundToInt(_currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }
    }

    // 좀비 전용: 데미지 처리
    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);
        UpdateHealthUI();
        if (_currentHealth <= 0f) HandleDeath();
    }

    // 플레이어(PlayerMovement) 전용: 외부에서 값을 받아 UI만 갱신
    public void UpdateUI(float current, float max)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = current;
        }
        if (fillImage != null)
        {
            fillImage.color = isZombie ? new Color32(235, 40, 40, 255) : new Color32(40, 215, 70, 255);
        }
        if (!isZombie && hpText != null)
        {
            hpText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
        }
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