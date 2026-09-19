using UnityEngine;

public class PlayScript : MonoBehaviour
{
    public void PlayGame()
    {
        int NextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene(NextSceneIndex);
    }

    public void TryAgain()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
    }
}
