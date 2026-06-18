using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    }
}