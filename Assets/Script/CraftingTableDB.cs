using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftingTableStat
{
    public string name;
    public string lore;
    public int Default;
    public float[] values;
    public string[] displayValues;
    public int type;
}

public class CraftingTableDB : MonoBehaviour
{
    public PlayerData PlayerData = new PlayerData();
    private Dictionary<string, int> playerCraftingTable = new Dictionary<string, int>();

    // 스탯이 업그레이드될 때마다 발행되는 이벤트 (구독자: PlayerMovement 등)
    public event Action<string> OnStatUpgraded;

    private Dictionary<string, CraftingTableStat> metadataDict = new Dictionary<string, CraftingTableStat>()
    {
        // ================= 체어맨 (type = 1) =================

        // --- 일반 등급부터 시작 (Default = 0) ---
        {
            "healthTraining", new CraftingTableStat
            {
                name = "체력 단련",
                type = 1,
                lore = "최대 체력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 25f, 50f, 75f, 100f },
                displayValues = new string[] {"25", "50", "75", "100"}
            }
        },
        {
            "chairFrameRainforce", new CraftingTableStat
            {
                name = "체어 프레임 강화",
                type = 1,
                lore = "의자 휘두르기의 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 1.1f, 1.25f, 1.4f, 1.6f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "lightChairFrame", new CraftingTableStat
            {
                name = "가벼운 체어 프레임",
                type = 1,
                lore = "의자 휘두르기의 쿨타임이 {0} 감소합니다.",
                Default = 0,
                values = new float[] { .9f, .75f, .6f, .4f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "sharpnessChairFrame", new CraftingTableStat
            {
                name = "날카로운 체어 프레임",
                type = 1,
                lore = "의자 찌르기의 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 1.1f, 1.25f, 1.4f, 1.6f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "lightHandel", new CraftingTableStat
            {
                name = "가벼운 손잡이",
                type = 1,
                lore = "의자 찌르기의 쿨타임이 {0} 감소합니다.",
                Default = 0,
                values = new float[] { .9f, .8f, .7f, .6f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "tacticalRetreat", new CraftingTableStat
            {
                name = "전략적 후퇴",
                type = 1,
                lore = "좀비에게 의자 찌르기 명중 시 {0}초 간 이동속도가 15% 증가합니다.",
                Default = 0,
                values = new float[] { 2f, 3f, 4f, 6f },
                displayValues = new string[] {"2", "3", "4", "6"}
            }
        },
        {
            "breakBlock", new CraftingTableStat
            {
                name = "방해물 격파",
                type = 1,
                lore = "오브젝트에 가하는 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 2f, 3f, 6f, 11f },
                displayValues = new string[] {"100%", "200%", "500%", "1000%"}
            }
        },

        // --- 고급 등급부터 시작 (Default = 1) ---
        {
            "heavySwing", new CraftingTableStat
            {
                name = "묵직한 휘두르기",
                type = 1,
                lore = "의자 휘두르기의 크리티컬 데미지가 {0} 증가합니다.",
                Default = 1,
                values = new float[] { 0f, 1.5f, 1.75f, 2f },
                displayValues = new string[] {"", "50%", "75%", "100%"}
            }
        },
        {
            "fastReady", new CraftingTableStat
            {
                name = "빠른 준비",
                type = 1,
                lore = "의자 찌르기로 카운터에 성공했을 경우 의자 찌르기의 쿨타임이 {0} 증가합니다.(최대 중첩 5회)",
                Default = 1,
                values = new float[] { 0f, 0.5f, .25f, 0f },
                displayValues = new string[] {"", "50%", "75%", "100%"}
            }
        },
        {
            "cautiousApproach", new CraftingTableStat
            {
                name = "신중한 접근",
                type = 1,
                lore = "1칸 이동할 때마다 크리티컬 확률이 {0} 증가합니다. 좀비를 공격할 경우 초기화됩니다.",
                Default = 1,
                values = new float[] { 0f, 0.05f, 0.1f, 0.15f },
                displayValues = new string[] {"", "5%", "10%", "15%"}
            }
        },
        {
            "feverTime", new CraftingTableStat
            {
                name = "피버 타임",
                type = 1,
                lore = "좀비 처치 시 15초간 크리티컬 확률이 {0} 증가합니다. (중첩 불가)",
                Default = 1,
                values = new float[] { 0f, 0.3f, 0.6f, 0.9f },
                displayValues = new string[] {"", "30%", "60%", "90%"}
            }
        },
        {
            "chairGuard", new CraftingTableStat
            {
                name = "체어 가드",
                type = 1,
                lore = "좀비에게 피격 시 {0} 확률로 받는 피해가 30% 감소합니다.",
                Default = 1,
                values = new float[] { 0f, 0.15f, 0.3f, 0.5f },
                displayValues = new string[] {"", "15%", "30%", "50%"}
            }
        },
        {
            "goldenChance", new CraftingTableStat
            {
                name = "절호의 찬스",
                type = 1,
                lore = "상태 이상에 걸린 좀비에게 가하는 공격력이 {0} 증가합니다.",
                Default = 1,
                values = new float[] { 0f, 1.3f, 1.5f, 1.7f },
                displayValues = new string[] {"", "30%", "50%", "70%"}
            }
        },

        // --- 영웅 등급부터 시작 (Default = 2) ---
        {
            "chainRush", new CraftingTableStat
            {
                name = "체인 러쉬",
                type = 1,
                lore = "좀비에게 의자 휘두르기 명중 시 2초 간 크리티컬 테미지가 {0} 증가합니다.(최대 중첩 5회)",
                Default = 2,
                values = new float[] { 0f, 0f, 1.25f, 1.5f },
                displayValues = new string[] {"", "", "25%", "50%"}
            }
        },
        {
            "plannedMistakes", new CraftingTableStat
            {
                name = "계획된 실수",
                type = 1,
                lore = "의자 찌르기가 쿨타임일 경우 의자 휘두르기의 공격력이 {0} 증가합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 1.5f, 2f },
                displayValues = new string[] {"", "", "50%", "100%"}
            }
        },
        {
            "breakthrough", new CraftingTableStat
            {
                name = "빈틈 돌파",
                type = 1,
                lore = "의자 찌르기로 카운터에 성공했을 경우 {0}초 무적 상태가 됩니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 1f, 2f },
                displayValues = new string[] {"", "", "1", "2"}
            }
        },
        {
            "backAttackChair", new CraftingTableStat
            {
                name = "백어택 체어",
                type = 1,
                lore = "좀비의 후방을 공격 시 {0} 확률로 크리티컬이 발생합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 0.5f, 1f },
                displayValues = new string[] {"", "", "50%", "100%"}
            }
        },
        {
            "giantAssassin", new CraftingTableStat
            {
                name = "자이언트 어쌔신",
                type = 1,
                lore = "보스를 공격할 때 {0}의 추가 데미지가 발생합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 15f, 30f },
                displayValues = new string[] {"", "", "15", "30"}
            }
        },

        // --- 전설 등급부터 시작 (Default = 3, 수치 성장 없이 1회 해금되는 능력) ---
        {
            "doubleAttack", new CraftingTableStat
            {
                name = "더블 어택",
                type = 1,
                lore = "공격을 명중시킬 경우 기본 공격력의 80%인 추가 공격을 1회 더 가합니다.",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },
        {
            "tomorrowChairKing", new CraftingTableStat
            {
                name = "내일은 체어왕",
                type = 1,
                lore = "의자 휘두르기가 푸시 능력을 가진 차지형 공격으로 강화됩니다.",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },
        {
            "chairComicManual", new CraftingTableStat
            {
                name = "만화로 배우는 체어 교본",
                type = 1,
                lore = "의자 휘두르기가 다양한 스타일을 가진 콤보형 공격으로 강화됩니다.",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },
        {
            "ultimateChairTechnique", new CraftingTableStat
            {
                name = "궁극의 체어 비기",
                type = 1,
                lore = "의자 찌르기가 쿨타임이 길지만 공격력이 매우 높은 체어 스피어로 강화됩니다.",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },

        // ================= 아처 (type = 2) =================

        // --- 일반 등급부터 시작 (Default = 0) ---
        {
            "archerHealthTraining", new CraftingTableStat
            {
                name = "체력 단련",
                type = 2,
                lore = "최대 체력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 25f, 50f, 75f, 100f },
                displayValues = new string[] {"25", "50", "75", "100"}
            }
        },
        {
            "bowStringUpgrade", new CraftingTableStat
            {
                name = "활시위 강화",
                type = 2,
                lore = "화살의 사거리가 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 1f, 2f, 3f, 5f },
                displayValues = new string[] {"1칸", "2칸", "3칸", "5칸"}
            }
        },
        {
            "arrowheadUpgrade", new CraftingTableStat
            {
                name = "화살촉 강화",
                type = 2,
                lore = "화살의 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 1.1f, 1.25f, 1.4f, 1.6f },
                displayValues = new string[] {"10%", "25%", "40%", "60%"}
            }
        },
        {
            "lightArrow", new CraftingTableStat
            {
                name = "가벼운 화살",
                type = 2,
                lore = "화살 발사의 쿨타임이 {0} 감소합니다.",
                Default = 0,
                values = new float[] { .9f, .8f, .7f, .6f },
                displayValues = new string[] {"10%", "20%", "30%", "40%"}
            }
        },
        {
            "thornArrowhead", new CraftingTableStat
            {
                name = "가시 화살촉",
                type = 2,
                lore = "화살이 {0}의 추가 데미지를 입힙니다.",
                Default = 0,
                values = new float[] { 4f, 8f, 12f, 16f },
                displayValues = new string[] {"4", "8", "12", "16"}
            }
        },
        {
            "combatRepositioning", new CraftingTableStat
            {
                name = "전투 재정비",
                type = 2,
                lore = "백스텝 사용 시 다음 화살의 공격력이 {0} 증가합니다.",
                Default = 0,
                values = new float[] { 1.2f, 1.4f, 1.6f, 1.8f },
                displayValues = new string[] {"20%", "40%", "60%", "80%"}
            }
        },
        {
            "lightRunningShoes", new CraftingTableStat
            {
                name = "가벼운 러닝화",
                type = 2,
                lore = "백스텝의 쿨타임이 {0} 감소합니다.",
                Default = 0,
                values = new float[] { .9f, .8f, .7f, .6f },
                displayValues = new string[] {"10%", "20%", "30%", "40%"}
            }
        },

        // --- 고급 등급부터 시작 (Default = 1) ---
        {
            "sharpArrowhead", new CraftingTableStat
            {
                name = "날카로운 화살촉",
                type = 2,
                lore = "화살의 크리티컬 확률이 {0} 증가합니다.",
                Default = 1,
                values = new float[] { 0f, 0.25f, 0.5f, 0.75f },
                displayValues = new string[] {"", "25%", "50%", "75%"}
            }
        },
        {
            "springShoes", new CraftingTableStat
            {
                name = "스프링 슈츠",
                type = 2,
                lore = "백스텝의 이동거리가 {0} 증가합니다.",
                Default = 1,
                values = new float[] { 0f, 1f, 2f, 3f },
                displayValues = new string[] {"", "1칸", "2칸", "3칸"}
            }
        },
        {
            "stableAim", new CraftingTableStat
            {
                name = "안정된 조준",
                type = 2,
                lore = "3칸 이내에 좀비가 없다면 화살의 공격력이 {0} 증가합니다.",
                Default = 1,
                values = new float[] { 0f, 1.3f, 1.5f, 1.7f },
                displayValues = new string[] {"", "30%", "50%", "70%"}
            }
        },
        {
            "focusedAttackMastery", new CraftingTableStat
            {
                name = "집중 공격 숙련",
                type = 2,
                lore = "화살 명중 시 3초간 공격력이 {0} 증가합니다. (최대 중첩 10회)",
                Default = 1,
                values = new float[] { 0f, 0.04f, 0.08f, 0.12f },
                displayValues = new string[] {"", "4%", "8%", "12%"}
            }
        },
        {
            "weakpointCapture", new CraftingTableStat
            {
                name = "약점 포착",
                type = 2,
                lore = "카운터에 성공했을 경우 다음 화살의 크리티컬 확률이 {0} 증가합니다.",
                Default = 1,
                values = new float[] { 0f, 0.5f, 0.75f, 1f },
                displayValues = new string[] {"", "50%", "75%", "100%"}
            }
        },
        {
            "crisisResponse", new CraftingTableStat
            {
                name = "위기 대처 능력",
                type = 2,
                lore = "현재 체력이 {0} 이하 일 때 이동속도가 15% 증가합니다.",
                Default = 1,
                values = new float[] { 0f, 0.25f, 0.5f, 0.75f },
                displayValues = new string[] {"", "25%", "50%", "75%"}
            }
        },

        // --- 영웅 등급부터 시작 (Default = 2) ---
        {
            "powerShot", new CraftingTableStat
            {
                name = "파워샷",
                type = 2,
                lore = "화살 발사의 쿨타임이 1.25배로 증가하지만 화살의 크리티컬 확률이 {0} 증가합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 0.5f, 1f },
                displayValues = new string[] {"", "", "50%", "100%"}
            }
        },
        {
            "multiShot", new CraftingTableStat
            {
                name = "멀티샷",
                type = 2,
                lore = "화살 발사 시 사선으로 20% 공격력의 화살을 {0} 더 발사합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 2f, 4f },
                displayValues = new string[] {"", "", "2발", "4발"}
            }
        },
        {
            "perfectGold", new CraftingTableStat
            {
                name = "퍼펙트 골드",
                type = 2,
                lore = "화살이 날아간 거리 1칸당 화살의 공격력이 {0} 증가합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 0.08f, 0.16f },
                displayValues = new string[] {"", "", "8%", "16%"}
            }
        },
        {
            "perfectDodge", new CraftingTableStat
            {
                name = "완벽한 회피",
                type = 2,
                lore = "백스텝 사용 시 {0} 무적 상태가 됩니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 1f, 2f },
                displayValues = new string[] {"", "", "1초", "2초"}
            }
        },
        {
            "preemptiveStrike", new CraftingTableStat
            {
                name = "선제 타격",
                type = 2,
                lore = "체력이 90% 이상인 적에게 화살의 공격력이 {0} 증가합니다.",
                Default = 2,
                values = new float[] { 0f, 0f, 1.5f, 2f },
                displayValues = new string[] {"", "", "50%", "100%"}
            }
        },

        // --- 전설 등급부터 시작 (Default = 3, 수치 성장 없이 1회 해금되는 능력) ---
        {
            "snipingBow", new CraftingTableStat
            {
                name = "스나이핑 보우",
                type = 2,
                lore = "화살의 속도가 25% 빨라지고, 날아간 거리 1칸당 크리티컬 데미지가 25% 증가합니다.",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },
        {
            "rapidFire", new CraftingTableStat
            {
                name = "래피드 파이어",
                type = 2,
                lore = "화살의 기본 공격력이 50% 감소하지만, 화살 발사가 키다운 형식의 고속 연사로 변경됩니다.",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },
        {
            "boldCharge", new CraftingTableStat
            {
                name = "대담한 돌진",
                type = 2,
                lore = "백스텝의 방향이 전방으로 변경됩니다. 다음 화살 발사의 쿨타임이 초기화되고 공격력이 150% 상승합니다.(카운터)",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },
        {
            "triArrow", new CraftingTableStat
            {
                name = "트라이 애로우",
                type = 2,
                lore = "백스텝이 화살 변경으로 대체됩니다. 마비 화살(카운터)과 독 화살을 사용할 수 있게 됩니다.",
                Default = 3,
                values = new float[] { 0f, 0f, 0f, 1f },
                displayValues = new string[] {"", "", "", "해금"}
            }
        },
    };

    private void Awake()
    {
        foreach (var pair in metadataDict)
        {
            if (!playerCraftingTable.ContainsKey(pair.Key))
            {
                playerCraftingTable.Add(pair.Key, pair.Value.Default);
            }
        }
    }

    public int GetCurrentLevel(string stat)
    {
        if (playerCraftingTable.ContainsKey(stat))
        {
            return playerCraftingTable[stat];
        }
        return 0; 
    }

    // stat 문자열을 받아 PlayerData에 기록된 레벨(인덱스)을 참조하여
    // 해당 인덱스의 values 값을 반환합니다. (예: healthTraining 레벨 0 -> 25f)
    public float GetCurrentValue(string stat)
    {
        if (!metadataDict.ContainsKey(stat)) return 0f;

        CraftingTableStat targetData = metadataDict[stat];
        int level = GetCurrentLevel(stat);

        var propertyInfo = typeof(PlayerData).GetProperty(stat);
        if (propertyInfo != null)
        {
            level = Mathf.RoundToInt((float)propertyInfo.GetValue(PlayerData));
        }

        level = Mathf.Clamp(level, 0, targetData.values.Length - 1);
        return targetData.values[level];
    }

    // 곱연산(공격력 배율, 쿨타임 감소 등)용 헬퍼.
    // 아직 해금되지 않아 값이 0인 구간은 1(변화 없음)로 취급합니다.
    public float GetMultiplier(string stat)
    {
        float value = GetCurrentValue(stat);
        return value <= 0f ? 1f : value;
    }

    public List<KeyValuePair<string, CraftingTableStat>> GetMetadataPool()
    {
        return new List<KeyValuePair<string, CraftingTableStat>>(metadataDict);
    }

    public void UpgradeStat(string stat)
    {
        if (!metadataDict.ContainsKey(stat)) return;

        CraftingTableStat targetData = metadataDict[stat];
        int currentLevel = playerCraftingTable[stat];

        // 최대 레벨(스택) 도달 시 업그레이드 불가 (values의 최대 길이를 넘어갈 수 없음)
        if (currentLevel >= targetData.values.Length - 1) return; 

        // 다음 스택(레벨) 수 계산
        int nextLevel = currentLevel + 1;

        var propertyInfo = typeof(PlayerData).GetProperty(stat);
        if (propertyInfo != null)
        {
            // PlayerData에 실제 float 수치 대신 "레벨 수치(float 캐스팅)"를 대입
            propertyInfo.SetValue(PlayerData, (float)nextLevel);
            
            // 딕셔너리 관리용 스택 레벨도 증가
            playerCraftingTable[stat] = nextLevel;

            Debug.Log($"{targetData.name} 스택 업그레이드: {currentLevel} -> {nextLevel}");

            // 실제 능력치를 사용하는 쪽(PlayerMovement 등)에 갱신 알림
            OnStatUpgraded?.Invoke(stat);
        }
    }
}