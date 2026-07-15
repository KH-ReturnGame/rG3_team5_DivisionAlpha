using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 
<<<<<<< HEAD
=======
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
>>>>>>> main

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;
<<<<<<< HEAD
    private float _timeToMove = 0.15f;
=======

    [Header("이동 설정")]
    [Tooltip("한 칸 이동하는 데 걸리는 시간입니다. 값이 작아질수록 이동 속도가 빨라집니다.")]
    public float timeToMove = 0.16f;
<<<<<<< HEAD
=======
    [SerializeField] private Tilemap transparentWallTilemap; 
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
>>>>>>> main

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    private SpriteRenderer _spriteRenderer; 
    private Vector3 _lookDirection = Vector3.down; 
    private byte _readyAlpha = 153; 
    private bool _isInvincible = false;

<<<<<<< HEAD
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
=======
<<<<<<< HEAD
    [Header("1스킬: 휘두르기 (J키 - 방향별 3x2 / 2x3 유동 범위)")]
    [SerializeField] private Image swingBorderImage;
=======
    [Header("체어 스킬 설정")]
    [SerializeField] private Image swingBorderImage;    
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
    [SerializeField] private Image swingCooldownImage;   
    [SerializeField] private Sprite swingWeaponEffectSprite;   
    public float swingDamage = 20f; 
    public float swingCooldown = 0.75f; 
<<<<<<< HEAD
    
    [HideInInspector] private float swingDuration = 0.25f; // 애니메이션 0.25초
    private float _swingCooldownTimer = 0f;
    private bool _isSwingReady = true;
    private byte _readyAlpha = 153; // 0.6f -> 255 * 0.6 = 153 (60%)

    [Header("2스킬: 카운터 (K키)")]
    [SerializeField] private GameObject counterEffectObject; 
    [SerializeField] private Image counterBorderImage;  // 2스킬 빨간 테두리 Image
    [SerializeField] private Image counterCoolDownImage; // 2스킬 안쪽 흰색 원형 게이지 Image
    [SerializeField] private Sprite counterWeaponEffectSprite;
    public float counterDamage = 40f;
=======
    private float swingDuration = 0.25f; 
    private float _swingCooldownTimer = 0f;
    private bool _isSwingReady = true;
    private GameObject _runtimeSwingWeaponObj;
    private Coroutine _swingEffectCoroutine;

    [Header("체어 2스킬: 카운터 (J키)")]
    [SerializeField] private Image counterBorderImage;   
    [SerializeField] private Image counterCoolDownImage; 
    [SerializeField] private Sprite counterWeaponEffectSprite;
    public float counterDamage = 40f; 
    public float counterCooldown = 7.0f; 
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
    [SerializeField] private float counterDuration = 0.3f;
    private float _counterCooldownTimer = 0f;
    private bool _isCounterReady = true;
<<<<<<< HEAD
    private bool _isInvincible = false; // 좀비 AI들이 체크하는 무적 상태 플래그

    private Vector3 _lookDirection = Vector3.down; 
    private Coroutine _swingEffectCoroutine;
    private Coroutine _counterEffectCoroutine;
    private GameObject _runtimeSwingWeaponObj;
    private GameObject _runtimeCounterWeaponObj;

    [Header("맵 설정")]
    [SerializeField] private UnityEngine.Tilemaps.Tilemap transparentWallTilemap;

    /// <summary>
    /// 외부 좀비 AI 스크립트에서 플레이어의 무적 여부를 확인할 수 있도록 공개하는 프로퍼티
    /// </summary>
=======
    private GameObject _runtimeCounterWeaponObj;
    private Coroutine _counterEffectCoroutine;

    [Header("아처 1스킬: 즉시 관통 화살 (K키)")]
    [SerializeField] private GameObject arrowEffectObject; 
    [SerializeField] private Image arrowBorderImage;   
    [SerializeField] private Image arrowCoolDownImage;   
    public float arrowCooldown = 1.0f; 
    public float arrowDuration = 0.2f; 
    public int arrowRange = 5;         
    private float _arrowCooldownTimer = 0f;
    private Coroutine _arrowEffectCoroutine;

    [Header("아처 2스킬: 백스텝 (L키)")]
    [SerializeField] private GameObject backstepEffectObject; 
    [SerializeField] private Image backstepBorderImage;  
    [SerializeField] private Image backstepCoolDownImage; 
    public float backstepCooldown = 5.0f; 
    public float backstepDuration = 0.3f; 
    private float _backstepCooldownTimer = 0f;
    private Coroutine _backstepEffectCoroutine;

>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
    public bool IsInvincible
    {
        get { return _isInvincible; }
        set { _isInvincible = value; }
    }
<<<<<<< HEAD
=======

    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public float chairSkill1Dmg = 20f;
    public float chairSkill2Dmg = 40f;
    public float archerSkill1Dmg = 10f; 
    public float archerSkill2Dmg = 0f;
    [Header("업그레이드 시스템 연동")]
    public float lifestealAmount = 0f;
    public float critChance = 0f;
    public float evasionChance = 0f;
    public bool canRevive = false;
    private bool _hasRevived = false;
    public bool isInvincible = false;
    public float counterDamageMultiplier = 3f;

    public UpgradeManager upgradeManager; 

    public void Heal(float amount) 
    { 
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth); 
    }

    public void TakeDamage(float damage)
    {
        if (upgradeManager != null && upgradeManager.isUnlocked[16]) return; 
        if (upgradeManager != null && upgradeManager.isUnlocked[15] && Random.value < evasionChance) 
        {
            Debug.Log("공격 회피!");
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0 && upgradeManager != null && upgradeManager.isUnlocked[14] && !_hasRevived)
        {
            currentHealth = maxHealth;
            _hasRevived = true;
            Debug.Log("부활!");
        }
    }
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
>>>>>>> main

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
<<<<<<< HEAD
        if (myHealthBar == null) myHealthBar = GetComponent<HealthBar>();
        
        CreateRuntimeWeaponEffects();
        UpdateGridSortingOrder();
        SetupUIProperties();
=======
<<<<<<< HEAD
        
        CreateRuntimeWeaponEffects();
        SetupUIProperties();
=======
        if (arrowEffectObject != null) arrowEffectObject.SetActive(false);
        if (backstepEffectObject != null) backstepEffectObject.SetActive(false);

        CreateRuntimeWeaponEffects();
        SetupUIProperties();

        ResetUIState(swingBorderImage, swingCooldownImage);
        ResetUIState(counterBorderImage, counterCoolDownImage);
        ResetUIState(arrowBorderImage, arrowCoolDownImage);
        ResetUIState(backstepBorderImage, backstepCoolDownImage);

        UpdateGridSortingOrder();
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
>>>>>>> main
    }

    private void SetupUIProperties()
    {
<<<<<<< HEAD
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
=======
        InitImageFilledProperty(swingBorderImage);
        InitImageFilledProperty(counterBorderImage);
        InitImageFilledProperty(arrowBorderImage);
        InitImageFilledProperty(backstepBorderImage);

        if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha);
        if (counterCoolDownImage != null) SetImageAlpha32(counterCoolDownImage, _readyAlpha);
<<<<<<< HEAD
=======
        if (arrowCoolDownImage != null) SetImageAlpha32(arrowCoolDownImage, _readyAlpha);
        if (backstepCoolDownImage != null) SetImageAlpha32(backstepCoolDownImage, _readyAlpha);
    }

    private void InitImageFilledProperty(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = false; 
        img.fillAmount = 1f; 
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
>>>>>>> main
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap == null) return false;
        Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
        return transparentWallTilemap.HasTile(cellPosition);
    }

    void Update()
    {
<<<<<<< HEAD
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
=======
<<<<<<< HEAD
        // 실시간 적용된 swingCooldown 수치로 UI 바 연산 진행
        if (_swingCooldownTimer > 0)
        {
            UpdateCooldownUI(ref _swingCooldownTimer, swingCooldown, swingBorderImage, swingCooldownImage);
        }
        else if (!_isSwingReady && Keyboard.current != null && !Keyboard.current.jKey.isPressed)
=======
        // 1. 일시정지 상태 확인
        if (UpgradeManager.isGamePaused) return;

        // 2. 쿨타임 처리
        HandleCooldownTimers();

        if (Keyboard.current == null) return;

        // 3. 이동 처리
        if (!_isMoving)
        {
            if (Keyboard.current.wKey.isPressed) TryMove(Vector3.up);
            else if (Keyboard.current.aKey.isPressed) TryMove(Vector3.left);
            else if (Keyboard.current.sKey.isPressed) TryMove(Vector3.down);
            else if (Keyboard.current.dKey.isPressed) TryMove(Vector3.right);
        }

        // 4. 스킬 입력
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _swingCooldownTimer <= 0 && _isSwingReady) TriggerSwingAttack();
        if (Keyboard.current.jKey.wasPressedThisFrame && _counterCooldownTimer <= 0 && _isCounterReady) TriggerCounterAttack();
        if (Keyboard.current.kKey.wasPressedThisFrame && _arrowCooldownTimer <= 0) TryArrowAttack();
        if (Keyboard.current.lKey.wasPressedThisFrame && _backstepCooldownTimer <= 0) TryBackstep();
    }

    private void TryMove(Vector3 direction)
    {
        _lookDirection = direction;
        ChangePlayerSprite(direction);
        if (!IsTileBlocked(transform.position + direction)) 
        {
            StartCoroutine(MovePlayer(direction));
        }
    }

    private void HandleCooldownTimers()
    {
        if (_swingCooldownTimer > 0) UpdateCooldownUI(ref _swingCooldownTimer, swingCooldown, swingBorderImage, swingCooldownImage);
        else if (!_isSwingReady && Keyboard.current != null && !Keyboard.current.spaceKey.isPressed)
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
        {
            _isSwingReady = true;
            if (swingBorderImage != null) swingBorderImage.fillAmount = 1f; 
            if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha); 
        }

<<<<<<< HEAD
        // 실시간 적용된 counterCooldown 수치로 UI 바 연산 진행
        if (_counterCooldownTimer > 0)
=======
        if (_counterCooldownTimer > 0) UpdateCooldownUI(ref _counterCooldownTimer, counterCooldown, counterBorderImage, counterCoolDownImage);
        else if (!_isCounterReady && Keyboard.current != null && !Keyboard.current.jKey.isPressed)
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
        {
            _isCounterReady = true;
            if (counterBorderImage != null) counterBorderImage.fillAmount = 1f; 
            if (counterCoolDownImage != null) SetImageAlpha32(counterCoolDownImage, _readyAlpha); 
        }

<<<<<<< HEAD
        // 1. 이동 처리
        if (Keyboard.current.wKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.up;
            ChangePlayerSprite(Vector3.up);
            if (!IsTileBlocked(transform.position + Vector3.up)) StartCoroutine(MovePlayer(Vector3.up));
        }
        else if (Keyboard.current.aKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.left;
            ChangePlayerSprite(Vector3.left);
            if (!IsTileBlocked(transform.position + Vector3.left)) StartCoroutine(MovePlayer(Vector3.left));
        }
        else if (Keyboard.current.sKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.down;
            ChangePlayerSprite(Vector3.down);
            if (!IsTileBlocked(transform.position + Vector3.down)) StartCoroutine(MovePlayer(Vector3.down));
        }
        else if (Keyboard.current.dKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.right;
            ChangePlayerSprite(Vector3.right);
            if (!IsTileBlocked(transform.position + Vector3.right)) StartCoroutine(MovePlayer(Vector3.right));
>>>>>>> main
        }

        if (Keyboard.current != null)
        {
<<<<<<< HEAD
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
=======
            TriggerSwingAttack();
        }

        // 3. 2스킬 카운터 (K키)
        if (Keyboard.current.kKey.wasPressedThisFrame && _counterCooldownTimer <= 0)
        {
            TriggerCounterAttack();
>>>>>>> main
        }
=======
        if (_arrowCooldownTimer > 0) UpdateCooldownUI(ref _arrowCooldownTimer, arrowCooldown, arrowBorderImage, arrowCoolDownImage);
        if (_backstepCooldownTimer > 0) UpdateCooldownUI(ref _backstepCooldownTimer, backstepCooldown, backstepBorderImage, backstepCoolDownImage);
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
    }

<<<<<<< HEAD
    public void OnGetHitByZombie(float damage)
    {
        if (_isInvincible) return;

        // 💡 1. 머리 위 체력바 깎기
        if (myHealthBar != null)
        {
            myHealthBar.TakeDamage(damage);
=======
    private void UpdateCooldownUI(ref float timer, float maxCooldown, Image borderImg, Image coolDownImg)
    {
        timer -= Time.deltaTime;
        if (timer > 0)
        {
            float fillRatio = (maxCooldown - timer) / maxCooldown;
            if (borderImg != null && borderImg.type == Image.Type.Filled) borderImg.fillAmount = fillRatio;
            if (coolDownImg != null)
            {
                Color cColor = coolDownImg.color;
                cColor.a = 0.25f; 
                coolDownImg.color = cColor;
                coolDownImg.fillAmount = 1f; 
            }
>>>>>>> main
        }

        // 💡 2. 왼쪽 상단 캔버스 화면 UI 체력바도 똑같이 깎기!
        if (playerHudHealthBar != null)
        {
<<<<<<< HEAD
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

=======
            timer = 0f;
            ResetUIState(borderImg, coolDownImg);
        }
    }

    private void ResetUIState(Image borderImg, Image coolDownImg)
    {
        if (borderImg != null) borderImg.fillAmount = 1f;
        if (coolDownImg != null)
        {
            Color cColor = coolDownImg.color;
            cColor.a = 0.6f; 
            coolDownImg.color = cColor;
            coolDownImg.fillAmount = 1f;
        }
<<<<<<< HEAD
        _isInvincible = false;
    }

    // 바라보는 방향에 맞춰 가로세로 범위가 정확히 정렬되어 들어가는 1스킬 연산
=======
    }

    public void ApplyLifesteal(int zombieCount)
    {
        Heal(zombieCount * lifestealAmount);
    }
    // [체어 1스킬] 휘두르기 연산
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
    private void TriggerSwingAttack()
    {
        Debug.Log("체어 1스킬 휘두르기 공격 (Space키)!");
        _isSwingReady = false;
        _swingCooldownTimer = swingCooldown; 

        if (swingBorderImage != null) swingBorderImage.fillAmount = 0f;

>>>>>>> main
        if (_swingEffectCoroutine != null) StopCoroutine(_swingEffectCoroutine);
        if (_runtimeSwingWeaponObj != null) _runtimeSwingWeaponObj.SetActive(false);

        _swingEffectCoroutine = StartCoroutine(InstantSwingRotationRoutine());

        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;

<<<<<<< HEAD
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

=======
<<<<<<< HEAD
        // 위나 아래를 바라보고 있으면 가로 3, 세로 2 판정 크기 세팅
=======
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
        if (_lookDirection == Vector3.up || _lookDirection == Vector3.down)
        {
            float attackLength = 2.0f; 
            float attackWidth = 1.0f;  

            if (_lookDirection == Vector3.up)
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
<<<<<<< HEAD
        // 좌우를 바라보고 있으면 가로 2, 세로 3 판정 크기 세팅
=======
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
        else if (_lookDirection == Vector3.right || _lookDirection == Vector3.left)
        {
            float attackLength = 2.0f; 
            float attackWidth = 1.0f;  

            if (_lookDirection == Vector3.right)
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

        // 좀비 컴포넌트들을 탐색하여 범위 안에 있으면 피격 처리
>>>>>>> main
        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
<<<<<<< HEAD
            
=======
>>>>>>> main
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                zombie.OnGetHitByPlayer(swingDamage, false);
            }
        }
    }

<<<<<<< HEAD
    private void TriggerCounterAttack()
    {
        _isCounterReady = false;
        _counterCooldownTimer = counterCooldown; 

=======
    // [체어 2스킬] 카운터 연산
    private void TriggerCounterAttack()
    {
        Debug.Log("체어 2스킬 카운터 발동 (J키)!");
        _isCounterReady = false;
        _counterCooldownTimer = counterCooldown; 

        if (counterBorderImage != null) counterBorderImage.fillAmount = 0f;

>>>>>>> main
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
<<<<<<< HEAD
            minY = myPos.y - counterLength; maxY = myPos.y;
=======
            minY = myPos.y - counterWidth; maxY = myPos.y;
>>>>>>> main
        }

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
<<<<<<< HEAD
            
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                Vector3 zombieDir = zombie.GetCurrentDirection(); 

=======
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                Vector3 zombieDir = zombie.GetCurrentDirection(); 
<<<<<<< HEAD

                // 서로 마주보고 있는 경우 카운터 성립
=======
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
>>>>>>> main
                if (Vector3.Distance(_lookDirection + zombieDir, Vector3.zero) < 0.1f)
                {
                    zombie.OnGetHitByPlayer(counterDamage, true);
                }
            }
        }
    }

<<<<<<< HEAD
    private void SetImageAlpha32(Image img, byte alpha)
    {
=======
    // [아처 1스킬] 즉시 관통 화살 발사 연산
    private void TryArrowAttack()
    {
        Debug.Log("아처 1스킬 즉시 관통 화살 발사 (K키)!");
        _arrowCooldownTimer = arrowCooldown; 
        
        if (arrowBorderImage != null) arrowBorderImage.fillAmount = 0f;

        if (_arrowEffectCoroutine != null) StopCoroutine(_arrowEffectCoroutine);
        _arrowEffectCoroutine = StartCoroutine(ShowArrowEffectRoutine());

        int myX = Mathf.RoundToInt(transform.position.x);
        int myY = Mathf.RoundToInt(transform.position.y);
        List<Vector2Int> targetTiles = GetFrontTiles(myX, myY, arrowRange);

        // 프로젝트 내 설계된 아처용 좀비 AI 혹은 기본 좀비 AI 대응 피격 처리
        ZombieAIWithCounter[] zombiesAc = Object.FindObjectsByType<ZombieAIWithCounter>(FindObjectsSortMode.None);
        foreach (ZombieAIWithCounter zombie in zombiesAc)
        {
            if (zombie == null) continue;
            int zombieX = Mathf.RoundToInt(zombie.transform.position.x);
            int zombieY = Mathf.RoundToInt(zombie.transform.position.y);

            foreach (Vector2Int tile in targetTiles)
            {
                if (zombieX == tile.x && zombieY == tile.y)
                {
                    zombie.OnGetHitByPlayer(); 
                    break; 
                }
            }
        }
    }

    // [아처 2스킬] 백스텝 연산
    private void TryBackstep()
    {
        if (_isMoving) return; 

        Debug.Log("아처 2스킬 백스텝 (L키)!");
        _backstepCooldownTimer = backstepCooldown; 
        
        if (backstepBorderImage != null) backstepBorderImage.fillAmount = 0f;

        if (_backstepEffectCoroutine != null) StopCoroutine(_backstepEffectCoroutine);
        _backstepEffectCoroutine = StartCoroutine(ShowBackstepEffectRoutine());
        Vector3 backstepDirection = -_lookDirection * 3f;
        Vector3 targetPos = transform.position + backstepDirection;

        if (!IsTileBlocked(targetPos))
        {
            StartCoroutine(MovePlayerBackstep(targetPos));
        }
    }

    private IEnumerator ShowArrowEffectRoutine()
    {
        if (arrowEffectObject == null) yield break;

        arrowEffectObject.transform.localPosition = _lookDirection * 0.5f; 

        if (_lookDirection == Vector3.right) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (_lookDirection == Vector3.left) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (_lookDirection == Vector3.up) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else if (_lookDirection == Vector3.down) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        arrowEffectObject.SetActive(true);
        yield return new WaitForSeconds(arrowDuration);
        arrowEffectObject.SetActive(false);
    }

    private IEnumerator ShowBackstepEffectRoutine()
    {
        if (backstepEffectObject == null) yield break;

        backstepEffectObject.transform.localPosition = Vector3.zero; 
        backstepEffectObject.SetActive(true);
        yield return new WaitForSeconds(backstepDuration);
        backstepEffectObject.SetActive(false);
    }

    private IEnumerator MovePlayerBackstep(Vector3 targetPos)
    {
        _isMoving = true;
        float elapsedTime = 0;
        _origPos = transform.position;
        _targetPos = targetPos;
        while (elapsedTime < backstepDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / backstepDuration);
            yield return null;
        }
        transform.position = _targetPos;
        
        UpdateGridSortingOrder();
        _isMoving = false;
    }

    private List<Vector2Int> GetFrontTiles(int myX, int myY, int range)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        Vector2Int directionInt = new Vector2Int(Mathf.RoundToInt(_lookDirection.x), Mathf.RoundToInt(_lookDirection.y));

        for (int i = 1; i <= range; i++)
        {
            tiles.Add(new Vector2Int(myX + directionInt.x * i, myY + directionInt.y * i));
        }
        return tiles;
    }

    private void SetImageAlpha32(Image img, byte alpha)
    {
        if (img == null) return;
>>>>>>> main
        Color32 c32 = img.color;
        c32.a = alpha;
        img.color = c32;
    }

    private void CreateRuntimeWeaponEffects()
    {
<<<<<<< HEAD
=======
        string playerSortingLayer = _spriteRenderer != null ? _spriteRenderer.sortingLayerName : "Default";
        int playerSortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder : 0;

>>>>>>> main
        if (swingWeaponEffectSprite != null)
        {
            _runtimeSwingWeaponObj = new GameObject("RuntimeSwingWeaponEffect", typeof(SpriteRenderer));
            _runtimeSwingWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeSwingWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = swingWeaponEffectSprite;
<<<<<<< HEAD
=======
<<<<<<< HEAD
            
=======
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
            sr.sortingLayerName = playerSortingLayer;
            sr.sortingOrder = playerSortingOrder + 5;
>>>>>>> main
            _runtimeSwingWeaponObj.SetActive(false);
        }

        if (counterWeaponEffectSprite != null)
        {
            _runtimeCounterWeaponObj = new GameObject("RuntimeCounterWeaponEffect", typeof(SpriteRenderer));
            _runtimeCounterWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeCounterWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = counterWeaponEffectSprite;
<<<<<<< HEAD
=======
<<<<<<< HEAD
            
=======
>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
            sr.sortingLayerName = playerSortingLayer;
            sr.sortingOrder = playerSortingOrder + 5;
>>>>>>> main
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
<<<<<<< HEAD
=======

>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
        while (elapsedTime < timeToMove) 
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / timeToMove);
            yield return null;
        }
        transform.position = _targetPos;
<<<<<<< HEAD
        UpdateGridSortingOrder();
        _isMoving = false;
    }

=======
<<<<<<< HEAD
       
        _isMoving = false;
    }

    // 외부 발판 트리거 전용 안전한 텔레포트 제어 함수
=======
        
        UpdateGridSortingOrder();
        _isMoving = false;
    }

>>>>>>> main
    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;
        int currentGridY = Mathf.RoundToInt(transform.position.y);
        _spriteRenderer.sortingOrder = 100 - currentGridY;
<<<<<<< HEAD
        
        if (_runtimeSwingWeaponObj != null) { SpriteRenderer sr = _runtimeSwingWeaponObj.GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 5; }
        if (_runtimeCounterWeaponObj != null) { SpriteRenderer sr = _runtimeCounterWeaponObj.GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 5; }
    }
=======

        if (arrowEffectObject != null)
        {
            SpriteRenderer sr = arrowEffectObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 1;
        }
        if (backstepEffectObject != null)
        {
            SpriteRenderer sr = backstepEffectObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 1; 
        }
    }

>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
    public void TeleportTo(Vector3 targetNewPos)
    {
        StopAllCoroutines(); 
        _isMoving = false;    

        transform.position = targetNewPos;
        _origPos = targetNewPos;
        _targetPos = targetNewPos;

<<<<<<< HEAD
        Debug.Log($"[TeleportTo] 플레이어가 그리드 예외 처리와 함께 {targetNewPos} 위치로 완벽 텔포되었습니다.");
    }

    /// <summary>
    /// 좀비가 플레이어를 성공적으로 공격했을 때 호출하는 피격 처리 메서드
    /// </summary>
    /// <param name="damage">좀비가 가하는 데미지 수치</param>
    public void OnGetHitByZombie(float damage)
    {
        if (_isInvincible) return;

        // TODO: 프로젝트 내 체력 시스템 구조에 맞춰 연산 추가 (예: PlayerHP -= damage;)
        Debug.Log($"[PlayerMovement] 좀비에게 공격당해 {damage}만큼 피격되었습니다.");

        // 피격 후 0.5초간 임시 무적 부여
        StartCoroutine(TempInvincibleRoutine(0.5f)); 
    }

    /// <summary>
    /// 피격 또는 특정 이벤트 후 임시로 무적 상태를 제어해주는 루틴
    /// </summary>
=======
        UpdateGridSortingOrder();
        Debug.Log($"[TeleportTo] 플레이어가 {targetNewPos} 위치로 안전하게 텔레포트되었습니다.");
    }

    public void OnGetHitByZombie(float damage)
    {
        if (_isInvincible) return;
        Debug.Log($"[PlayerMovement] 좀비에게 공격당해 {damage}만큼 피격되었습니다.");
        StartCoroutine(TempInvincibleRoutine(0.5f)); 
    }

>>>>>>> 5d5dd9fb8b0732e075564ca32c2ccf5be2eb366f
    private IEnumerator TempInvincibleRoutine(float duration)
    {
        _isInvincible = true;
        yield return new WaitForSeconds(duration);
        _isInvincible = false;
    }
>>>>>>> main
}