using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    void Start()
    {
        _currentHealth = maxHealth;
        InitHealthBarUI();
    }

    // 💡 [초록색 안 채워지는 버그 해결]: 유니티 UI 컴포넌트 로딩 순서에 영향받지 않게 Update에서도 동기화 처리 보완
    void Update()
    {
        if (hpSlider != null && hpSlider.value != _currentHealth)
        {
            hpSlider.value = _currentHealth;
        }
    }

    private void InitHealthBarUI()
    {
        if (hpSlider == null) return;

        hpSlider.minValue = 0f;
        hpSlider.maxValue = maxHealth;
        hpSlider.value = _currentHealth;

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
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);

        if (hpSlider != null)
        {
            hpSlider.value = _currentHealth;
        }

        if (_currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);

        if (hpSlider != null)
        {
            hpSlider.value = _currentHealth;
        }
    }

    private void HandleDeath()
    {
        if (isZombie)
        {
            Debug.Log($"{gameObject.name} 좀비가 죽었습니다.");
            Destroy(gameObject); 
        }
        else
        {
            Debug.Log("인간(플레이어)이 사망했습니다.");
        }
    }
}