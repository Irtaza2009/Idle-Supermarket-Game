using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float groundSnapDistance = 0.15f;

    private CharacterController characterController;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        characterController.stepOffset = 0f;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 movement = cameraRight * input.x + cameraForward * input.y;

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (!characterController.isGrounded && verticalVelocity <= 0f &&
            Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, groundSnapDistance))
        {
            float controllerBottom = transform.position.y + characterController.center.y - characterController.height * 0.5f;
            float groundGap = controllerBottom - groundHit.point.y;

            if (groundGap > 0f)
            {
                transform.position += Vector3.down * groundGap;
                verticalVelocity = -2f;
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;
        characterController.Move(movement * (moveSpeed * Time.deltaTime));
    }
}
