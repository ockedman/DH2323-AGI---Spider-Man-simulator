using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPOV : MonoBehaviour
{
    [Header("Character Data")]
    public Transform character;
    public Vector3 headOffset = new Vector3(0, 1.2f, 0);

    [Header("Rotation Data")]
    public float sensX;
    public float sensY;
    private float xRotation;
    private float yRotation;
    private bool canRotate = true;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        canRotate = Scenario.instance.fixedRotation;
    }

    public bool GetCanRotate() => canRotate;
    public void AugmentRotations(float xRot, float yRot)
    {
        xRotation += xRot;
        yRotation += yRot;
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        canRotate = Input.GetKeyDown("l") ? !canRotate : canRotate;
        //Debug.Log("can rotate is " + canRotate);
    }

    void LateUpdate()
    {
        float xInput = 0f;
        float yInput = 0f;

        if (!canRotate)
        {
            xInput = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX / 2;
            yInput = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY / 4;
        }


        //Debug.Log("the xInput is " + xInput + ". The yInput is " + yInput);

        yRotation += xInput;
        xRotation -= yInput;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        transform.position = character.position + headOffset;

        character.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
