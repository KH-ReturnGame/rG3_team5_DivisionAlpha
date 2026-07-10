using UnityEngine;

public class StageTeleportTrigger : MonoBehaviour
{
    // 유니티 인스펙터에서 어떤 스테이지로 갈지 쉽게 선택할 수 있도록 드롭다운 메뉴를 만듭니다.
    public enum TargetStage { Stage2, Stage3 }
    
    [Header("이동할 목표 스테이지 선택")]
    [Tooltip("이 발판을 밟았을 때 이동할 스테이지를 선택하세요.")]
    public TargetStage targetStage = TargetStage.Stage2;

    [Header("3스테이지 연출용 임시 공간 좌표")]
    [Tooltip("시네마틱 애니메이션(카메라가 캐릭터에서 위로 올라가는 연출)을 보여줄 오직 배경과 캐릭터만 있는 임시 맵 좌표입니다.")]
    // 💡 [수정] 기본 시작 좌표를 요청하신 (149, 122)로 고정 세팅했습니다.
    public Vector3 stage3DummyPosition = new Vector3(149f, 122f, 0f);

    [Header("오차 방지 여유 공간 (패딩)")]
    [Tooltip("발판 바로 옆에 걸쳐있는 좀비가 안 지워지는 것을 막기 위해, 발판 Y좌표보다 살짝 위까지 지우도록 설정하는 여유 거리입니다.")]
    public float safetyPadding = 0.5f;

    // 플레이어가 발판(Trigger 콜라이더)을 밟았을 때 실행됩니다.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 오브젝트가 플레이어인지 태그로 확인합니다.
        if (collision.CompareTag("Player"))
        {
            // 1. 기준 Y좌표 설정 (현재 이 발판의 Y좌표를 기준으로 삼습니다)
            float referenceY = transform.position.y + safetyPadding;

            // 2. 현재 맵에 존재하는 모든 좀비(ZombieAI2_0)를 찾아 배열로 가져옵니다.
            ZombieAI2_0[] allZombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);

            int destroyedCount = 0;

            // 3. 루프를 돌며 발판 기준 아래쪽에 있는 좀비만 골라 삭제합니다.
            foreach (ZombieAI2_0 zombie in allZombies)
            {
                if (zombie == null) continue;
                
                // 좀비의 Y좌표가 기준선보다 아래에 있다면 삭제
                if (zombie.transform.position.y <= referenceY)
                {
                    if (zombie.gameObject != null)
                    {
                        Destroy(zombie.gameObject);
                        destroyedCount++;
                    }
                }
            }

            // 4. 선택한 목표 스테이지에 따라 지정된 원래 이동 좌표를 설정합니다.
            Vector3 teleportPosition = Vector3.zero;

            if (targetStage == TargetStage.Stage2)
            {
                teleportPosition = new Vector3(28f, 64f, 0f);
            }
            else if (targetStage == TargetStage.Stage3)
            {
                teleportPosition = new Vector3(29f, 123f, 0f); // 여기가 원래 3스테이지 진짜 전투 시작 좌표
            }

            // 5. 플레이어 컴포넌트 및 물리 속도 제어
            PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero; // 물리 속도 초기화 (밀림 방지)
                }

                // 만약 목표가 3스테이지라면 바로 가기 전에 컷신 매니저를 실행합니다.
                if (targetStage == TargetStage.Stage3 && Stage3CinematicManager.Instance != null)
                {
                    // 매니저에게 플레이어, (149, 122) 연출 좌표, 그리고 돌아와야 할 진짜 3스테이지 좌표(29, 123)를 넘깁니다.
                    Stage3CinematicManager.Instance.PlayStage3Cinematic(playerMovement, stage3DummyPosition, teleportPosition);
                    Debug.Log($"[스테이지 전환] 3스테이지 연출 공간({stage3DummyPosition})으로 진입 후, 5초 카메라 연출 시작. 최종 목적지: {teleportPosition}");
                }
                else
                {
                    // 2스테이지거나 매니저가 없을 때는 기존대로 안전하게 바로 순간이동 처리합니다.
                    playerMovement.TeleportTo(teleportPosition);
                    Debug.Log($"[스테이지 전환] 성공적으로 이동 완료 (목표 좌표: {teleportPosition}). 이전 좀비 {destroyedCount}마리 제거됨.");
                }
            }
            else
            {
                // 예외 처리용 기본 이동
                collision.transform.position = teleportPosition;
            }
        }
    }

    // 에디터 뷰에서 보라색 선 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        float visualizeY = transform.position.y + safetyPadding;
        Vector3 leftPoint = new Vector3(-1000f, visualizeY, 0f);
        Vector3 rightPoint = new Vector3(1000f, visualizeY, 0f);
        Gizmos.DrawLine(leftPoint, rightPoint);
    }
}