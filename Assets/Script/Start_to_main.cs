using UnityEngine;
using UnityEngine.SceneManagement; // 씬 전환을 위해 반드시 필요한 네임스페이스

public class SceneChanger : MonoBehaviour
{
    // 씬 이름을 텍스트로 받아서 전환하는 함수
    public void ChangeScene_To_Main()
    {
        SceneManager.LoadScene("MGL");
    }

    public void ChangeScene_To_Tutorial()
    {
        SceneManager.LoadScene("Tutorial");
    }

}