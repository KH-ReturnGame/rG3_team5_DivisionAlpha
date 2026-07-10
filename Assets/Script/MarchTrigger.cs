using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MarchTrigger : MonoBehaviour
{
    [Header("[ 진격선 영역 설정 ]")]
    [Tooltip("진격선의 가로 칸 수 (X축 타일 수)")]
    public int width = 3;
    [Tooltip("진격선의 세로 칸 수 (Y축 타일 수)")]
    public int height = 1;
    [Tooltip("타일 한 칸의 크기 (기본값: 1)")]
    public float tileSize = 1f;

    [Header("[ 좀비 소환 위치 (월드 좌표) ]")]
    [Tooltip("좀비가 소환될 정확한 X, Y 좌표를 입력하세요.")]
    public Vector2 spawnPosition;

    [System.Serializable]
    public struct ZombieSpawnData
    {
        [Tooltip("소환할 좀비 프리팹")]
        public GameObject zombiePrefab;
        [Tooltip("이전 좀비가 스폰된 후, 이번 좀비가 나오기까지의 대기 시간 (초)")]
        public float delayBeforeSpawn;
    }

    [Header("[ 소환할 좀비 목록 ]")]
    [Tooltip("인스펙터에서 +를 눌러 생성 순서대로 좀비와 대기 시간을 설정하세요.")]
    public List<ZombieSpawnData> zombieList = new List<ZombieSpawnData>();

    private BoxCollider2D boxCollider;
    private bool isTriggered = false; // 진격이 한 번만 울리도록 체크하는 변수

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        
        // 밟아서 작동해야 하므로 이 트리거를 활성화
        boxCollider.isTrigger = true;

        // 게임 시작 시 설정한 칸수에 맞춰 콜라이더 크기를 최종 동기화
        UpdateColliderSize();
    }

    private void Start()
    {
        // 만약 진격선 위치를 잡기 위해 오브젝트에 SpriteRenderer를 붙여두었다면,
        // 게임이 시작될 때 자동으로 인게임에서 보이지 않도록 끕니다.
        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            spriteRenderer.enabled = false;
        }
    }

    // 인스펙터에서 값이 수정될 때 에디터(Scene 뷰)에 실시간으로 반영되도록 함
    private void OnValidate()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        UpdateColliderSize();
    }

    // 가로, 세로 타일 수에 맞게 콜라이더의 크기를 조절하는 함수
    private void UpdateColliderSize()
    {
        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(width * tileSize, height * tileSize);
        }
    }

    // 플레이어가 진격선을 밟았을 때 실행되는 함수
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 진격이 울렸거나, 밟은 오브젝트의 태그가 "Player"가 아니면 통과
        if (isTriggered || !collision.CompareTag("Player")) return;

        // 진격선 즉시 비활성화 (단 한 번만 작동)
        isTriggered = true; 

        // 좀비 연속 소환 코루틴 시작
        StartCoroutine(SpawnZombieRoutine());
    }

    // 설정된 시간차를 두고 좀비를 소환하는 로직
    private IEnumerator SpawnZombieRoutine()
    {
        foreach (var zombieData in zombieList)
        {
            if (zombieData.zombiePrefab == null) continue;

            // 대기 시간이 설정되어 있다면 그만큼 기다린 후 소환
            if (zombieData.delayBeforeSpawn > 0f)
            {
                yield return new WaitForSeconds(zombieData.delayBeforeSpawn);
            }

            // 지정된 맵 좌표(spawnPosition)에 좀비 생성
            Instantiate(zombieData.zombiePrefab, spawnPosition, Quaternion.identity);
        }
    }

    // 에디터(Scene 뷰)에서만 영역을 시각적으로 보여주는 기능 (Game 뷰나 빌드본에선 안 보임)
    private void OnDrawGizmos()
    {
        // 1. 진격선 영역 그리기 (초록색 반투명 네모박스)
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Vector3 triggerSize = new Vector3(width * tileSize, height * tileSize, 0.1f);
        Gizmos.DrawCube(transform.position, triggerSize);

        // 진격선 테두리선 (선명한 초록색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, triggerSize);

        // 2. 좀비가 소환될 좌표 시각화 (빨간색 조준점 모양)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnPosition, 0.25f);
        Gizmos.DrawLine(new Vector3(spawnPosition.x - 0.4f, spawnPosition.y, 0f), new Vector3(spawnPosition.x + 0.4f, spawnPosition.y, 0f));
        Gizmos.DrawLine(new Vector3(spawnPosition.x, spawnPosition.y - 0.4f, 0f), new Vector3(spawnPosition.x, spawnPosition.y + 0.4f, 0f));

        // 3. 진격선 중심과 좀비 소환 위치를 노란 선으로 연결 (어느 선을 밟으면 어디서 나오는지 한눈에 보기 위함)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, spawnPosition);
    }
}