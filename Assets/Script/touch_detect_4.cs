using UnityEngine;

public class touch_detect4 : MonoBehaviour
{
    public GameObject panel;
    public GameObject button;
    

    void OnTriggerEnter2D(Collider2D coll)
    {
        if(coll.CompareTag("Player"))
        {
            Time.timeScale = 0;
            panel.SetActive(true);
            
            button.SetActive(true);
        }
    }

    void Start()
    {
        

    }

}
