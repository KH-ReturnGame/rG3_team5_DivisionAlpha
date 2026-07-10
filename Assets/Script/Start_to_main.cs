using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 반드시 필요한 네임스페이스

public class SceneChanger : MonoBehaviour
{
    // 씬 이름을 텍스트로 받아서 전환하는 함수
    public GameObject tutorialImage; // 튜토리얼 이미지 오브젝트를 연결할 변수
    public GameObject Image;
    public void ChangeSceneByIndex()
    {
        SceneManager.LoadScene("MGL");
    }

    public void TutorialImage_On()
    {
        tutorialImage.SetActive(true);
        Image.SetActive(false); // 튜토리얼 이미지가 켜질 때 다른 이미지를 끔
    }

    public void TutorialImage_Off()
    {
        tutorialImage.SetActive(false);
        Image.SetActive(true); // 튜토리얼 이미지가 꺼질 때 다른 이미지를 켬
    }
}