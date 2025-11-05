using UnityEngine;

public class RopeController : MonoBehaviour
{
    [Header("Player")]
    public LineRenderer _webRenderer;
    public PlayerController player;
    public float playerWeight;

    [Header("Position vectors")]
    public Vector3 startPoint;
    public Vector3 direction;

    [Header("Point properties")]
    public int nbPoints = 0;
    public int pointsThusFar = 0;
    public float distBetweenPoints = 0;

    [Header("Points and velocities")]
    public Vector3[] currPositions;
    public Vector3[] oldPositions;
    public Vector3[] velocities;

    [Header("Bools")]
    public bool isAttached = false;
    public bool hasReached = false;

    [Header("Collision and dropping")]
    private int envMask;
    public float collRadius = 0.01f;
    public float timeDropped = -1f;

    [Header("Parameters")]
    public float destroyDelay = 5f;
    public Vector3 gravityScale = new Vector3(0f, -9.81f, 0f);  // Gravité standard
    public int unitCreatedPerFrame = 5;
    public int nIterations = 5;
    public float damping = 0.98f;  // Amortissement plus réaliste
    public float restitution = 0.3f;  // Coefficient de restitution pour les rebonds

    void Awake()
    {
        player = GameObject.Find("Player").GetComponent<PlayerController>();
        _webRenderer = GetComponent<LineRenderer>();
        startPoint = transform.position;

        _webRenderer.positionCount = 1;
        _webRenderer.startWidth = 0.15f;
        _webRenderer.endWidth = 0.15f;
        _webRenderer.material = new Material(Shader.Find("Unlit/Color"));
        _webRenderer.material.color = Color.white;

        _webRenderer.enabled = true;
        _webRenderer.SetPosition(0, startPoint);

        isAttached = true;
        envMask = LayerMask.GetMask("Environment");
    }

    void Update()
    {
        playerWeight = player.GetWeight();
        bool isOnAir = player.GetIsOnAir();
        if (!isOnAir && isAttached) playerWeight = float.MaxValue;

        ExtendRope();
        WebMove();
        DestroyRope();
    }

    void LateUpdate()
    {
        if (pointsThusFar == 0) return;
        for (int i = 0; i <= pointsThusFar; i++)
        {
            _webRenderer.SetPosition(i, currPositions[i]);
        }
    }

    public bool GetReached()
    {
        return hasReached;
    }

    public void Dettach()
    {
        isAttached = false;
        timeDropped = Time.time;
    }

    public void InitializeVectors()
    {
        currPositions = new Vector3[nbPoints + 1];
        oldPositions = new Vector3[nbPoints + 1];
        velocities = new Vector3[nbPoints + 1];

        currPositions[0] = startPoint;
        oldPositions[0] = startPoint;
        velocities[0] = player.GetVelocity();

        for (int i = 1; i <= nbPoints; i++)
        {
            velocities[i] = Vector3.zero;
        }
    }

    public void InitializeAboutPoints(float ropeLength)
    {
        if (ropeLength < 6f)
        {
            nbPoints = 10;
        }
        else
        {
            nbPoints = 50;
        }
        distBetweenPoints = ropeLength / nbPoints;
    }

    public void FindReachedPoint(Ray cameraRay, RaycastHit shootRay)
    {
        direction = shootRay.point - startPoint;
        float ropeLength = direction.magnitude;

        InitializeAboutPoints(ropeLength);
        InitializeVectors();

        direction = direction.normalized;
        _webRenderer.positionCount += unitCreatedPerFrame;

        for (int i = 1; i <= unitCreatedPerFrame; i++)
        {
            Vector3 nextPoint = startPoint + distBetweenPoints * i * direction;
            _webRenderer.SetPosition(i, nextPoint);

            currPositions[i] = nextPoint;
            oldPositions[i] = nextPoint - (direction * distBetweenPoints * 0.5f);  // Meilleure initialisation
            velocities[i] = direction * 10f;  // Vitesse initiale de lancement
        }

        pointsThusFar = unitCreatedPerFrame;
    }

    public void FindEndPoint(Ray cameraRay, float range)
    {
        direction = cameraRay.direction;

        float ropeLength = range;
        InitializeAboutPoints(ropeLength);
        InitializeVectors();

        direction = direction.normalized;
        _webRenderer.positionCount += unitCreatedPerFrame;

        for (int i = 1; i <= unitCreatedPerFrame; i++)
        {
            Vector3 nextPoint = startPoint + distBetweenPoints * i * direction;
            _webRenderer.SetPosition(i, nextPoint);

            currPositions[i] = nextPoint;
            oldPositions[i] = nextPoint - (direction * distBetweenPoints * 0.5f);
            velocities[i] = direction * 10f;
        }

        pointsThusFar = unitCreatedPerFrame;
    }

    public void ExtendRope()
    {
        if (isAttached && !hasReached && pointsThusFar < nbPoints)
        {
            _webRenderer.positionCount += unitCreatedPerFrame;
            
            // Décaler les points existants
            for (int i = pointsThusFar; i > 0; i--)
            {
                if (i + unitCreatedPerFrame <= nbPoints)
                {
                    currPositions[i + unitCreatedPerFrame] = currPositions[i];
                    oldPositions[i + unitCreatedPerFrame] = oldPositions[i];
                    velocities[i + unitCreatedPerFrame] = velocities[i];
                }
            }

            // Ajouter les nouveaux points
            for (int i = 1; i <= unitCreatedPerFrame && i <= nbPoints; i++)
            {
                Vector3 nextPoint = startPoint + distBetweenPoints * i * direction;
                currPositions[i] = nextPoint;
                oldPositions[i] = nextPoint - (direction * distBetweenPoints * 0.5f);
                velocities[i] = direction * 10f;
            }
            
            pointsThusFar += unitCreatedPerFrame;
            if (pointsThusFar > nbPoints) pointsThusFar = nbPoints;

            if (pointsThusFar >= nbPoints)
            {
                CheckEndCollision();
            }
        }
    }

    private void WebMove()
    {
        if (pointsThusFar == 0) return;

        float ts = Time.deltaTime / nIterations;
        
        for (int n = 0; n < nIterations; n++)
        {
            // Intégration de Verlet
            for (int i = 0; i <= pointsThusFar; i++)
            {
                IntegrateVerlet(i, ts);
            }

            // Contraintes de distance IMPORTANTES
            for (int i = 0; i < pointsThusFar; i++)
            {
                SolveDistanceConstraint(i, i + 1);
            }

            // Contraintes de collision
            for (int i = 0; i <= pointsThusFar; i++)
            {
                SolveCollisionConstraint(i);
            }
        }

        // Mise à jour des vitesses après toutes les contraintes
        UpdateVelocities();
    }

    public void IntegrateVerlet(int i, float ts)
    {
        // Point fixe au bout
        if (i == nbPoints && hasReached)
        {
            return;
        }

        // Point attaché au joueur
        if (i == 0 && isAttached)
        {
            oldPositions[0] = currPositions[0];
            currPositions[0] = player.transform.position;
            velocities[0] = player.GetVelocity();
            return;
        }

        // Intégration de Verlet avec amortissement
        Vector3 temp = currPositions[i];
        Vector3 vel = (currPositions[i] - oldPositions[i]) * damping;
        vel += gravityScale * ts * ts;
        
        currPositions[i] = currPositions[i] + vel;
        oldPositions[i] = temp;
        velocities[i] = vel / ts;
    }

    public void SolveDistanceConstraint(int i, int j)
    {
        Vector3 delta = currPositions[j] - currPositions[i];
        float distance = delta.magnitude;
        
        if (distance > 0.0001f)  // Éviter division par zéro
        {
            float error = (distBetweenPoints - distance) / distance;
            Vector3 correction = delta * error * 0.5f;

            float w1 = GetWeight(i);
            float w2 = GetWeight(j);
            float totalWeight = w1 + w2;

            if (totalWeight > 0)
            {
                // Correction pondérée
                currPositions[i] -= correction * (w1 / totalWeight);
                currPositions[j] += correction * (w2 / totalWeight);
            }
        }
    }

    public void SolveCollisionConstraint(int i)
    {
        Vector3 pos = currPositions[i];
        
        // Vérifier s'il y a collision
        Collider[] colliders = Physics.OverlapSphere(pos, collRadius, envMask);
        
        if (colliders.Length > 0)
        {
            // Trouver le point de collision le plus proche
            Vector3 closestPoint = colliders[0].ClosestPoint(pos);
            Vector3 normal = (pos - closestPoint).normalized;
            
            if (normal == Vector3.zero)
                normal = Vector3.up;
            
            // Projeter le point hors de la collision
            Vector3 correctedPos = closestPoint + normal * (collRadius * 1.01f);
            
            // Calculer la vitesse réfléchie pour le rebond
            Vector3 vel = currPositions[i] - oldPositions[i];
            Vector3 velNormal = Vector3.Project(vel, normal);
            Vector3 velTangent = vel - velNormal;
            
            // Appliquer la restitution et la friction
            vel = velTangent * 0.9f - velNormal * restitution;
            
            // Mettre à jour les positions
            oldPositions[i] = correctedPos - vel;
            currPositions[i] = correctedPos;
        }
    }

    public float GetWeight(int i)
    {
        if (i == 0 && isAttached) return 0f;  // Point fixe
        if (i == nbPoints && hasReached) return 0f;  // Point fixe
        return 1f;  // Point libre
    }

    public void UpdateVelocities()
    {
        for (int i = 0; i <= pointsThusFar; i++)
        {
            velocities[i] = (currPositions[i] - oldPositions[i]) / Time.deltaTime;
        }
    }

    public void DestroyRope()
    {
        if (!isAttached && Time.time >= timeDropped + destroyDelay)
        {
            Destroy(gameObject);
        }
    }

    public void CheckEndCollision()
    {
        if (pointsThusFar <= 0) return;

        Vector3 lastPoint = currPositions[pointsThusFar];
        Collider[] hitColliders = Physics.OverlapSphere(lastPoint, collRadius * 2f, envMask);

        if (hitColliders.Length > 0)
        {
            hasReached = true;
            // Fixer le point final exactement sur la surface
            Vector3 closestPoint = hitColliders[0].ClosestPoint(lastPoint);
            Vector3 normal = (lastPoint - closestPoint).normalized;
            if (normal == Vector3.zero) normal = Vector3.up;
            currPositions[pointsThusFar] = closestPoint + normal * collRadius;
            oldPositions[pointsThusFar] = currPositions[pointsThusFar];
        }
    }

    void OnDrawGizmos()
    {
        if (currPositions == null || pointsThusFar == 0) return;

        // Dessiner les points de la corde
        for (int i = 0; i <= pointsThusFar; i++)
        {
            Vector3 pos = currPositions[i];
            
            // Couleur différente selon l'état
            Color color = Color.white;
            if (i == 0) color = Color.green;  // Point de départ
            else if (i == pointsThusFar) color = hasReached ? Color.red : Color.yellow;  // Point final
            
            Gizmos.color = color;
            Gizmos.DrawWireSphere(pos, collRadius * 2f);
        }

        // Dessiner les segments
        Gizmos.color = Color.cyan;
        for (int i = 0; i < pointsThusFar; i++)
        {
            Gizmos.DrawLine(currPositions[i], currPositions[i + 1]);
        }
    }
}