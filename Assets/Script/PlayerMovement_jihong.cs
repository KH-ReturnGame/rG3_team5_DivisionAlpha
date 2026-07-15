/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;

    [Header("이동 설정")]
    [Tooltip("한 칸 이동하는 데 걸리는 시간입니다. 값이 작아질수록 이동 속도가 빨라집니다.")]
    public float timeToMove = 0.16f;
    [SerializeField] private Tilemap transparentWallTilemap; 

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    private SpriteRenderer _spriteRenderer; 
    private Vector3 _lookDirection = Vector3.down; 
    private byte _readyAlpha = 153; // 60% 투명도
    private bool _isInvincible = false;
    private bool _isStunned = false; // 백스텝 후딜레이 등 임시 행동불가 상태 체크

    [Header("플레이어 실시간 체력 설정")]
    public float maxHealth = 50f; // 인간 맥스 체력 50
    public float currentHealth = 50f;

    [Header("데미지 설정 (실시간 조정 가능)")]
    public float chairSkill1Dmg = 20f;   // 체어 1스킬 (J키) 데미지
    public float chairSkill2Dmg = 40f;   // 체어 2스킬 (K키) 데미지
    public float archerSkill1Dmg = 10f;  // 아처 1스킬 (L키) 데미지
    public float archerSkill2Dmg = 40f;  // 아처 2스킬 (;키) 카운터 데미지 (체어 카운터와 동일하게 40 적용)

    [Header("체어 1스킬: 스윙 (J키)")]
    [SerializeField] private Image swingBorderImage;     
    [SerializeField] private Image swingCooldownImage;   
    [SerializeField] private Sprite swingWeaponEffectSprite;   
    public float swingCooldown = 0.75f; 
    
    private float swingDuration = 0.25f; 
    private float _swingCooldownTimer = 0f;
    private bool _isSwingReady = true;
    private GameObject _runtimeSwingWeaponObj;
    private Coroutine _swingEffectCoroutine;

    [Header("체어 2스킬: 카운터 (K키)")]
    [SerializeField] private GameObject counterEffectObject; 
    [SerializeField] private Image counterBorderImage;    
    [SerializeField] private Image counterCoolDownImage;  
    [SerializeField] private Sprite counterWeaponEffectSprite;
    public float counterCooldown = 7.0f; 
    [SerializeField] private float counterDuration = 0.3f;
    private float _counterCooldownTimer = 0f;
    private bool _isCounterReady = true;
    private GameObject _runtimeCounterWeaponObj;
    private Coroutine _counterEffectCoroutine;

    [Header("아처 1스킬: 즉시 관통 화살 (L키)")]
    [SerializeField] private Sprite arrowEffectSprite; 
    [SerializeField] private Image arrowBorderImage;      
    [SerializeField] private Image arrowCoolDownImage;    
    public float arrowCooldown = 1.0f; 
    public float arrowDuration = 0.3f; // 화살이 날아가는 총 시간
    public int arrowRange = 5;         
    private float _arrowCooldownTimer = 0f;
    private Coroutine _arrowEffectCoroutine;
    private GameObject _runtimeArrowObj; // 코드가 내부에서 실시간으로 생성해서 쓸 화살 오브젝트

    [Header("아처 2스킬: 백스텝 & 활 활성화 (;키)")]
    [SerializeField] private GameObject backstepEffectObject; 
    [SerializeField] private Image backstepBorderImage;    
    [SerializeField] private Image backstepCoolDownImage;  
    
    // 활 스프라이트를 받을 인스펙터 전용 퍼블릭 칸
    public Sprite backstepBowSprite; 
    
    public float backstepCooldown = 5.0f; 
    public float backstepDuration = 0.15f; // 최대 거리(3칸) 대쉬 기준 시간
    public float backstepEndDelay = 0.2f;  // 백스텝 완료 후 제자리 정지 시간 (0.2초)
    private float _backstepCooldownTimer = 0f;
    private Coroutine _backstepEffectCoroutine;
    private GameObject _runtimeBowObj; // 백스텝 시 나타날 활 오브젝트

    public bool IsInvincible
    {
        get { return _isInvincible; }
        set { _isInvincible = value; }
    }

    [Header("업그레이드 시스템 연동")]
    public float lifestealAmount = 0f;
    public float critChance = 0f;
    public float evasionChance = 0f;
    public bool canRevive = false;
    private bool _hasRevived = false;
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
        currentHealth = Mathf.Max(currentHealth, 0f); // 음수 방지
        Debug.Log($"[TakeDamage] 플레이어 체력 차감 성공. 현재 체력: {currentHealth}");

        if (currentHealth <= 0 && upgradeManager != null && upgradeManager.isUnlocked[14] && !_hasRevived)
        {
            currentHealth = maxHealth;
            _hasRevived = true;
            Debug.Log("부활!");
        }
    }

    void Start()
    {
        // 체력바 관련 수동 UI 갱신 코드는 이제 체력바 스크립트에서 알아서 처리하므로 삭제했습니다.
    }

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (backstepEffectObject != null) backstepEffectObject.SetActive(false);
        
        CreateRuntimeWeaponEffects();
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap == null) return false;
        Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
        return transparentWallTilemap.HasTile(cellPosition);
    }

    void Update()
    {
        if (UpgradeManager.isGamePaused) return;

        HandleCooldownTimers();

        if (Keyboard.current == null) return;

        if (!_isMoving && !_isStunned)
        {
            if (Keyboard.current.wKey.isPressed) TryMove(Vector3.up);
            else if (Keyboard.current.aKey.isPressed) TryMove(Vector3.left);
            else if (Keyboard.current.sKey.isPressed) TryMove(Vector3.down);
            else if (Keyboard.current.dKey.isPressed) TryMove(Vector3.right);
        }

        if (!_isStunned)
        {
            if (Keyboard.current.jKey.wasPressedThisFrame && _swingCooldownTimer <= 0 && _isSwingReady) TriggerSwingAttack();
            if (Keyboard.current.kKey.wasPressedThisFrame && _counterCooldownTimer <= 0 && _isCounterReady) TriggerCounterAttack();
            if (Keyboard.current.lKey.wasPressedThisFrame && _arrowCooldownTimer <= 0) TryArrowAttack();
            if (Keyboard.current.semicolonKey.wasPressedThisFrame && _backstepCooldownTimer <= 0) TryBackstep();
        }
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
        else if (!_isSwingReady && Keyboard.current != null && !Keyboard.current.jKey.isPressed)
        {
            _isSwingReady = true;
            if (swingBorderImage != null) swingBorderImage.fillAmount = 1f; 
            if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha); 
        }

        if (_counterCooldownTimer > 0) UpdateCooldownUI(ref _counterCooldownTimer, counterCooldown, counterBorderImage, counterCoolDownImage);
        else if (!_isCounterReady && Keyboard.current != null && !Keyboard.current.kKey.isPressed)
        {
            _isCounterReady = true;
            if (counterBorderImage != null) counterBorderImage.fillAmount = 1f; 
            if (counterCoolDownImage != null) SetImageAlpha32(counterCoolDownImage, _readyAlpha); 
        }

        if (_arrowCooldownTimer > 0) UpdateCooldownUI(ref _arrowCooldownTimer, arrowCooldown, arrowBorderImage, arrowCoolDownImage);
        if (_backstepCooldownTimer > 0) UpdateCooldownUI(ref _backstepCooldownTimer, backstepCooldown, backstepBorderImage, backstepCoolDownImage);
    }

    private void UpdateCooldownUI(ref float timer, float maxCooldown, Image borderImg, Image coolDownImg)
    {
        timer -= Time.deltaTime;
        if (timer > 0)
        {
            float fillRatio = (maxCooldown - timer) / maxCooldown;
            if (borderImg != null) borderImg.fillAmount = fillRatio;
            if (coolDownImg != null)
            {
                Color cColor = coolDownImg.color;
                cColor.a = 0.25f; 
                coolDownImg.color = cColor;
                coolDownImg.fillAmount = 1f - fillRatio; 
            }
        }
        else
        {
            timer = 0f;
            if (borderImg != null) borderImg.fillAmount = 1f;
            if (coolDownImg != null)
            {
                Color cColor = coolDownImg.color;
                cColor.a = 0.6f; 
                coolDownImg.color = cColor;
                coolDownImg.fillAmount = 1f;
            }
        }
    }

    public void ApplyLifesteal(int zombieCount)
    {
        Heal(zombieCount * lifestealAmount);
    }

    private void TriggerSwingAttack()
    {
        _isSwingReady = false;
        _swingCooldownTimer = swingCooldown; 

        if (swingBorderImage != null) swingBorderImage.fillAmount = 0f;

        if (_swingEffectCoroutine != null) StopCoroutine(_swingEffectCoroutine);
        if (_runtimeSwingWeaponObj != null) _runtimeSwingWeaponObj.SetActive(false);

        _swingEffectCoroutine = StartCoroutine(InstantSwingRotationRoutine());

        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;

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

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                zombie.OnGetHitByPlayer(chairSkill1Dmg, false); 
            }
        }
    }

    private void TriggerCounterAttack()
    {
        _isCounterReady = false;
        _counterCooldownTimer = counterCooldown; 

        if (counterBorderImage != null) counterBorderImage.fillAmount = 0f;

        if (_counterEffectCoroutine != null) StopCoroutine(_counterEffectCoroutine);
        if (_runtimeCounterWeaponObj != null) _runtimeCounterWeaponObj.SetActive(false);

        _counterEffectCoroutine = StartCoroutine(ShowCounterEffectRoutine());

        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;
        
        float counterLength = 2.2f; 
        float counterWidth = 0.5f;  

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
                    // 두 번째 인자 isCounter를 true로 넘겨 좀비를 그로기 상태로 전환시킵니다.
                    zombie.OnGetHitByPlayer(chairSkill2Dmg, true); 
                }
            }
        }
    }

    private void TryArrowAttack()
    {
        _arrowCooldownTimer = arrowCooldown; 
        
        if (arrowBorderImage != null) arrowBorderImage.fillAmount = 0f;

        if (_arrowEffectCoroutine != null) StopCoroutine(_arrowEffectCoroutine);
        _arrowEffectCoroutine = StartCoroutine(ShowArrowEffectRoutine());

        int myX = Mathf.RoundToInt(transform.position.x);
        int myY = Mathf.RoundToInt(transform.position.y);
        List<Vector2Int> targetTiles = GetFrontTiles(myX, myY, arrowRange);

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            int zombieX = Mathf.RoundToInt(zombie.transform.position.x);
            int zombieY = Mathf.RoundToInt(zombie.transform.position.y);

            foreach (Vector2Int tile in targetTiles)
            {
                if (zombieX == tile.x && zombieY == tile.y)
                {
                    zombie.OnGetHitByPlayer(archerSkill1Dmg, false); 
                    break; 
                }
            }
        }
    }

    private void TryBackstep()
    {
        if (_isMoving || _isStunned) return; 

        _backstepCooldownTimer = backstepCooldown; 
        
        if (backstepBorderImage != null) backstepBorderImage.fillAmount = 0f;

        Vector3 backDirection = -_lookDirection;
        Vector3 targetPos = transform.position;
        int actualTilesMoved = 0; 

        for (int i = 1; i <= 3; i++)
        {
            Vector3 nextCheckPos = transform.position + (backDirection * i);
            if (!IsTileBlocked(nextCheckPos))
            {
                targetPos = nextCheckPos;
                actualTilesMoved = i;
            }
            else
            {
                break; 
            }
        }

        // 아처 2스킬 사용 즉시 정면 2칸 카운터 판정 수행 및 좀비 그로기 유발
        TriggerArcherBackstepCounter();

        if (targetPos != transform.position)
        {
            float scaledDuration = backstepDuration * ((float)actualTilesMoved / 3f);

            if (_backstepEffectCoroutine != null) StopCoroutine(_backstepEffectCoroutine);
            _backstepEffectCoroutine = StartCoroutine(ShowBackstepEffectRoutine(scaledDuration));
            StartCoroutine(MovePlayerBackstep(targetPos, scaledDuration));
        }
    }

    // 아처 2스킬 시전 시 캐릭터 정면 2칸 카운터 판정 및 좀비 그로기 처리 함수
    private void TriggerArcherBackstepCounter()
    {
        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;
        
        float counterLength = 2.2f; // 정면 약 2칸 범위
        float counterWidth = 0.5f;  

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

                // 서로 마주보고 있는 상황 체크
                if (Vector3.Distance(_lookDirection + zombieDir, Vector3.zero) < 0.1f)
                {
                    // ⭐ [연동 완료] 두 번째 인자인 isCounter를 true로 넘겨 체어와 동일하게 그로기 유발 및 데미지 3배 상태로 만듭니다.
                    zombie.OnGetHitByPlayer(archerSkill2Dmg, true); 
                    Debug.Log("[아처 2스킬] 정면 카운터 및 좀비 그로기 유발 성공!");
                }
            }
        }
    }

    private IEnumerator ShowArrowEffectRoutine()
    {
        if (_runtimeArrowObj == null) yield break;

        _runtimeArrowObj.transform.position = transform.position; 

        if (_lookDirection == Vector3.left) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); 
        else if (_lookDirection == Vector3.right) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); 
        else if (_lookDirection == Vector3.up) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f); 
        else if (_lookDirection == Vector3.down) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); 

        _runtimeArrowObj.SetActive(true);

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (_lookDirection * arrowRange);
        float elapsed = 0f;

        while (elapsed < arrowDuration)
        {
            elapsed += Time.deltaTime;
            _runtimeArrowObj.transform.position = Vector3.Lerp(startPos, endPos, elapsed / arrowDuration);
            yield return null;
        }

        _runtimeArrowObj.SetActive(false);
    }

    private IEnumerator ShowBackstepEffectRoutine(float duration)
    {
        if (backstepEffectObject == null) yield break;

        backstepEffectObject.transform.localPosition = Vector3.zero; 
        backstepEffectObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        backstepEffectObject.SetActive(false);
    }

    private IEnumerator MovePlayerBackstep(Vector3 targetPos, float duration)
    {
        _isMoving = true;
        _isStunned = true; 
        
        float elapsedTime = 0;
        _origPos = transform.position;
        _targetPos = targetPos;

        Vector3 originalLookDir = _lookDirection;      
        Vector3 backStepLookDir = -_lookDirection;      

        ChangePlayerSprite(backStepLookDir);

        if (_runtimeBowObj != null)
        {
            // 활을 플레이어의 1칸 앞에 오프셋 위치시켜 생성
            _runtimeBowObj.transform.localPosition = originalLookDir;
            
            if (originalLookDir == Vector3.left) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            else if (originalLookDir == Vector3.right) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            else if (originalLookDir == Vector3.up) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            else if (originalLookDir == Vector3.down) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            _runtimeBowObj.SetActive(true);
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / duration);
            yield return null;
        }
        transform.position = _targetPos;
        _isMoving = false;

        ChangePlayerSprite(originalLookDir);

        // 대쉬(백스텝 이동)가 끝나는 즉시 활 활성화 해제
        if (_runtimeBowObj != null)
        {
            _runtimeBowObj.SetActive(false);
        }

        yield return new WaitForSeconds(backstepEndDelay);

        _isStunned = false; 
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
        Color32 c32 = img.color;
        c32.a = alpha;
        img.color = c32;
    }

    private void CreateRuntimeWeaponEffects()
    {
        string playerSortingLayer = _spriteRenderer != null ? _spriteRenderer.sortingLayerName : "Default";
        int playerSortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder : 0;

        if (swingWeaponEffectSprite != null)
        {
            _runtimeSwingWeaponObj = new GameObject("RuntimeSwingWeaponEffect", typeof(SpriteRenderer));
            _runtimeSwingWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeSwingWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = swingWeaponEffectSprite;
            sr.sortingLayerName = playerSortingLayer;
            sr.sortingOrder = playerSortingOrder + 5;
            _runtimeSwingWeaponObj.SetActive(false);
        }

        if (counterWeaponEffectSprite != null)
        {
            _runtimeCounterWeaponObj = new GameObject("RuntimeCounterWeaponEffect", typeof(SpriteRenderer));
            _runtimeCounterWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeCounterWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = counterWeaponEffectSprite;
            sr.sortingLayerName = playerSortingLayer;
            sr.sortingOrder = playerSortingOrder + 5;
            _runtimeCounterWeaponObj.SetActive(false);
        }

        if (arrowEffectSprite != null)
        {
            _runtimeArrowObj = new GameObject("RuntimeArrowEffect", typeof(SpriteRenderer));
            _runtimeArrowObj.transform.SetParent(transform, false);
            SpriteRenderer sr = _runtimeArrowObj.GetComponent<SpriteRenderer>();
            sr.sprite = arrowEffectSprite;
            sr.sortingLayerName = playerSortingLayer;
            sr.sortingOrder = playerSortingOrder + 6; 
            _runtimeArrowObj.SetActive(false);
        }

        // 백스텝용 전용 스프라이트(backstepBowSprite) 적용
        if (backstepBowSprite != null)
        {
            _runtimeBowObj = new GameObject("RuntimeBowEffect", typeof(SpriteRenderer));
            _runtimeBowObj.transform.SetParent(transform, false);
            SpriteRenderer bowSr = _runtimeBowObj.GetComponent<SpriteRenderer>();
            bowSr.sprite = backstepBowSprite; 
            bowSr.sortingLayerName = playerSortingLayer;
            bowSr.sortingOrder = playerSortingOrder + 4; 
            _runtimeBowObj.SetActive(false);
        }
        else if (arrowEffectSprite != null) 
        {
            _runtimeBowObj = new GameObject("RuntimeBowEffect", typeof(SpriteRenderer));
            _runtimeBowObj.transform.SetParent(transform, false);
            SpriteRenderer bowSr = _runtimeBowObj.GetComponent<SpriteRenderer>();
            bowSr.sprite = arrowEffectSprite; 
            bowSr.sortingLayerName = playerSortingLayer;
            bowSr.sortingOrder = playerSortingOrder + 4; 
            _runtimeBowObj.SetActive(false);
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

        while (elapsedTime < timeToMove) 
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / timeToMove);
            yield return null;
        }
        transform.position = _targetPos;
        _isMoving = false;
    }

    public void TeleportTo(Vector3 targetNewPos)
    {
        StopAllCoroutines(); 
        _isMoving = false;    
        _isStunned = false;   

        transform.position = targetNewPos;
        _origPos = targetNewPos;
        _targetPos = targetNewPos;

        Debug.Log($"[TeleportTo] 플레이어가 {targetNewPos} 위치로 안전하게 텔레포트되었습니다.");
    }

    public void OnGetHitByZombie(float damage)
    {
        TakeDamage(damage); 
        StartCoroutine(TempInvincibleRoutine(0.5f)); 
    }

    private IEnumerator TempInvincibleRoutine(float duration)
    {
        _isInvincible = true;
        yield return new WaitForSeconds(duration);
        _isInvincible = false;
    }
    public Vector3 GetLookDirection()
    {
        return _lookDirection;
    }
}
*/