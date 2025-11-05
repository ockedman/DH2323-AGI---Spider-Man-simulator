using UnityEngine;

[DefaultExecutionOrder(0)]
public class RopeController : MonoBehaviour
{
    [Header("Player")]
    public PlayerController player;
    private WebShooter shooter;
    private LineRenderer _webRenderer;
    private float playerWeight;

    [Header("Position vectors")]
    private Vector3 startPoint;
    private Vector3 direction;

    [Header("Point properties")]
    public int nbPoints = 100;
    private int pointsThusFar = 0;
    public float distBetweenPoints = 1.5f;
    private float actualDistance;

    [Header("Points and velocities")]
    private Vector3[] currPositions;
    private Vector3[] oldPositions;
    private Vector3[] velocities;

    [Header("Bools")]
    private bool isAttached = false;
    private bool hasReached = false;
    private bool passedLimit = false;
    private bool[] sticked;

    [Header("Collision and dropping")]
    private int envMask;
    public float collRadius = 0.02f;
    private float timeDropped = -1f;
    public float gravityDamping = 0.995f;

    [Header("Simulation Parameters")]
    public float destroyDelay = 5f;
    public Vector3 gravityScale = new Vector3(0f, -20f, 0f);
    public int unitCreatedPerFrame = 1;
    public int nIterations;

    [Header("Elasticity Parameters")]
    public float stiffness;
    public float elasticDamping;

    [Header("Debug Info")]
    public Vector3 lastForceApplied = Vector3.zero;
    public float lastForceMagnitude = 0f;
    public float currentStretch = 0f;
    public Vector3 playerVelocityBeforeForce = Vector3.zero;
    public Vector3 playerVelocityAfterForce = Vector3.zero;
    public int forcesAppliedThisFrame = 0;
    public float totalForceThisFrame = 0f;

    void Awake()
    {
        if (player == null)
            player = GameObject.Find("Player")?.GetComponent<PlayerController>();
        
        playerWeight = player.GetWeight();
        shooter = player.GetWebShooter();

        _webRenderer = GetComponent<LineRenderer>();
        if (_webRenderer == null)
        {
            return;
        }

        //startPoint = player.transform.position;
        startPoint = shooter.transform.position;
        //Debug.Log("the start point is " + startPoint);

        _webRenderer.positionCount = 1;
        _webRenderer.startWidth = 0.15f;
        _webRenderer.endWidth = 0.15f;

        _webRenderer.material = new Material(Shader.Find("Unlit/Color"));
        _webRenderer.material.color = Color.white;

        _webRenderer.enabled = true;
        _webRenderer.SetPosition(0, startPoint);

        isAttached = true;
        envMask = LayerMask.GetMask("Environment");

        actualDistance = distBetweenPoints;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        bool isOnAir = player.GetIsOnAir();

        //startPoint = player.transform.position;
        startPoint = shooter.transform.position;

        ExtendRope();
        //PrintPositions();
        WebMove();
        DestroyRope();
    }

    void Update()
    {
        //Debug.Log("the actual distance is " + actualDistance);
    }

    void LateUpdate()
    {
        AdaptRenderer();
    }

    public void PrintPositions()
    {
        for (int i = 0; i < pointsThusFar; i++)
        {
            Debug.Log(currPositions[i]);
        }
    }

    public Vector3 GetFirstPoint() => currPositions[0];

    public bool GetReached() => hasReached;

    public bool GetPassedLimit() => passedLimit;

    public Vector3[] GetOldPositions() => oldPositions;

    public Vector3[] GetCurrPositions() => currPositions;

    public void Dettach()
    {
        Debug.Log("we just dettached");
        isAttached = false;
        timeDropped = Time.time;
        velocities[0] = velocities[1];
    }

    public void InitializeRope(Ray ropeRay)
    {
        Debug.Log("we create the rope");
        direction = ropeRay.direction.normalized;

        currPositions = new Vector3[nbPoints + 1];
        oldPositions = new Vector3[nbPoints + 1];
        velocities = new Vector3[nbPoints + 1];
        sticked = new bool[nbPoints + 1];

        Vector3 playerVel = player != null ? player.GetVelocity() : Vector3.zero;

        currPositions[0] = startPoint;
        oldPositions[0] = startPoint - playerVel * Time.fixedDeltaTime;
        velocities[0] = playerVel;

        pointsThusFar = 0;

        for (int i = 1; i <= nbPoints; i++)
        {
            currPositions[i] = startPoint;
            oldPositions[i] = startPoint;
            velocities[i] = Vector3.zero;
            sticked[i] = false;
        }
    }

    public void ExtendRope()
    {
        if (!isAttached || hasReached || pointsThusFar >= nbPoints) return;

        Vector3 lastPoint = currPositions[pointsThusFar];
        Vector3 ropeDirection = direction;
        Ray ropeRay = new Ray(lastPoint, ropeDirection);

        RaycastHit extendHit;
        if (Physics.Raycast(ropeRay, out extendHit, unitCreatedPerFrame * distBetweenPoints))
        {
            float distToSurface = (extendHit.point - lastPoint).magnitude;
            int pieces = 0;

            if (distToSurface <= unitCreatedPerFrame * distBetweenPoints * 1.01f)
            {
                pieces = unitCreatedPerFrame;
            }
            else
            {
                pieces = Mathf.FloorToInt(distToSurface / distBetweenPoints);
            }

            float newDistance = distToSurface / pieces;

            AddPoints(pieces, newDistance);
            Vector3 newLastPoint = extendHit.point;
            if (pointsThusFar < nbPoints)
            {
                currPositions[pointsThusFar + 1] = newLastPoint;
                oldPositions[pointsThusFar + 1] = newLastPoint;
            }
            //Debug.Log("last step, we have " + pointsThusFar + " and add " + pieces + " points");
            pointsThusFar = Mathf.Min(pointsThusFar + 1, nbPoints);

            //Debug.Log("we reached the end, with " + nbPoints + " points, but also " + pointsThusFar + " technically thus far");
            hasReached = true;
            nbPoints = pointsThusFar;

            float scale = 0.15f;
            ScaleEveryVelocity(scale);
        }
        else
        {
            int pointsToAdd = Mathf.Min(unitCreatedPerFrame, nbPoints - pointsThusFar);
            AddPoints(pointsToAdd, distBetweenPoints);
        }
    }

    public void AddPoints(int pointsToAdd, float actualDistance)
    {
        if (pointsThusFar >= 1)
        {
            Vector3[] newCurrPositions = new Vector3[pointsThusFar];
            Vector3[] newOldPositions = new Vector3[pointsThusFar];
            Vector3[] newVelocities = new Vector3[pointsThusFar];

            for (int i = 1; i <= pointsThusFar; i++)
            {
                int newIndex = i + pointsToAdd;
                if (newIndex <= nbPoints)
                {
                    newCurrPositions[i - 1] = currPositions[i] + direction * actualDistance * pointsToAdd;
                    //oldPositions[newIndex] = oldPositions[i] + direction * actualDistance * pointsToAdd;
                    newOldPositions[i - 1] = currPositions[i];
                    newVelocities[i - 1] = velocities[i];
                    //Debug.Log("Point " + newIndex + " was translated from " + newOldPositions[i - 1] + " to " + newCurrPositions[i - 1]);
                    //Debug.Log("Its velocity is of " + velocities[newIndex]);
                }
            }

            for (int i = pointsToAdd + 1; i <= pointsToAdd + pointsThusFar; i++)
            {
                currPositions[i] = newCurrPositions[i - pointsToAdd - 1];
                oldPositions[i] = newOldPositions[i - pointsToAdd - 1];
                velocities[i] = newVelocities[i - pointsToAdd - 1];
            }
        }

        for (int i = 1; i <= pointsToAdd; i++)
        {
            Vector3 nextPoint = startPoint + actualDistance * i * direction;
            currPositions[i] = nextPoint;
            oldPositions[i] = startPoint;
            velocities[i] = direction * actualDistance * 25f;
            //Debug.Log("the velocity of point just created " + i + " is " + velocities[i]);
            //Debug.Log("we add point " + i + ", before at " + oldPositions[i] + ", to " + currPositions[i]);
        }

        pointsThusFar = Mathf.Min(pointsThusFar + pointsToAdd, nbPoints);
    }

    public void ScaleEveryVelocity(float s)
    {
        for (int i = 0; i <= pointsThusFar; i++)
        {
            velocities[i] *= s;
        }
    }

    public void Retract()
    {
        actualDistance *= 0.95f;
    }

    public void Elongate()
    {
        actualDistance *= 1.05f;
    }

    public void WebMove()
    {
        if (currPositions == null || pointsThusFar == 0) return;

        float t_unit = Time.fixedDeltaTime;
        float ts = t_unit / nIterations;
        if (ts <= 0f) return;

        for (int n = 0; n < nIterations; n++)
        {
            for (int i = 0; i <= pointsThusFar; i++) velocities[i] += ts * gravityScale * 2;
            ScaleEveryVelocity(gravityDamping);
            for (int i = 0; i <= pointsThusFar; i++) ApplyGravity(i, ts);
            for (int i = 0; i < pointsThusFar; i++) SolveDistanceConstraint(i, i + 1, ts);
            for (int i = 0; i <= pointsThusFar; i++) SolveCollisionConstraint(i);
            AdaptVelocities(ts);
        }

        //CorrectFirstPoint();
    }

    public void ApplyGravity(int i, float ts)
    {
        if (sticked[i]) return;

        // we don't move the end point when it's attached to the building
        if (i == nbPoints && hasReached)
        {
            //Debug.Log("its the last point that attached, we don't move");
            currPositions[i] = oldPositions[i] = currPositions[i];
            return;
        }

        // we don't move the start point when it's attached to the player on the ground
        if (i == 0 && isAttached)
        {
            oldPositions[0] = currPositions[0];
            currPositions[0] = shooter.transform.position;
            //currPositions[0] = player.transform.position;
            velocities[i] = player.GetVelocity();
            //velocities[i] = Vector3.zero;
            return;
        }

        Vector3 currentPos = currPositions[i];
        Vector3 vel = velocities[i];

        // PBD
        Vector3 nextPos = currentPos + ts * vel;

        oldPositions[i] = currentPos;
        currPositions[i] = nextPos;
        velocities[i] = vel;
    }

    public void SolveDistanceConstraint(int i, int j, float ts)
    {
        if (i > pointsThusFar || j > pointsThusFar) return;

        Vector3 delta = currPositions[j] - currPositions[i];
        float dist = delta.magnitude;
        if (dist <= 0.0001f) return;

        float diff = (dist - actualDistance) / dist;

        float w1 = GetWeight(i);
        float w2 = GetWeight(j);
        float wSum = w1 + w2;
        if (wSum <= 0f) return;

        currPositions[i] += delta * (w1 / wSum) * diff;
        currPositions[j] -= delta * (w2 / wSum) * diff;


        if (i == 0 && isAttached && hasReached)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            float constraintError = dist - actualDistance;

            if (constraintError > 0.01f)
            {
                /*
                Vector3 ropeDir = delta.normalized;

                // Projette et annule toute vélocité qui éloigne du point d'attache
                Vector3 currentVel = rb.linearVelocity;
                float velAlongRope = Vector3.Dot(currentVel, ropeDir);

                if (velAlongRope > 0) // S'éloigne
                {
                    // Annule complètement la composante radiale
                    Vector3 tangentialVel = currentVel - ropeDir * velAlongRope;
                    rb.linearVelocity = tangentialVel;
                }

                // Force de rappel très forte mais proportionnelle
                float stiffness = playerWeight * 5f; // Proportionnel à la masse
                Vector3 correctionForce = ropeDir * constraintError * stiffness;

                rb.AddForce(correctionForce, ForceMode.Force);
                */


                float stretchFactor = Mathf.Clamp01((w1 / wSum) * diff);

                Vector3 force = delta.normalized * stretchFactor * stiffness;
                //force = (currPositions[0] - player.transform.position) * stiffness;
                force -= player.GetVelocity() * elasticDamping;
                //force *= ts;
                Vector3 aforce = rb.GetAccumulatedForce();
                //Debug.Log("the accumulated force is " + aforce);
                rb.AddForce(force, ForceMode.Force);
                Vector3 aforce2 = rb.GetAccumulatedForce();
                //Debug.Log("that force now is " + aforce2 + " for a difference of " + (aforce2 - aforce));

            }
        }
    }

    public void SolveCollisionConstraint(int i)
    {
        if (i > pointsThusFar) return;
        if (i == pointsThusFar && hasReached) return;
        if (i == 0 && isAttached) return;

        Vector3 p = currPositions[i];
        Vector3 pOld = oldPositions[i];
        Vector3 move = p - pOld;
        float dist = move.sqrMagnitude;
        if (dist < 1e-6f)
        {
            //ResolveStaticPenetration(i);
            return;
        }

        Vector3 dir = move / dist;
        float maxDistance = dist + collRadius;

        RaycastHit hit;
        if (Physics.SphereCast(pOld, collRadius, dir, out hit, maxDistance, envMask))
        {
            Vector3 n = hit.normal.normalized;
            Vector3 q = hit.point + n * collRadius;

            float C = Vector3.Dot(p - q, n);

            if (C < 0f)
            {
                Vector3 correction = -C * n;
                //currPositions[i] += correction;
                //oldPositions[i] += correction;
                currPositions[i] = q;
                oldPositions[i] = q;
            }
        }
        else
        {
            ResolveStaticPenetration(i);
        }
    }

    private void ResolveStaticPenetration(int i)
    {
        Vector3 p = currPositions[i];
        Collider[] colliders = Physics.OverlapSphere(p, collRadius, envMask);
        foreach (Collider c in colliders)
        {
            Vector3 closest = c.ClosestPoint(p);
            Vector3 dir = (p - closest);
            float dist = dir.magnitude;
            if (dist < 1e-6f) continue;
            dir /= dist;

            float penetration = collRadius - dist;
            if (penetration > 0f)
            {
                Vector3 correction = dir * penetration;
                currPositions[i] += correction;
                oldPositions[i] += correction;
            }
        }
    }

    private void CorrectFirstPoint()
    {
        if (!hasReached || pointsThusFar == 0 || !MustCorrectFirstPos()) return;

        Vector3 playerPos = player.transform.position;
        Vector3 firstPoint = currPositions[0];
        Vector3 pointToPlayer = firstPoint - currPositions[1];

        Vector3 actualPos = currPositions[1] + pointToPlayer.normalized * actualDistance * 1f;
        Vector3 distToGo = actualPos - firstPoint;

        //Debug.Log("the difference vector between the points is: " + pointToPlayer);
        //Debug.Log("the point should actually be at " + actualPos + ", instead of " + firstPoint);
        //Debug.Log("the character is at " + playerPos + " and receives an energy push of " + distToGo);

        //player.transform.position = actualPos;
        //player.transform.position = Vector3.Lerp(playerPos, actualPos, Time.fixedDeltaTime * 10f);
        //player.GetComponent<Rigidbody>().AddForce(distToGo, ForceMode.Force);
        //player.GetComponent<Rigidbody>().AddForce(distToGo * 80f, ForceMode.Acceleration);
    }

    public bool MustCorrectFirstPos()
    {
        if (!hasReached) return false;
        if (pointsThusFar == 0) return false;

        Vector3 pointToPlayer = currPositions[1] - currPositions[0];
        float firstDist = pointToPlayer.magnitude;

        return firstDist > actualDistance * 1.2;
    }

    public float GetWeight(int i)
    {
        float m = 0.2f;
        if (i == 0 && isAttached) m = playerWeight;
        else if (sticked[i]) m = Mathf.Infinity;
        else if (i == pointsThusFar && hasReached) m = Mathf.Infinity;

        return 1f / m;
    }

    public void AdaptVelocities(float ts)
    {
        if (ts <= 0f) return;

        for (int i = 0; i <= pointsThusFar; i++)
        {
            velocities[i] = (currPositions[i] - oldPositions[i]) / ts;
            if (hasReached && i == pointsThusFar - 1)
            {
                //Debug.Log("new velocity of " + velocities[i]);
            }
        }
    }

    public void DestroyRope()
    {
        if (!isAttached && timeDropped >= 0 && Time.time >= timeDropped + destroyDelay)
            Destroy(gameObject);
    }

    public void AdaptRenderer()
    {
        if (currPositions == null || pointsThusFar == 0) return;

        int expectedCount = Mathf.Min(pointsThusFar + 1, currPositions.Length);
        if (_webRenderer.positionCount != expectedCount)
            _webRenderer.positionCount = expectedCount;

        for (int i = 0; i < expectedCount; i++)
            _webRenderer.SetPosition(i, currPositions[i]);
    }

    void OnDrawGizmos()
    {
        if (currPositions == null || pointsThusFar == 0) return;

        // Corde normale
        Gizmos.color = Color.green;
        for (int i = 0; i < pointsThusFar; i++)
            Gizmos.DrawLine(currPositions[i], currPositions[i + 1]);

        // Points de corde
        Gizmos.color = Color.blue;
        for (int i = 0; i <= pointsThusFar; i++)
        {
            Gizmos.DrawWireSphere(currPositions[i], 0.3f);
        }

        // NOUVEAU: Visualise la force appliquée
        if (lastForceApplied.magnitude > 0.1f && isAttached)
        {
            Gizmos.color = Color.red;
            Vector3 forceStart = player.transform.position;
            Vector3 forceEnd = forceStart + lastForceApplied.normalized * Mathf.Min(lastForceMagnitude / 500f, 10f);

            // Flèche de force
            Gizmos.DrawLine(forceStart, forceEnd);
            Gizmos.DrawWireSphere(forceEnd, 0.5f);

        }

        // Visualise l'étirement
        if (isAttached && hasReached && pointsThusFar > 0)
        {
            Vector3 delta = currPositions[1] - currPositions[0];
            float dist = delta.magnitude;
            float stretch = dist - actualDistance;

            if (stretch > 0.1f)
            {
                // Rouge si étiré
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, Mathf.Clamp01(stretch / 2f));
                Gizmos.DrawLine(currPositions[0], currPositions[1]);
                Gizmos.DrawWireSphere(currPositions[0], 0.5f + stretch * 0.3f);

            }
        }

        // Vélocité du joueur
        if (player != null && isAttached)
        {
            Gizmos.color = Color.cyan;
            Vector3 vel = player.GetVelocity();
            Gizmos.DrawRay(player.transform.position, vel * 0.2f);

        }
    }
}