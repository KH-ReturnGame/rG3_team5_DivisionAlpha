using UnityEngine;

public class detect : MonoBehaviour
{
    // 플레이어가 Trigger 영역에 들어오는 순간 실행

    public GameObject craftingTableManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 부딪힌 오브젝트의 태그가 "Player"인지 확인
        if (collision.CompareTag("Player"))
        {
            Debug.Log("앗! 플레이어가 스프라이트에 부딪혔다!");
            craftingTableManager.SetActive(true);
        }
    }
}