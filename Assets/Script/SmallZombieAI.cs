using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SmallZombie : MonoBehaviour
{
    public Transform playerTransform;
    private bool _isAction = false; 

    [Header("개발용 디버그 설정")]
    public bool showGizmosConstantly = true;

    [Header("좀비 고유 스탯 설정")]
    public float timeToMove = 0.3f;       
    public float explosionDamage = 30f; // 자폭 데미지  
    
    [Header("인식 범위 (추적 시작 조건)")]
    public int detectionRangeX = 4;       
    public int detectionRangeY = 4;       

    [Header("코드로 제어하는 투명벽 타일맵 (자동 연결됨)")]
    [SerializeField] private Tilemap transparentWallTilemap; 

    [Header("방향별 스프라이트 등록")]
    public Sprite spriteDefault; 
    public Sprite spriteUp;    
    public Sprite spriteDown;  
    public Sprite spriteLeft;  

    private SpriteRenderer _spriteRenderer; 
    private Vector3 _currentDirection = Vector3.down; 
    private bool _isChasing = false; 
    private bool _isExploding = false; // 자폭 시퀀스 가동 여부

    private HealthBar _myHealthBar;
    private PlayerMovement _playerMovement;

    private readonly Vector3[] _directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _myHealthBar = GetComponent<HealthBar>(); 
        UpdateGridSortingOrder();
    }

    void Start()
    {
        if (_spriteRenderer != null && spriteDefault != null)
        {
            _spriteRenderer.sprite = spriteDefault;
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
        // 자폭 시퀀스가 시작되면 기존의 추적/이동/오버랩 데미지 체크를 모두 중단합니다.
        if (_isExploding) return;

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

        // 인간이 좀비 기준 3x3칸(자기 칸 포함 사방 1칸 이내) 안에 들어왔는지 체크
        if (CheckPlayerInExplosionTriggerRange())
        {
            yield return StartCoroutine(ExplosionSequenceRoutine());
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

    // 좀비 기준 3x3 범위 내에 플레이어가 있는지 판단하는 함수
    private bool CheckPlayerInExplosionTriggerRange()
    {
        if (playerTransform == null) return false;

        Vector3 zCenter = GetZombieGridCenter();
        int zX = Mathf.RoundToInt(zCenter.x);
        int zY = Mathf.RoundToInt(zCenter.y);
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        // 중심으로부터 X, Y가 각각 1칸 이하로 떨어져 있으면 3x3 범위 내에 있음
        return (Mathf.Abs(pX - zX) <= 1 && Mathf.Abs(pY - zY) <= 1);
    }

    // 3초간 깜빡인 후 1-3-5-3-1 범위로 터지는 시퀀스
    private IEnumerator ExplosionSequenceRoutine()
    {
        _isExploding = true;
        float duration = 3.0f;
        float timer = 0f;
        float blinkInterval = 0.3f; // 깜빡임 간격 (시간이 갈수록 빨라지게 설정 가능)

        Color originalColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;

        // 3초간 깜빡이기
        while (timer < duration)
        {
            if (_spriteRenderer != null)
            {
                // 빨간색으로 깜빡이도록 연출
                _spriteRenderer.color = (_spriteRenderer.color == originalColor) ? Color.red : originalColor;
            }
            
            // 자폭 카운트다운 도중에도 서서히 깜빡임 속도가 빨라지도록 동적 조절
            float progress = timer / duration;
            blinkInterval = Mathf.Lerp(0.3f, 0.05f, progress);

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        if (_spriteRenderer != null) _spriteRenderer.color = originalColor;

        // 폭발 범위 계산 및 플레이어 타격 체크
        if (CheckPlayerInExplosionArea() && _playerMovement != null && !_playerMovement.IsInvincible)
        {
            _playerMovement.OnGetHitByZombie(explosionDamage);
        }

        // 폭발 이펙트 출력이나 사운드 플레이를 여기에 추가할 수 있습니다.
        Debug.Log($"{gameObject.name}가 1-3-5-3-1 범위로 자폭했습니다!");

        // 좀비 오브젝트 소멸
        Destroy(gameObject);
    }

    // 플레이어가 가로 기준 1-3-5-3-1 다이아몬드 폭발 범위에 있는지 체크
    private bool CheckPlayerInExplosionArea()
    {
        if (playerTransform == null) return false;

        Vector3 zCenter = GetZombieGridCenter();
        int zX = Mathf.RoundToInt(zCenter.x);
        int zY = Mathf.RoundToInt(zCenter.y);
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        // 좀비 중심과의 상대 좌표 계산
        int offsetX = pX - zX;
        int offsetY = pY - zY;

        // Y축 거리별로 허용되는 가로(X축) 폭 계산
        // Y가 0(중심선)일 때 가로 5칸 (즉, offsetX가 -2, -1, 0, 1, 2 가능 -> 절대값 <= 2)
        // Y가 1, -1일 때 가로 3칸 (즉, offsetX가 -1, 0, 1 가능 -> 절대값 <= 1)
        // Y가 2, -2일 때 가로 1칸 (즉, offsetX가 0만 가능 -> 절대값 <= 0)
        
        int absOffsetY = Mathf.Abs(offsetY);
        if (absOffsetY > 2) return false; // 세로로 2칸을 초과하면 범위 밖

        int maxAllowedSubX = 2 - absOffsetY; // 다이아몬드 범위 수학적 공식(|X| + |Y| <= 2)

        return Mathf.Abs(offsetX) <= maxAllowedSubX;
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

        while (elapsedTime < timeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origPos, destination, elapsedTime / timeToMove);
            yield return null;
        }

        transform.position = destination;
        AlignToGridCenter(); 
        UpdateGridSortingOrder();
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

    // 플레이어한테 공격 받았을 때 처리 (카운터 기능 삭제됨)
    public void OnGetHitByPlayer(float damage, bool isCounter)
    {
        if (_myHealthBar != null)
        {
            _myHealthBar.TakeDamage(damage);
        }
    }

    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;
        _spriteRenderer.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y);
    }

    private void DrawGizmosLogic()
    {
        Vector3 zCenter = GetZombieGridCenter();
        
        // 1. 인식 범위 디버그 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(zCenter, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 1));
        
        // 2. 자폭 트리거 범위 3x3 (주황색)
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(zCenter, new Vector3(3, 3, 1));

        // 3. 자폭 범위 1-3-5-3-1 다이아몬드 디버그 (빨간색 기즈모)
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

    private void OnDrawGizmosSelected()
    {
        if (!showGizmosConstantly) DrawGizmosLogic();
    }

    private void OnDrawGizmos()
    {
        if (showGizmosConstantly) DrawGizmosLogic();
    }

    public void ForceStartChasing() { _isChasing = true; }
}