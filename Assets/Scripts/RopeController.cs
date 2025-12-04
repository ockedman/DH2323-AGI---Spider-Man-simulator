using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.Collections.Generic;
using static UnityEngine.UI.Slider;
using TMPro;

[DefaultExecutionOrder(0)]
public class RopeController : MonoBehaviour
{
    [Header("Player")]
    public PlayerController player;
    private WebShooter shooter;
    private LineRenderer _webRenderer;
    private float playerMass;

    [Header("Position vectors")]
    private Vector3 startPoint;
    private Vector3 direction;

    [Header("Point properties")]
    private int nbPoints = 100;
    public float pointMass = 0.2f;
    private int pointsThusFar = 0;
    public float distBetweenPoints = 1.5f;
    private float actualDistance;
    private float compliance = 0.01f;

    [Header("Points and velocities")]
    private Vector3[] currPositions;
    private Vector3[] oldPositions;
    private Vector3[] velocities;
    private Vector3[] trueOldPositions;
    private float[] lambdasDist;
    private float[] lambdasColl;

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

    void Awake()
    {
        UpdateParams();

        if (player == null)
            player = GameObject.Find("Player")?.GetComponent<PlayerController>();

        shooter = player.GetWebShooter();

        _webRenderer = GetComponent<LineRenderer>();
        if (_webRenderer == null)
        {
            return;
        }

        startPoint = shooter.transform.position;

        _webRenderer.positionCount = 1;
        _webRenderer.startWidth = 0.2f;
        _webRenderer.endWidth = 0.2f;
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

        startPoint = shooter.transform.position;

        ExtendRope();
        WebMove();
        DestroyRope();
    }

    void LateUpdate()
    {
        AdaptRenderer();
    }

    public void PrintPositions()
    {
        for (int i = 0; i < pointsThusFar; i++)
        {
            UnityEngine.Debug.Log(currPositions[i]);
        }
    }

    public Vector3 GetFirstPoint() => currPositions[0];
    public bool GetReached() => hasReached;
    public bool GetPassedLimit() => passedLimit;
    public Vector3[] GetOldPositions() => oldPositions;
    public Vector3[] GetCurrPositions() => currPositions;
    public Vector3[] GetTrueOldPositions() => trueOldPositions;
    public float GetActualDistance() => actualDistance;
    public int GetPointsThusFar() => pointsThusFar;

    public void UpdateParams()
    {
        nbPoints = GlobalParameters.instance.numberPoints;
        playerMass = GlobalParameters.instance.playerMass;
        distBetweenPoints = GlobalParameters.instance.distBetweenPoints;
        collRadius = GlobalParameters.instance.collRadius;
        pointMass = GlobalParameters.instance.ropePointMass;
        gravityDamping = GlobalParameters.instance.velDamping;
        compliance = GlobalParameters.instance.compliance;
    }

    public void Dettach()
    {
        isAttached = false;
        timeDropped = Time.time;
        velocities[0] = velocities[1];
    }

    public void InitializeRope(Ray ropeRay)
    {
        direction = ropeRay.direction.normalized;

        currPositions = new Vector3[nbPoints + 1];
        oldPositions = new Vector3[nbPoints + 1];
        velocities = new Vector3[nbPoints + 1];
        trueOldPositions = new Vector3[nbPoints + 1];
        sticked = new bool[nbPoints + 1];
        lambdasDist = new float[nbPoints + 1];
        lambdasColl = new float[nbPoints + 1];

        Vector3 playerVel = player != null ? player.GetVelocity() : Vector3.zero;

        currPositions[0] = startPoint;
        oldPositions[0] = startPoint - playerVel * Time.fixedDeltaTime;
        trueOldPositions[0] = startPoint - playerVel * Time.fixedDeltaTime;
        velocities[0] = playerVel;
        lambdasDist[0] = 0f;
        lambdasColl[0] = 0f;

        pointsThusFar = 0;

        for (int i = 1; i <= nbPoints; i++)
        {
            currPositions[i] = startPoint;
            oldPositions[i] = startPoint;
            velocities[i] = Vector3.zero;
            trueOldPositions[i] = startPoint;
            lambdasDist[i] = 0f;
            lambdasColl[i] = 0f;
            sticked[i] = false;
        }
    }

    public void ExtendRope()
    {
        if (!isAttached || hasReached || pointsThusFar >= nbPoints)
        {
            return;
        }

        Vector3 lastPoint = currPositions[pointsThusFar];
        Vector3 ropeDirection = direction;
        Ray ropeRay = new Ray(lastPoint, ropeDirection);

        RaycastHit extendHit;
        if (Physics.Raycast(ropeRay, out extendHit, unitCreatedPerFrame * distBetweenPoints * 1.15f))
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
            pointsThusFar = Mathf.Min(pointsThusFar + 1, nbPoints);

            hasReached = true;
            nbPoints = pointsThusFar;

            float scale = 0.15f;
            ScaleEveryVelocity(scale);

            if (Scenario.instance.fixedMoving) Scenario.instance.nowMoving = true;
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
                    newOldPositions[i - 1] = currPositions[i];
                    newVelocities[i - 1] = velocities[i];
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

        for (int i = 0; i < pointsThusFar; i++) trueOldPositions[i] = oldPositions[i];

        float t_unit = Time.fixedDeltaTime;
        float ts = t_unit / nIterations;
        if (ts <= 0f) return;

        Analyser.instance.stopwatch.Restart();

        if (GlobalParameters.instance.integration == GlobalParameters.Integration.Forward)
        {
            IntegratePositionAll(t_unit);
            IntegrateVelocityAll(t_unit);
            ScaleEveryVelocity(gravityDamping);
        }
        else if (GlobalParameters.instance.integration == GlobalParameters.Integration.Backward)
        {
            IntegrateVelocityAll(t_unit);
            ScaleEveryVelocity(gravityDamping);
            IntegratePositionAll(t_unit);
        }

        for (int n = 0; n < nIterations; n++)
        {
            for (int i = 0; i < pointsThusFar; i++) SolveDistanceConstraint(i, i + 1, ts);
            for (int i = 0; i <= pointsThusFar; i++) SolveCollisionConstraint(i, ts);
        }

        AdaptVelocities(t_unit);

        Analyser.instance.stopwatch.Stop();
        Analyser.instance.GetCPUResources().Add((float)Analyser.instance.stopwatch.Elapsed.TotalMilliseconds);

        Analyser.instance.EvaluateIteration(this);
    }

    public void IntegrateVelocityAll(float ts)
    {
        for (int i = 0; i <= pointsThusFar; i++) IntegrateVelocity(i, ts);
    }

    public void IntegrateVelocity(int i, float ts)
    {
        Vector3 forces = gravityScale / GetWeight(i);
        velocities[i] += ts * forces * GetWeight(i);
    }

    public void IntegratePositionAll(float ts)
    {
        for (int i = 0; i <= pointsThusFar; i++) IntegratePosition(i, ts);
    }

    public void IntegratePosition(int i, float ts)
    {
        if (sticked[i]) return;

        // we don't move the end point when it's attached to the building
        if (i == nbPoints && hasReached)
        {
            return;
        }

        // we don't move the start point when it's attached to the player on the ground
        if (i == 0 && isAttached)
        {
            oldPositions[0] = currPositions[0];
            currPositions[0] = shooter.transform.position;
            velocities[i] = player.GetVelocity();
            return;
        }

        Vector3 currentPos = currPositions[i];
        Vector3 vel = velocities[i];

        Vector3 nextPos = currentPos + ts * vel;

        oldPositions[i] = currentPos;
        currPositions[i] = nextPos;
    }

    public void SolveDistanceConstraint(int i, int j, float ts)
    {
        if (i > pointsThusFar || j > pointsThusFar) return;

        Vector3 delta = currPositions[j] - currPositions[i];
        float dist = delta.magnitude;
        if (dist <= 0.0001f) return;

        float w1 = GetWeight(i);
        float w2 = GetWeight(j);
        float wSum = w1 + w2;
        if (wSum <= 0f) return;

        float C = -dist + actualDistance;
        Vector3 n = delta / dist;
        float deltaLambda = 0f;

        if (GlobalParameters.instance.methode == GlobalParameters.Methods.PBD)
        {
            deltaLambda = -C / wSum;
        }

        else if (GlobalParameters.instance.methode == GlobalParameters.Methods.XPBD)
        {
            float alphats = compliance / (ts * ts);
            deltaLambda = -(C + alphats * lambdasDist[i]) / (wSum + alphats);

            lambdasDist[i] += deltaLambda;
        }

        Vector3 correctioni = deltaLambda * w1 * n;
        Vector3 correctionj = -deltaLambda * w2 * n;

        currPositions[i] += correctioni;
        currPositions[j] += correctionj;

        if (i == 0 && isAttached && hasReached)
        {
            Vector3 playerPos = player.GetCurrPos();
            float ratio = 1f;
            player.ropeForce += correctioni * ratio / (ts * ts) * playerMass;
            player.currPos += correctioni * (1f - ratio);

        }
    }

    public void SolveCollisionConstraint(int i, float ts)
    {
        if (i > pointsThusFar) return;
        if (i == pointsThusFar && hasReached) return;
        if (i == 0 && isAttached) return;

        Vector3 p = currPositions[i];
        Vector3 prev = oldPositions[i];
        Vector3 advance = p - prev;
        float dist = advance.magnitude;
        if (dist < 1e-6f)
        {
            SolveStaticPenetration(i);
            return;
        }

        Vector3 dir = advance.normalized;
        float maxDistance = dist + collRadius;

        RaycastHit hit;
        if (Physics.SphereCast(prev, collRadius, dir, out hit, maxDistance, envMask))
        {
            Vector3 n = hit.normal.normalized;
            Vector3 q = hit.point + n * collRadius;

            float C = Vector3.Dot(p - q, n);

            if (C < 0f)
            {
                float w = GetWeight(i);
                float deltaLambda = 0f;

                if (GlobalParameters.instance.methode == GlobalParameters.Methods.PBD)
                {
                    deltaLambda = -C / w;
                }
                else if (GlobalParameters.instance.methode == GlobalParameters.Methods.XPBD)
                {
                    float alphats = compliance / (ts * ts);
                    deltaLambda = -(C + alphats * lambdasColl[i]) / (w + alphats);
                    lambdasColl[i] += deltaLambda;
                }

                Vector3 correction = -deltaLambda * w * n * 0.1f;
                currPositions[i] += correction;
                currPositions[i] = q;

                sticked[i] = true;

                if (i == pointsThusFar) hasReached = true;
            }
        }
        else
        {
            SolveStaticPenetration(i);
        }
    }

    private void SolveStaticPenetration(int i)
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

            float insideDist = collRadius - dist;
            if (insideDist > 0f)
            {
                Vector3 correction = dir * insideDist;
                currPositions[i] += correction;
                oldPositions[i] += correction;
            }
        }
    }

    public float GetWeight(int i)
    {
        float m = pointMass;
        if (i == 0 && isAttached) m = playerMass;
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
            oldPositions[i] = currPositions[i];
        }
    }

    public void DestroyRope()
    {
        if (!isAttached && timeDropped >= 0 && Mathf.Abs(Time.time - timeDropped) > destroyDelay)
        {
            Destroy(gameObject);
            if (Scenario.instance.analysing) Analyser.instance.SaveResults();
        }
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
}