using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target; // The target to follow (e.g., the player)

    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -7); // Offset from the target position
    [SerializeField] private float followSpeed = 5f; // Speed of the camera follow
    [SerializeField] private float lookAheadFactor = 0.2f; // How much to look ahead in the direction of movement

    private void LateUpdate()
    {
        if (target != null)
        {
            // Calculate the desired position based on the target's position and the offset
            Vector3 desiredPosition = target.position + offset;

            // If the target has a rigidbody, look ahead in the direction it's moving
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null && targetRb.velocity.magnitude > 0.1f)
            {
                // Add a look-ahead offset based on the target's velocity
                desiredPosition += targetRb.velocity.normalized * lookAheadFactor * targetRb.velocity.magnitude;
            }

            // Smoothly move the camera towards the desired position
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

            // Make the camera look at the target, like cinemachine does, but ill eventually switch to cinemachine so just chilling with this one.
            // transform.LookAt(target);
        }
    }
}