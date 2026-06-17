using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;

    // NetworkVariable: 서버가 값을 관리하고 모든 클라이언트에 자동 동기화됨
    // Vector2 위치를 네트워크로 공유
    private NetworkVariable<Vector2> networkPosition = new NetworkVariable<Vector2>(
        default,
        NetworkVariableReadPermission.Everyone,   // 모두 읽기 가능
        NetworkVariableWritePermission.Server     // 서버만 쓰기 가능
    );

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        // 내 캐릭터면 카메라 따라오게 설정
        if (IsOwner)
        {
            Camera.main.GetComponent<CameraFollow>()?.SetTarget(transform);
        }

        // 내 캐릭터가 아니면 색깔로 구분
        if (!IsOwner)
        {
            GetComponent<SpriteRenderer>().color = Color.red;
        }
    }

    void Update()
    {
        // IsOwner: 이 오브젝트가 내 것일 때만 입력 받기
        if (!IsOwner) return;

        HandleMovement();
    }

    void HandleMovement()
    {
        // 키보드 입력
        float h = Input.GetAxisRaw("Horizontal"); // A/D 또는 ←/→
        float v = Input.GetAxisRaw("Vertical");   // W/S 또는 ↑/↓

        Vector2 direction = new Vector2(h, v).normalized;
        Vector2 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;

        // 클라이언트 → 서버에 이동 요청 보내기
        MoveServerRpc(newPosition);

        // 자신은 바로 이동 (반응성을 위해)
        rb.MovePosition(newPosition);
    }

    // [ServerRpc]: 클라이언트가 호출하면 서버에서 실행됨
    [ServerRpc]
    private void MoveServerRpc(Vector2 newPos)
    {
        // 서버가 위치값을 업데이트 → 자동으로 모든 클라이언트에 동기화
        networkPosition.Value = newPos;
    }

    void FixedUpdate()
    {
        // 내 캐릭터 아닌 경우 → 네트워크 위치로 부드럽게 보간
        if (!IsOwner)
        {
            rb.MovePosition(
                Vector2.Lerp(rb.position, networkPosition.Value, Time.fixedDeltaTime * 10f)
            );
        }
    }
}