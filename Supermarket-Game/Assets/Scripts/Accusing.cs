using UnityEngine;
using UnityEngine.InputSystem;

public class Accusing : MonoBehaviour
{
    [Header("Interaction")]

    [SerializeField] private float accuseRange = 3f;
    [SerializeField] private GameObject accusePrompt;
    [SerializeField] private RatingSystem ratingSystem;
    [SerializeField] private CheckoutSystem checkoutSystem;

    private CustomerController closestCustomer;

    private void Awake()
    {
        if (checkoutSystem == null)
        {
            checkoutSystem = FindFirstObjectByType<CheckoutSystem>();
        }

        SetAccusePromptVisible(false);
    }

    private void Update()
    {
        if (checkoutSystem != null && checkoutSystem.IsPlayerInside)
        {
            closestCustomer = null;
            SetAccusePromptVisible(false);
            return;
        }

        FindClosestCustomer();

        if (closestCustomer != null && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            AccuseCustomer();
        }
    }

    private void FindClosestCustomer()
    {
        CustomerController[] customers = 
            FindObjectsByType<CustomerController>();

        CustomerController nearest = null;
        float closestDistance = accuseRange;

        foreach (CustomerController customer in customers)
        {
            float distance = Vector3.Distance(
                transform.position,
                customer.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearest = customer;
            }
        }

        if (nearest != closestCustomer)
        {
            closestCustomer = nearest;
            SetAccusePromptVisible(closestCustomer != null);
        }
    }



    public void AccuseCustomer()
    {
        if (closestCustomer == null)
            return;

        CustomerController accusedCustomer = closestCustomer;

        accusedCustomer.Accused();
        if (ratingSystem != null)
        {
            ratingSystem.RecordAccusation(accusedCustomer.IsStealer);
        }

        closestCustomer = null;
        SetAccusePromptVisible(false);
    }

    private void SetAccusePromptVisible(bool isVisible)
    {
        if (accusePrompt != null)
        {
            accusePrompt.SetActive(isVisible);
        }
    }

}
