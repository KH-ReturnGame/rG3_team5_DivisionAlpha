using UnityEngine;

public class touch_detect3 : MonoBehaviour
{
    public GameObject image;
    public GameObject button;
    public GameObject zombie;

    void OnTriggerEnter2D(Collider2D coll)
    {
        if(coll.CompareTag("Player"))
        {
            Time.timeScale = 0;
            image.SetActive(true);
            zombie.SetActive(true);
            button.SetActive(true);
        }
    }

    void Start()
    {
        zombie.SetActive(false);
    }

}
