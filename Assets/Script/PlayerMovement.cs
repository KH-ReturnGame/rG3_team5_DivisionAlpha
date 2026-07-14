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
    private byte _readyAlpha = 153; 
    private bool _isInvincible = false;

    [Header("체어 스킬 설정")]
    [SerializeField] private Image swingBorderImage;    
    [SerializeField] private Image swingCooldownImage;   
    [SerializeField] private Sprite swingWeaponEffectSprite;   
    public float swingDamage = 20f; 
    public float swingCooldown = 0.75f; 
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
    [SerializeField] private float counterDuration = 0.3f;
    private float _counterCooldownTimer = 0f;
    private bool _isCounterReady = true;
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

    public bool IsInvincible
    {
        get { return _isInvincible; }
        set { _isInvincible = value; }
    }

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

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (arrowEffectObject != null) arrowEffectObject.SetActive(false);
        if (backstepEffectObject != null) backstepEffectObject.SetActive(false);

        CreateRuntimeWeaponEffects();
        SetupUIProperties();

        ResetUIState(swingBorderImage, swingCooldownImage);
        ResetUIState(counterBorderImage, counterCoolDownImage);
        ResetUIState(arrowBorderImage, arrowCoolDownImage);
        ResetUIState(backstepBorderImage, backstepCoolDownImage);

        UpdateGridSortingOrder();
    }

    private void SetupUIProperties()
    {
        InitImageFilledProperty(swingBorderImage);
        InitImageFilledProperty(counterBorderImage);
        InitImageFilledProperty(arrowBorderImage);
        InitImageFilledProperty(backstepBorderImage);

        if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha);
        if (counterCoolDownImage != null) SetImageAlpha32(counterCoolDownImage, _readyAlpha);
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
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap == null) return false;
        Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
        return transparentWallTilemap.HasTile(cellPosition);
    }

    void Update()
    {
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
        {
            _isSwingReady = true;
            if (swingBorderImage != null) swingBorderImage.fillAmount = 1f; 
            if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha); 
        }

        if (_counterCooldownTimer > 0) UpdateCooldownUI(ref _counterCooldownTimer, counterCooldown, counterBorderImage, counterCoolDownImage);
        else if (!_isCounterReady && Keyboard.current != null && !Keyboard.current.jKey.isPressed)
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
            if (borderImg != null && borderImg.type == Image.Type.Filled) borderImg.fillAmount = fillRatio;
            if (coolDownImg != null)
            {
                Color cColor = coolDownImg.color;
                cColor.a = 0.25f; 
                coolDownImg.color = cColor;
                coolDownImg.fillAmount = 1f; 
            }
        }
        else
        {
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
    }

    public void ApplyLifesteal(int zombieCount)
    {
        Heal(zombieCount * lifestealAmount);
    }
    // [체어 1스킬] 휘두르기 연산
    private void TriggerSwingAttack()
    {
        Debug.Log("체어 1스킬 휘두르기 공격 (Space키)!");
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
                zombie.OnGetHitByPlayer(swingDamage, false);
            }
        }
    }

    // [체어 2스킬] 카운터 연산
    private void TriggerCounterAttack()
    {
        Debug.Log("체어 2스킬 카운터 발동 (J키)!");
        _isCounterReady = false;
        _counterCooldownTimer = counterCooldown; 

        if (counterBorderImage != null) counterBorderImage.fillAmount = 0f;

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
            minY = myPos.y - counterWidth; maxY = myPos.y;
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
        
        UpdateGridSortingOrder();
        _isMoving = false;
    }

    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;
        int currentGridY = Mathf.RoundToInt(transform.position.y);
        _spriteRenderer.sortingOrder = 100 - currentGridY;

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

    public void TeleportTo(Vector3 targetNewPos)
    {
        StopAllCoroutines(); 
        _isMoving = false;    

        transform.position = targetNewPos;
        _origPos = targetNewPos;
        _targetPos = targetNewPos;

        UpdateGridSortingOrder();
        Debug.Log($"[TeleportTo] 플레이어가 {targetNewPos} 위치로 안전하게 텔레포트되었습니다.");
    }

    public void OnGetHitByZombie(float damage)
    {
        if (_isInvincible) return;
        Debug.Log($"[PlayerMovement] 좀비에게 공격당해 {damage}만큼 피격되었습니다.");
        StartCoroutine(TempInvincibleRoutine(0.5f)); 
    }

    private IEnumerator TempInvincibleRoutine(float duration)
    {
        _isInvincible = true;
        yield return new WaitForSeconds(duration);
        _isInvincible = false;
    }
}