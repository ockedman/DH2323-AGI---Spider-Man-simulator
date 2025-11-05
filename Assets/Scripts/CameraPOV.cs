using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPOV : MonoBehaviour
{
    public Transform character;
    public Vector3 headOffset = new Vector3(0, 1.2f, 0);

    public float sensX;
    public float sensY;

    private float xRotation;
    private float yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
    }


    void Update()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;


        //var gamepad = Gamepad.current;
        //if (gamepad == null)
        //{
            //Debug.Log("Aucune manette détectée !");
            //return;
        //}

        // On affiche les principales valeurs :
        //Debug.Log($"A: {gamepad.buttonEast.isPressed}, Stick Gauche: {gamepad.rightStick.ReadValue()}");

    }

    void LateUpdate()
    {
        float xInput = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX / 2;
        float yInput = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY / 4;

        //Debug.Log("the xInput is " + xInput + ". The yInput is " + yInput);

        yRotation += xInput;
        xRotation -= yInput;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        transform.position = character.position + headOffset;

        character.rotation = Quaternion.Euler(0, yRotation, 0);
        
    }
}
