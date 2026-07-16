using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAi : MonoBehaviour
{
    [Header("UI 및 연출 설정")]
    public GameObject targetCanvas2; 
    public Sprite spriteDefault, spriteRoar, spriteUp, spriteDown, spriteLeft;
    private SpriteRenderer _spriteRenderer;

    [Header("설정 및 프리팹")]
    public float moveSpeed = 0.2f;
    [SerializeField] private GameObject warningTilePrefab;

    private Transform _playerTransform;
    private bool _isStunned = false;
    private bool _isBossStarted = false; // 보스 시작 상태 관리

    void Awake() 
    { 
        _spriteRenderer = GetComponent<SpriteRenderer>(); 
        // 시작 시 보스 숨김
        gameObject.SetActive(false); 
        if (targetCanvas2 != null) targetCanvas2.SetActive(false);
    }

    // 플레이어가 보스 방 트리거에 진입했을 때 실행
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !_isBossStarted)
        {
            _isBossStarted = true;
            _playerTransform = collision.transform;
            if (targetCanvas2 != null) targetCanvas2.SetActive(true);
            StartCoroutine(BossPatternLoop());
        }
    }

    void Update()
    {
        if (_spriteRenderer != null) _spriteRenderer.sortingOrder = -(Mathf.RoundToInt(transform.position.y));
    }

    private Vector3 ClampPositionToField(Vector3 pos) => 
        new Vector3(Mathf.Clamp(pos.x, 9f, 50f), Mathf.Clamp(pos.y, 140f, 168f), 0);

    private IEnumerator BossPatternLoop()
    {
        while (true)
        {
            if (!_isStunned)
            {
                for (int i = 0; i < 2; i++) 
                {
                    yield return StartCoroutine(ExecutePattern(Random.Range(3, 7)));
                    yield return new WaitForSeconds(0.2f); // 루프 안정성을 위한 짧은 대기
                }
                yield return StartCoroutine(Pattern_CounterAttack());
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator ExecutePattern(int pattern)
    {
        switch (pattern)
        {
            case 3: yield return StartCoroutine(Pattern_4DashAttack()); break;
            case 4: yield return StartCoroutine(Pattern_5Kung()); break;
            case 5: yield return StartCoroutine(Pattern_TeleportAttack()); break;
            case 6: yield return StartCoroutine(Pattern_BerserkDash()); break;
        }
    }

    private IEnumerator Pattern_CounterAttack()
    {
        transform.position = ClampPositionToField(new Vector3(30, 154, 0));
        SetRoarSprite();
        GameObject warning = Instantiate(warningTilePrefab, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(1.5f);
        Destroy(warning);
        _isStunned = true; yield return new WaitForSeconds(5.0f); _isStunned = false;
        SetDefaultSprite();
    }

    private IEnumerator Pattern_4DashAttack()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 dir = (_playerTransform.position - transform.position).normalized;
            Vector3 target = ClampPositionToField(transform.position + (dir * 3));
            yield return StartCoroutine(MoveToPosWithDamage(target, 0.2f, 30f));
        }
    }

    private IEnumerator Pattern_5Kung()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 center = new Vector3(Random.Range(16, 44), Random.Range(147, 162), 0);
            yield return new WaitForSeconds(2.0f);
            if (_playerTransform != null && Vector3.Distance(_playerTransform.position, center) < 3.5f)
                _playerTransform.GetComponent<PlayerMovement>().TakeDamage(30f);
            StartCoroutine(ShakeCamera(0.2f, 0.3f));
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator Pattern_TeleportAttack()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 dir = _playerTransform.GetComponent<PlayerMovement>().lookDirection;
            transform.position = ClampPositionToField(_playerTransform.position + (dir * 5));
            if (_playerTransform != null && Vector3.Distance(_playerTransform.position, transform.position) < 3.0f)
                _playerTransform.GetComponent<PlayerMovement>().TakeDamage(30f);
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator Pattern_BerserkDash()
    {
        _spriteRenderer.color = Color.red;
        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(MoveToPosWithDamage(new Vector3(_playerTransform.position.x, transform.position.y, 0), 0.3f, 30f));
            yield return StartCoroutine(MoveToPosWithDamage(new Vector3(transform.position.x, _playerTransform.position.y, 0), 0.3f, 30f));
        }
        _spriteRenderer.color = Color.white;
    }

    private IEnumerator MoveToPosWithDamage(Vector3 target, float speed, float damage)
    {
        Vector3 start = transform.position;
        float elapsed = 0;
        while (elapsed < speed) 
        { 
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, ClampPositionToField(target), elapsed / speed); 
            if (_playerTransform != null && Vector3.Distance(transform.position, _playerTransform.position) < 0.5f)
                _playerTransform.GetComponent<PlayerMovement>().TakeDamage(damage);
            yield return null; 
        }
        transform.position = ClampPositionToField(target);
    }

    private IEnumerator ShakeCamera(float d, float m)
    {
        Vector3 orig = Camera.main.transform.localPosition;
        float e = 0;
        while (e < d) { Camera.main.transform.localPosition = orig + (Vector3)Random.insideUnitCircle * m; e += Time.deltaTime; yield return null; }
        Camera.main.transform.localPosition = orig;
    }

    private void SetRoarSprite() => _spriteRenderer.sprite = spriteRoar;
    private void SetDefaultSprite() => _spriteRenderer.sprite = spriteDefault;
}