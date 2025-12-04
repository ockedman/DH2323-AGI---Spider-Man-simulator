using UnityEngine;

public class WebShooter : MonoBehaviour
{
    public WebShooter shooter;

    [Header("Shooting Properties")]
    private RaycastHit shootHit;
    private bool canShoot = true;
    private bool isWebbing = false;
    private bool isShooting = false;
    private bool retracting = false;
    private bool elongating = false;

    [Header("Rope Data")]
    public GameObject ropePrefab;
    private RopeController currRope = null;
    public GameObject ropeGroup;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        ropePrefab = Resources.Load<GameObject>("Prefabs/Rope");
        if (ropePrefab == null) Debug.LogError("we have no ropePrefab attached");
    }

    // Update is called once per frame
    void Update()
    {
        ShooterInput();
    }

    void FixedUpdate()
    {
        Shoot();

        if (currRope == null) return;
        Retractor();
        Elongator();
        CheckAttached();
    }

    public bool GetCanShoot() => canShoot;
    public void SetCanShoot(bool ableShoot) => canShoot = ableShoot;
    public void SetShooting(bool shoot) => isShooting = shoot;

    public RopeController GetRope()
    {
        if (currRope)
        {
            return this.currRope;
        }
        else
        {
            return null;
        }
    }

    public void ShooterInput()
    {
        if (Scenario.instance.controlShooter)
        {
            isShooting = Scenario.instance.isShooting;
        }
        else
        {
            isShooting = Input.GetKeyDown("mouse 0") ? !isShooting : isShooting;
        }

        float scrolling = Input.GetAxis("Mouse ScrollWheel");
        if (scrolling > 0.05f)
        {
            retracting = true;
            elongating = false;
        }
        else if (scrolling < -0.05f)
        {
            retracting = false;
            elongating = true;
        }
        else
        {
            retracting = false;
            elongating = false;
        }

        canShoot = Input.GetKeyDown("o") ? !canShoot : canShoot;
    }

    private void Shoot()
    {
        if (!canShoot) return;

        if (isShooting)
        {
            // if we're already shooting a web
            if (isWebbing)
            {
                return;
            }

            if (ropePrefab == null)
            {
                return;
            }

            isWebbing = true;
            GameObject ropeObject = Instantiate(ropePrefab, transform.position, transform.rotation);

            if (ropeObject == null)
            {
                Debug.LogError("We didn't create the rope");
                return;
            }

            RopeController newRope = ropeObject.GetComponent<RopeController>();
            currRope = newRope;
            newRope.transform.SetParent(ropeGroup.transform);

            Ray cameraRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height * 0.6f, 0));
            cameraRay.origin = shooter.gameObject.transform.position;

            currRope.InitializeRope(cameraRay);
        }
    }

    public void Retractor()
    {
        if (retracting && currRope.GetReached())
        {
            currRope.Retract();
        }
    }

    public void Elongator()
    {
        if (elongating && currRope.GetReached())
        {
            currRope.Elongate();
        }
    }

    private void CheckAttached()
    {
        if (!isShooting && isWebbing)
        {
            isWebbing = false;
            currRope.Dettach();
            currRope = null;
            if (Scenario.instance.fixedMoving) Scenario.instance.nowMoving = false;
        }
    }
}
