using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayer : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 1f; // Speed of movement
    public GameObject leftEyeTracker;
    public float wPos = 0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 xyJoystickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        Vector2 wJoystickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // XY JOYSTICK MOVEMENT
        if (xyJoystickInput.magnitude > 0.1f) // Add a small dead zone to avoid unintended movement
        {
            Vector3 moveDirection = new Vector3(xyJoystickInput.x, 0, xyJoystickInput.y); // X and Z movement
            moveDirection = Quaternion.Euler(0, leftEyeTracker.transform.rotation.eulerAngles.y, 0) * moveDirection; // Convert to world space
            transform.position += moveDirection * moveSpeed * Time.deltaTime; // Apply movement
        }

        // W JOYSTICK MOVEMENT  
        if (wJoystickInput.magnitude > 0.1f) // Add a small dead zone to avoid unintended movement
        {
            wPos += wJoystickInput.y * moveSpeed * Time.deltaTime; // Get the Y movement value
        }

        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)) // Detect button press
        {
            // Start moving the cube from its current position
            Vector3 moveDirection = new Vector3(0, 1, 0); // X and Z movement
            moveDirection = Quaternion.Euler(0, leftEyeTracker.transform.rotation.eulerAngles.y, 0) * moveDirection; // Convert to world space
            transform.position += moveDirection * moveSpeed * Time.deltaTime; // Apply movement
        }

        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger)) // Detect button press
        {
            // Start moving the cube from its current position
            Vector3 moveDirection = new Vector3(0, -1, 0); // X and Z movement
            //DO NOT CONVERT TO WORLD SPACE
            transform.position += moveDirection * moveSpeed * Time.deltaTime; // Apply movement
        }

        if (OVRInput.GetDown(OVRInput.Button.One)) // Detect button press
        {
            // Start moving the cube from its current position
            wPos = 0f;
        }
    }

    public void ResetPlayer() {
        wPos = 0f;
    }
}
