using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    public float m_Speed = 12f;
    public float m_TurnSpeed = 180f;
    public float m_WheelRotateSpeed = 90f;

    private Rigidbody m_Rigidbody;              // Reference used to move the tank.
    private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    private string m_TurnAxisName;              // The name of the input axis for turning.
    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.
    private Vector3 m_MouseInputValue;          // The current value of the mouse input
    private int floorMask;                      // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    private List<GameObject> m_wheels = new List<GameObject>();
    private GameObject m_turret;
    private float camRayLength = 100f;          // The length of the ray from the camera into the scene.



    private void Awake()
    {
        // Create a layer mask for the floor layer.
        floorMask = LayerMask.GetMask("Ground");

        m_Rigidbody = GetComponent<Rigidbody>();

        Transform[] children = GetComponentsInChildren<Transform>();
        for (var i = 0; i < children.Length; i++)
        {
            // Get all wheels in the children
            if (children[i].name.Contains("wheel"))
            {
                m_wheels.Add(children[i].gameObject);
            }

            // Get turret
            if (children[i].name.Contains("Turret"))
            {
                m_turret = children[i].gameObject;
            }
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        m_MovementAxisName = "Vertical";
        m_TurnAxisName = "Horizontal";
    }

    private void Update()
    {
        // Store the value of both input axes.
        m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
        m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        m_MouseInputValue = Input.mousePosition;
    }

    private void FixedUpdate()
    {
        // Adjust the rigidbodies position and orientation in FixedUpdate.
        Move();
        Turn();
        RotateWheels();
        RotateTurret();
    }

    private void OnEnable()
    {
        // When the tank is turned on, make sure it's not kinematic.
        m_Rigidbody.isKinematic = false;

        // Also reset the input values.
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
    }

    private void OnDisable()
    {
        // When the tank is turned off, set it to kinematic so it stops moving.
        m_Rigidbody.isKinematic = true;
    }

    private void Move()
    {
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
        Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;

        // Apply this movement to the rigidbody's position.
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }


    private void Turn()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

        // Make this into a rotation in the y axis.
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

        // Apply this rotation to the rigidbody's rotation.
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }

    private void RotateWheels()
    {
        //var b = m_MovementInputValue > 0.0f;
        for (var i = 0; i < m_wheels.Count; i++)
        {
            GameObject w = m_wheels[i];
            float turn = m_MovementInputValue * m_Speed * Time.deltaTime;
            Quaternion turnRotation = Quaternion.Euler(turn, 0f, 0f);
            w.transform.localRotation = w.transform.localRotation * turnRotation;
        }
    }

    private void RotateTurret()
    {
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Create a RaycastHit variable to store information about what was hit by the ray.
        RaycastHit floorHit;

        // Perform the raycast and if it hits something on the floor layer...
        if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
        {
            Vector3 hitPosition = floorHit.point;
            Vector3 direction = hitPosition - m_turret.transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion turnRotation = Quaternion.LookRotation(direction);
                m_turret.transform.rotation = turnRotation;
            }
        }
    }
}