using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ChangeColorOnClick : MonoBehaviour
{
    private Vector3 initialPosition;
    private Vector3 initialControllerPosition;
    private Quaternion initialRotation;
    private Quaternion initialControllerRotation;
    public GameObject cameraRig;
    private bool isMovingDrag = false;

    private bool isMovingXYZ = false;
    private bool isMovingXZW = false;
    private bool allowedToMove = true;
    private float sens = 2f;

    private Matrix4x4 rotationMatrix;
    private Matrix4x4 initialMatrixXYZ;
    private Matrix4x4 initialMatrixXZW;
    private bool onebuttondown;

    void Start()
    {
        allowedToMove = true;
        rotationMatrix = Matrix4x4.identity; // Initialize the rotation matrix to identity
        initialMatrixXYZ = Matrix4x4.identity; // Initialize the initial matrix for XYZ rotation
        initialMatrixXZW = Matrix4x4.identity; // Initialize the initial matrix for XZW rotation
        
        // Store the initial position of the GameObject
        initialPosition = transform.position;
    }
    
    Matrix4x4 ReformatXZWMatrix(Matrix4x4 matrix)
    {
        // Reformat the matrix to only include X and Z axes
        Matrix4x4 newMatrix = new Matrix4x4();
        newMatrix.m00 = matrix.m00; 
        newMatrix.m01 = 0; 
        newMatrix.m02 = matrix.m01; 
        newMatrix.m03 = matrix.m02; 

        newMatrix.m10 = 0; 
        newMatrix.m11 = 1; 
        newMatrix.m12 = 0; 
        newMatrix.m13 = 0; 

        newMatrix.m20 = matrix.m10; 
        newMatrix.m21 = 0;
        newMatrix.m22 = matrix.m11; 
        newMatrix.m23 = matrix.m12; 

        newMatrix.m30 = matrix.m20;
        newMatrix.m31 = 0;
        newMatrix.m32 = matrix.m21;
        newMatrix.m33 = matrix.m22;

        return newMatrix;
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start)) // Detect button press
        {
            allowedToMove = !allowedToMove; // Toggle the allowedToMove variable
        }
        if (allowedToMove) {

        /* HOPING TO GET THIS TO WORK
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // Detect button press
        {
            onebuttondown = true;
            initialMatrixXYZ = rotationMatrix;
            // Start moving the cube from its current position
            isMovingXYZ = true;

            initialRotation = transform.rotation; // Store the initial rotation
            initialControllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch); // Store the controller rotation
        }

        if (isMovingXYZ)
        {
            // Move the cube relative to the controller's movement
            Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

            transform.rotation = initialRotation * Quaternion.Inverse(initialControllerRotation) * controllerRotation; // Update the rotation matrix
        }

        if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger)) // Detect button release
        {
            onebuttondown = false;
            isMovingXYZ = false; // Stop moving the cube
        } */

        /* XYZ ROTATION OLD */
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // Detect button press
        {
            onebuttondown = true;
            initialMatrixXYZ = rotationMatrix;
            // Start moving the cube from its current position
            isMovingXYZ = true;

            initialRotation = transform.rotation; // Store the initial rotation
            initialControllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch); // Store the controller rotation
        }

        if (isMovingXYZ)
        {
            // Move the cube relative to the controller's movement
            Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

            Vector3 eu = (initialRotation * Quaternion.Inverse(initialControllerRotation) * controllerRotation).eulerAngles;
            //eu.z = -eu.z;
            controllerRotation = Quaternion.Euler(eu);

            rotationMatrix = initialMatrixXYZ * Matrix4x4.Rotate(controllerRotation); // Update the rotation matrix
        }

        if (OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger)) // Detect button release
        {
            onebuttondown = false;
            isMovingXYZ = false; // Stop moving the cube
        }



        /* XZW ROTATION */
        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger)) // Detect button press
        {
            // Start moving the cube from its current position
            isMovingXZW = true;
            initialMatrixXZW = rotationMatrix;

            initialRotation = transform.rotation; // Store the initial rotation
            initialControllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch); // Store the controller rotation
        }

        if (isMovingXZW && !onebuttondown) // Check if the button is held down
        {
            // Move the cube relative to the controller's movement
            Quaternion controllerRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
            Quaternion myguy = initialRotation * Quaternion.Inverse(initialControllerRotation) * controllerRotation;
            Vector3 myguyeu = myguy.eulerAngles;
            myguyeu.z = 0; // Set the Z rotation to 0
            myguy = Quaternion.Euler(myguyeu); // Create a new quaternion with the modified Z rotation
            rotationMatrix = initialMatrixXZW * ReformatXZWMatrix(Matrix4x4.Rotate(myguy)); // Update the rotation matrix
        }

        if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger)) // Detect button release
        {
            isMovingXZW = false; // Stop moving the cube
        }



        /* DRAGGING FOR DEBUG PURPOSES */
        if (OVRInput.GetDown(OVRInput.Button.Three)) // Detect button press
        {
            // Start moving the cube from its current position
            isMovingDrag = true;
            initialPosition = transform.position; // Update the starting position
            initialControllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch); // Store the controller position
        }

        if (isMovingDrag)
        {
            // Move the cube relative to the controller's movement
            Vector3 controllerPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            transform.position = initialPosition + sens*(controllerPosition - initialControllerPosition); // Add relative movement
        }

        if (OVRInput.GetUp(OVRInput.Button.Three)) // Detect button release
        {
            isMovingDrag = false; // Stop moving the cube
        }

         if (OVRInput.GetDown(OVRInput.Button.One)) // Detect button release
        {
            rotationMatrix = Matrix4x4.identity;
            ChangeColor();
        }
        }
    }





        //movement in x y z diretion with joysticks (but walking should move as well)
    
        
    

    private void ChangeColor()
    {
        // Change the color of the GameObject this script is attached to
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Random.ColorHSV();
        }
    }

    public void ResetShape() {
        rotationMatrix =  Matrix4x4.identity; // Reset the rotation matrix to identity
        ChangeColor(); // Change color when the button is pressed
    }


    public Matrix4x4 getMatrix(){
        return rotationMatrix;
    }
}