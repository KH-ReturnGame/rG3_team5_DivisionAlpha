using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DoubleStrikeAttack : MonoBehaviour
{
    private PlayerMovement _playerMovement;
    private SpriteRenderer _playerSpriteRenderer;

    [Header("공격 설정")]
    [Tooltip("기본 공격(첫 번째 타격)의 데미지입니다.")]
    public float baseDamage = 20f;
    [Tooltip("첫 번째 타격과 두 번째 타격 사이의 시간 간격(초)입니다.")]
    public float doubleStrikeDelay = 0.15f;
    [Tooltip("공격 쿨다운 시간입니다.")]
    public float attackCooldown = 0.75f;

    [Header("이펙트 설정")]
    [SerializeField] private Sprite attackEffectSprite;
    [SerializeField] private float effectDuration = 0.25f;

    [Header("UI 쿨다운 연동")]
    [SerializeField] private Image attackBorderImage;
    [SerializeField] private Image attackCooldownImage;

    private float _cooldownTimer = 0f;
    private bool _isAttackReady = true;
    private byte _readyAlpha = 153; // 60% 투명도

    private GameObject _runtimeEffectObj;
    private Coroutine _attackCoroutine;

    void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerSpriteRenderer = GetComponent<SpriteRenderer>();

        CreateRuntimeEffect();
    }

    void Update()
    {
        if (UpgradeManager.isGamePaused) return;

        HandleCooldown();

        // M키를 입력했을 때 쿨다운이 끝났다면 연속 공격 실행
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            if (_cooldownTimer <= 0 && _isAttackReady)
            {
                TriggerDoubleStrike();
            }
        }
    }

    private void HandleCooldown()
    {
        if (_cooldownTimer > 0)
        {
            _cooldownTimer -= Time.deltaTime;
            float fillRatio = (attackCooldown - _cooldownTimer) / attackCooldown;

            if (attackBorderImage != null) attackBorderImage.fillAmount = fillRatio;
            if (attackCooldownImage != null)
            {
                Color c = attackCooldownImage.color;
                c.a = 0.25f;
                attackCooldownImage.color = c;
                attackCooldownImage.fillAmount = 1f - fillRatio;
            }
        }
        else if (!_isAttackReady && Keyboard.current != null && !Keyboard.current.jKey.isPressed)
        {
            _isAttackReady = true;
            if (attackBorderImage != null) attackBorderImage.fillAmount = 1f;
            if (attackCooldownImage != null)
            {
                Color c = attackCooldownImage.color;
                c.a = (float)_readyAlpha / 255f;
                attackCooldownImage.color = c;
                attackCooldownImage.fillAmount = 1f;
            }
        }
    }

    private void TriggerDoubleStrike()
    {
        _isAttackReady = false;
        _cooldownTimer = attackCooldown;

        if (attackBorderImage != null) attackBorderImage.fillAmount = 0f;

        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        _attackCoroutine = StartCoroutine(DoubleStrikeRoutine());
    }

    private IEnumerator DoubleStrikeRoutine()
    {
        // 1차 공격 (100% 데미지)
        ExecuteAreaAttack(baseDamage);
        StartCoroutine(PlayVisualEffect());

        // 타격 간 대기 시간
        yield return new WaitForSeconds(doubleStrikeDelay);

        // 2차 공격 (60% 데미지)
        ExecuteAreaAttack(baseDamage * 0.6f);
        StartCoroutine(PlayVisualEffect());
    }

    private void ExecuteAreaAttack(float damage)
    {
        if (_playerMovement == null) return;

        Vector3 myPos = transform.position;
        Vector3 lookDir = _playerMovement.GetLookDirection();

        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;

        float attackLength = 2.0f; 
        float attackWidth = 1.0f;  

        // 플레이어가 바라보는 방향에 따라 공격 범위 연산 (체어맨 메커니즘 참고)
        if (lookDir == Vector3.up || lookDir == Vector3.down)
        {
            if (lookDir == Vector3.up)
            {
                minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth;
                minY = myPos.y; maxY = myPos.y + attackLength;
            }
            else
            {
                minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth;
                minY = myPos.y - attackLength; maxY = myPos.y;
            }
        }
        else if (lookDir == Vector3.right || lookDir == Vector3.left)
        {
            if (lookDir == Vector3.right)
            {
                minX = myPos.x; maxX = myPos.x + attackLength;
                minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth;
            }
            else
            {
                minX = myPos.x - attackLength; maxX = myPos.x;
                minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth;
            }
        }

        // 범위 내 적(좀비) 타격 처리
        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                zombie.OnGetHitByPlayer(damage, false);
            }
        }
    }

    // 체어맨 이펙트 3단계 회전 루틴 참고
    private IEnumerator PlayVisualEffect()
    {
        if (_runtimeEffectObj == null) yield break;

        _runtimeEffectObj.SetActive(false); // 이전 잔상이 켜져있다면 초기화

        Vector3 lookDir = _playerMovement != null ? _playerMovement.GetLookDirection() : Vector3.down;
        float baseAngle = 0f;
        if (lookDir == Vector3.right) baseAngle = 0f;
        else if (lookDir == Vector3.up) baseAngle = 90f;
        else if (lookDir == Vector3.left) baseAngle = 180f;
        else if (lookDir == Vector3.down) baseAngle = 270f;

        _runtimeEffectObj.SetActive(true);

        float phaseTime = effectDuration / 3f;
        SetEffectTransform(baseAngle - 50f); yield return new WaitForSeconds(phaseTime);
        SetEffectTransform(baseAngle); yield return new WaitForSeconds(phaseTime);
        SetEffectTransform(baseAngle + 50f); yield return new WaitForSeconds(phaseTime);

        _runtimeEffectObj.SetActive(false);
    }

    private void SetEffectTransform(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * 1.0f;
        _runtimeEffectObj.transform.localPosition = offset;
        _runtimeEffectObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CreateRuntimeEffect()
    {
        if (attackEffectSprite == null) return;

        string sortingLayer = _playerSpriteRenderer != null ? _playerSpriteRenderer.sortingLayerName : "Default";
        int sortingOrder = _playerSpriteRenderer != null ? _playerSpriteRenderer.sortingOrder : 0;

        _runtimeEffectObj = new GameObject("RuntimeDoubleStrikeEffect", typeof(SpriteRenderer));
        _runtimeEffectObj.transform.SetParent(transform, false);

        SpriteRenderer sr = _runtimeEffectObj.GetComponent<SpriteRenderer>();
        sr.sprite = attackEffectSprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder + 5;

        _runtimeEffectObj.SetActive(false);
    }
}