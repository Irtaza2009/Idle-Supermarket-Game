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
            ClearSelectedCustomer();
            return;
        }

        FindClosestCustomer();

        if (closestCustomer != null && Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
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
                if (!customer.CanBeSeenByCamera())
                {
                    continue;
                }

                closestDistance = distance;
                nearest = customer;
            }
        }

        if (nearest != closestCustomer)
        {
            if (closestCustomer != null)
            {
                closestCustomer.SetAccuseHighlighted(false);
            }
            closestCustomer = nearest;

            if (closestCustomer != null)
            {
                closestCustomer.SetAccuseHighlighted(true);
            }

            SetAccusePromptVisible(closestCustomer != null);
        }
    }

    public void AccuseCustomer()
    {
        if (closestCustomer == null)
            return;

        CustomerController accusedCustomer = closestCustomer;

        accusedCustomer.SetAccuseHighlighted(false);
        accusedCustomer.Accused();

        if (ratingSystem != null)
        {
            ratingSystem.RecordAccusation(accusedCustomer.IsStealer);
        }

        ClearSelectedCustomer();
    }

    private void ClearSelectedCustomer()
    {
        if (closestCustomer != null)
        {
            closestCustomer.SetAccuseHighlighted(false);
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
