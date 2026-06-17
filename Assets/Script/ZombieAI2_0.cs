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
    public float timeToMove = 0.3f;       
    public float attackDamage = 10f;      
    
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

    public bool IsWaitingToAttack => _isWaitingToAttack;

    private HealthBar _myHealthBar;
    private PlayerMovement _playerMovement;

    // 상하좌우 이동 방향 정의 (그리드 한 칸씩)
    private readonly Vector3[] _directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };

    public Vector3 GetCurrentDirection()
    {
        return _currentDirection; 
    }

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _myHealthBar = GetComponent<HealthBar>(); 
        UpdateGridSortingOrder();
    }

    void Start()
    {
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
        if (_attackCooldownTimer > 0) _attackCooldownTimer -= Time.deltaTime;

        if (playerTransform != null && _playerMovement != null)
        {
            if (CheckPlayerOverlap() && !_playerMovement.IsInvincible)
            {
                _playerMovement.OnGetHitByZombie(attackDamage);
            }
        }

        if (!_isAction && playerTransform != null)
        {
            if (!_isChasing)
            {
                _isChasing = CheckPlayerInDetectionRange();
            }

            if (_isChasing)
            {
                StartCoroutine(ZombieMainRoutine());
            }
        }
    }

    private IEnumerator ZombieMainRoutine()
    {
        _isAction = true;
        
        AlignToGridCenter(); 

        if ((CheckPlayerWithinFrontSteps() || CheckPlayerOverlap()) && _attackCooldownTimer <= 0)
        {
            _attackCoroutine = StartCoroutine(AttackWarningRoutine());
            yield return _attackCoroutine;
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

    // 💡 [핵심 변경] 와리가리를 완벽히 파쇄하는 BFS 최단거리 추적 루틴
    private IEnumerator ZombieTrackAndMoveRoutine()
    {
        Vector3 startPos = GetZombieGridCenter();
        Vector3 targetPos = new Vector3(Mathf.RoundToInt(playerTransform.position.x), Mathf.RoundToInt(playerTransform.position.y), 0f);

        if (startPos == targetPos) yield break;

        // BFS 알고리즘을 통해 투명벽이 없는 최단거리 다음 이동 방향 검색
        Vector3 nextMoveDirection = FindNextStepBFS(startPos, targetPos);

        // 만약 길이 완전히 막혀서 갈 곳이 없다면 제자리에 대기
        if (nextMoveDirection == Vector3.zero) yield break;

        _currentDirection = nextMoveDirection;
        ChangeZombieSprite(nextMoveDirection);

        float elapsedTime = 0;
        Vector3 origPos = transform.position;
        Vector3 destination = origPos + nextMoveDirection;

        while (elapsedTime < timeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origPos, destination, elapsedTime / timeToMove);
            yield return null;
        }

        transform.position = destination;
        UpdateGridSortingOrder();
    }

    // 💡 투명벽 타일맵을 피해 목적지까지 가는 '최단 경로의 첫걸음 방향'을 찾아내는 BFS 함수
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
                // 그리드 단위 정수 좌표화
                next = new Vector3(Mathf.RoundToInt(next.x), Mathf.RoundToInt(next.y), 0f);

                if (!visited.Contains(next))
                {
                    // 목적지 타일이거나 혹은 투명벽 타일맵에 막혀있지 않은 길인 경우만 탐색 확장
                    if (next == target || !IsTileBlocked(next))
                    {
                        visited.Add(next);
                        parentMap[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
        }

        // 경로를 찾았다면 목적지에서부터 거꾸로 추적하여 '시작점 바로 다음 칸'을 도출
        if (found)
        {
            Vector3 curr = target;
            while (parentMap.ContainsKey(curr) && parentMap[curr] != start)
            {
                curr = parentMap[curr];
            }
            return (curr - start).normalized;
        }

        return Vector3.zero; // 길이 완전히 막힌 경우
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

    private void ChangeZombieSprite(Vector3 direction)
    {
        if (_spriteRenderer == null) return;

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
        float finalDamage = damage;
        if (_isGroggy)
        {
            finalDamage = damage * 3f;
            Debug.Log($"{gameObject.name} 그로기 지속 피격! 3배 대미지: {finalDamage}");
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

    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;
        _spriteRenderer.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y);
    }

    private void DrawGizmosLogic()
    {
        Vector3 zCenter = GetZombieGridCenter();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(zCenter, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 1));
        
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