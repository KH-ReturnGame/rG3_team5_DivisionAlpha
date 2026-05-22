using System;
using System.Collections.Generic;
using UnityEngine;
public class CraftingTableStat
{
    public string name;
    public string lore;
    public int Default;
    public float[] values;
    public string[] displayValues;
}

public class CraftingTableDB : MonoBehaviour
{
    public List<KeyValuePair<string, CraftingTableStat>> GetMetadataPool()
    {
        return new List<KeyValuePair<string, CraftingTableStat>>(metadataDict);
    }
    public PlayerData PlayerData = new PlayerData();
    private Dictionary<string, int> playerCraftingTable = new Dictionary<string, int>();

    private Dictionary<string, CraftingTableStat> metadataDict = new Dictionary<string, CraftingTableStat>()
    {
        {
            "breakBlock", new CraftingTableStat
            {
                name = "방해물 격파",
                lore = "오브젝트에 가하는 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 2f, 3f, 6f, 11f },
                displayValues = new string[] {"100%", "200%", "500%", "1000%"}
            }
        },
        {
            "lightChairFrame", new CraftingTableStat
            {
                name = "가벼운 체어 프레임",
                lore = "의자 휘두르기의 쿨타임이 {0} 감소합니다.",
                Default = 0,
                values = new float[] { .9f, .75f, .6f, .4f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "chairFrameRainforce", new CraftingTableStat 
            {
                name = "체어 프레임 강화",
                lore = "의자 휘두르기의 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 1.1f, 1.25f, 1.4f, 1.6f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "plannedMistakes", new CraftingTableStat 
            {
                name = "계획된 실수",
                lore = "의자 찌르기가 쿨타임일 경우 의자 휘두르기의 공격력이 {0} 증가합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 1.5f, 2f },
                displayValues = new string[] {"", "", "50%", "100%"}
            }
        },
        {
            "chainRush", new CraftingTableStat 
            {
                name = "체인 러쉬",
                lore = "좀비에게 의자 휘두르기 명중 시 2초 간 크리티컬 테미지가 {0} 증가합니다.(최대 중첩 5회)",
                Default = 2,
                values = new float[] { 0f, 0f, 1.25f, 1.5f },
                displayValues = new string[] {"", "", "25%", "50%"}
            }
        },
        {
            "lightHandel", new CraftingTableStat 
            {
                name = "가벼운 손잡이",
                lore = "의자 찌르기의 쿨타임이 {0} 감소합니다.",
                Default = 0,
                values = new float[] { .9f, .8f, .7f, .6f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "sharpnessChairFrame", new CraftingTableStat 
            {
                name = "날카로운 체어 프레임",
                lore = "의자 찌르기의 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 1.1f, 1.25f, 1.4f, 1.6f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "tacticalRetreat", new CraftingTableStat 
            {
                name = "전략적 후퇴",
                lore = "좀비에게 의자 찌르기 명중 시 {0}초 간 이동속도가 15% 증가합니다.",
                Default = 0,
                values = new float[] { 2f, 3f, 4f, 6f },
                displayValues = new string[] {"2", "3", "4", "6"}
            }
        },
        {
            "fastReady", new CraftingTableStat 
            {
                name = "빠른 준비",
                lore = "의자 찌르기로 카운터에 성공했을 경우 의자 찌르기의 쿨타임이 {0} 증가합니다.(최대 중첩 5회)",
                Default = 1,
                values = new float[] { 0f, 0.5f, .25f, 0f },
                displayValues = new string[] {"", "50%", "75%", "100%"}
            }
        },
        {
            "breakthrough", new CraftingTableStat 
            {
                name = "빈틈 돌파",
                lore = "의자 찌르기로 카운터에 성공했을 경우 {0}초 무적 상태가 됩니다.",
                Default = 1,
                values = new float[] { 0f, 0f, 1f, 2f },
                displayValues = new string[] {"", "", "1", "2"}
            }
        },
    };
    public void UpgradeStat(string stat)
    {
        CraftingTableStat targetData = metadataDict[stat];
        int currentLevel = playerCraftingTable[stat];

        float nextValue = targetData.values[currentLevel];

        var propertyInfo = typeof(PlayerData).GetProperty(stat);
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(PlayerData, nextValue);
            playerCraftingTable[stat]++; // 레벨 증가

            // 텍스트 조립
            string displayPercent = targetData.displayValues[currentLevel];
            string finalLoreText = string.Format(targetData.lore, displayPercent);

            // tmpro
        }
    }
}