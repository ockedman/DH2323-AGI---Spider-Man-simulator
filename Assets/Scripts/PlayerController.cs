using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(-50)]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float groundDrag = 5f;
    public float jumpForce = 10f;

    [Header("Character Data")]
    private float playerHeight;
    public float weight = 80f;
    private Vector3 movement = new Vector3(0, 0, 0);
    private Rigidbody rb;

    [Header("Keybinds")]
    private KeyCode jumpKey = KeyCode.Space;
    private KeyCode advanceKey = KeyCode.W;
    private KeyCode returnKey = KeyCode.S;
    private KeyCode leftKey = KeyCode.A;
    private KeyCode rightKey = KeyCode.D;
    private KeyCode slowKey = KeyCode.V;

    [Header("Ground Check")]
    private int envMask;
    private bool isOnFloor = true;
    private float rayDistance;

    [Header("Scene Data")]
    public float gravityScale = -5f;
    public float slowTime = 0.3f;
    public float startX = 0f;
    public float startY = 0f;
    public float startZ = 0f;
    public float groudY = 0f;

    [Header("Inputs")]
    private float horizontalMovementInput;
    private float verticalMovementInput;
    private bool toAdvance = false;
    private bool toReturn = false;
    private bool toGoLeft = false;
    private bool toGoRight = false;
    private bool toJump = false;
    private bool slowMo = false;

    [Header("WebShooter")]
    private WebShooter shooter;
    private bool isSwinging = false;

    private void Awake()
    {
        playerHeight = transform.localScale.y;

        Transform obstacle = GameObject.Find("Buildings").transform;
        if (obstacle)
        {
            int nbBuildings = obstacle.childCount;
            int index_b = Random.Range(0, nbBuildings);

            Transform startBuilding = obstacle.GetChild(index_b);
            float highestY = startBuilding.localScale.y / 2f + startBuilding.position.y + playerHeight + 0.1f;
            startY = highestY;

            startX = startBuilding.position.x;
            startZ = startBuilding.position.z;
        }
        else
        {
            startY = groudY + (playerHeight * 0.5f) + 0.1f;
        }

        transform.position = new Vector3(startX, startY, startZ);

        envMask = LayerMask.GetMask("Environment");

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        //Physics.gravity = new Vector3(0f, gravityScale, 0f);

        shooter = GetComponentInChildren<WebShooter>();
        if (shooter == null)
        {
            Debug.LogError("We don't have a WebShooter");
        }

        float halfHeight = GetComponent<CapsuleCollider>().height;
        rayDistance = halfHeight + 0.1f;
    }

    private void Start()
    {
    }

    private void FixedUpdate()
    {
        Move();
        Jump();
        SpeedControl();
        ApplyDamping();
        SwingMove();
    }

    private void Update()
    {
        PlayerInput();
        CheckWebbing();
        CheckOnFloor();
    }

    private void LateUpdate()
    {
    }

    private void OnDisable()
    {
        rb.isKinematic = true;
    }

    public float GetWeight()
    {
        return weight;
    }

    public bool GetIsOnAir() => !isOnFloor;

    public Vector3 GetVelocity() => rb.linearVelocity;

    public WebShooter GetWebShooter() => shooter;

    private void PlayerInput()
    {
        //Debug.Log("the up view is " + verticalMovementInput);

        horizontalMovementInput = Input.GetAxis("Horizontal");
        verticalMovementInput = Input.GetAxis("Vertical");

        //toAdvance = Input.GetKey(advanceKey);
        //toReturn = Input.GetKey(returnKey);
        //toGoLeft = Input.GetKey(leftKey);
        //toGoRight = Input.GetKey(rightKey);
        toJump = Input.GetAxis("Jump") > 0.1f;
        slowMo = Input.GetAxis("Slow") > 0.1f;
    }

    private void Move()
    {
        if (slowMo)
        {
            Time.timeScale = slowTime;
        }
        else
        {
            Time.timeScale = 1;
        }


        movement = transform.forward * verticalMovementInput + transform.right * horizontalMovementInput;
        rb.AddForce(movement.normalized * moveSpeed * 10f, ForceMode.Force);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitVel.x, rb.linearVelocity.y, limitVel.z);
        }
    }

    private void Jump()
    {
        if (toJump && isOnFloor)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void SwingMove()
    {
        // if we're not in the air, we don't move
        if (isOnFloor)
        {
            return;
        }

        // if the shooter has a rope and that rope is attached to a building
        else if (isSwinging)
        {
            PlayerSwing();
        }

        // we're just flowing in the air
        else
        {
            PlayerRest();
        }
    }

    private void PlayerSwing()
    {
        /*
        RopeController rope = shooter.GetRope();
        if (!rope) return;

        Vector3[] oldPos = rope.GetOldPositions();
        Vector3[] currPos = rope.GetCurrPositions();

        oldPos[0] = currPos[0];
        currPos[0] = transform.position;
        */
    }

    private void PlayerRest()
    {
        return;
    }

    private void CheckOnFloor()
    {
        isOnFloor = Physics.Raycast(transform.position, Vector3.down, rayDistance, envMask);
    }

    private void CheckWebbing()
    {
        if (shooter.GetRope() && shooter.GetRope().GetReached())
        {
            isSwinging = true;
        }
        else
        {
            isSwinging = false;
        }
    }

    private void ApplyDamping()
    {
        if (isOnFloor)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0f;
        }
    }
}