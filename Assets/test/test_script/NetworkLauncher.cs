using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;  // TextMeshPro 사용 (없으면 일반 InputField로 변경)

public class NetworkLauncher : MonoBehaviour
{
    [Header("UI 연결")]
    public TMP_InputField ipInputField;  // IP 입력창
    public GameObject lobbyPanel;        // 로비 패널 (UI 전체)

    // ───────────────────────────────
    // 버튼: Host 시작
    // ───────────────────────────────
    public void StartHost()
    {
        // IP는 자동으로 내 IP 사용, 포트 7777
        SetConnectionData("0.0.0.0", 7777);

        NetworkManager.Singleton.StartHost();
        Debug.Log("Host 시작됨! 내 IP를 상대에게 알려주세요.");

        lobbyPanel.SetActive(false); // 로비 UI 숨기기
        SpawnPlayer();
    }

    // ───────────────────────────────
    // 버튼: Client로 접속
    // ───────────────────────────────
    public void StartClient()
    {
        string ip = ipInputField.text;

        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("IP를 입력하세요!");
            return;
        }

        SetConnectionData(ip, 7777);

        NetworkManager.Singleton.StartClient();
        Debug.Log($"{ip} 에 접속 시도 중...");

        lobbyPanel.SetActive(false);
    }

    // ───────────────────────────────
    // 연결 정보 설정 (내부용)
    // ───────────────────────────────
    private void SetConnectionData(string ip, ushort port)
    {
        // NetworkManager에서 UnityTransport 컴포넌트 가져오기
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);
    }

    // ───────────────────────────────
    // 플레이어 스폰 (Host만 호출)
    // ───────────────────────────────
    private void SpawnPlayer()
    {
        // Player 프리팹을 네트워크에 스폰
        // NetworkManager에 등록된 프리팹이어야 함
        var playerObj = NetworkManager.Singleton.ConnectedClients[
            NetworkManager.Singleton.LocalClientId
        ];
        // 실제로는 아래 방식이 더 안정적:
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // 서버(호스트)가 접속한 클라이언트 각각의 플레이어 스폰
            GameObject player = Instantiate(Resources.Load<GameObject>("Prefabs/Player"));
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        }
    }
}