using System.Collections;
using UnityEngine;

public class ZombieAIWithCounter : MonoBehaviour
{
    public Transform playerTransform;
    private bool _isAction = false; 

    [Header("이동 설정")]
    [SerializeField] private float timeToMove = 0.3f; // 1칸 이동에 걸리는 시간 0.3초 고정

    [Header("방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    private SpriteRenderer _spriteRenderer; 

    [Header("카운터 시스템 설정")]
    public GameObject warningSquarePrefab; 
    private Vector3 _currentDirection = Vector3.down; 
    private bool _isWaitingToAttack = false; 
    private Coroutine _attackCoroutine;
    private GameObject _currentWarningEffect; 

    [Header("공격 쿨타임 설정")]
    [SerializeField] private float attackCooldown = 5.0f; // 쿨타임 5초 고정
    private float _attackCooldownTimer = 0f; 

    void Awake()
    {
        // [오타 수정 완료] 중복 코드를 지우고 정상적으로 컴포넌트를 가져옵니다.
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 게임 시작할 때 첫 자리에서 레이어 정렬 한번 실행
        UpdateGridSortingOrder();
    }

    void Update()
    {
        if (_attackCooldownTimer > 0) _attackCooldownTimer -= Time.deltaTime;

        if (!_isAction && playerTransform != null)
        {
            StartCoroutine(ZombieMainRoutine());
        }
    }

    private IEnumerator ZombieMainRoutine()
    {
        _isAction = true;
        AlignToGridCenter(); 

        if (CheckPlayerInFront() && _attackCooldownTimer <= 0)
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

    private bool CheckPlayerInFront()
    {
        int pX = Mathf.RoundToInt(playerTransform.position.x);
        int pY = Mathf.RoundToInt(playerTransform.position.y);

        Vector3 frontOneStep = transform.position + _currentDirection;
        Vector3 frontTwoStep = transform.position + (_currentDirection * 2);

        int f1X = Mathf.RoundToInt(frontOneStep.x);
        int f1Y = Mathf.RoundToInt(frontOneStep.y);
        int f2X = Mathf.RoundToInt(frontTwoStep.x);
        int f2Y = Mathf.RoundToInt(frontTwoStep.y);

        return (pX == f1X && pY == f1Y) || (pX == f2X && pY == f2Y);
    }

    private IEnumerator AttackWarningRoutine()
    {
        _isWaitingToAttack = true;

        // 정확히 정면 1.0칸 앞 타일 정중앙에 장판 소환
        Vector3 warningPos = transform.position + _currentDirection;
        
        if (warningSquarePrefab != null)
        {
            _currentWarningEffect = Instantiate(warningSquarePrefab, warningPos, Quaternion.identity);
            
            if (_currentDirection == Vector3.left || _currentDirection == Vector3.right)
                _currentWarningEffect.transform.rotation = Quaternion.Euler(0, 0, 90);
        }

        Debug.Log("좀비 카운터 대기 시작 (1.5초)");
        yield return new WaitForSeconds(1.5f);

        _isWaitingToAttack = false;
        if (_currentWarningEffect != null) Destroy(_currentWarningEffect);

        Debug.Log("좀비 공격 성공!");
        _attackCooldownTimer = attackCooldown; 
    }

    private IEnumerator ZombieTrackAndMoveRoutine()
    {
        int dx = Mathf.RoundToInt(playerTransform.position.x - transform.position.x);
        int dy = Mathf.RoundToInt(playerTransform.position.y - transform.position.y);

        if (dx == 0 && dy == 0) yield break;

        Vector3 moveDirection = Vector3.zero;
        int absX = Mathf.Abs(dx);
        int absY = Mathf.Abs(dy);

        if (absX >= absY) moveDirection = (dx > 0) ? Vector3.right : Vector3.left;
        else moveDirection = (dy > 0) ? Vector3.up : Vector3.down;

        if (moveDirection != Vector3.zero)
        {
            _currentDirection = moveDirection;
            ChangeZombieSprite(moveDirection);
        }

        float elapsedTime = 0;
        Vector3 origPos = transform.position;
        Vector3 targetPos = origPos + moveDirection;

        while (elapsedTime < timeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(origPos, targetPos, elapsedTime / timeToMove);
            yield return null;
        }

        transform.position = targetPos;

        // [레이어 정렬] 이동이 끝나 그리드 타일에 안착하면 레이어 순서 갱신
        UpdateGridSortingOrder();
    }

    private void ChangeZombieSprite(Vector3 direction)
    {
        if (_spriteRenderer == null) return;
        if (direction == Vector3.up && spriteUp != null) _spriteRenderer.sprite = spriteUp;
        else if (direction == Vector3.down && spriteDown != null) _spriteRenderer.sprite = spriteDown;
        else if (direction == Vector3.left && spriteLeft != null) _spriteRenderer.sprite = spriteLeft;
        else if (direction == Vector3.right && spriteRight != null) _spriteRenderer.sprite = spriteRight;
    }

    public void OnGetHitByPlayer()
    {
        if (_isWaitingToAttack)
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
            _isWaitingToAttack = false;
            
            if (_currentWarningEffect != null) 
            {
                Destroy(_currentWarningEffect);
            }

            StartCoroutine(GroggyRoutine());
        }
    }

    private IEnumerator GroggyRoutine()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr.color;
        sr.color = Color.gray; 

        Debug.Log("카운터 성공! 좀비 그로기 기절 (3초)");
        yield return new WaitForSeconds(3.0f);

        sr.color = originalColor;
        _attackCooldownTimer = attackCooldown; 
        _isAction = false;
    }

    // 그리드 정수 Y 좌표를 기반으로 레이어 순서를 정하는 함수
    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;

        int currentGridY = Mathf.RoundToInt(transform.position.y);
        _spriteRenderer.sortingOrder = 100 - currentGridY;
    }
}