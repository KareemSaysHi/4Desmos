using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIScript : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Canvas startCanvas;
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private TMP_Text shapeSelector;

    [SerializeField] private GameObject shapeObject;
    [SerializeField] private GameObject cameraRig;

    private ChangeColorOnClick changeColorOnClickScript;
    private MovePlayer movePlayerScript;

    bool canMove = true;
    private string[] shapeList = { "D^4", "D^2 x D^2", "I x D^3", "I^4", "Hyperbaloid", "Cross Cap", "Hopf Surface" };
    private int currentShape; 

    void Start()
    {
        startCanvas.enabled = true;
        menuCanvas.enabled = false;
        changeColorOnClickScript = shapeObject.GetComponent<ChangeColorOnClick>();
        movePlayerScript = cameraRig.GetComponent<MovePlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
        startCanvas.enabled = false;
        menuCanvas.enabled = !menuCanvas.enabled;
        }

        float rightJoystickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;

        // XY JOYSTICK MOVEMENT
        if (menuCanvas.enabled && rightJoystickInput > 0.2f && canMove) // Add a small dead zone to avoid unintended movement
        {
            canMove = false;
            currentShape += 1;
            currentShape %= shapeList.Length;        
            shapeSelector.text = shapeList[currentShape];
            ResetShapes();

        }
        if (menuCanvas.enabled && rightJoystickInput < -0.2f && canMove) // Add a small dead zone to avoid unintended movement
        {
            canMove = false;
            currentShape += shapeList.Length - 1;
            currentShape %= shapeList.Length;
            shapeSelector.text = shapeList[currentShape];
            ResetShapes();

        }
        if (rightJoystickInput < 0.2f && rightJoystickInput > -0.2f)
        {
            canMove = true;
        }
        
    }

    public int getCurrentShape(){
        return currentShape;
    }

    public void ResetShapes(){
        changeColorOnClickScript.ResetShape();
        movePlayerScript.ResetPlayer();
    }
}
