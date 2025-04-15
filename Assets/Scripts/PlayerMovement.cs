using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Fusion;
using Cinemachine;
/* redid this script, now there is no mix and the movement seems to be working well,
 while retaining the player within track boundaries, i didnt want to use raycasts to detect bounds for this so went with navmesh, but later if cars are jumping off ramps and all that we can deactivate for that brief second and perhaps have a hybrid sys, on touchdown we reactivate the navmesh or something. I also would love to learn more from the team infact and how they tackle such challenges.*/
public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f;
    public float maxSpeed = 10f;
    public float targetSpeed = 10f;
    public float acceleration = 10f;
    public float deceleration = 12f;
    private float currentSpeed = 0f;

    // currentMoveDirection: +1 for forward, -1 for reverse.
    private float currentMoveDirection = 1f;


    private NavMeshPath path;
    private int currentPathIndex = 0;

    // Camera
    public CinemachineVirtualCamera vCam;

    // State
    private Vector3 targetPosition;
    private bool hasPath = false;

    [Header("Wheel Visuals")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;

    [Range(0f, 45f)]
    public float maxWheelTurnAngle = 30f;

    [Header("Steering Behavior")]
    public float steeringSensitivity = 2f;
    public float maxSteerAngle = 45f;

    private void Awake()
    {
        path = new NavMeshPath();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            vCam = FindObjectOfType<CinemachineVirtualCamera>();
            vCam.Follow = transform;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        // INPUT
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Rotate the visual front wheels based solely on horizontal input.
        RotateVisualFrontWheels(horizontal);

        Vector3 input = new Vector3(horizontal, 0, vertical);

        // Only consider vertical movement input
        if (Mathf.Abs(vertical) > 0.01f)
        {
            // Determine the desired movement direction from input
            float desiredDirection = Mathf.Sign(vertical);

            // If the desired direction is opposite to the current movement direction...
            if (desiredDirection != currentMoveDirection)
            {
                // Decelerate until nearly stopped
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Runner.DeltaTime);

                // Once nearly stopped, switch the direction
                if (currentSpeed < 0.1f)
                {
                    currentMoveDirection = desiredDirection;
                }
            }
            else
            {
                // Accelerate normally in the current direction.
                // (Use a lower max speed for reverse; forward uses full maxSpeed.)
                float targetSpeed = (vertical > 0f) ? maxSpeed : maxSpeed * 0.5f;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Runner.DeltaTime);
            }

            // Calculate a NavMesh path when there is vertical input.
            Vector3 worldDirection = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * input.normalized;
            Vector3 destination = transform.position + worldDirection * 2f;

            if (NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path))
            {
                if (path.corners.Length > 1)
                {
                    currentPathIndex = 1;
                    hasPath = true;
                }
            }
        }
        else
        {
            // No vertical input? Decelerate.
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Runner.DeltaTime);
            // Optionally, you can disable path once fully stopped.
            if (currentSpeed < 0.01f)
                hasPath = false;
        }

        // MOVEMENT!
        if (hasPath && path != null && path.corners.Length > currentPathIndex)
        {
            Vector3 nextCorner = path.corners[currentPathIndex];
            Vector3 moveDir = nextCorner - transform.position;
            moveDir.y = 0f;

            // If we are close enough to the next point on the path, advance to the following point.
            if (moveDir.magnitude < stoppingDistance)
            {
                currentPathIndex++;
                if (currentPathIndex >= path.corners.Length)
                {
                    hasPath = false;
                    return;
                }
                nextCorner = path.corners[currentPathIndex];
                moveDir = nextCorner - transform.position;
                moveDir.y = 0f;
            }

            if (moveDir.sqrMagnitude > 0.001f)
            {
                // --- STEERING ---
                float speedFactor = Mathf.Clamp01(moveDir.magnitude / (moveSpeed * Runner.DeltaTime));
                float steerInput = Mathf.Clamp(horizontal, -1f, 1f);

                // Invert the steering input when in reverse.
                float effectiveSteerInput = currentMoveDirection > 0 ? steerInput : -steerInput;
                float steerAngle = effectiveSteerInput * maxSteerAngle * speedFactor * steeringSensitivity;
                Quaternion steerRotation = Quaternion.AngleAxis(steerAngle * Runner.DeltaTime, Vector3.up);
                // Rotate the car based on steering input.
                transform.rotation = steerRotation * transform.rotation;

                // --- MOVING ---
                // Move in the direction the car is facing.
                Vector3 move = transform.forward * currentSpeed * currentMoveDirection * Runner.DeltaTime;
                transform.position += move;
            }
        }
        else if (currentSpeed > 0.01f)
        {
            // If no path is active but the car still has speed, let it coast.
            Vector3 move = transform.forward * currentSpeed * currentMoveDirection * Runner.DeltaTime;
            transform.position += move;

            if (Mathf.Abs(horizontal) > 0.01f)
            {
                float steerAngle = horizontal * maxSteerAngle * steeringSensitivity;
                // In reverse, invert the steering adjustment.
                if (currentMoveDirection < 0f)
                    steerAngle = -steerAngle;
                Quaternion steerRotation = Quaternion.AngleAxis(steerAngle * Runner.DeltaTime, Vector3.up);
                transform.rotation = steerRotation * transform.rotation;
            }
            // Continue decelerating.
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Runner.DeltaTime);
        }
    }

    // This helper rotates the visual front wheels based on horizontal input.
    private void RotateVisualFrontWheels(float horizontalInput)
    {
        if (frontLeftWheel == null || frontRightWheel == null)
            return;

        float steerAngle = Mathf.Clamp(horizontalInput, -1f, 1f) * maxWheelTurnAngle;
        Quaternion steerRotation = Quaternion.Euler(0f, steerAngle, 0f);
        frontLeftWheel.localRotation = steerRotation;
        frontRightWheel.localRotation = steerRotation;
    }

}
