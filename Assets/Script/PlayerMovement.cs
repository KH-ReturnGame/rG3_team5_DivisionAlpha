using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;

    public Vector3 lookDirection = Vector3.down; 

    [Header("이동 설정")]
    [Tooltip("한 칸 이동하는 데 걸리는 시간입니다. 값이 작아질수록 이동 속도가 빨라집니다.")]
    public float timeToMove = 0.16f;
    [SerializeField] private Tilemap transparentWallTilemap; 

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    [Header("체력 UI 연결")]
    public Slider[] hpSliders = new Slider[2]; 
    public TMP_Text[] hpTexts = new TMP_Text[2];

    private SpriteRenderer _spriteRenderer; 
    private Vector3 _lookDirection = Vector3.down; 
    private byte _readyAlpha = 153; 
    private bool _isInvincible = false;
    private bool _isStunned = false; 
    private Coroutine _invincibleCoroutine;

    [Header("플레이어 실시간 체력 설정")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;
    [Tooltip("피격 후 무적 지속 시간입니다.")]
    public float invulnerabilityDuration = 0.5f;

    [Header("데미지 설정 (실시간 조정 가능)")]
    public float chairSkill1Dmg = 20f;   
    public float chairSkill2Dmg = 40f;   
    public float archerSkill1Dmg = 10f;  
    public float archerSkill2Dmg = 40f;  

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

    [Header("체어 2스킬: 카운터 (L키)")]
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

    [Header("아처 1스킬: 즉시 관통 화살 (K키)")]
    [SerializeField] private Sprite arrowEffectSprite; 
    [SerializeField] private Image arrowBorderImage;               
    [SerializeField] private Image arrowCoolDownImage;             
    public float arrowCooldown = 1.0f; 
    public float arrowDuration = 0.2f; 
    public int arrowRange = 5;         
    private float _arrowCooldownTimer = 0f;
    private Coroutine _arrowEffectCoroutine;
    private GameObject _runtimeArrowObj; 

    [Header("아처 2스킬: 백스텝 & 활 활성화 (;키)")]
    [SerializeField] private GameObject backstepEffectObject; 
    [SerializeField] private Image backstepBorderImage;      
    [SerializeField] private Image backstepCoolDownImage;  
    
    public Sprite backstepBowSprite; 
    public float backstepCooldown = 5.0f; 
    public float backstepDuration = 0.15f; 
    public float backstepEndDelay = 0.2f;  
    private float _backstepCooldownTimer = 0f;
    private Coroutine _backstepEffectCoroutine;
    private GameObject _runtimeBowObj; 

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

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (backstepEffectObject != null) backstepEffectObject.SetActive(false);
        CreateRuntimeWeaponEffects();
        UpdatePlayerUI();
    }

    void Update()
    {
        if (UpgradeManager.isGamePaused) return;

        _spriteRenderer.sortingOrder = -(Mathf.RoundToInt(transform.position.y));

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
            else if (Keyboard.current.kKey.wasPressedThisFrame && _arrowCooldownTimer <= 0) TryArrowAttack();
            else if (Keyboard.current.lKey.wasPressedThisFrame && _counterCooldownTimer <= 0 && _isCounterReady) TriggerCounterAttack();
            else if (Keyboard.current.semicolonKey.wasPressedThisFrame && _backstepCooldownTimer <= 0) TryBackstep();
        }
    }

    private void UpdatePlayerUI()
    {
        for (int i = 0; i < hpSliders.Length; i++)
        {
            if (hpSliders[i] != null)
            {
                hpSliders[i].maxValue = maxHealth;
                hpSliders[i].value = currentHealth;
                if (hpSliders[i].fillRect != null)
                    hpSliders[i].fillRect.GetComponent<Image>().color = Color.green;
            }
            if (hpTexts[i] != null)
            {
                hpTexts[i].text = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (_isInvincible) return;
        if (upgradeManager != null && upgradeManager.isUnlocked[16]) return; 
        if (upgradeManager != null && upgradeManager.isUnlocked[15] && Random.value < evasionChance) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f); 
        
        UpdatePlayerUI(); 

        if (currentHealth <= 0)
        {
            if (upgradeManager != null && upgradeManager.isUnlocked[14] && !_hasRevived)
            {
                currentHealth = maxHealth;
                _hasRevived = true;
                UpdatePlayerUI(); 
            }
            else
            {
                TriggerGameOver(); 
            }
        }
        else
        {
            if (_invincibleCoroutine != null) StopCoroutine(_invincibleCoroutine);
            _invincibleCoroutine = StartCoroutine(InvincibleBlinkRoutine());
        }
    }

    private void TriggerGameOver()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveFinalScore();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("end_scene");
    }
    
    private IEnumerator InvincibleBlinkRoutine()
    {
        _isInvincible = true;
        float elapsed = 0f;
        while (elapsed < invulnerabilityDuration)
        {
            if (_spriteRenderer != null) _spriteRenderer.enabled = !_spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        _isInvincible = false;
        if (_spriteRenderer != null) _spriteRenderer.enabled = true;
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap == null) return false;
        Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
        return transparentWallTilemap.HasTile(cellPosition);
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
        else if (!_isCounterReady && Keyboard.current != null && !Keyboard.current.lKey.isPressed)
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

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdatePlayerUI(); 
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
        float minX = myPos.x, maxX = myPos.x, minY = myPos.y, maxY = myPos.y;
        if (_lookDirection == Vector3.up || _lookDirection == Vector3.down)
        {
            float attackLength = 2.0f; float attackWidth = 1.0f; 
            if (_lookDirection == Vector3.up) { minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth; minY = myPos.y; maxY = myPos.y + attackLength; }
            else { minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth; minY = myPos.y - attackLength; maxY = myPos.y; }
        }
        else
        {
            float attackLength = 2.0f; float attackWidth = 1.0f; 
            if (_lookDirection == Vector3.right) { minX = myPos.x; maxX = myPos.x + attackLength; minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth; }
            else { minX = myPos.x - attackLength; maxX = myPos.x; minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth; }
        }

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
                zombie.OnGetHitByPlayer(chairSkill1Dmg, false); 
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
        float minX = myPos.x, maxX = myPos.x, minY = myPos.y, maxY = myPos.y;
        float counterLength = 2.2f; float counterWidth = 0.5f; 

        if (_lookDirection == Vector3.right) { minX = myPos.x; maxX = myPos.x + counterLength; minY = myPos.y - counterWidth; maxY = myPos.y + counterWidth; }
        else if (_lookDirection == Vector3.left) { minX = myPos.x - counterLength; maxX = myPos.x; minY = myPos.y - counterWidth; maxY = myPos.y + counterWidth; }
        else if (_lookDirection == Vector3.up) { minX = myPos.x - counterWidth; maxX = myPos.x + counterWidth; minY = myPos.y; maxY = myPos.y + counterLength; }
        else { minX = myPos.x - counterWidth; maxX = myPos.x + counterWidth; minY = myPos.y - counterLength; maxY = myPos.y; }

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                if (Vector3.Distance(_lookDirection + zombie.GetCurrentDirection(), Vector3.zero) < 0.1f)
                    zombie.OnGetHitByPlayer(chairSkill2Dmg, true); 
            }
        }
    }

    private void TryArrowAttack()
    {
        _arrowCooldownTimer = arrowCooldown; 
        if (arrowBorderImage != null) arrowBorderImage.fillAmount = 0f;
        if (_arrowEffectCoroutine != null) StopCoroutine(_arrowEffectCoroutine);
        _arrowEffectCoroutine = StartCoroutine(ShowArrowEffectRoutine(_lookDirection));
        StartCoroutine(DelayedDamageRoutine(arrowDuration * 0.5f, _lookDirection));
    }

    private IEnumerator ShowArrowEffectRoutine(Vector3 shootDirection)
    {
        if (_runtimeArrowObj == null) yield break;
        _runtimeArrowObj.transform.position = transform.position; 
        if (shootDirection == Vector3.up) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (shootDirection == Vector3.down) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (shootDirection == Vector3.left) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else if (shootDirection == Vector3.right) _runtimeArrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        _runtimeArrowObj.SetActive(true);
        Vector3 startPos = transform.position; Vector3 endPos = startPos + (shootDirection * arrowRange); float elapsed = 0f;
        while (elapsed < arrowDuration) { elapsed += Time.deltaTime; _runtimeArrowObj.transform.position = Vector3.Lerp(startPos, endPos, elapsed / arrowDuration); yield return null; }
        _runtimeArrowObj.SetActive(false);
    }

    private IEnumerator DelayedDamageRoutine(float delay, Vector3 shootDirection)
    {
        yield return new WaitForSeconds(delay); 
        List<Vector2Int> targetTiles = GetFrontTiles(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), arrowRange, shootDirection);
        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (Vector2Int tile in targetTiles)
        {
            foreach (ZombieAI2_0 zombie in zombies)
            {
                if (zombie == null) continue;
                if (Mathf.RoundToInt(zombie.transform.position.x) == tile.x && Mathf.RoundToInt(zombie.transform.position.y) == tile.y)
                    zombie.OnGetHitByPlayer(archerSkill1Dmg, false); 
            }
        }
    }

    private List<Vector2Int> GetFrontTiles(int myX, int myY, int range, Vector3 direction)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        Vector2Int dir = new Vector2Int(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.y));
        for (int i = 1; i <= range; i++) tiles.Add(new Vector2Int(myX + dir.x * i, myY + dir.y * i));
        return tiles;
    }

    private void TryBackstep()
    {
        if (_isStunned) return; 
        _backstepCooldownTimer = backstepCooldown; 
        if (backstepBorderImage != null) backstepBorderImage.fillAmount = 0f;
        Vector3 backDirection = -_lookDirection; Vector3 targetPos = transform.position; int actualTilesMoved = 0; 
        for (int i = 1; i <= 3; i++)
        {
            Vector3 next = transform.position + (backDirection * i);
            if (!IsTileBlocked(next)) { targetPos = next; actualTilesMoved = i; } else break;
        }
        TriggerArcherBackstepCounter();
        if (targetPos != transform.position)
        {
            float duration = backstepDuration * ((float)actualTilesMoved / 3f);
            StopAllCoroutines(); _isMoving = false;
            if (_backstepEffectCoroutine != null) StopCoroutine(_backstepEffectCoroutine);
            _backstepEffectCoroutine = StartCoroutine(ShowBackstepEffectRoutine(duration));
            StartCoroutine(MovePlayerBackstep(targetPos, duration));
        }
    }

    private void TriggerArcherBackstepCounter()
    {
        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x, minY = myPos.y, maxY = myPos.y;
        float len = 2.2f, wid = 0.5f; 
        if (_lookDirection == Vector3.right) { minX = myPos.x; maxX = myPos.x + len; minY = myPos.y - wid; maxY = myPos.y + wid; }
        else if (_lookDirection == Vector3.left) { minX = myPos.x - len; maxX = myPos.x; minY = myPos.y - wid; maxY = myPos.y + wid; }
        else if (_lookDirection == Vector3.up) { minX = myPos.x - wid; maxX = myPos.x + wid; minY = myPos.y; maxY = myPos.y + len; }
        else { minX = myPos.x - wid; maxX = myPos.x + wid; minY = myPos.y - len; maxY = myPos.y; }

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 pos = zombie.transform.position;
            if (pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY)
            {
                if (Vector3.Distance(_lookDirection + zombie.GetCurrentDirection(), Vector3.zero) < 0.1f)
                    zombie.OnGetHitByPlayer(archerSkill2Dmg, true); 
            }
        }
    }

    private IEnumerator ShowBackstepEffectRoutine(float duration)
    {
        if (backstepEffectObject == null) yield break;
        backstepEffectObject.transform.localPosition = Vector3.zero; backstepEffectObject.SetActive(true);
        yield return new WaitForSeconds(duration); backstepEffectObject.SetActive(false);
    }

    private IEnumerator MovePlayerBackstep(Vector3 targetPos, float duration)
    {
        _isMoving = true; _isStunned = true; float elapsed = 0; _origPos = transform.position; _targetPos = targetPos;
        Vector3 origDir = _lookDirection; Vector3 backDir = -_lookDirection; ChangePlayerSprite(backDir);
        if (_runtimeBowObj != null)
        {
            _runtimeBowObj.transform.localPosition = origDir;
            if (origDir == Vector3.left) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            else if (origDir == Vector3.right) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            else if (origDir == Vector3.up) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            else if (origDir == Vector3.down) _runtimeBowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            _runtimeBowObj.SetActive(true);
        }
        while (elapsed < duration) { elapsed += Time.deltaTime; transform.position = Vector3.Lerp(_origPos, _targetPos, elapsed / duration); yield return null; }
        transform.position = _targetPos; _isMoving = false; ChangePlayerSprite(origDir);
        if (_runtimeBowObj != null) _runtimeBowObj.SetActive(false);
        yield return new WaitForSeconds(backstepEndDelay); _isStunned = false; 
    }

    private void SetImageAlpha32(Image img, byte alpha)
    {
        if (img == null) return;
        Color32 c32 = img.color; c32.a = alpha; img.color = c32;
    }

    private void CreateRuntimeWeaponEffects()
    {
        string layer = _spriteRenderer != null ? _spriteRenderer.sortingLayerName : "Default";
        int order = _spriteRenderer != null ? _spriteRenderer.sortingOrder : 0;
        if (swingWeaponEffectSprite != null) { _runtimeSwingWeaponObj = new GameObject("RuntimeSwingWeaponEffect", typeof(SpriteRenderer)); _runtimeSwingWeaponObj.transform.SetParent(transform, false); SpriteRenderer sr = _runtimeSwingWeaponObj.GetComponent<SpriteRenderer>(); sr.sprite = swingWeaponEffectSprite; sr.sortingLayerName = layer; sr.sortingOrder = order + 5; _runtimeSwingWeaponObj.SetActive(false); }
        if (counterWeaponEffectSprite != null) { _runtimeCounterWeaponObj = new GameObject("RuntimeCounterWeaponEffect", typeof(SpriteRenderer)); _runtimeCounterWeaponObj.transform.SetParent(transform, false); SpriteRenderer sr = _runtimeCounterWeaponObj.GetComponent<SpriteRenderer>(); sr.sprite = counterWeaponEffectSprite; sr.sortingLayerName = layer; sr.sortingOrder = order + 5; _runtimeCounterWeaponObj.SetActive(false); }
        if (arrowEffectSprite != null) { _runtimeArrowObj = new GameObject("RuntimeArrowEffect", typeof(SpriteRenderer)); _runtimeArrowObj.transform.SetParent(transform, false); SpriteRenderer sr = _runtimeArrowObj.GetComponent<SpriteRenderer>(); sr.sprite = arrowEffectSprite; sr.sortingLayerName = layer; sr.sortingOrder = order + 6; _runtimeArrowObj.SetActive(false); }
        if (backstepBowSprite != null) { _runtimeBowObj = new GameObject("RuntimeBowEffect", typeof(SpriteRenderer)); _runtimeBowObj.transform.SetParent(transform, false); SpriteRenderer sr = _runtimeBowObj.GetComponent<SpriteRenderer>(); sr.sprite = backstepBowSprite; sr.sortingLayerName = layer; sr.sortingOrder = order + 4; _runtimeBowObj.SetActive(false); }
    }

    private IEnumerator InstantSwingRotationRoutine()
    {
        if (_runtimeSwingWeaponObj == null) yield break;
        float baseA = 0f;
        if (_lookDirection == Vector3.right) baseA = 0f; else if (_lookDirection == Vector3.up) baseA = 90f; else if (_lookDirection == Vector3.left) baseA = 180f; else baseA = 270f;
        _runtimeSwingWeaponObj.SetActive(true);
        SetEffectTransform(baseA - 50f); yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseA); yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseA + 50f); yield return new WaitForSeconds(swingDuration / 3f);
        _runtimeSwingWeaponObj.SetActive(false);
    }

    private void SetEffectTransform(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        _runtimeSwingWeaponObj.transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * 1.0f;
        _runtimeSwingWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator ShowCounterEffectRoutine()
    {
        if (_runtimeCounterWeaponObj == null) yield break;
        _runtimeCounterWeaponObj.transform.localPosition = _lookDirection * 1.2f;
        if (_lookDirection == Vector3.right) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (_lookDirection == Vector3.left) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (_lookDirection == Vector3.up) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        _runtimeCounterWeaponObj.SetActive(true);
        yield return new WaitForSeconds(counterDuration); _runtimeCounterWeaponObj.SetActive(false);
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
        _isMoving = true; float elapsed = 0; _origPos = transform.position; _targetPos = _origPos + direction;
        while (elapsed < timeToMove) { elapsed += Time.deltaTime; transform.position = Vector3.Lerp(_origPos, _targetPos, elapsed / timeToMove); yield return null; }
        transform.position = _targetPos; _isMoving = false;
    }

    public void TeleportTo(Vector3 targetTo)
    {
        StopAllCoroutines(); _isMoving = false; _isStunned = false; 
        transform.position = targetTo; _origPos = targetTo; _targetPos = targetTo;
    }

    public void OnGetHitByZombie(float damage) { TakeDamage(damage); }
}