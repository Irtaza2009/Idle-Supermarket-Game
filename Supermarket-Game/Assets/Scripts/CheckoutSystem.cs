using UnityEngine;
using UnityEngine.InputSystem;

public class CheckoutSystem : MonoBehaviour
{
    [SerializeField] private Transform cashierPoint;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private GameObject checkoutPrompt;

    private Transform playerTransform;

    public bool IsPlayerInside { get; private set; }

    private void Awake()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        SetPromptVisible(false);
    }

    private void Update()
    {
        IsPlayerInside = playerTransform != null && cashierPoint != null &&
                         Vector3.Distance(playerTransform.position, cashierPoint.position) <= interactionRange;

        bool canCheckout = IsPlayerInside && customerManager != null &&
                           customerManager.GetFirstQueuedCustomer() != null;
        SetPromptVisible(canCheckout);

        if (canCheckout && Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
        {
            CustomerController customer = customerManager.GetFirstQueuedCustomer();
            if (customer != null)
            {
                customer.FulfillPurchase();
            }
        }
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (checkoutPrompt != null)
        {
            checkoutPrompt.SetActive(isVisible);
        }
    }
}