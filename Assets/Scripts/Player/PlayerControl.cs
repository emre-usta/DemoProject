using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
// ... existing code ...
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody rb;
    private Vector3 moveDirection;

    private Animator animator; // Add this line

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>(); // Add this line
    }
    private void FixedUpdate()
    {
        Vector2 input = InputManager.Instance.MovementInput;

        if (input.sqrMagnitude > 0.0025f)
        {
            Vector3 isoDirection = new Vector3(input.x, 0, input.y);
            isoDirection = Quaternion.Euler(0, 45, 0) * isoDirection; // Isometric XZ plane

            moveDirection = isoDirection.normalized * moveSpeed;

            // Use MovePosition for smooth movement
            Vector3 targetPosition = rb.position + new Vector3(moveDirection.x, 0, moveDirection.z) * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);

            // Face movement direction (optional)
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.z));
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.2f));
            }
        }

        float speed = input.magnitude * moveSpeed; // this works well for anim
        animator.SetFloat("Speed", speed);
    }
}
// ... existing code ...