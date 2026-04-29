using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 360f; // Degrees per second - how fast flashlight turns
    [SerializeField] private float minAngle = -135f; // Left limit
    [SerializeField] private float maxAngle = 135f;  // Right limit (total 270 degrees)
    [SerializeField] private float downwardTilt = 45f; // How much flashlight points down (X rotation)

    [Header("References")]
    [SerializeField] private Transform playerTransform; // Reference to parent player

    private Camera mainCamera;
    private float currentLocalAngle = 0f; // Current rotation relative to player's forward

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! Make sure your camera is tagged as MainCamera.");
        }

        // If player transform not assigned, assume parent is the player
        if (playerTransform == null)
        {
            playerTransform = transform.parent;
        }

        if (playerTransform == null)
        {
            Debug.LogError("Flashlight needs a player transform reference! Either assign it or make sure this is a child of the player.");
        }
    }

    void Update()
    {
        HandleFlashlightRotation();
    }

    void HandleFlashlightRotation()
    {
        if (mainCamera == null || playerTransform == null) return;

        // Get mouse position in world space
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, playerTransform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);

            // Calculate direction from player to mouse (not flashlight to mouse - important!)
            Vector3 directionToMouse = (mouseWorldPos - playerTransform.position);
            directionToMouse.y = 0; // Keep on horizontal plane

            if (directionToMouse.magnitude > 0.1f) // Dead zone to prevent jitter near player
            {
                // Calculate angle from player's forward to mouse direction
                Vector3 playerForward = playerTransform.forward;
                float angleToMouse = Vector3.SignedAngle(playerForward, directionToMouse, Vector3.up);

                // Determine target angle based on constraints
                float targetAngle;

                if (angleToMouse >= minAngle && angleToMouse <= maxAngle)
                {
                    // Mouse is in allowed range - aim directly at it
                    targetAngle = angleToMouse;
                }
                else
                {
                    // Mouse is in blind spot - clamp to nearest edge and HOLD there
                    // Don't rotate to follow the mouse in the blind spot
                    if (angleToMouse > maxAngle)
                    {
                        // Mouse is beyond right limit (between 135° and 180°)
                        targetAngle = maxAngle;
                    }
                    else
                    {
                        // Mouse is beyond left limit (between -180° and -135°)
                        targetAngle = minAngle;
                    }
                }

                // Calculate the angular distance to target
                float angularDistance = Mathf.DeltaAngle(currentLocalAngle, targetAngle);

                // Check if we would cross the blind spot by taking the shortest path
                bool wouldCrossBlindSpot = false;

                if (Mathf.Sign(currentLocalAngle) != Mathf.Sign(targetAngle))
                {
                    // We're on opposite sides of 0° - check if the path crosses blind spot
                    float absCurrentAngle = Mathf.Abs(currentLocalAngle);
                    float absTargetAngle = Mathf.Abs(targetAngle);

                    // If both are near the limits (close to 135°), we'd cross the blind spot
                    if (absCurrentAngle > 90f && absTargetAngle > 90f)
                    {
                        wouldCrossBlindSpot = true;
                    }
                }

                // If we would cross blind spot, take the long way (through 0°)
                if (wouldCrossBlindSpot)
                {
                    // Force rotation to go the long way by inverting direction
                    if (currentLocalAngle > 0)
                    {
                        // Currently on right side, rotate left toward 0 first
                        float angleToZero = -currentLocalAngle;
                        float step = Mathf.Sign(angleToZero) * rotationSpeed * Time.deltaTime;
                        currentLocalAngle += step;

                        // Once we pass 0, we can start moving toward target
                        if (currentLocalAngle <= 0)
                        {
                            currentLocalAngle = Mathf.MoveTowardsAngle(currentLocalAngle, targetAngle, rotationSpeed * Time.deltaTime);
                        }
                    }
                    else
                    {
                        // Currently on left side, rotate right toward 0 first  
                        float angleToZero = -currentLocalAngle;
                        float step = Mathf.Sign(angleToZero) * rotationSpeed * Time.deltaTime;
                        currentLocalAngle += step;

                        // Once we pass 0, we can start moving toward target
                        if (currentLocalAngle >= 0)
                        {
                            currentLocalAngle = Mathf.MoveTowardsAngle(currentLocalAngle, targetAngle, rotationSpeed * Time.deltaTime);
                        }
                    }
                }
                else
                {
                    // Safe to take direct path
                    currentLocalAngle = Mathf.MoveTowardsAngle(currentLocalAngle, targetAngle, rotationSpeed * Time.deltaTime);
                }

                // Clamp to ensure we never go outside allowed range
                currentLocalAngle = Mathf.Clamp(currentLocalAngle, minAngle, maxAngle);

                // Apply rotation (local rotation relative to player)
                transform.localRotation = Quaternion.Euler(downwardTilt, currentLocalAngle, 0f);
                // downwardTilt on X keeps the spotlight pointing down at the ground
                // currentLocalAngle on Y rotates it left/right relative to player
                // Note: 90f on X keeps the spotlight pointing down at the ground
                // currentLocalAngle on Y rotates it left/right relative to player
            }
        }
    }

    // Optional: Visualize the allowed rotation range in editor
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.yellow;
        Vector3 playerPos = playerTransform.position;

        // Draw the allowed arc
        Vector3 minDirection = Quaternion.Euler(0, minAngle, 0) * playerTransform.forward;
        Vector3 maxDirection = Quaternion.Euler(0, maxAngle, 0) * playerTransform.forward;

        Gizmos.DrawRay(playerPos, minDirection * 5f);
        Gizmos.DrawRay(playerPos, maxDirection * 5f);

        // Draw arc between min and max
        int segments = 20;
        float angleStep = (maxAngle - minAngle) / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = minAngle + (angleStep * i);
            float angle2 = minAngle + (angleStep * (i + 1));

            Vector3 dir1 = Quaternion.Euler(0, angle1, 0) * playerTransform.forward;
            Vector3 dir2 = Quaternion.Euler(0, angle2, 0) * playerTransform.forward;

            Gizmos.DrawLine(playerPos + dir1 * 5f, playerPos + dir2 * 5f);
        }

        // Draw blind spot in red
        Gizmos.color = Color.red;
        Vector3 blindSpotMin = Quaternion.Euler(0, maxAngle, 0) * playerTransform.forward;
        Vector3 blindSpotMax = Quaternion.Euler(0, minAngle + 360f, 0) * playerTransform.forward;
        Gizmos.DrawRay(playerPos, blindSpotMin * 4f);
        Gizmos.DrawRay(playerPos, blindSpotMax * 4f);
    }
}