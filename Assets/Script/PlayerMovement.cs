using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;
    private float _timeToMove = 0.15f;

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    private SpriteRenderer _spriteRenderer; 

    [Header("1스킬: 휘두르기 (J키 - 3x2 6칸)")]
    [SerializeField] private Image swingBorderImage;
    [SerializeField] private Image swingCooldownImage;   
    [SerializeField] private Sprite swingWeaponEffectSprite;   
    [SerializeField] private float swingDamage = 20f; 
    
    private float swingDuration = 0.25f; 
    private float swingCooldown = 0.75f; 
    private float _swingCooldownTimer = 0f;
    private bool _isSwingReady = true;

    [Header("2스킬: 카운터 (K키 - 1x2 2칸, 마주볼때만)")]
    [SerializeField] private Image counterBorderImage;
    [SerializeField] private Image counterCooldownImage;  
    [SerializeField] private Sprite counterWeaponEffectSprite;  
    [SerializeField] private float counterDamage = 50f; 
    
    private float counterDuration = 0.3f;
    private float counterCooldown = 7.0f; 
    private float _counterCooldownTimer = 0f;
    private bool _isCounterReady = true;

    private Vector3 _lookDirection = Vector3.down; 
    
    private Coroutine _swingEffectCoroutine;
    private Coroutine _counterEffectCoroutine;

    private GameObject _runtimeSwingWeaponObj;
    private GameObject _runtimeCounterWeaponObj;

    private byte _readyAlpha = 100; 
    private byte _activeAlpha = 50;  

    [Header("무적 상태 시스템")]
    private bool _isInvincible = false;
    private float _invincibleDuration = 1.0f; 

    [Header("머리 위 체력바 컴포넌트 강제 연결")]
    [SerializeField] private HealthBar myHealthBar; 

    // 💡 [핵심 수정] 왼쪽 상단 화면 UI 체력바도 머리 위랑 똑같이 HealthBar 컴포넌트로 다이렉트 연결!
    [Header("📺 화면 고정 플레이어 체력 UI 설정")]
    [Tooltip("왼쪽 상단 캔버스에 배치된 체력바의 'HealthBar' 컴포넌트를 드래그앤드롭 하세요.")]
    [SerializeField] private HealthBar playerHudHealthBar;

    [Header("플레이어 벽 충돌 설정")]
    [SerializeField] private LayerMask obstacleLayer;

    public Vector3 CurrentLookDirection => _lookDirection;
    public bool IsInvincible => _isInvincible;

    [Header("투명벽 타일맵 직접 연결")]
    [SerializeField] private UnityEngine.Tilemaps.Tilemap transparentWallTilemap;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (myHealthBar == null) myHealthBar = GetComponent<HealthBar>();
        
        CreateRuntimeWeaponEffects();
        UpdateGridSortingOrder();
        SetupUIProperties();
    }

    private void SetupUIProperties()
    {
        if (swingBorderImage != null)
        {
            swingBorderImage.type = Image.Type.Filled;
            swingBorderImage.fillMethod = Image.FillMethod.Radial360;
            swingBorderImage.fillOrigin = (int)Image.Origin360.Top;
            swingBorderImage.fillClockwise = false; 
            swingBorderImage.fillAmount = 1f; 
        }

        if (counterBorderImage != null)
        {
            counterBorderImage.type = Image.Type.Filled;
            counterBorderImage.fillMethod = Image.FillMethod.Radial360;
            counterBorderImage.fillOrigin = (int)Image.Origin360.Top;
            counterBorderImage.fillClockwise = false; 
            counterBorderImage.fillAmount = 1f; 
        }

        if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha);
        if (counterCooldownImage != null) SetImageAlpha32(counterCooldownImage, _readyAlpha);
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap == null) return false;
        Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
        return transparentWallTilemap.HasTile(cellPosition);
    }

    void Update()
    {
        if (_swingCooldownTimer > 0)
        {
            _swingCooldownTimer -= Time.deltaTime;
            if (swingBorderImage != null) swingBorderImage.fillAmount = _swingCooldownTimer / swingCooldown;
        }
        else if (!_isSwingReady && Keyboard.current != null && !Keyboard.current.jKey.isPressed)
        {
            _isSwingReady = true;
            if (swingBorderImage != null) swingBorderImage.fillAmount = 1f; 
            if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha); 
        }

        if (_counterCooldownTimer > 0)
        {
            _counterCooldownTimer -= Time.deltaTime;
            if (counterBorderImage != null) counterBorderImage.fillAmount = _counterCooldownTimer / counterCooldown;
        }
        else if (!_isCounterReady && Keyboard.current != null && !Keyboard.current.kKey.isPressed)
        {
            _isCounterReady = true;
            if (counterBorderImage != null) counterBorderImage.fillAmount = 1f; 
            if (counterCooldownImage != null) SetImageAlpha32(counterCooldownImage, _readyAlpha);
        }

        if (!_isMoving && Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) { _lookDirection = Vector3.up; ChangePlayerSprite(Vector3.up); if (!IsTileBlocked(transform.position + Vector3.up)) StartCoroutine(MovePlayer(Vector3.up)); }
            else if (Keyboard.current.aKey.isPressed) { _lookDirection = Vector3.left; ChangePlayerSprite(Vector3.left); if (!IsTileBlocked(transform.position + Vector3.left)) StartCoroutine(MovePlayer(Vector3.left)); }
            else if (Keyboard.current.sKey.isPressed) { _lookDirection = Vector3.down; ChangePlayerSprite(Vector3.down); if (!IsTileBlocked(transform.position + Vector3.down)) StartCoroutine(MovePlayer(Vector3.down)); }
            else if (Keyboard.current.dKey.isPressed) { _lookDirection = Vector3.right; ChangePlayerSprite(Vector3.right); if (!IsTileBlocked(transform.position + Vector3.right)) StartCoroutine(MovePlayer(Vector3.right)); }
        }

        if (Keyboard.current != null)
        {
            if (_isSwingReady)
            {
                if (Keyboard.current.jKey.isPressed) SetImageAlpha32(swingCooldownImage, _activeAlpha);
                if (Keyboard.current.jKey.wasReleasedThisFrame) TriggerSwingAttack();
            }

            if (_isCounterReady)
            {
                if (Keyboard.current.kKey.isPressed) SetImageAlpha32(counterCooldownImage, _activeAlpha);
                if (Keyboard.current.kKey.wasReleasedThisFrame) TriggerCounterAttack();
            }
        }
    }

    public void OnGetHitByZombie(float damage)
    {
        if (_isInvincible) return;

        // 💡 1. 머리 위 체력바 깎기
        if (myHealthBar != null)
        {
            myHealthBar.TakeDamage(damage);
        }

        // 💡 2. 왼쪽 상단 캔버스 화면 UI 체력바도 똑같이 깎기!
        if (playerHudHealthBar != null)
        {
            playerHudHealthBar.TakeDamage(damage);
        }

        // 무적 시간 코루틴 돌리기
        if (myHealthBar != null || playerHudHealthBar != null)
        {
            StartCoroutine(InvincibleRoutine());
        }
    }

    private IEnumerator InvincibleRoutine()
    {
        _isInvincible = true;
        float elapsed = 0f;
        while (elapsed < _invincibleDuration)
        {
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = (c.a == 1f) ? 0.4f : 1f; 
                _spriteRenderer.color = c;
            }
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (_spriteRenderer != null)
        {
            Color c = _spriteRenderer.color;
            c.a = 1f;
            _spriteRenderer.color = c;
        }
        _isInvincible = false;
    }

    private void TriggerSwingAttack()
    {
        _isSwingReady = false;
        _swingCooldownTimer = swingCooldown; 

        if (_swingEffectCoroutine != null) StopCoroutine(_swingEffectCoroutine);
        if (_runtimeSwingWeaponObj != null) _runtimeSwingWeaponObj.SetActive(false);

        _swingEffectCoroutine = StartCoroutine(InstantSwingRotationRoutine());

        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;

        float attackLength = 3.0f; 
        float attackWidth = 1.0f;  

        if (_lookDirection == Vector3.right)
        {
            minX = myPos.x; maxX = myPos.x + attackLength;
            minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth;
        }
        else if (_lookDirection == Vector3.left)
        {
            minX = myPos.x - attackLength; maxX = myPos.x;
            minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth;
        }
        else if (_lookDirection == Vector3.up)
        {
            minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth;
            minY = myPos.y; maxY = myPos.y + attackLength;
        }
        else if (_lookDirection == Vector3.down)
        {
            minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth;
            minY = myPos.y - attackLength; maxY = myPos.y;
        }

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                zombie.OnGetHitByPlayer(swingDamage, false);
            }
        }
    }

    private void TriggerCounterAttack()
    {
        _isCounterReady = false;
        _counterCooldownTimer = counterCooldown; 

        if (_counterEffectCoroutine != null) StopCoroutine(_counterEffectCoroutine);
        if (_runtimeCounterWeaponObj != null) _runtimeCounterWeaponObj.SetActive(false);

        _counterEffectCoroutine = StartCoroutine(ShowCounterEffectRoutine());

        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;
        
        float counterLength = 2.0f; 
        float counterWidth = 0.4f;  

        if (_lookDirection == Vector3.right)
        {
            minX = myPos.x; maxX = myPos.x + counterLength;
            minY = myPos.y - counterWidth; maxY = myPos.y + counterWidth;
        }
        else if (_lookDirection == Vector3.left)
        {
            minX = myPos.x - counterLength; maxX = myPos.x;
            minY = myPos.y - counterWidth; maxY = myPos.y + counterWidth;
        }
        else if (_lookDirection == Vector3.up)
        {
            minX = myPos.x - counterWidth; maxX = myPos.x + counterWidth;
            minY = myPos.y; maxY = myPos.y + counterLength;
        }
        else if (_lookDirection == Vector3.down)
        {
            minX = myPos.x - counterWidth; maxX = myPos.x + counterWidth;
            minY = myPos.y - counterLength; maxY = myPos.y;
        }

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                Vector3 zombieDir = zombie.GetCurrentDirection(); 

                if (Vector3.Distance(_lookDirection + zombieDir, Vector3.zero) < 0.1f)
                {
                    zombie.OnGetHitByPlayer(counterDamage, true);
                }
            }
        }
    }

    private void SetImageAlpha32(Image img, byte alpha)
    {
        Color32 c32 = img.color;
        c32.a = alpha;
        img.color = c32;
    }

    private void CreateRuntimeWeaponEffects()
    {
        if (swingWeaponEffectSprite != null)
        {
            _runtimeSwingWeaponObj = new GameObject("RuntimeSwingWeaponEffect", typeof(SpriteRenderer));
            _runtimeSwingWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeSwingWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = swingWeaponEffectSprite;
            _runtimeSwingWeaponObj.SetActive(false);
        }

        if (counterWeaponEffectSprite != null)
        {
            _runtimeCounterWeaponObj = new GameObject("RuntimeCounterWeaponEffect", typeof(SpriteRenderer));
            _runtimeCounterWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeCounterWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = counterWeaponEffectSprite;
            _runtimeCounterWeaponObj.SetActive(false);
        }
    }

    private IEnumerator InstantSwingRotationRoutine()
    {
        if (_runtimeSwingWeaponObj == null) yield break;

        float baseAngle = 0f;
        if (_lookDirection == Vector3.right) baseAngle = 0f;
        else if (_lookDirection == Vector3.up) baseAngle = 90f;
        else if (_lookDirection == Vector3.left) baseAngle = 180f;
        else if (_lookDirection == Vector3.down) baseAngle = 270f;

        _runtimeSwingWeaponObj.SetActive(true);

        SetEffectTransform(baseAngle - 50f); yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseAngle); yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseAngle + 50f); yield return new WaitForSeconds(swingDuration / 3f);

        _runtimeSwingWeaponObj.SetActive(false);
    }

    private void SetEffectTransform(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * 1.0f;
        _runtimeSwingWeaponObj.transform.localPosition = offset;
        _runtimeSwingWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator ShowCounterEffectRoutine()
    {
        if (_runtimeCounterWeaponObj == null) yield break;

        _runtimeCounterWeaponObj.transform.localPosition = _lookDirection * 1.2f;

        if (_lookDirection == Vector3.right) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (_lookDirection == Vector3.left) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (_lookDirection == Vector3.up) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else if (_lookDirection == Vector3.down) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        _runtimeCounterWeaponObj.SetActive(true);
        yield return new WaitForSeconds(counterDuration);
        _runtimeCounterWeaponObj.SetActive(false);
    }

    private void ChangePlayerSprite(Vector3 direction)
    {
        if (_spriteRenderer == null) return;
        if (direction == Vector3.up && spriteUp != null) _spriteRenderer.sprite = spriteUp;
        else if (direction == Vector3.down && spriteDown != null) _spriteRenderer.sprite = spriteDown;
        else if (direction == Vector3.left && spriteLeft != null) _spriteRenderer.sprite = spriteLeft;
        else if (direction == Vector3.right && spriteRight != null) _spriteRenderer.sprite = spriteRight;
    }

    private IEnumerator MovePlayer(Vector3 direction)
    {
        _isMoving = true;
        float elapsedTime = 0;
        _origPos = transform.position;
        _targetPos = _origPos + direction;
        while (elapsedTime < _timeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / _timeToMove);
            yield return null;
        }
        transform.position = _targetPos;
        UpdateGridSortingOrder();
        _isMoving = false;
    }

    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;
        int currentGridY = Mathf.RoundToInt(transform.position.y);
        _spriteRenderer.sortingOrder = 100 - currentGridY;
        
        if (_runtimeSwingWeaponObj != null) { SpriteRenderer sr = _runtimeSwingWeaponObj.GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 5; }
        if (_runtimeCounterWeaponObj != null) { SpriteRenderer sr = _runtimeCounterWeaponObj.GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 5; }
    }
}