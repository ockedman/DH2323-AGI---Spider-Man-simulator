using UnityEngine;

public class WebShooter : MonoBehaviour
{
    //public KeyCode webKey = KeyCode.LeftShift;Z
    public WebShooter shooter;

    private RaycastHit shootHit;                            // A raycast hit to get information about what was hit.
    private bool isWebbing = false;
    private bool isShooting = false;
    private bool retracting = false;
    private bool elongating = false;
    public GameObject ropePrefab;
    private RopeController currRope = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        //Debug.Log("=== WEBSHOOTER AWAKE ===");
        ropePrefab = Resources.Load<GameObject>("Prefabs/Rope");
        if (ropePrefab == null) Debug.LogError("we have no ropePrefab attached");
        //else Debug.LogError("we have a webshooter");
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
        Retracter();
        Elongater();
        CheckAttached();
    }

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
        isShooting = Input.GetAxis("Shoot") > 0.1f;
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
        //retracting = Input.GetAxis("Mouse ScrollWheel") > 0.05f;
        //elongating = Input.GetAxis("Mouse ScrollWheel") < 0.05f;
        //Debug.Log("the scroll wheel is " + scrolling);
    }

    private void Shoot()
    {
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

            Ray cameraRay = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height * 0.6f, 0));
            //Debug.Log(Input.mousePosition);
            cameraRay.origin = shooter.gameObject.transform.position;

            currRope.InitializeRope(cameraRay);
        }
    }

    public void Retracter()
    {
        if (retracting && currRope.GetReached())
        {
            //currRope.actualDistance = currRope.actualDistance * 0.2f;
            //currRope.actualDistance *= 0.2f;
            //currRope.actualDistance = Mathf.Clamp(currRope.actualDistance, 0f, currRope.distBetweenPoints);
            //Debug.Log("the new rope distance is " + currRope.actualDistance);
            currRope.Retract();
        }
    }

    public void Elongater()
    {
        if (elongating && currRope.GetReached())
        {
            //currRope.actualDistance *= 1.1f;
            //Debug.Log("now somehow elongating");
            //currRope.actualDistance = Mathf.Clamp(currRope.actualDistance, 0f, currRope.distBetweenPoints);
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
            //Debug.Log("the webshooter dettached");
        }
    }
}
