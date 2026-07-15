using UnityEngine;
using UnityEngine.SceneManagement;

public class dead_detecter : MonoBehaviour
{
    public PlayerMovement playermovement;

    void Start()
    {
        playermovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if(playermovement.currentHealth <= 0)
        {
            SceneManager.LoadScene("end_scene");
        }
    }


}
