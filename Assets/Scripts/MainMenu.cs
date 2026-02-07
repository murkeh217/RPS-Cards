using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    public void OpenScene(int id)
    {
        SceneManager.LoadScene(id);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
