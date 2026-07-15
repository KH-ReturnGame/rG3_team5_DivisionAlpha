using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps; 

public class ZombieAI2_0 : MonoBehaviour
{
    public Transform playerTransform;
    private bool _isAction = false; 

    [Header("개발용 디버그 설정")]
    public bool showGizmosConstantly = true;

    [Header("좀비 고유 스탯 설정")]
    public int defeatScore = 100;        // 인스펙터에서 수정 가능한 좀비별 처치 점수
    public float timeToMove = 0.3f;       
    public float attackDamage = 10f;      
    public float explosionDamage = 30f;  // 자폭용 데미지 스탯
    
    [Header("인식 범위 (추적 시작 조건)")]
    public int detectionRangeX = 4;       
    public int detectionRangeY = 4;       
    
    [Header("공격 트리거 조건 (n칸 이내!)")]
    public int n_triggerFrontSteps = 1;   

    [Header("공격 사각형 범위 크기 설정")]
    public int m_attackWidth = 1;        
    public int k_attackHeight = 2;       

    [Header("코드로 제어하는 투명벽 타일맵 (자동 연결됨)")]
    [SerializeField] private Tilemap transparentWallTilemap; 

    [Header("방향별 스프라이트 등록")]
    public Sprite spriteDefault; 
    public Sprite spriteUp;    
    public Sprite spriteDown;  
    public Sprite spriteLeft;  

    private SpriteRenderer _spriteRenderer; 

    [Header("카운터 시스템 설정")]
    public GameObject warningSquarePrefab; 
    private Vector3 _currentDirection = Vector3.down; 
    private bool _isWaitingToAttack = false; 
    private bool _isGroggy = false; 
    private Coroutine _attackCoroutine;
    private GameObject _currentWarningEffect; 

    [Header("공격 쿨타임 설정")]
    public float attackCooldown = 5.0f;   
    private float _attackCooldownTimer = 0f; 

    private bool _isChasing = false; 
    private bool _isExploding = false; // 자폭 작동 여부 플래그

    public bool IsWaitingToAttack => _isWaitingToAttack;

    private HealthBar _myHealthBar;
    private PlayerMovement _playerMovement;

    private readonly Vector3[] _directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };

    // 태그를 저장할 내부 변수
    private string _zombieTag;

    [Header("RunningZombie 설정")]
    public int runningSteps = 5; // 이동할 칸 수
    public float runningTimeToMove = 0.15f; // 인스펙터에서 직접 조정하는 러닝좀비 전용 이동 속도
    private bool _runningToRight = true; // 현재 오른쪽 이동 중인지 여부
    private int _runningStepsRemaining = 0; // 카운터 해제 후 남은 이동 칸 수 계산용

    [Header("MouseZombie 설정")]
    public Sprite labGlassSprite; // 실험통 이미지 등록용
    private bool _isMouseInvincible = false;
    private bool _isRevealed = false;

    [Header("KendoZombie 설정")]
    public int kendoDashDistance = 3;    
    public float kendoDashCooldown = 5f;
    public float kendoDashTimeToMove = 0.15f; // 인스펙터에서 직접 조정하는 검도좀비 대쉬 속도
    private float _kendoDashTimer = 0f;

    [Header("AngryZombie 설정")]
    public float angryDashTimeToMove = 0.09f; // 인스펙터에서 직접 조정하는 앵그리좀비 전용 대쉬 속도

    // BombZombie용 내부 변수
    private bool _bombZombieTriggered = false; 

    // GhostZombie용 내부 변수
    private bool _ghostWaiting = false;

    public Vector3 GetCurrentDirection()
    {
        return _currentDirection; 
    }

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _myHealthBar = GetComponent<HealthBar>(); 
        
    }

    void Start()
    {
        _zombieTag = gameObject.tag;

        if (_spriteRenderer != null && spriteDefault != null)
        {
            _spriteRenderer.sprite = spriteDefault;
        }

        if (_zombieTag == "MouseZombie")
        {
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        }

        if (transparentWallTilemap == null)
        {
            GameObject wallObj = GameObject.Find("Transparent_Wall_Tilemap");
            if (wallObj != null)
            {
                transparentWallTilemap = wallObj.GetComponent<Tilemap>();
            }
            else
            {
                transparentWallTilemap = Object.FindFirstObjectByType<Tilemap>();
            }
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            _playerMovement = playerTransform.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        if (_isExploding) return; 

        if (_spriteRenderer != null)
        {
        _spriteRenderer.sortingOrder = -(Mathf.RoundToInt(transform.position.y));
        }

        if (_attackCooldownTimer > 0) _attackCooldownTimer -= Time.deltaTime;
        if (_kendoDashTimer > 0) _kendoDashTimer -= Time.deltaTime;

        if (playerTransform != null && _playerMovement != null)
        {
            if (_zombieTag != "SmallZombie" && _zombieTag != "BombZombie" && !(_zombieTag == "MouseZombie" && !_isRevealed))
            {
                if (CheckPlayerOverlap() && !_playerMovement.IsInvincible)
                {
                    _playerMovement.OnGetHitByZombie(attackDamage);
                }
            }
        }

        if (!_isAction && playerTransform != null)
        {
            if (_zombieTag == "BombZombie")
            {
                if (!_isChasing)
                {
                    _isChasing = _bombZombieTriggered || CheckPlayerInDetectionRange();
                }
            }
            else if (_zombieTag == "RunningZombie")
            {
                _isChasing = true;
            }
            else
            {
                if (!_isChasing)
                {
                    _isChasing = CheckPlayerInDetectionRange();
                }
            }

            if (_isChasing)
            {
                StartCoroutine(ZombieMainRoutine());
            }
        }
    }

    // =================================================================
    // 🧠 메인 AI 분기 루틴 (Main AI Routing)
    // =================================================================
    private IEnumerator ZombieMainRoutine()
    {
        _isAction = true;
        AlignToGridCenter(); 

        if (_zombieTag == "SmallZombie")
        {
            if (CheckPlayerInSmallTriggerRange())
            {
                yield return StartCoroutine(SmallZombieExplosionRoutine());
            }
            else
            {
                yield return StartCoroutine(ZombieTrackAndMoveRoutine());
            }
        }
        else if (_zombieTag == "BombZombie")
        {
            if (CheckPlayerInSmallTriggerRange())
            {
                yield return StartCoroutine(BombZombieExplosionRoutine());
            }
            else
            {
                yield return StartCoroutine(BombZombieRoamRoutine());
            }
        }
        else if (_zombieTag == "AngryZombie")
        {
            yield return StartCoroutine(AngryZombieDashRoutine());
        }
        else if (_zombieTag == "RunningZombie")
        {
            yield return StartCoroutine(RunningZombieRoutine());
        }
        else if (_zombieTag == "GhostZombie")
        {
            yield return StartCoroutine(GhostZombieRoutine());
        }
        else if (_zombieTag == "KendoZombie")
        {
            if (CheckPlayerInKendoRange() && _kendoDashTimer <= 0)
            {
                yield return StartCoroutine(KendoZombieDashRoutine());
            }
            else
            {
                yield return StartCoroutine(ZombieTrackAndMoveRoutine());
            }
        }
        else if (_zombieTag == "MouseZombie")
        {
            yield return StartCoroutine(MouseZombieRoutine());
        }
        else if (_zombieTag == "BigZombie" || _zombieTag == "CrazyZombie" || _zombieTag == "PowerZombie" || _zombieTag == "NormalZombie" || gameObject.CompareTag("Untagged"))
        {
            if ((CheckPlayerWithinFrontSteps() || CheckPlayerOverlap()) && _attackCooldownTimer <= 0)
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
            yield return StartCoroutine(ZombieTrackAndMoveRoutine());
        }

        _isAction = false;
    }

    private void AlignToGridCenter()
    {
        int fixedX = Mathf.RoundToInt(transform.position.x);
        int fixedY = Mathf.RoundToInt(transform.position.y);
        transform.position = new Vector3(fixedX, fixedY, 0);
    }

    private Vector3 GetZombieGridCenter()
    {
        return new Vector3(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 0);
    }

    private bool CheckPlayerInDetectionRange()
    {
        Vector3 zCenter = GetZombieGridCenter();
        int zX = Mathf.RoundToInt(zCenter.x);
        int zY = Mathf.RoundToInt(zCenter.y);
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        return (Mathf.Abs(pX - zX) <= detectionRangeX && Mathf.Abs(pY - zY) <= detectionRangeY);
    }

    // =================================================================
    // ⚔️ [공통형 / 카운터 대상 좀비용] 공격 감지 및 경고 패턴 (Big, Crazy, Power, Normal)
    // =================================================================
    private bool CheckPlayerWithinFrontSteps()
    {
        if (playerTransform == null) return false;
        
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);
        Vector3 zCenter = GetZombieGridCenter();

        for (int i = 1; i <= n_triggerFrontSteps; i++)
        {
            Vector3 checkPos = zCenter + (_currentDirection * i);
            int cX = Mathf.RoundToInt(checkPos.x);
            int cY = Mathf.RoundToInt(checkPos.y);

            if (pX == cX && pY == cY)
            {
                return true; 
            }
        }
        return false;
    }

    private bool CheckPlayerOverlap()
    {
        if (playerTransform == null) return false;
        Vector3 zCenter = GetZombieGridCenter();
        int zX = Mathf.RoundToInt(zCenter.x);
        int zY = Mathf.RoundToInt(zCenter.y);
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        return (zX == pX && zY == pY);
    }

    private bool CheckPlayerInMxKRange()
    {
        if (playerTransform == null) return false;
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        Vector3 centerAttackPos = GetAttackBoxCenter();

        float halfWidth = m_attackWidth / 2f;
        float halfHeight = k_attackHeight / 2f;

        float minX = centerAttackPos.x - halfWidth;
        float maxX = centerAttackPos.x + halfWidth;
        float minY = centerAttackPos.y - halfHeight;
        float maxY = centerAttackPos.y + halfHeight;

        if (_currentDirection == Vector3.left || _currentDirection == Vector3.right)
        {
            halfWidth = k_attackHeight / 2f;
            halfHeight = m_attackWidth / 2f;
            minX = centerAttackPos.x - halfWidth;
            maxX = centerAttackPos.x + halfWidth;
            minY = centerAttackPos.y - halfHeight;
            maxY = centerAttackPos.y + halfHeight;
        }

        return (pX >= minX && pX <= maxX && pY >= minY && pY <= maxY);
    }

    private Vector3 GetAttackBoxCenter()
    {
        Vector3 zCenter = GetZombieGridCenter();
        float offset = (k_attackHeight % 2 == 0) ? (k_attackHeight / 2f) - 0.5f : (k_attackHeight - 1) / 2f;
        Vector3 rawCenter = zCenter + (_currentDirection * offset);

        if (m_attackWidth % 2 == 0)
        {
            if (_currentDirection == Vector3.up || _currentDirection == Vector3.down)
                rawCenter.x += 0.5f;
            else
                rawCenter.y += 0.5f;
        }

        return rawCenter;
    }

    private IEnumerator AttackWarningRoutine()
    {
        _isWaitingToAttack = true;
        Vector3 warningCenterPos = GetAttackBoxCenter();
        
        if (warningSquarePrefab != null)
        {
            _currentWarningEffect = Instantiate(warningSquarePrefab, warningCenterPos, Quaternion.identity);
            if (_currentDirection == Vector3.up || _currentDirection == Vector3.down)
            {
                _currentWarningEffect.transform.localScale = new Vector3(m_attackWidth, k_attackHeight, 1);
            }
            else 
            {
                _currentWarningEffect.transform.localScale = new Vector3(k_attackHeight, m_attackWidth, 1);
            }
        }

        yield return new WaitForSeconds(1.5f);

        _isWaitingToAttack = false;
        if (_currentWarningEffect != null) Destroy(_currentWarningEffect);

        if (CheckPlayerInMxKRange() && _playerMovement != null && !_playerMovement.IsInvincible)
        {
            _playerMovement.OnGetHitByZombie(attackDamage);
        }

        _attackCooldownTimer = attackCooldown; 
    }

    // =================================================================
    // 💥 [SmallZombie] 전용 보라색 자폭 루틴
    // =================================================================
    private bool CheckPlayerInSmallTriggerRange()
    {
        if (playerTransform == null) return false;
        Vector3 zCenter = GetZombieGridCenter();
        int zX = Mathf.RoundToInt(zCenter.x);
        int zY = Mathf.RoundToInt(zCenter.y);
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        return (Mathf.Abs(pX - zX) <= 1 && Mathf.Abs(pY - zY) <= 1);
    }

    private IEnumerator SmallZombieExplosionRoutine()
    {
        _isExploding = true;
        float duration = 2.0f;
        float timer = 0f;
        float blinkInterval = 0.3f;

        Color originalColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        Color purpleColor = new Color(0.6f, 0f, 1f);

        while (timer < duration)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = (_spriteRenderer.color == originalColor) ? purpleColor : originalColor;
            }
            float progress = timer / duration;
            blinkInterval = Mathf.Lerp(0.3f, 0.05f, progress);

            yield return StartCoroutine(ZombieTrackAndMoveRoutine());
            timer += blinkInterval;
        }

        if (_spriteRenderer != null) _spriteRenderer.color = originalColor;
        TriggerExplosionDamage();
    }

    // =================================================================
    // 💣 [BombZombie] 전용 인간 인식 시 랜덤 로밍 및 빠른 빨간색 자폭 루틴
    // =================================================================
    private IEnumerator BombZombieExplosionRoutine()
    {
        _isExploding = true;
        float duration = 3.0f; 
        float timer = 0f;
        float blinkInterval = 0.4f;

        Color originalColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        float savedTimeToMove = timeToMove;

        while (timer < duration)
        {
            if (duration - timer <= 0.6f)
            {
                timeToMove = savedTimeToMove * 0.4f; 
                blinkInterval = 0.05f; 
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = (_spriteRenderer.color == originalColor) ? Color.red : originalColor;
            }

            yield return StartCoroutine(BombZombieRoamRoutine()); 
            timer += blinkInterval;
        }

        timeToMove = savedTimeToMove;
        if (_spriteRenderer != null) _spriteRenderer.color = originalColor;
        TriggerExplosionDamage();
    }

    private void TriggerExplosionDamage()
    {
        if (CheckPlayerInSmallExplosionArea() && _playerMovement != null && !_playerMovement.IsInvincible)
        {
            _playerMovement.OnGetHitByZombie(explosionDamage);
        }

        ZombieAI2_0[] allZombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        Vector3 myCenter = GetZombieGridCenter();

        foreach (ZombieAI2_0 otherZombie in allZombies)
        {
            if (otherZombie == this || otherZombie == null) continue;

            Vector3 otherCenter = otherZombie.GetZombieGridCenter();
            
            if (CheckPositionInSmallExplosionArea(myCenter, otherCenter))
            {
                otherZombie.OnGetHitByPlayer(explosionDamage, false);
            }
        }

        // 💡 [선택 세팅] 자폭 좀비들이 스스로 터졌을 때도 누적 점수에 반영하고 싶다면 주석을 해제하세요.
        // if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(defeatScore);

        Destroy(gameObject);
    }

    private bool CheckPlayerInSmallExplosionArea()
    {
        if (playerTransform == null) return false;
        return CheckPositionInSmallExplosionArea(GetZombieGridCenter(), playerTransform.position);
    }

    private bool CheckPositionInSmallExplosionArea(Vector3 centerPos, Vector3 targetPos)
    {
        int offsetX = Mathf.RoundToInt(targetPos.x) - Mathf.RoundToInt(centerPos.x);
        int offsetY = Mathf.RoundToInt(targetPos.y) - Mathf.RoundToInt(centerPos.y);

        int absOffsetY = Mathf.Abs(offsetY);
        if (absOffsetY > 2) return false; 

        int maxAllowedSubX = 2 - absOffsetY; 
        return Mathf.Abs(offsetX) <= maxAllowedSubX;
    }

    private IEnumerator BombZombieRoamRoutine()
    {
        Vector3 currentPos = GetZombieGridCenter();
        Vector3 randomDir = _directions[Random.Range(0, _directions.Length)];
        Vector3 targetPos = currentPos + randomDir;

        if (!IsTileBlocked(targetPos))
        {
            _currentDirection = randomDir;
            ChangeZombieSprite(randomDir); 

            float elapsedTime = 0;
            while (elapsedTime < timeToMove)
            {
                elapsedTime += Time.deltaTime;
                transform.position = Vector3.Lerp(currentPos, targetPos, elapsedTime / timeToMove);
                yield return null;
            }
            transform.position = targetPos;
            AlignToGridCenter();
            
        }
        else
        {
            yield return null;
        }
    }

    // =================================================================
    // 😡 [AngryZombie] 전용 분노 5단 대쉬 루틴
    // =================================================================
    private IEnumerator AngryZombieDashRoutine()
    {
        _isWaitingToAttack = true; 

        for (int i = 0; i < 5; i++)
        {
            if (_isGroggy) break;
            yield return StartCoroutine(AngryZombieTrackAndDashStepRoutine());
        }

        _isWaitingToAttack = false;

        if (!_isGroggy)
        {
            yield return new WaitForSeconds(1.0f); 
        }
    }

    private IEnumerator AngryZombieTrackAndDashStepRoutine()
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

        while (elapsedTime < angryDashTimeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origPos, destination, elapsedTime / angryDashTimeToMove);
            yield return null;
        }

        transform.position = destination;
        AlignToGridCenter(); 
      
    }

    // =================================================================
    // 🏃 [RunningZombie] 전용 가로 왕복 질주 루틴
    // =================================================================
    private IEnumerator RunningZombieRoutine()
    {
        Vector3 startPos = GetZombieGridCenter();
        _currentDirection = _runningToRight ? Vector3.right : Vector3.left;

        if (_spriteRenderer != null && spriteLeft != null)
        {
            _spriteRenderer.sprite = spriteLeft; 
            _spriteRenderer.flipX = !_runningToRight; 
        }

        Vector3 targetPos = startPos + (_currentDirection * runningSteps);

        _isWaitingToAttack = true;
        if (warningSquarePrefab != null)
        {
            Vector3 warningCenter = startPos + (_currentDirection * (runningSteps / 2f));
            _currentWarningEffect = Instantiate(warningSquarePrefab, warningCenter, Quaternion.identity);
            _currentWarningEffect.transform.localScale = new Vector3(runningSteps, 1, 1);
        }

        yield return new WaitForSeconds(1.0f); 

        _isWaitingToAttack = false;
        if (_currentWarningEffect != null) Destroy(_currentWarningEffect);

        _runningStepsRemaining = runningSteps;
        while (_runningStepsRemaining > 0)
        {
            if (_isGroggy)
            {
                yield return new WaitUntil(() => !_isGroggy);
                _currentDirection = _runningToRight ? Vector3.right : Vector3.left;
                if (_spriteRenderer != null) _spriteRenderer.flipX = !_runningToRight;
            }

            Vector3 currentPos = GetZombieGridCenter();
            Vector3 nextPos = currentPos + _currentDirection;

            if (!IsTileBlocked(nextPos))
            {
                float elapsedTime = 0;
                while (elapsedTime < runningTimeToMove)
                {
                    elapsedTime += Time.deltaTime;
                    transform.position = Vector3.Lerp(currentPos, nextPos, elapsedTime / runningTimeToMove);
                    yield return null;
                }
                transform.position = nextPos;
                AlignToGridCenter();
               
            }
            _runningStepsRemaining--;
        }

        _runningToRight = !_runningToRight;
        yield return new WaitForSeconds(3.0f);
    }

    // =================================================================
    // 👻 [GhostZombie] 전용 플레이어 위치 강제 순간이동 루틴
    // =================================================================
    private IEnumerator GhostZombieRoutine()
    {
        if (_ghostWaiting) yield break;

        Vector3 playerPos = new Vector3(Mathf.RoundToInt(playerTransform.position.x), Mathf.RoundToInt(playerTransform.position.y), 0f);

        if (warningSquarePrefab != null)
        {
            _currentWarningEffect = Instantiate(warningSquarePrefab, playerPos, Quaternion.identity);
            _currentWarningEffect.transform.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(1.0f); 

        if (_currentWarningEffect != null) Destroy(_currentWarningEffect);

        transform.position = playerPos;
        AlignToGridCenter();
    

        if (CheckPlayerOverlap() && _playerMovement != null && !_playerMovement.IsInvincible)
        {
            _playerMovement.OnGetHitByZombie(attackDamage);
        }

        _ghostWaiting = true;
        yield return new WaitForSeconds(4.0f);
        _ghostWaiting = false;
    }

    // =================================================================
    // 🐭 [MouseZombie] 전용 실험통 은신 및 기습 루틴
    // =================================================================
    private IEnumerator MouseZombieRoutine()
    {
        if (!_isRevealed)
        {
            _isRevealed = true;
            _isMouseInvincible = true;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
                if (labGlassSprite != null) _spriteRenderer.sprite = labGlassSprite; 
            }

            yield return new WaitForSeconds(1.0f); 

            _isMouseInvincible = false;
            ChangeZombieSprite(_currentDirection); 
        }

        yield return StartCoroutine(ZombieTrackAndMoveRoutine());
    }

   // =================================================================
    // ⚔️ [KendoZombie] 전용 정면 감지 및 고속 대쉬 베기 루틴
    // =================================================================
    private bool CheckPlayerInKendoRange()
    {
        if (playerTransform == null) return false;
        Vector3 zCenter = GetZombieGridCenter();
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        for (int i = 1; i <= kendoDashDistance; i++)
        {
            Vector3 checkPos = zCenter + (_currentDirection * i);
            if (pX == Mathf.RoundToInt(checkPos.x) && pY == Mathf.RoundToInt(checkPos.y)) return true;
        }
        return false;
    }

    // 💡 벽 충돌 여부를 확인하는 헬퍼 함수 (기존 플레이어 로직 참고)
    private bool IsTileBlockedByWall(Vector3 targetPos)
    {
        // 1. 투명벽 타일맵이 연결되어 있다면 체크
        if (transparentWallTilemap != null)
        {
            Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
            if (transparentWallTilemap.HasTile(cellPosition)) return true;
        }

        // 2. 만약 obstacleLayer(물리 레이어)도 사용 중이라면 Physics2D OverlapCircle 등으로 추가 체크 가능
        // Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.2f, obstacleLayer);
        // if (hit != null) return true;

        return false;
    }

    private IEnumerator KendoZombieDashRoutine()
    {
        Vector3 startPos = GetZombieGridCenter();
        Vector3 targetPos = startPos; 

        // 💡 [수정] 대시할 최대 거리만큼 한 칸씩 전진해보며 벽이 있는지 검사합니다.
        int actualDashDistance = 0;
        for (int i = 1; i <= kendoDashDistance; i++)
        {
            Vector3 nextCheckPos = startPos + (_currentDirection * i);
            
            // 전방에 벽이 있다면, 그 전 칸까지만 이동하고 루프를 탈출합니다.
            if (IsTileBlockedByWall(nextCheckPos))
            {
                break;
            }
            
            actualDashDistance = i; // 벽이 없다면 이동 가능한 거리 갱신
        }

        // 최종 목적지를 벽에 부딪히지 않는 안전한 좌표로 설정합니다.
        targetPos = startPos + (_currentDirection * actualDashDistance);

        ChangeZombieSprite(_currentDirection); 

        // 💡 [수정] 실제로 대시한 거리에 비례해서 대시 시간을 조절 (벽에 바로 막혔는데 세월아 네월아 움직이는 현상 방지)
        float adjustedDashTime = kendoDashTimeToMove;
        if (kendoDashDistance > 0)
        {
            adjustedDashTime = kendoDashTimeToMove * ((float)actualDashDistance / kendoDashDistance);
        }

        // 실제 이동 연산 (벽에 막힌 위치까지만 Lerp 이동)
        if (actualDashDistance > 0)
        {
            float elapsedTime = 0;
            while (elapsedTime < adjustedDashTime)
            {
                elapsedTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / adjustedDashTime);
                yield return null;
            }
        }
        
        transform.position = targetPos;
        AlignToGridCenter();
       
        // 공격 판정
        if ((CheckPlayerWithinFrontSteps() || CheckPlayerOverlap()) && _playerMovement != null && !_playerMovement.IsInvincible)
        {
            _playerMovement.OnGetHitByZombie(attackDamage * 1.5f);
        }

        _kendoDashTimer = kendoDashCooldown; 
        yield return new WaitForSeconds(0.5f);
    }
    // =================================================================
    // 🗺️ [공통 기능] BFS 기반 플레이어 추적 및 그리드 한 칸 이동 루틴
    // =================================================================
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

        while (elapsedTime < timeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origPos, destination, elapsedTime / timeToMove);
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

            if (current == target)
            {
                found = true;
                break;
            }

            foreach (Vector3 dir in _directions)
            {
                Vector3 next = current + dir;
                next = new Vector3(Mathf.RoundToInt(next.x), Mathf.RoundToInt(next.y), 0f);

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
            while (parentMap.ContainsKey(curr) && parentMap[curr] != start)
            {
                curr = parentMap[curr];
            }
            return (curr - start).normalized;
        }

        return Vector3.zero;
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap != null)
        {
            Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
            if (transparentWallTilemap.HasTile(cellPosition))
            {
                return true; 
            }
        }
        return false;
    }

    // =================================================================
    // 🎨 [공통 기능] 렌더링 및 플레이어 타격/카운터 처리 피드백
    // =================================================================
    private void ChangeZombieSprite(Vector3 direction)
    {
        if (_spriteRenderer == null || (_zombieTag == "MouseZombie" && _isMouseInvincible)) return;

        if (direction == Vector3.up && spriteUp != null)
        {
            _spriteRenderer.sprite = spriteUp; _spriteRenderer.flipX = false;
        }
        else if (direction == Vector3.down && spriteDown != null)
        {
            _spriteRenderer.sprite = spriteDown; _spriteRenderer.flipX = false;
        }
        else if (direction == Vector3.left && spriteLeft != null)
        {
            _spriteRenderer.sprite = spriteLeft; _spriteRenderer.flipX = false;
        }
        else if (direction == Vector3.right && spriteLeft != null)
        {
            _spriteRenderer.sprite = spriteLeft; _spriteRenderer.flipX = true; 
        }
    }

    public void OnGetHitByPlayer(float damage, bool isCounter)
    {
        if (_zombieTag == "BombZombie" && !_isExploding)
        {
            _bombZombieTriggered = true; 
            return; 
        }
        if (_zombieTag == "MouseZombie" && _isMouseInvincible) return;

        // 💡 [수정 대기안내] 원래 사용하시는 HealthBar 컴포넌트 내부에서 좀비의 hp가 0이 될 때 파괴처리를 유도하므로,
        // 좀비 사망을 확실하게 다루기 위해 HealthBar 스크립트 내부에서도 `ScoreManager.Instance.AddScore(defeatScore);`를 호출해 주는 것이 베스트입니다.
        // 현재 코드에서는 우선 피격 당했을 때 체력을 깎는 용도로 전달됩니다.
        if (_zombieTag == "SmallZombie" || _zombieTag == "GhostZombie" || _zombieTag == "KendoZombie")
        {
            if (_myHealthBar != null) _myHealthBar.TakeDamage(damage);
            return;
        }

        float finalDamage = damage;
        if (_isGroggy)
        {
            finalDamage = damage * 3f;
        }

        if (_myHealthBar != null)
        {
            _myHealthBar.TakeDamage(finalDamage);
        }

        if (isCounter && _isWaitingToAttack && !_isGroggy)
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            _isWaitingToAttack = false;
            if (_currentWarningEffect != null) Destroy(_currentWarningEffect);
            
            StartCoroutine(GroggyRoutine());
        }
    }

    private IEnumerator GroggyRoutine()
    {
        _isGroggy = true; 
        Color originalColor = _spriteRenderer.color;
        _spriteRenderer.color = Color.gray; 
        yield return new WaitForSeconds(3.0f);
        _spriteRenderer.color = originalColor;
        _isGroggy = false; 
        _attackCooldownTimer = attackCooldown; 
        _isAction = false;
    }

   
    // =================================================================
    // 📐 [디버그 기능] 기즈모 그리기 에디터 전용 로직
    // =================================================================
    private void DrawGizmosLogic()
    {
        Vector3 zCenter = GetZombieGridCenter();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(zCenter, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 1));
        
        if (_zombieTag == "SmallZombie" || _zombieTag == "BombZombie")
        {
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireCube(zCenter, new Vector3(3, 3, 1));

            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            for (int y = -2; y <= 2; y++)
            {
                int maxValidX = 2 - Mathf.Abs(y);
                for (int x = -maxValidX; x <= maxValidX; x++)
                {
                    Gizmos.DrawWireCube(zCenter + new Vector3(x, y, 0), new Vector3(0.9f, 0.9f, 1));
                }
            }
        }
        else
        {
            Vector3 centerAttackPos = GetAttackBoxCenter();
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(centerAttackPos, 0.15f);

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            if (_currentDirection == Vector3.up || _currentDirection == Vector3.down)
            {
                Gizmos.DrawWireCube(centerAttackPos, new Vector3(m_attackWidth, k_attackHeight, 1));
            }
            else
            {
                Gizmos.DrawWireCube(centerAttackPos, new Vector3(k_attackHeight, m_attackWidth, 1));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmosConstantly) DrawGizmosLogic();
    }

    private void OnDrawGizmos()
    {
        if (showGizmosConstantly) DrawGizmosLogic();
    }

    private void OnDestroy()
    {
        if (_currentWarningEffect != null)
        {
            Destroy(_currentWarningEffect);
        }
    }

    public void ForceStartChasing() { _isChasing = true; }
}