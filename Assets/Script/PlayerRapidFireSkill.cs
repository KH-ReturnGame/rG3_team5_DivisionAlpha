//--------------필독------------------
//////////////////////
//     public Vector3 GetLookDirection()
//     {
//         return _lookDirection;
//     }
//이거 플레이어 무브먼트에 추가할 것
///////////////////
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRapidFireSkill : MonoBehaviour
{
    [Header("참조 스크립트")]
    [SerializeField] private PlayerMovement playerMovement; // 플레이어 방향(_lookDirection) 참조용

    [Header("화살 외형 및 연사 설정")]
    [SerializeField] private Sprite arrowSprite;           // 화살 스프라이트
    public float fireRate = 0.25f;                          // 연사 속도 (주기: 낮을수록 빠름, 0.15초당 1발)
    public float arrowDamage = 10f;                          // 화살 한 발당 데미지
    public float arrowDuration = 0.25f;                     // 화살이 날아가는 총 시간
    public int arrowRange = 8;                              // 화살 사거리 (타일 수)

    private float _fireCooldownTimer = 0f;
    private string _sortingLayerName = "Default";
    private int _sortingOrder = 0;

    // 오브젝트 풀링 (가비지 컬렉션 방지 및 성능 최적화)
    private Queue<GameObject> _arrowPool = new Queue<GameObject>();
    private const int INITIAL_POOL_SIZE = 15;

    void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        // 플레이어 정렬 레이어 기준으로 설정 복사
        SpriteRenderer playerSr = GetComponent<SpriteRenderer>();
        if (playerSr != null)
        {
            _sortingLayerName = playerSr.sortingLayerName;
            _sortingOrder = playerSr.sortingOrder + 5; 
        }

        InitializePool();
    }

    void Update()
    {
        if (UpgradeManager.isGamePaused) return;

        // 쿨타임 타이머 진행
        if (_fireCooldownTimer > 0)
        {
            _fireCooldownTimer -= Time.deltaTime;
        }

        // 새 Input System 기준으로 Keyboard의 'N' 키가 꾹 눌려있는지 실시간 검사
        if (Keyboard.current != null && Keyboard.current.nKey.isPressed)
        {
            // 쿨타임이 끝났다면 이동을 멈추지 않고(움직이면서) 화살 발사
            if (_fireCooldownTimer <= 0)
            {
                FireArrow();
            }
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            GameObject arrow = new GameObject("SkillArrow_Pooled", typeof(SpriteRenderer));
            SpriteRenderer sr = arrow.GetComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder = _sortingOrder;
            
            arrow.transform.SetParent(transform);
            arrow.SetActive(false);
            _arrowPool.Enqueue(arrow);
        }
    }

    private GameObject GetPooledArrow()
    {
        if (_arrowPool.Count > 0)
        {
            GameObject arrow = _arrowPool.Dequeue();
            return arrow;
        }
        else
        {
            GameObject arrow = new GameObject("SkillArrow_Pooled", typeof(SpriteRenderer));
            SpriteRenderer sr = arrow.GetComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.sortingLayerName = _sortingLayerName;
            sr.sortingOrder = _sortingOrder;
            arrow.transform.SetParent(transform);
            return arrow;
        }
    }

    private void ReturnToPool(GameObject arrow)
    {
        arrow.SetActive(false);
        _arrowPool.Enqueue(arrow);
    }

    private void FireArrow()
    {
        _fireCooldownTimer = fireRate;

        // 플레이어가 현재 바라보고 있는 방향 실시간 획득 (이동 중에도 자연스럽게 반영됨)
        Vector3 lookDir = GetPlayerLookDirection();

        // 1. 화살 시각 효과 오브젝트 생성 및 날리기 연출
        GameObject arrowObj = GetPooledArrow();
        StartCoroutine(ArrowMoveRoutine(arrowObj, lookDir));

        // 2. 타일 기반 범위 내의 좀비 감지 및 피격 판정
        int myX = Mathf.RoundToInt(transform.position.x);
        int myY = Mathf.RoundToInt(transform.position.y);
        List<Vector2Int> targetTiles = GetFrontTiles(myX, myY, arrowRange, lookDir);

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
                    // 이동 중에 여러 대를 맞춰도 정밀하게 일반 피격 데미지 적용
                    zombie.OnGetHitByPlayer(arrowDamage, false); 
                    break;
                }
            }
        }
    }

    private IEnumerator ArrowMoveRoutine(GameObject arrowObj, Vector3 lookDir)
    {
        // 발사 순간의 플레이어 위치에서 시작
        Vector3 startPos = transform.position; 
        arrowObj.transform.position = startPos;

        // 방향에 맞춰 화살 스프라이트 회전
        if (lookDir == Vector3.left) arrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (lookDir == Vector3.right) arrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (lookDir == Vector3.up) arrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        else if (lookDir == Vector3.down) arrowObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        arrowObj.SetActive(true);

        // 플레이어가 이동 중이므로 화살은 발사되었을 당시의 독립적인 직선 목표 좌표(endPos)로 날아갑니다.
        Vector3 endPos = startPos + (lookDir * arrowRange);
        float elapsed = 0f;

        while (elapsed < arrowDuration)
        {
            elapsed += Time.deltaTime;
            arrowObj.transform.position = Vector3.Lerp(startPos, endPos, elapsed / arrowDuration);
            yield return null;
        }

        ReturnToPool(arrowObj);
    }

    private List<Vector2Int> GetFrontTiles(int myX, int myY, int range, Vector3 lookDir)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        Vector2Int directionInt = new Vector2Int(Mathf.RoundToInt(lookDir.x), Mathf.RoundToInt(lookDir.y));

        for (int i = 1; i <= range; i++)
        {
            tiles.Add(new Vector2Int(myX + directionInt.x * i, myY + directionInt.y * i));
        }
        return tiles;
    }

    private Vector3 GetPlayerLookDirection()
    {
        if (playerMovement != null)
        {
            return playerMovement.GetLookDirection(); 
        }
        return Vector3.down;
    }
}