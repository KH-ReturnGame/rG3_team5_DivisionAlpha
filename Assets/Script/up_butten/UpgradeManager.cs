using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public PlayerMovement player;
    public GameObject upgradePanel;
    private Dictionary<int, int> upgradeCounts = new Dictionary<int, int>();
    public bool[] isUnlocked = new bool[20];

    // 게임 로직을 일시정지시키기 위한 변수 (필요 시 PlayerMovement에서 참조)
    // 다른 스크립트에서 UpgradeManager.isGamePaused로 즉시 접근 가능
    public static bool isGamePaused = false;

    void Start()
    {
        for (int i = 1; i <= 16; i++) upgradeCounts[i] = 0;
        StartCoroutine(UpgradeTimerRoutine());
    }

    IEnumerator UpgradeTimerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f); // 일반 시간 흐름 사용
            OpenUpgradeSelection();
        }
    }

    public void OpenUpgradeSelection()
    {
        isGamePaused = true;
        // Time.timeScale = 0f; // 이 코드를 제거합니다.
        
        upgradePanel.SetActive(true);
        
        // 추가 권장사항: 게임 내 캐릭터 움직임이나 적의 이동을 멈추려면
        // PlayerMovement나 EnemyController에서 isGamePaused를 체크하게 만드세요.
        if (player != null) player.enabled = false; 
    }

    public void OnSelectUpgrade(int id)
    {
        upgradeCounts[id]++;
        ApplyUpgrade(id);
        
        isGamePaused = false;
        // Time.timeScale = 1f; // 이 코드도 제거합니다.
        
        upgradePanel.SetActive(false);
        if (player != null) player.enabled = true;
    }

    void ApplyUpgrade(int id)
    {
        isUnlocked[id] = true;
        isGamePaused = false; // 선택 시 정지 해제
        upgradePanel.SetActive(false);

        switch (id)
        {
            case 1: player.maxHealth += 25; player.Heal(25); break;
            case 2: player.lifestealAmount += 2; break;
            case 3: player.timeToMove *= 0.9f; break;
            case 4: player.critChance += 0.1f; break;
            case 5: player.chairSkill1Dmg += 5; break;
            case 6: player.chairSkill2Dmg += 5; break;
            case 7: player.archerSkill1Dmg += 5; break;
            case 8: player.archerSkill2Dmg += 5; break;
            case 9: player.swingCooldown = Mathf.Max(0.5f, player.swingCooldown - 0.2f); break;
            case 10: player.counterCooldown = Mathf.Max(0.5f, player.counterCooldown - 0.4f); break;
            case 11: player.arrowCooldown = Mathf.Max(0.5f, player.arrowCooldown - 0.2f); break;
            case 12: player.backstepCooldown = Mathf.Max(0.5f, player.backstepCooldown - 0.4f); break;
            case 13: if (upgradeCounts[13] >= 2) player.counterDamageMultiplier = 4f; break;
            case 14: if (upgradeCounts[14] >= 2) player.canRevive = true; break;
            case 15: if (upgradeCounts[15] >= 2) player.evasionChance = 0.5f; break;
            case 16: if (upgradeCounts[16] >= 2) StartCoroutine(InvincibleRoutine()); break;
        }
    }

    IEnumerator InvincibleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(20f);
            player.IsInvincible = true;
            yield return new WaitForSeconds(5f);
            player.IsInvincible = false;
        }
    }
}