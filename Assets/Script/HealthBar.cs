using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 제어용

public class HealthBar : MonoBehaviour
{
    [Header("캐릭터 종류 설정")]
    public bool isZombie = false;

    [Header("체력 설정")]
    public float maxHealth = 100f; 
    private float _currentHealth;

    [Header("UI 컴포넌트 연결")]
    public Slider hpSlider;
    public Image backgroundImage;
    public Image fillImage;
    
    [Header("선택 사항 (필요한 경우에만 연결)")]
    [Tooltip("체력 수치 텍스트를 표시하고 싶은 체력바에만 이 칸을 연결하세요. 원치 않으면 비워두시면 됩니다.")]
    public TMP_Text hpText; // 💡여기에 연결이 있을 때만 텍스트가 작동합니다.

    void Start()
    {
        _currentHealth = maxHealth;
        InitHealthBarUI();
    }

    // [초록색 안 채워지는 버그 해결]: 유니티 UI 컴포넌트 로딩 순서에 영향받지 않게 Update에서도 동기화 처리 보완
    void Update()
    {
        if (hpSlider != null && hpSlider.value != _currentHealth)
        {
            UpdateHealthUI();
        }
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

    // 슬라이더 바와 체력 텍스트 수치를 동기화하는 함수
    private void UpdateHealthUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = _currentHealth;
        }

        // 💡 핵심 수정 부분: 플레이어(인간)이면서 'hpText 변수에 TMP 오브젝트가 연결되어 있을 때만' 텍스트를 갱신합니다.
        // 중앙 체력바처럼 이 칸을 비워두면(Null) 이 아래 코드는 실행되지 않으므로 텍스트가 뜨지 않습니다.
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

        UpdateHealthUI();

        if (_currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);

        UpdateHealthUI();
    }

    private void HandleDeath()
    {
        if (isZombie)
        {
            Debug.Log($"{gameObject.name} 좀비가 죽었습니다.");

            // 좀비가 죽을 때 해당 좀비가 가진 스코어를 ScoreManager에 더해줍니다.
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