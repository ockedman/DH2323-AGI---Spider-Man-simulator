using UnityEngine;

public class Scenario : MonoBehaviour
{
    [Header("Scenario Properties")]
    public bool analysing;
    private bool willAnalyse;

    [Header("Player Information")]
    public PlayerController player;
    public bool fixedMoving;
    public bool nowMoving;
    public float horizontalMoving;
    public float verticalMoving;
    private WebShooter shooter;

    [Header("Shooting Times")]
    public float startTime;
    public float endTime;
    public float startTime2;
    public float endTime2;

    [Header("Control Shooting")]
    public bool controlShooter;
    public bool isShooting;

    [Header("Camera Rotation")]
    public CameraPOV camera;
    public bool fixedRotation;
    public float xRotation;
    public float yRotation;

    public static Scenario instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;

        if (player != null)
        {
            shooter = player.GetWebShooter();
        }

        camera.AugmentRotations(xRotation, yRotation);

        if (startTime2 != -1f)
        {
            if (analysing) willAnalyse = true;
            analysing = false;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if ((Mathf.Abs(Time.time - startTime) < 1e-4) || (Mathf.Abs(Time.time - startTime2) < 1e-4))
        {
            ShootWeb();
        }
        else if ((Mathf.Abs(Time.time - endTime) < 1e-4) || (Mathf.Abs(Time.time - endTime2) < 1e-4))
        {
            if (willAnalyse) analysing = true;
            StopShooting();
        }
    }

    public void ShootWeb()
    {
        isShooting = true;
    }

    public void StopShooting()
    {
        isShooting = false;
    }
}
