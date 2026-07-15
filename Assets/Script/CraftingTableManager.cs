using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class CardSlotUI
{
    public Button button;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI loreText;
    public TextMeshProUGUI typeText;
    public RawImage iconRaw;
}

public class CraftingTableManager : MonoBehaviour
{
    public CraftingTableDB db;
    public GameObject rootObject;
    public CardSlotUI first;
    public CardSlotUI second;
    public CardSlotUI third;

    private int pendingOpenCount = 0;

    private static readonly Dictionary<int, string> RarityColorHex = new Dictionary<int, string>
    {
        { 0, "#53585C" },
        { 1, "#285E9D" },
        { 2, "#7D2471" },
        { 3, "#F0AE44" },
    };

    private void Awake()
    {
        // 최상위 UI 루트 오브젝트를 시작할 때 꺼둡니다.
        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 오브젝트가 활성화될 때 코루틴이 실행되도록 OnEnable로 변경 (안전성 강화)
        Debug.Log("[CraftingTableManager] OnEnable - 타이머 코루틴을 시작합니다.");
        StopAllCoroutines();
        StartCoroutine(CoCraftingTableTimer());
    }

    private IEnumerator CoCraftingTableTimer()
    {
        while (true)
        {
            Debug.Log("[CraftingTableManager] 30초 대기 시작...");
            yield return new WaitForSeconds(30f); //[MGL] 여기 30f 바꾸면 타이머

            Debug.Log("[CraftingTableManager] 30초 경과! UI 오픈 카운트 n회 충전합니다.");
            pendingOpenCount += 2; //[MGL] 2 바꾸면 횟수
            
            // 현재 UI가 켜져 있지 않은 상태라면 바로 오픈
            if (rootObject != null && !rootObject.activeSelf)
            {
                OpenCraftingTableUI();
            }
            else
            {
                Debug.Log("[CraftingTableManager] 이미 UI가 열려있어 닫힌 후 연속 실행 대기합니다.");
            }
        }
    }

    private void UpdateSlotUI(CardSlotUI slot, string statID, CraftingTableStat statData)
    {
        int currentLevel = db.GetCurrentLevel(statID);

        slot.nameText.text = statData.name;
        int displayIndex = Mathf.Min(currentLevel, statData.displayValues.Length - 1);
        string displayValue = statData.displayValues[displayIndex] ?? "0";
        slot.loreText.text = string.Format(statData.lore, displayValue);

        Texture iconTexture = Resources.Load<Texture>("image/ct/icon/" + statID);
        if (iconTexture != null)
        {
            slot.iconRaw.texture = iconTexture;
        }
        else
        {
            Debug.LogWarning($"[CraftingTableManager] 아이콘을 찾을 수 없습니다: image/ct/icon/{statID}");
        }

        if (RarityColorHex.TryGetValue(currentLevel, out string hex)
            && ColorUtility.TryParseHtmlString(hex, out Color rarityColor))
        {
            slot.iconRaw.color = rarityColor;
        }
        else
        {
            Debug.LogWarning($"[CraftingTableManager] 알 수 없는 스택 레벨 값입니다: {currentLevel}");
        }

        switch (statData.type)
        {
            case 1: slot.typeText.text = "체어맨 스킬"; break;
            case 2: slot.typeText.text = "아처 스킬"; break;
        }

        slot.button.onClick.RemoveAllListeners();
        slot.button.onClick.AddListener(() => OnCardSelected(statID));
    }

    public void OpenCraftingTableUI()
    {
        if (db == null)
        {
            Debug.LogError("[CraftingTableManager] db(CraftingTableDB)가 할당되지 않았습니다!");
            return;
        }

        List<KeyValuePair<string, CraftingTableStat>> rolledCards = RollThreeCards(onlyLegendary: false);

        if (rolledCards.Count == 0)
        {
            Debug.LogWarning("[CraftingTableManager] 롤링할 수 있는 카드가 없습니다.");
            return;
        }

        // 슬롯 오브젝트 자체 활성화/비활성화 처리
        if (first?.button != null) first.button.gameObject.SetActive(rolledCards.Count >= 1);
        if (second?.button != null) second.button.gameObject.SetActive(rolledCards.Count >= 2);
        if (third?.button != null) third.button.gameObject.SetActive(rolledCards.Count >= 3);

        if (rolledCards.Count >= 1) UpdateSlotUI(first, rolledCards[0].Key, rolledCards[0].Value);
        if (rolledCards.Count >= 2) UpdateSlotUI(second, rolledCards[1].Key, rolledCards[1].Value);
        if (rolledCards.Count >= 3) UpdateSlotUI(third, rolledCards[2].Key, rolledCards[2].Value);

        if (rootObject != null)
        {
            rootObject.SetActive(true);
            Debug.Log("[CraftingTableManager] UI 오픈 완료!");
        }
        Time.timeScale = 0;
    }

    private void OnCardSelected(string upgradeID)
    {
        Debug.Log($"[CraftingTableManager] 카드 선택됨: {upgradeID}");
        db.UpgradeStat(upgradeID);
        
        // 남은 기회를 차감합니다.
        pendingOpenCount--;
        Debug.Log($"[CraftingTableManager] 남은 선택 횟수: {pendingOpenCount}");

        if (pendingOpenCount > 0)
        {
            // 중요: 아직 선택 기회가 남았다면 UI를 끄지 않고, 
            // 새로운 카드를 뽑아서 화면 내용만 즉시 갱신합니다!
            OpenCraftingTableUI();
        }
        else
        {
            // 모든 기회를 소진했을 때만 UI를 완전히 비활성화합니다.
            if (rootObject != null)
            {
                rootObject.SetActive(false);
                Debug.Log("[CraftingTableManager] 모든 카드 선택 완료. UI를 닫습니다.");
            }
        }
        Time.timeScale = 1;
    }

    public List<KeyValuePair<string, CraftingTableStat>> RollThreeCards(bool onlyLegendary = false)
    {
        var allCards = db.GetMetadataPool();
        List<KeyValuePair<string, CraftingTableStat>> filteredCards = new List<KeyValuePair<string, CraftingTableStat>>();

        foreach (var card in allCards)
        {
            if (onlyLegendary)
            {
                if (card.Value.Default == 3) filteredCards.Add(card);
            }
            else
            {
                if (card.Value.Default < 3) filteredCards.Add(card);
            }
        }

        for (int i = filteredCards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            var temp = filteredCards[i];
            filteredCards[i] = filteredCards[randomIndex];
            filteredCards[randomIndex] = temp;
        }

        List<KeyValuePair<string, CraftingTableStat>> choices = new List<KeyValuePair<string, CraftingTableStat>>();
        int maxCount = Mathf.Min(3, filteredCards.Count);
        for (int i = 0; i < maxCount; i++)
        {
            choices.Add(filteredCards[i]);
        }

        return choices;
    }
}