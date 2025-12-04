using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float groundDrag = 5f;
    public float jumpForce = 10f;

    [Header("Character Data")]
    private float playerHeight;
    public float mass = 80f;
    private Vector3 movement = new Vector3(0, 0, 0);

    [Header("PBD Data")]
    public Vector3 currPos = new Vector3(0, 0, 0);
    private Vector3 oldPos = new Vector3(0, 0, 0);
    private Vector3 playerVel = new Vector3(0, 0, 0);
    public Vector3 ropeForce = Vector3.zero;

    [Header("Collision Data")]
    private int envMask;
    private bool isOnFloor = false;
    private float radius;
    public float playerRadius = 0.01f;
    private int nIterations;

    [Header("Scene Data")]
    public float gravityScale = -9.81f;

    [Header("Inputs")]
    private float horizontalMovementInput;
    private float verticalMovementInput;
    private bool toJump = false;

    [Header("WebShooter")]
    private WebShooter shooter;

    private void Awake()
    {
        UpdateParams();

        playerHeight = transform.localScale.y;

        oldPos = transform.position;
        currPos = transform.position;

        envMask = LayerMask.GetMask("Environment");

        shooter = GetComponentInChildren<WebShooter>();
        if (shooter == null)
        {
            Debug.LogWarning("No webshooter");
        }
    }

    private void FixedUpdate()
    {
        ApplyPBD();
        transform.position = currPos;
    }

    private void Update()
    {
        //UpdateParams();
        PlayerInput();
    }

    public float GetMass() => mass;
    public bool GetIsOnAir() => !isOnFloor;
    public Vector3 GetCurrPos() => currPos;
    public Vector3 GetVelocity() => playerVel;
    public WebShooter GetWebShooter() => shooter;

    public void UpdateParams()
    {
        mass = GlobalParameters.instance.playerMass;
        nIterations = GlobalParameters.instance.nIterations;
    }

    private void PlayerInput()
    {
        if (Scenario.instance.fixedMoving)
        {
            horizontalMovementInput = Scenario.instance.nowMoving ? Scenario.instance.horizontalMoving : 0f;
            verticalMovementInput = Scenario.instance.nowMoving ? Scenario.instance.verticalMoving : 0f;
        }
        else
        {
            horizontalMovementInput = Input.GetAxis("Horizontal");
            verticalMovementInput = Input.GetAxis("Vertical");
        }
        toJump = Input.GetKey(KeyCode.Space);

        movement = transform.forward * verticalMovementInput + transform.right * horizontalMovementInput;
    }

    void ApplyPBD()
    {
        Vector3 forces = Vector3.zero;

        forces += new Vector3(0, gravityScale, 0) * mass;
        forces += movement.normalized * moveSpeed * mass;
        forces += ropeForce;

        if (toJump && isOnFloor)
        {
            forces += Vector3.up * jumpForce * mass;
            isOnFloor = false;
        }

        Vector3 acceleration = forces / mass;
        playerVel += acceleration * Time.fixedDeltaTime;
        playerVel *= 0.98f;

        if (isOnFloor)
        {
            playerVel.x *= groundDrag;
            playerVel.z *= groundDrag;
        }

        currPos = oldPos + playerVel * Time.fixedDeltaTime;

        SolveCollisionConstraint();

        playerVel = (currPos - oldPos) / Time.fixedDeltaTime;
        oldPos = currPos;
        ropeForce = Vector3.zero;
    }

    private void SolveCollisionConstraint()
    {
        isOnFloor = false;

        for (int iter = 0; iter < nIterations; iter++)
        {
            Vector3 bottom = currPos - Vector3.up * (playerHeight - radius);
            Vector3 top = currPos + Vector3.up * (playerHeight - radius);

            Vector3 advance = currPos - oldPos;
            float advDist = advance.magnitude;

            if (advDist < 1e-6)
            {
                break;
            }

            advance = advance.normalized;

            Vector3 prevBottom = oldPos - Vector3.up * (playerHeight - radius);
            Vector3 prevTop = oldPos + Vector3.up * (playerHeight - radius);

            RaycastHit hit;

            if (Physics.CapsuleCast(prevBottom, prevTop, radius, advance, out hit, advDist + playerRadius, envMask))
            {
                float collDist = hit.distance - playerRadius;
                if (collDist < 0) collDist = 0;

                currPos = oldPos + advance * collDist;

                float veloNormal = Vector3.Dot(playerVel, hit.normal);

                if (veloNormal < 0)
                {
                    playerVel -= hit.normal * veloNormal;
                }

                if (hit.normal.y > 0.7f)
                {
                    isOnFloor = true;

                    if (playerVel.y < 0)
                    {
                        playerVel.y = 0;
                    }
                }
            }
            else
            {
                break;
            }
        }

        Vector3 newBottom = currPos - Vector3.up * (playerHeight - radius);
        Vector3 newTop = currPos + Vector3.up * (playerHeight - radius);

        if (Physics.CheckCapsule(newBottom, newTop, radius, envMask))
        {
            RaycastHit newHit;
            if (Physics.Raycast(currPos, Vector3.down, out newHit, playerHeight * 2, envMask))
            {
                currPos = newHit.point + Vector3.up * playerHeight;
                playerVel.y = Mathf.Max(0, playerVel.y);
                isOnFloor = true;
            }
        }

        if (isOnFloor)
        {
            RaycastHit groundRay;
            if (Physics.Raycast(currPos, Vector3.down, out groundRay, playerHeight + playerRadius, envMask))
            {
                float groundDist = groundRay.distance - playerHeight;

                if (Mathf.Abs(groundDist) < playerRadius)
                {
                    currPos.y = groundRay.point.y + playerHeight;
                    if (playerVel.y < 0.1f)
                    {
                        playerVel.y = 0;
                    }
                }
            }
        }
    }
}