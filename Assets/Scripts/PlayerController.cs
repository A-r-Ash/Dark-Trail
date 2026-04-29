using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 360f;

    private Rigidbody rb;
    private Camera    mainCamera;
    private Vector2   moveInput;
    private float     speedMultiplier = 1f;
    private bool      isImmobilized;

    private Vector3   cameraBaseLocalPos;
    private bool      isShaking;

    public bool IsMoving => !isImmobilized && moveInput.magnitude > 0.01f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogError("No main camera found! Tag your camera as MainCamera.");
        else
            cameraBaseLocalPos = mainCamera.transform.localPosition;

        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.useGravity  = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        HandleRotation();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
    }

    public void SetImmobilized(bool immobilized, float duration = 0f)
    {
        isImmobilized = immobilized;
        if (immobilized && duration > 0f)
            StartCoroutine(ClearImmobilizedAfter(duration));
    }

    public void ScreenShake(float intensity, float duration)
    {
        if (mainCamera == null) return;
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    IEnumerator ClearImmobilizedAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        isImmobilized = false;
    }

    IEnumerator ShakeRoutine(float intensity, float duration)
    {
        isShaking = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - elapsed / duration;
            Vector2 offset = Random.insideUnitCircle * intensity * t;
            mainCamera.transform.localPosition = cameraBaseLocalPos + new Vector3(offset.x, 0f, offset.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.localPosition = cameraBaseLocalPos;
        isShaking = false;
    }

    void HandleMovement()
    {
        if (isImmobilized)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);

        if (movement.magnitude > 0.01f)
            rb.linearVelocity = movement.normalized * moveSpeed * speedMultiplier;
        else
            rb.linearVelocity = Vector3.zero;
    }

    void HandleRotation()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos  = ray.GetPoint(distance);
            Vector3 directionToMouse = mouseWorldPos - transform.position;
            directionToMouse.y = 0;

            if (directionToMouse.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToMouse);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }
}