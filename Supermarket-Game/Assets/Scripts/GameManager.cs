using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CashSystem cashSystem;
    [SerializeField] private RatingSystem ratingSystem;
    [SerializeField] private bool loadNextSceneOnLoss = true;

    private bool lossTriggered;

    private void Awake()
    {
        if (cashSystem == null)
        {
            cashSystem = FindFirstObjectByType<CashSystem>();
        }

        if (ratingSystem == null)
        {
            ratingSystem = FindFirstObjectByType<RatingSystem>();
        }
    }

    private void Update()
    {
        if (lossTriggered || cashSystem == null || ratingSystem == null)
        {
            return;
        }

        if (cashSystem.CurrentCash <= 0 || ratingSystem.CurrentRating <= 0f)
        {
            TriggerLoss();
        }
    }

    private void TriggerLoss()
    {
        lossTriggered = true;

        if (!loadNextSceneOnLoss)
        {
            return;
        }

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("no next scene (i have to add new (lost) scene)");
        }
    }
}