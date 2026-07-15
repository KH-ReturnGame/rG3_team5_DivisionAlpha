using System;

[Serializable]
public class PlayerData
{
    // ==========================================
    // 체어맨 (type = 1) 능력치 레벨 (0 ~ 3)
    // ==========================================

    // --- 일반 등급 (Default = 0) ---
    public float healthTraining { get; set; } = 0f;
    public float chairFrameRainforce { get; set; } = 0f;
    public float lightChairFrame { get; set; } = 0f;
    public float sharpnessChairFrame { get; set; } = 0f;
    public float lightHandel { get; set; } = 0f;
    public float tacticalRetreat { get; set; } = 0f;
    public float breakBlock { get; set; } = 0f;

    // --- 고급 등급 (Default = 1) ---
    public float heavySwing { get; set; } = 1f;
    public float fastReady { get; set; } = 1f;
    public float cautiousApproach { get; set; } = 1f;
    public float feverTime { get; set; } = 1f;
    public float chairGuard { get; set; } = 1f;
    public float goldenChance { get; set; } = 1f;

    // --- 영웅 등급 (Default = 2) ---
    public float chainRush { get; set; } = 2f;
    public float plannedMistakes { get; set; } = 2f;
    public float breakthrough { get; set; } = 2f;
    public float backAttackChair { get; set; } = 2f;
    public float giantAssassin { get; set; } = 2f;

    // --- 전설 등급 (Default = 3) ---
    public float doubleAttack { get; set; } = 3f;
    public float tomorrowChairKing { get; set; } = 3f;
    public float chairComicManual { get; set; } = 3f;
    public float ultimateChairTechnique { get; set; } = 3f;


    // ==========================================
    // 아처 (type = 2) 능력치 레벨 (0 ~ 3)
    // ==========================================

    // --- 일반 등급 (Default = 0) ---
    public float archerHealthTraining { get; set; } = 0f;
    public float bowStringUpgrade { get; set; } = 0f;
    public float arrowheadUpgrade { get; set; } = 0f;
    public float lightArrow { get; set; } = 0f;
    public float thornArrowhead { get; set; } = 0f;
    public float combatRepositioning { get; set; } = 0f;
    public float lightRunningShoes { get; set; } = 0f;

    // --- 고급 등급 (Default = 1) ---
    public float sharpArrowhead { get; set; } = 1f;
    public float springShoes { get; set; } = 1f;
    public float stableAim { get; set; } = 1f;
    public float focusedAttackMastery { get; set; } = 1f;
    public float weakpointCapture { get; set; } = 1f;
    public float crisisResponse { get; set; } = 1f;

    // --- 영웅 등급 (Default = 2) ---
    public float powerShot { get; set; } = 2f;
    public float multiShot { get; set; } = 2f;
    public float perfectGold { get; set; } = 2f;
    public float perfectDodge { get; set; } = 2f;
    public float preemptiveStrike { get; set; } = 2f;

    // --- 전설 등급 (Default = 3) ---
    public float snipingBow { get; set; } = 3f;
    public float rapidFire { get; set; } = 3f;
    public float boldCharge { get; set; } = 3f;
    public float triArrow { get; set; } = 3f;
}