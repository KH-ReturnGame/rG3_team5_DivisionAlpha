using System.Collections;
using System.Collections.Generic;
using UnityEngine;
<<<<<<< HEAD
using UnityEngine.UI; // 💡 UI 컴포넌트(Text 또는 Image)를 제어하기 위해 필수 추가!

// 💡 인스펙터 창에서 좀비와 대기 시간을 직관적으로 묶어서 세팅하기 위한 클래스
[System.Serializable]
public class ZombieSpawnData
{
    [Tooltip("소환할 좀비의 프리팹 원본")]
    public ZombieAI2_0 zombiePrefab;
    
    [Tooltip("이 좀비가 소환된 후, 다음 좀비가 나올 때까지 대기할 시간 (초)")]
    public float nextSpawnDelay = 1.0f;
}

public class MarchTrigger : MonoBehaviour
{
    [Header("진격 라인 범위 설정")]
    public int triggerWidth = 1;
    public int triggerHeight = 4;

    [Header("📍 고정 소환 좌표 (X, Y)")]
    [Tooltip("모든 좀비가 태어날 단 하나의 고정 좌표입니다.")]
    public Vector2Int spawnCoordinate;

    [Header("🧟 진격 스폰 시퀀스 설정")]
    [Tooltip("소환할 좀비 종류와 다음 좀비까지의 딜레이를 순서대로 추가하세요.")]
    public List<ZombieSpawnData> spawnSequence = new List<ZombieSpawnData>();

    [Header("📢 진격 알림 UI 설정")]
    [Tooltip("화면에 표시할 진격 알림 UI 오브젝트를 넣어주세요.")]
    public GameObject marchingAlertUI;
    
    [Tooltip("진격 알림 UI를 화면에 몇 초 동안 띄울지 설정합니다.")]
    public float uiDisplayTime = 3.0f; // 💡 형님의 요청대로 기본값 3초 세팅!

    [Tooltip("텍스트나 이미지가 깜빡거리는 속도 (낮을수록 천천히, 높을수록 빠르게)")]
    public float blinkSpeed = 10f;

    [Header("디버그용 설정")]
    public Color triggerColor = new Color(1f, 0.5f, 0f, 0.4f);

    private Transform _playerTransform;
    private bool _hasTriggered = false;

    // UI 깜빡임 연출을 제어하기 위한 컴포넌트 변수
    private Text _alertText;
    private Image _alertImage;
    private CanvasGroup _alertCanvasGroup;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;

        // UI 내부 컴포넌트 미리 캐싱 작업
        if (marchingAlertUI != null)
        {
            _alertText = marchingAlertUI.GetComponentInChildren<Text>();
            _alertImage = marchingAlertUI.GetComponent<Image>();
            _alertCanvasGroup = marchingAlertUI.GetComponent<CanvasGroup>();
            
            // 시작할 때는 UI 꺼두기
            marchingAlertUI.SetActive(false);
        }
    }

    void Update()
    {
        if (_hasTriggered || _playerTransform == null) return;

        if (CheckPlayerInTriggerRange())
        {
            _hasTriggered = true; 
            StartCoroutine(MarchCustomSpawnRoutine()); 
        }
    }

    private IEnumerator MarchCustomSpawnRoutine()
    {
        Debug.Log($"🚨 진격 발동! 고정 좌표 ({spawnCoordinate.x}, {spawnCoordinate.y}) 에서 순차 스폰을 시작합니다.");

        // 라인 밟자마자 UI 켜기
        if (marchingAlertUI != null)
        {
            marchingAlertUI.SetActive(true);
            // 💡 3초 동안 깜빡이다가 꺼지는 전용 UI 연출 코루틴 실행
            StartCoroutine(FlashAndHideAlertUI());
        }

        Vector3 spawnPos = new Vector3(spawnCoordinate.x, spawnCoordinate.y, 0f);

        // 좀비 소환 시퀀스는 UI 깜빡임과 상관없이 뒤에서 원래대로 돌아감
        for (int i = 0; i < spawnSequence.Count; i++)
        {
            ZombieSpawnData data = spawnSequence[i];

            if (data.zombiePrefab != null)
            {
                ZombieAI2_0 newZombie = Instantiate(data.zombiePrefab, spawnPos, Quaternion.identity);
                newZombie.ForceStartChasing();
                Debug.Log($"🧟 [{i + 1}번째] {data.zombiePrefab.name} 생성 완료!");
            }
            else
            {
                Debug.LogWarning($"⚠️ [{i + 1}번째] 스폰 데이터에 좀비 프리팹이 비어있습니다.");
            }

            if (i < spawnSequence.Count - 1)
            {
                yield return new WaitForSeconds(data.nextSpawnDelay);
            }
        }
    }

    // 💡 [핵심 연출] 3초 동안 깜빡깜빡 거리다가 사라지는 마법의 루틴
    private IEnumerator FlashAndHideAlertUI()
    {
        float timer = 0f;

        // 원본 색상들 미리 기억
        Color? originalTextColor = _alertText != null ? _alertText.color : (Color?)null;
        Color? originalImageColor = _alertImage != null ? _alertImage.color : (Color?)null;

        while (timer < uiDisplayTime)
        {
            timer += Time.deltaTime;

            // Sin 함수를 써서 0에서 1 사이를 부드럽게 왔다갔다하는 알파값 계산
            float alpha = (Mathf.Sin(timer * blinkSpeed) + 1f) / 2f;

            // 1방식. 오브젝트에 CanvasGroup이 붙어있다면 통째로 깜빡이기 (가장 추천)
            if (_alertCanvasGroup != null)
            {
                _alertCanvasGroup.alpha = alpha;
            }
            else
            {
                // 2방식. 만약 CanvasGroup이 없으면 텍스트나 이미지의 알파값을 개별 조절
                if (_alertText != null && originalTextColor.HasValue)
                {
                    _alertText.color = new Color(originalTextColor.Value.r, originalTextColor.Value.g, originalTextColor.Value.b, alpha);
                }
                if (_alertImage != null && originalImageColor.HasValue)
                {
                    _alertImage.color = new Color(originalImageColor.Value.r, originalImageColor.Value.g, originalImageColor.Value.b, alpha);
                }
            }

            yield return null;
        }

        // 3초 종료 후 원래 색상 복구 및 UI 완전 비활성화
        if (_alertText != null && originalTextColor.HasValue) _alertText.color = originalTextColor.Value;
        if (_alertImage != null && originalImageColor.HasValue) _alertImage.color = originalImageColor.Value;
        if (_alertCanvasGroup != null) _alertCanvasGroup.alpha = 1f;

        marchingAlertUI.SetActive(false);
    }

    private bool CheckPlayerInTriggerRange()
    {
        int pX = Mathf.RoundToInt(_playerTransform.position.x);
        int pY = Mathf.RoundToInt(_playerTransform.position.y);

        Vector3 centerPos = transform.position;
        float halfWidth = triggerWidth / 2f;
        float halfHeight = triggerHeight / 2f;

        float minX = centerPos.x - halfWidth;
        float maxX = centerPos.x + halfWidth;
        float minY = centerPos.y - halfHeight;
        float maxY = centerPos.y + halfHeight;

        if (triggerWidth % 2 == 0) { minX += 0.5f; maxX += 0.5f; }
        if (triggerHeight % 2 == 0) { minY += 0.5f; maxY += 0.5f; }

        return (pX >= minX && pX <= maxX && pY >= minY && pY <= maxY);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = triggerColor;
        Vector3 centerPos = transform.position;
        
        float offsetX = (triggerWidth % 2 == 0) ? 0.5f : 0f;
        float offsetY = (triggerHeight % 2 == 0) ? 0.5f : 0f;
        Vector3 gizmoCenter = new Vector3(centerPos.x + offsetX, centerPos.y + offsetY, centerPos.z);

        Gizmos.DrawCube(gizmoCenter, new Vector3(triggerWidth, triggerHeight, 0.1f));
        Gizmos.DrawWireCube(gizmoCenter, new Vector3(triggerWidth, triggerHeight, 0.1f));

        Gizmos.color = Color.red;
        Vector3 spawnTarget = new Vector3(spawnCoordinate.x, spawnCoordinate.y, 0f);
        Gizmos.DrawSphere(spawnTarget, 0.25f);
=======

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
>>>>>>> main
    }
}