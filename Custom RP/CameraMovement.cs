using UnityEngine;

public class CameraMovement : MonoBehaviour {

    public float movementSpeed = 3.0f;
    public float movementRange = 5.0f;

    private Vector3 initialPosition;

    private void Start() {
        initialPosition = transform.position;
    }

    private void Update() {

        Vector3 targetPosition = initialPosition + Vector3.right * Mathf.Sin(Time.time * movementSpeed) * movementRange;

        transform.position = targetPosition;
    }
}






