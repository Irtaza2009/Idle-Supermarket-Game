using TMPro;
using UnityEngine;

public class CashSystem : MonoBehaviour
{
    [SerializeField] private int startingCash = 200;
    [SerializeField] private TMP_Text cashText;

    public int CurrentCash { get; private set; }

    private void Awake()
    {
        CurrentCash = startingCash;
        UpdateCashText();
    }

    public void EarnCash(int amount)
    {
        CurrentCash += Mathf.Max(0, amount);
        UpdateCashText();
    }

    public void LoseCash(int amount)
    {
        CurrentCash = Mathf.Max(0, CurrentCash - Mathf.Max(0, amount));
        UpdateCashText();
    }

    private void UpdateCashText()
    {
        if (cashText != null)
        {
            cashText.text = $"Cash: ${CurrentCash}";
        }
    }
}