using UnityEngine;
using UnityEngine.SceneManagement;

public class tutorial_manager_script : MonoBehaviour
{
    public GameObject panal_1;
    public GameObject botton_1;
    public GameObject panal_2;
    public GameObject botton_2;
    public GameObject panal_3;
    public GameObject botton_3;
    public GameObject botton_4;
    
    
    void Start()
    {
        Time.timeScale = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void tutorial_botton_0()
    {
        Time.timeScale = 1;
        panal_1.SetActive(false);
        botton_1.SetActive(false);

    }

    public void tutorial_botton_1()
    {
        Time.timeScale = 1;
        panal_2.SetActive(false);
        botton_2.SetActive(false);
    }

    public void tutorial_botton_2()
    {
        Time.timeScale = 1;
        panal_3.SetActive(false);
        botton_3.SetActive(false);
    }


    public void tutorial_botton_3()
    {
        SceneManager.LoadScene("start_scene");
    }
    
}
