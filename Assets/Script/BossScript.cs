using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps; 
using UnityEngine.UI; // UI 시스템 사용을 위해 필수

public class WitchTrilogyBoss : MonoBehaviour
{
    public static WitchTrilogyBoss Instance { get; private set; }

    public enum BossPhase { Phase1, Phase2, Phase3 }
    public enum GroggyState { Normal, Groggy }

    [Header("=== [보스 UI 연결] ===")]
    [Tooltip("체력바, 게이지, 보스 사진 등을 모두 담고 있는 최상위 마스터 UI 오브젝트 (Canvas 또는 Panel)")]
    public GameObject bossUIPanel; 
    [Tooltip("상단 Canvas에 배치한 보스 체력 Slider")]
    public Slider bossHPSlider;
    [Tooltip("상단 Canvas에 배치한 보스 보라색 게이지 Slider")]
    public Slider bossPurpleGaugeSlider;
    
    // 💡 Canvas 내부에서 정상적으로 출력되도록 UI Image 타입으로 변경했습니다!
    [Tooltip("보스임을 표시하는 연출용 UI Image 오브젝트")]
    public Image bossIllustrationImage;

    [Header("=== [보스 상태 및 페이지] ===")]
    public BossPhase currentPhase = BossPhase.Phase1;
    public GroggyState groggyState = GroggyState.Normal;
    public float maxHealth = 2000f;
    public float currentHealth;

    [Header("=== [기획 1. 보라색 게이지 (에너지)] ===")]
    [Range(0f, 100f)] public float purpleGauge = 0f;
    public float energyIncreaseAmount = 15f;    
    public float energyDecreasePerSecond = 0.2f; 
    public bool isEnragedStage2 = false;         

    [Header("=== [기획 1. 2단계 버프 능력치] ===")]
    public float bonusDefense = 10f;
    public float bonusDamage = 15f;

    [Header("=== [ZombieAI2_0 기본 베이스 설정] ===")]
    public Transform playerTransform;
    private bool _isAction = false; 
    public bool showGizmosConstantly = true;
    public int defeatScore = 5000;       
    
    [Tooltip("천천히 이동할 때 한 칸당 소요 시간")] public float normalTimeToMove = 0.4f;       
    [Tooltip("연출적으로 빠르게 이동할 때 한 칸당 소요 시간")] public float fastTimeToMove = 0.15f;       
    private float _currentTimeToMove = 0.4f; 

    public float attackDamage = 15f;      
    public int detectionRangeX = 10;       
    public int detectionRangeY = 10;       
    public int n_triggerFrontSteps = 2;   
    public int m_attackWidth = 3;        
    public int k_attackHeight = 3;       

    [SerializeField] private Tilemap transparentWallTilemap; 

    [Header("1️⃣ 일반 이동 스프라이트 (천천히 이동할 때)")]
    public Sprite spriteDefault; 
    public Sprite spriteUp;    
    public Sprite spriteDown;  
    public Sprite spriteLeft;  

    [Header("2️⃣ 폭주 이동 스프라이트 (빠르게 이동할 때)")]
    public Sprite fastSpriteUp;
    public Sprite fastSpriteDown;
    public Sprite fastSpriteLeft;

    [Header("3️⃣ 공격 스프라이트 (공격 방향별)")]
    public Sprite attackSpriteUp;
    public Sprite attackSpriteDown;
    public Sprite attackSpriteLeft;

    private SpriteRenderer _spriteRenderer; 
    private bool _isFastMovingState = false; 

    public GameObject warningSquarePrefab; 
    private Vector3 _currentDirection = Vector3.down; 
    private bool _isWaitingToAttack = false; 
    private Coroutine _attackCoroutine;
    private GameObject _currentWarningEffect; 

    public float attackCooldown = 4.0f;   
    private float _attackCooldownTimer = 0f; 
    private bool _isChasing = false; 

    private PlayerMovement _playerMovement;
    private readonly Vector3[] _directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };

    public Vector3 GetCurrentDirection() => _currentDirection;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        purpleGauge = 0f; 
        _currentTimeToMove = normalTimeToMove; 

        if (_spriteRenderer != null && spriteDefault != null) _spriteRenderer.sprite = spriteDefault;

        if (transparentWallTilemap == null)
        {
            GameObject wallObj = GameObject.Find("Transparent_Wall_Tilemap");
            if (wallObj != null) transparentWallTilemap = wallObj.GetComponent<Tilemap>();
            else transparentWallTilemap = Object.FindFirstObjectByType<Tilemap>();
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null) _playerMovement = playerTransform.GetComponent<PlayerMovement>();

        // 강제로 UI 초기 수치 적용
        InitUI();

        if (bossIllustrationImage != null) bossIllustrationImage.gameObject.SetActive(true); 
        if (bossUIPanel != null) bossUIPanel.SetActive(false); 
    }

    void Update()
    {
        if (groggyState == GroggyState.Groggy) return;

        if (_attackCooldownTimer > 0) _attackCooldownTimer -= Time.deltaTime;
        
        HandlePurpleGaugeDecrease();

        if (_isChasing && !_isAction)
        {
            StartCoroutine(ZombieMainRoutine());
        }
    }

    public void ShowBossUI()
    {
        if (bossUIPanel != null)
        {
            bossUIPanel.SetActive(true);
            InitUI(); // 등판 순간 한 번 더 확실하게 수치 정렬
            Debug.Log("[보스 등판 UI 일제 활성화] 체력바 및 보스 일러스트 일괄 표시.");
        }
        else
        {
            if (bossIllustrationImage != null) bossIllustrationImage.gameObject.SetActive(true);
        }
    }

    private void InitUI()
    {
        if (bossHPSlider != null)
        {
            bossHPSlider.maxValue = maxHealth;
            bossHPSlider.value = currentHealth;
        }
        if (bossPurpleGaugeSlider != null)
        {
            bossPurpleGaugeSlider.maxValue = 100f;
            bossPurpleGaugeSlider.value = purpleGauge; 
        }
    }

    private void UpdateHPUI()
    {
        if (bossHPSlider != null) bossHPSlider.value = currentHealth;
    }

    private void UpdatePurpleGaugeUI()
    {
        if (bossPurpleGaugeSlider != null) bossPurpleGaugeSlider.value = purpleGauge;
    }

    public void SetMovementSpeedState(bool isFast)
    {
        _isFastMovingState = isFast;
        _currentTimeToMove = isFast ? fastTimeToMove : normalTimeToMove;
        ChangeZombieSprite(_currentDirection);
    }

    public void InjectEnergy()
    {
        float minLimit = isEnragedStage2 ? 50f : 0f;
        purpleGauge = Mathf.Clamp(purpleGauge + energyIncreaseAmount, minLimit, 100f);
        UpdatePurpleGaugeUI(); 
        CheckEnrageStage();
    }

    private void HandlePurpleGaugeDecrease()
    {
        float minLimit = isEnragedStage2 ? 50f : 0f;
        
        if (purpleGauge > minLimit)
        {
            purpleGauge -= energyDecreasePerSecond * Time.deltaTime;
            if (purpleGauge < minLimit) purpleGauge = minLimit;
            UpdatePurpleGaugeUI(); 
        }
        CheckEnrageStage();
    }

    private void CheckEnrageStage()
    {
        if (!isEnragedStage2 && purpleGauge >= 50f)
        {
            isEnragedStage2 = true;
            attackDamage += bonusDamage;
            Debug.Log("[보스 시스템 버프] 보라색 게이지 50% 돌파! 공격 데미지가 상승했습니다.");
        }
    }

    public void TakeDamage(float damage)
    {
        float finalDefense = isEnragedStage2 ? bonusDefense : 0f;
        float finalDamage = Mathf.Max(damage - finalDefense, 1f);

        currentHealth -= finalDamage;
        if (currentHealth < 0f) currentHealth = 0f;
        
        UpdateHPUI(); 
        Debug.Log($"[보스 피격] 체력: {currentHealth}/{maxHealth} (적용 데미지: {finalDamage})");

        CheckPhaseTransition();
    }

    private void CheckPhaseTransition()
    {
        float healthPercent = (currentHealth / maxHealth) * 100f;

        if (currentPhase == BossPhase.Phase1 && healthPercent <= 70f)
        {
            currentPhase = BossPhase.Phase2;
            Debug.Log("[페이지 전환] 2페이지 돌입!");
        }
        else if (currentPhase == BossPhase.Phase2 && healthPercent <= 25f)
        {
            currentPhase = BossPhase.Phase3;
            Debug.Log("[페이지 전환] 3페이지 돌입!");
        }
    }

    private IEnumerator ZombieMainRoutine()
    {
        _isAction = true;

        if (playerTransform != null && CheckPlayerInDetectionRange())
        {
            if (_attackCooldownTimer <= 0 && CheckPlayerWithinFrontSteps())
            {
                _attackCoroutine = StartCoroutine(AttackWarningRoutine());
                yield return _attackCoroutine;
            }
            else
            {
                yield return StartCoroutine(ZombieTrackAndMoveRoutine());
            }
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        _isAction = false;
    }

    private void AlignToGridCenter()
    {
        transform.position = new Vector3(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 0);
    }

    private Vector3 GetZombieGridCenter() => new Vector3(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 0);

    private bool CheckPlayerInDetectionRange()
    {
        if (playerTransform == null) return false;
        Vector3 zCenter = GetZombieGridCenter();
        return (Mathf.Abs(Mathf.RoundToInt(playerTransform.position.x) - Mathf.RoundToInt(zCenter.x)) <= detectionRangeX &&
                Mathf.Abs(Mathf.RoundToInt(playerTransform.position.y) - Mathf.RoundToInt(zCenter.y)) <= detectionRangeY);
    }

    private bool CheckPlayerWithinFrontSteps()
    {
        if (playerTransform == null) return false;
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);
        Vector3 zCenter = GetZombieGridCenter();

        for (int i = 1; i <= n_triggerFrontSteps; i++)
        {
            Vector3 checkPos = zCenter + (_currentDirection * i);
            if (pX == Mathf.RoundToInt(checkPos.x) && pY == Mathf.RoundToInt(checkPos.y)) return true;
        }
        return false;
    }

    private bool CheckPlayerInMxKRange()
    {
        if (playerTransform == null) return false;
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);
        Vector3 centerAttackPos = GetAttackBoxCenter();

        float halfWidth = m_attackWidth / 2f;
        float halfHeight = k_attackHeight / 2f;
        if (_currentDirection == Vector3.left || _currentDirection == Vector3.right)
        {
            halfWidth = k_attackHeight / 2f;
            halfHeight = m_attackWidth / 2f;
        }
        return (pX >= centerAttackPos.x - halfWidth && pX <= centerAttackPos.x + halfWidth &&
                pY >= centerAttackPos.y - halfHeight && pY <= centerAttackPos.y + halfHeight);
    }

    private Vector3 GetAttackBoxCenter()
    {
        Vector3 zCenter = GetZombieGridCenter();
        float offset = (k_attackHeight % 2 == 0) ? (k_attackHeight / 2f) - 0.5f : (k_attackHeight - 1) / 2f;
        Vector3 rawCenter = zCenter + (_currentDirection * offset);
        if (m_attackWidth % 2 == 0)
        {
            if (_currentDirection == Vector3.up || _currentDirection == Vector3.down) rawCenter.x += 0.5f;
            else rawCenter.y += 0.5f;
        }
        return rawCenter;
    }

    private IEnumerator AttackWarningRoutine()
    {
        _isWaitingToAttack = true;
        Vector3 warningCenterPos = GetAttackBoxCenter();
        
        ChangeZombieAttackSprite(_currentDirection);

        if (warningSquarePrefab != null)
        {
            _currentWarningEffect = Instantiate(warningSquarePrefab, warningCenterPos, Quaternion.identity);
            _currentWarningEffect.transform.localScale = (_currentDirection == Vector3.up || _currentDirection == Vector3.down) ? 
                new Vector3(m_attackWidth, k_attackHeight, 1) : new Vector3(k_attackHeight, m_attackWidth, 1);
        }

        yield return new WaitForSeconds(1.5f);

        _isWaitingToAttack = false;
        if (_currentWarningEffect != null) Destroy(_currentWarningEffect);

        if (CheckPlayerInMxKRange() && _playerMovement != null && !_playerMovement.IsInvincible)
        {
            _playerMovement.OnGetHitByZombie(attackDamage);
        }

        ChangeZombieSprite(_currentDirection);

        _attackCooldownTimer = attackCooldown; 
    }

    private IEnumerator ZombieTrackAndMoveRoutine()
    {
        Vector3 startPos = GetZombieGridCenter();
        Vector3 targetPos = new Vector3(Mathf.RoundToInt(playerTransform.position.x), Mathf.RoundToInt(playerTransform.position.y), 0f);
        if (startPos == targetPos) yield break;

        Vector3 nextMoveDirection = FindNextStepBFS(startPos, targetPos);
        if (nextMoveDirection == Vector3.zero) yield break;

        _currentDirection = nextMoveDirection;
        ChangeZombieSprite(nextMoveDirection); 

        float elapsedTime = 0;
        Vector3 origPos = startPos; 
        Vector3 destination = origPos + nextMoveDirection;

        while (elapsedTime < _currentTimeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origPos, destination, elapsedTime / _currentTimeToMove);
            yield return null;
        }

        transform.position = destination;
        AlignToGridCenter(); 
    }

    private Vector3 FindNextStepBFS(Vector3 start, Vector3 target)
    {
        Queue<Vector3> queue = new Queue<Vector3>();
        HashSet<Vector3> visited = new HashSet<Vector3>();
        Dictionary<Vector3, Vector3> parentMap = new Dictionary<Vector3, Vector3>();

        queue.Enqueue(start);
        visited.Add(start);
        bool found = false;

        while (queue.Count > 0)
        {
            Vector3 current = queue.Dequeue();
            if (current == target) { found = true; break; }

            foreach (Vector3 dir in _directions)
            {
                Vector3 next = new Vector3(Mathf.RoundToInt(current.x + dir.x), Mathf.RoundToInt(current.y + dir.y), 0f);
                if (!visited.Contains(next))
                {
                    if (next == target || !IsTileBlocked(next))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        if (found)
        {
            Vector3 curr = target;
            while (parentMap.ContainsKey(curr) && parentMap[curr] != start) curr = parentMap[curr];
            return (curr - start).normalized;
        }
        return Vector3.zero;
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap == null) return false;
        return transparentWallTilemap.HasTile(transparentWallTilemap.WorldToCell(targetPos));
    }

    private void ChangeZombieSprite(Vector3 direction)
    {
        if (_spriteRenderer == null || _isWaitingToAttack) return;

        if (_isFastMovingState)
        {
            if (direction == Vector3.up && fastSpriteUp != null) { _spriteRenderer.sprite = fastSpriteUp; _spriteRenderer.flipX = false; }
            else if (direction == Vector3.down && fastSpriteDown != null) { _spriteRenderer.sprite = fastSpriteDown; _spriteRenderer.flipX = false; }
            else if (direction == Vector3.left && fastSpriteLeft != null) { _spriteRenderer.sprite = fastSpriteLeft; _spriteRenderer.flipX = false; }
            else if (direction == Vector3.right && fastSpriteLeft != null) { _spriteRenderer.sprite = fastSpriteLeft; _spriteRenderer.flipX = true; }
        }
        else
        {
            if (direction == Vector3.up && spriteUp != null) { _spriteRenderer.sprite = spriteUp; _spriteRenderer.flipX = false; }
            else if (direction == Vector3.down && spriteDown != null) { _spriteRenderer.sprite = spriteDown; _spriteRenderer.flipX = false; }
            else if (direction == Vector3.left && spriteLeft != null) { _spriteRenderer.sprite = spriteLeft; _spriteRenderer.flipX = false; }
            else if (direction == Vector3.right && spriteLeft != null) { _spriteRenderer.sprite = spriteLeft; _spriteRenderer.flipX = true; }
        }
    }

    private void ChangeZombieAttackSprite(Vector3 direction)
    {
        if (_spriteRenderer == null) return;

        if (direction == Vector3.up && attackSpriteUp != null) { _spriteRenderer.sprite = attackSpriteUp; _spriteRenderer.flipX = false; }
        else if (direction == Vector3.down && attackSpriteDown != null) { _spriteRenderer.sprite = attackSpriteDown; _spriteRenderer.flipX = false; }
        else if (direction == Vector3.left && attackSpriteLeft != null) { _spriteRenderer.sprite = attackSpriteLeft; _spriteRenderer.flipX = false; }
        else if (direction == Vector3.right && attackSpriteLeft != null) { _spriteRenderer.sprite = attackSpriteLeft; _spriteRenderer.flipX = true; }
    }

    public void TriggerGroggy(float duration) => StartCoroutine(GroggyRoutine(duration));
    private IEnumerator GroggyRoutine(float duration)
    {
        groggyState = GroggyState.Groggy;
        Color originalColor = _spriteRenderer.color;
        _spriteRenderer.color = Color.gray; 
        yield return new WaitForSeconds(duration);
        _spriteRenderer.color = originalColor;
        groggyState = GroggyState.Normal;
        _isAction = false;
    }

    private void DrawGizmosLogic()
    {
        Vector3 zCenter = GetZombieGridCenter();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(zCenter, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 1));
    }

    private void OnDrawGizmos() { if (showGizmosConstantly) DrawGizmosLogic(); }
    public void ForceStartChasing() { _isChasing = true; }
}