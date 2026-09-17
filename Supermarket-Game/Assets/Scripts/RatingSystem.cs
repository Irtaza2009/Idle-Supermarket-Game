using TMPro;
using UnityEngine;

public class RatingSystem : MonoBehaviour
{
    [SerializeField] private float startingRating = 5f;
    [SerializeField] private float correctAccusationChange = 0.1f;
    [SerializeField] private float wrongAccusationPenalty = 0.5f;
    [SerializeField] private TMP_Text ratingText;

    public float CurrentRating { get; private set; }

    private void Awake()
    {
        CurrentRating = Mathf.Clamp(startingRating, 0f, 5f);
        UpdateRatingText();
    }

    public void RecordAccusation(bool accusedStealer)
    {
        float change = accusedStealer ? correctAccusationChange : -wrongAccusationPenalty;
        CurrentRating = Mathf.Clamp(CurrentRating + change, 0f, 5f);
        UpdateRatingText();
    }

    private void UpdateRatingText()
    {
        if (ratingText != null)
        {
            ratingText.text = $"Rating: {CurrentRating:0.0}/5.0";
        }
    }
}