using UnityEngine;
using UnityEngine.UI;

public class Monster_State : MonoBehaviour
{
    public GameObject hpBarPrefab;   // Inspector에서 HP_bar 프리팹 연결
    public int HP = 100;
    public int maxHP = 100;
    public int ATK = 10;

    private GameObject hpBarInstance;
    private Image hpFillImage;

    void Start()
    {
        // 프리팹을 좀비 오브젝트의 자식으로 생성
        hpBarInstance = Instantiate(hpBarPrefab, transform);
        hpBarInstance.transform.localPosition = new Vector3(0, 1.5f, 0); // 머리 위 위치

        // Fill 이미지 가져오기 (HP_bar 프리팹 안에 Fill 오브젝트 있어야 함)
        hpFillImage = hpBarInstance.transform.Find("Fill").GetComponent<Image>();
    }

    void Update()
    {
        if (hpFillImage != null)
        {
            float ratio = (float)HP / maxHP;
            hpFillImage.fillAmount = ratio;
        }

        // HP 바가 항상 카메라를 바라보게
        hpBarInstance.transform.rotation = Camera.main.transform.rotation;
    }
}
