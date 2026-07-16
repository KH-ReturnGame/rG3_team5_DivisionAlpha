using UnityEngine;

public class touch_detect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject panal_2;
    public GameObject button_2;
    public GameObject skill_1;
    public GameObject skill_2;
    public GameObject skill_3;
    public GameObject skill_4;
    public GameObject skill_base_1;
    public GameObject skill_base_2;
    public GameObject skill_base_3;
    public GameObject skill_base_4;

    
    void OnTriggerEnter2D(Collider2D coll)
    {
        if(coll.CompareTag("Player"))
        {
            panal_2.SetActive(true);
            button_2.SetActive(true);
            skill_1.SetActive(true);
            skill_2.SetActive(true);
            skill_3.SetActive(true);
            skill_4.SetActive(true);
            skill_base_1.SetActive(true);
            skill_base_2.SetActive(true);
            skill_base_3.SetActive(true);
            skill_base_4.SetActive(true);
            Time.timeScale = 0;
        }
    }

    // Update is called once per frame
    void Start()
    {
        panal_2.SetActive(false);
        button_2.SetActive(false);
        skill_1.SetActive(false);
        skill_2.SetActive(false);
        skill_3.SetActive(false);
        skill_4.SetActive(false);
        skill_base_1.SetActive(false);
        skill_base_2.SetActive(false);
        skill_base_3.SetActive(false);
        skill_base_4.SetActive(false);
    }
}
