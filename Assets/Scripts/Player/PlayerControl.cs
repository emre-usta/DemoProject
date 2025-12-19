using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody rb;
    private Vector3 moveDirection;

    private Animator animator; 
    
    private bool movementEnabled = true;
    private bool forceIdle = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }
    private void FixedUpdate()
    {
        if (!movementEnabled)
        {
            if (animator == null) animator.SetFloat("speed", 0f);
            return; 
        }
        
        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.IsPaintingMode)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            animator.SetFloat("Speed", 0f);
            return;
        }

        Vector2 input = InputManager.Instance.MovementInput;

        if (input.sqrMagnitude > 0.0025f)
        {
            Vector3 isoDirection = new Vector3(input.x, 0, input.y);
            isoDirection = Quaternion.Euler(0, 45, 0) * isoDirection;

            moveDirection = isoDirection.normalized * moveSpeed;

            Vector3 targetPosition =
                rb.position +
                new Vector3(moveDirection.x, 0, moveDirection.z) * Time.fixedDeltaTime;

            rb.MovePosition(targetPosition);

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.z));
                rb.MoveRotation(
                    Quaternion.Slerp(rb.rotation, targetRotation, 0.2f)
                );
            }
        }

        float speed = forceIdle ? 0f : (input.magnitude * moveSpeed);
        animator.SetFloat("Speed", speed);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    public void ForceIdle(bool idle)
    {
        forceIdle = idle;
        if (idle && animator != null)
            animator.SetFloat("Speed", 0f);
    }
}
