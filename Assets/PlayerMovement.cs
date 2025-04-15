using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using static UnityEngine.GridBrushBase;

public class PlayerMovement : MonoBehaviour
{
    // NavMesh Components
    private NavMeshAgent navAgent;
    private NavMeshPath path; //for path calculation.
    [Header("Car Settings")]
    [SerializeField] private Transform carModel;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float accelerationRate = 8f;
    [SerializeField] private float decelerationRate = 10f;
    [SerializeField] private float brakeForce = 10f;
    // Car related
    private float currentSpeed;
    private float targetSpeed;
    private float currentSteeringAngle;
    private float rotationDirection = 0f;
  
    private bool isReversing = false;
    private bool isBraking = false;
    private Vector3 moveDirection;
    [Header("Steering Settings")]
    [SerializeField] private float steeringSensitivity = 5f;
    [SerializeField] private float maxSteeringAngle = 45f;
    [SerializeField] private float steeringResetSpeed = 5f;
    [SerializeField] private float wheelTurnAngle = 30f;
    private void Awake()
    {
        // Get NavMeshAgent reference
        navAgent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent for car control
        navAgent.updateRotation = false; // We'll handle rotation manually
        navAgent.updatePosition = true;
        navAgent.acceleration = accelerationRate * 2; // Setting acceleration to be responsive
        navAgent.angularSpeed = steeringSensitivity * 20; // Angular speed for turning

        // Get carModel reference if not set
        if (carModel == null && transform.childCount > 0)
            carModel = transform.GetChild(0);

        // Initialize move direction
        moveDirection = transform.forward;
    }

    private void Update()
    {
        HandleInput();
        ApplyMovement();
        ApplyRotation();
    }
    private void HandleInput()
    {
        // Get raw input
        float accelerationInput = Input.GetAxis("Vertical");
        float steeringInput = Input.GetAxis("Horizontal");

        // Handle throttle, brake and reverse
        if (accelerationInput < 0)
        {
            // If car is not moving, enable reversing
            if (currentSpeed < 0.1f && !isReversing)
            {
                isReversing = true;
                targetSpeed = -maxSpeed * 0.5f; // Reverse at half max speed
            }
            // If car is moving forward, apply brakes
            else if (currentSpeed > 0.1f)
            {
                isBraking = true;
                targetSpeed = 0;
            }
        }
        else if (accelerationInput > 0)
        {
            // If car is reversing and we press W, go forward
            if (isReversing)
            {
                isReversing = false;
            }

            isBraking = false;
            targetSpeed = maxSpeed * accelerationInput;
        }
        else
        {
            // No acceleration input, slow down naturally
            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                targetSpeed = 0;
            }

            isBraking = false;
        }

        // Handle steering
        if (currentSpeed > 0.5f || (isReversing && Mathf.Abs(currentSpeed) > 0.2f))
        {
            // Calculate steering based on input
            float targetSteeringAngle = steeringInput * maxSteeringAngle;

            // Apply steering sensitivity
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, Time.deltaTime * steeringSensitivity);

            // Determine rotation direction for the car
            rotationDirection = steeringInput;
        }
        else
        {
            // If car is not moving, gradually reset steering
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, 0f, Time.deltaTime * steeringResetSpeed);
            rotationDirection = 0f;
        }
    }

    private void ApplyMovement()
    {
        // Calculate the acceleration/deceleration rate based on whether we're braking
        float accelRate = isBraking ? brakeForce :
                         (targetSpeed > currentSpeed) ? accelerationRate : decelerationRate;

        // Smoothly adjust current speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * accelRate);

        // Apply speed to NavMeshAgent
        navAgent.speed = Mathf.Abs(currentSpeed);

        // Direction will be forward or backward based on whether we're reversing
        Vector3 targetDirection = isReversing ? -transform.forward : transform.forward;

      

        moveDirection = Vector3.Lerp(moveDirection, targetDirection, Time.deltaTime * 5f);

        // Set the NavMeshAgent's destination to a point in front of the car
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            navAgent.SetDestination(transform.position + moveDirection * 10f);
        }
        else
        {
            // When nearly stopped, just set destination to current position
            navAgent.SetDestination(transform.position);
        }

        // When reversing, we need to manually move since NavMeshAgent doesn't support backwards movement
        if (isReversing)
        {
            // Disable NavMeshAgent's auto-position update temporarily
            navAgent.updatePosition = false;

            // Move the transform directly
            transform.position += moveDirection * (Mathf.Abs(currentSpeed) * Time.deltaTime);

            // Update the agent's position to match the transform
            navAgent.nextPosition = transform.position;

            // Re-enable position updates
            navAgent.updatePosition = true;
        }
    }
        private void ApplyRotation()
    {
        // Only rotate if the car is moving
        if (Mathf.Abs(currentSpeed) > 0.5f)
        {
            float rotationAmount = currentSteeringAngle * (isReversing ? -1 : 1) * Time.deltaTime;

           

            // Apply rotation
            transform.Rotate(0, rotationAmount, 0);

            // Visually tilt the car model
            if (carModel != null)
            {
                Vector3 targetRotation = new Vector3(0, carModel.localEulerAngles.y, 0);
                carModel.localRotation = Quaternion.Slerp(carModel.localRotation, Quaternion.Euler(targetRotation), Time.deltaTime * 5f);
            }
        }
    }
}


