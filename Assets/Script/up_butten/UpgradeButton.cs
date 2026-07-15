using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))] // 버튼 컴포넌트가 없을 경우 자동으로 추가
public class UpgradeButton : MonoBehaviour
{
    public int upgradeID; // 인스펙터에서 1~16번 설정
    private UpgradeManager manager;

    void Start()
    {
        // 최신 버전의 Unity에서는 Object.FindFirstObjectByType을 사용해도 되지만, 
        // 성능을 위해 참조를 캐싱합니다.
        manager = Object.FindFirstObjectByType<UpgradeManager>();
        
        // 버튼 컴포넌트 참조
        Button btn = GetComponent<Button>();
        
        // 중복 등록 방지를 위해 기존 리스너 삭제 후 추가
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClickButton);
    }

    void OnClickButton()
    {
        if (manager != null)
        {
            Debug.Log($"Upgrade {upgradeID} 선택됨 dk섹스하고싶다"); // 신호가 잘 가는지 확인용 로그
            manager.OnSelectUpgrade(upgradeID);
        }
        else
        {
            Debug.LogError("UpgradeManager를 찾을 수 없습니다!");
        }
    }
}