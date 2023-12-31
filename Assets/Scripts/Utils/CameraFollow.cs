using UnityEngine;

public class CameraFollow : MonoBehaviour {
    [Header("Follow Movement Settings")]
    [SerializeField] private float followSpeed = 0.1f;
    [SerializeField] private Vector3 offset;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        // Move the camera's position towards the player's position at the speed set by followSpeed with linear interpolation
        transform.position = Vector3.Lerp(transform.position, PlayerController.Instance.transform.position + offset, followSpeed);
    }
}
