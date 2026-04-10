using UnityEngine;

public class player_hp : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 특정 레이어만 감지
        if (other.CompareTag("monster"))
        {
            Debug.Log("dead");
            
        }

        if (other.CompareTag("item"))
        {
            Debug.Log("get");
        }
    }
}
