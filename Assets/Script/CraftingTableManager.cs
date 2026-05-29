using System;
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
    public RawImage rarityRaw;
    public RawImage iconRaw;
    public RawImage typeRaw;
}
public class CraftingTableManager : MonoBehaviour
{
    public CraftingTableDB db;
    public GameObject rootObject;
    public CardSlotUI first;
    public CardSlotUI second;
    public CardSlotUI third;

    public Texture rarityNormal;
    public Texture rarityRare;
    public Texture rarityHero;
    public Texture rarityLegendary;

    public Texture type1;
    public Texture type2;
    public Texture type3;

    private void UpdateSlotUI(CardSlotUI slot, string statID, CraftingTableStat statData)
    {
        int currentLevel = db.GetCurrentLevel(statID);

        slot.nameText.text = statData.name;
        int displayIndex = Mathf.Min(currentLevel, statData.displayValues.Length - 1);
        string displayValue = statData.displayValues[displayIndex] ?? "0";
        slot.loreText.text = string.Format(statData.lore, displayValue);

        if (statData.Default == 0) slot.rarityRaw.texture = rarityNormal;
        else if (statData.Default == 1) slot.rarityRaw.texture = rarityRare;
        else if (statData.Default == 2) slot.rarityRaw.texture = rarityHero;
        else slot.rarityRaw.texture = rarityLegendary;

        /*
        switch (statData.type)
        {
            case 1: slot.typeRaw.texture = type1; break;
            case 2: slot.typeRaw.texture = type2; break;
            case 3: slot.typeRaw.texture = type3; break;
        }
        */

        // 4. 아이콘(Icon)은 리소스 폴더에서 찾거나 수동 세팅 (예시)
        // slot.iconRaw.texture = Resources.Load<Texture>("Icons/" + statData.iconName);

        slot.button.onClick.RemoveAllListeners();
        slot.button.onClick.AddListener(() => OnCardSelected(statID));
        
        rootObject.SetActive(true);
    }

    public void OpenCraftingTableUI()
    {
        List<KeyValuePair<string, CraftingTableStat>> rolledCards = RollThreeCards();

        if (rolledCards.Count >= 1) UpdateSlotUI(first, rolledCards[0].Key, rolledCards[0].Value);
        if (rolledCards.Count >= 2) UpdateSlotUI(second, rolledCards[1].Key, rolledCards[1].Value);
        if (rolledCards.Count >= 3) UpdateSlotUI(third, rolledCards[2].Key, rolledCards[2].Value);
    }

    private void OnCardSelected(string upgradeID)
    {
        db.UpgradeStat(upgradeID);
        rootObject.gameObject.SetActive(false);
    }

    public List<KeyValuePair<string, CraftingTableStat>> RollThreeCards()
    {
        var allCards = db.GetMetadataPool();
        for (int i = allCards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            var temp = allCards[i];
            allCards[i] = allCards[randomIndex];
            allCards[randomIndex] = temp;
        }

        List<KeyValuePair<string, CraftingTableStat>> choices = new List<KeyValuePair<string, CraftingTableStat>>();
        for (int i = 0; i < 3; i++)
        {
            if (i < allCards.Count) choices.Add(allCards[i]);
        }
        return choices;
    }
}