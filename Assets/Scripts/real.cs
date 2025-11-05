using UnityEngine;

public class RopeController2 : MonoBehaviour
{
	[Header("Player")]
	public LineRenderer _webRenderer;
	public PlayerController player;
	public float playerWeight;

	[Header("Position vectors")]
	public Vector3 startPoint;
	public Vector3 direction;
	public Vector3 corrFirstPoint = Vector3.zero;

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
	public bool passedLimit = false;

	[Header("Collision and dropping")]
	private int envMask;
	public float collRadius = 0.002f;
	public float timeDropped = -1f;

	[Header("Parameters")]
	public float destroyDelay = 5f;
	public Vector3 gravityScale = new Vector3(0f, -9.81f, 0f);
	public int unitCreatedPerFrame = 5;
	public int nIterations = 30;

	public bool enableDebug = true;

	void Awake()
	{
		if (player == null)
			player = GameObject.Find("Player")?.GetComponent<PlayerController>();

		_webRenderer = GetComponent<LineRenderer>();
		if (_webRenderer == null)
		{
			enabled = false;
			return;
		}

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

	void FixedUpdate()
	{
		CheckEndCollision();
	}

	void Update()
	{
		if (player == null) return;

		playerWeight = player.GetWeight();
		bool isOnAir = player.GetIsOnAir();
		if (!isOnAir && isAttached) playerWeight = Mathf.Infinity;

		ExtendRope();
		WebMove();
		DestroyRope();
	}

	void LateUpdate()
	{
		AdaptRenderer();
	}

	public Vector3 GetFirstPoint()
	{
		if (currPositions != null) return currPositions[0];
		else return Vector3.zero;
	}

	public bool GetReached() => hasReached;

	public bool GetPassedLimit() => passedLimit;

	public Vector3 GetCorrFirstPoint() => corrFirstPoint;

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
		velocities[0] = player != null ? player.GetVelocity() : Vector3.zero;

		for (int i = 1; i <= nbPoints; i++)
		{
			currPositions[i] = startPoint;
			oldPositions[i] = startPoint;
			velocities[i] = Vector3.zero;
		}
	}

	public void InitializeAboutPoints(float ropeLength)
	{
		nbPoints = ropeLength < 6f ? 30 : 150;
		distBetweenPoints = ropeLength / nbPoints;
		Debug.Log($"Rope initialized with {nbPoints} points, spacing = {distBetweenPoints}");
	}

	public void FindReachedPoint(Ray cameraRay, RaycastHit shootRay)
	{
		direction = (shootRay.point - startPoint).normalized;
		float ropeLength = (shootRay.point - startPoint).magnitude;

		InitializeAboutPoints(ropeLength);
		InitializeVectors();
		CreateInitialPoints();
	}

	public void FindEndPoint(Ray cameraRay, float range)
	{
		direction = cameraRay.direction.normalized;
		InitializeAboutPoints(range);
		InitializeVectors();
		CreateInitialPoints();
	}

	private void CreateInitialPoints()
	{
		int pointsToCreate = Mathf.Min(unitCreatedPerFrame, nbPoints);
		_webRenderer.positionCount = pointsToCreate + 1;

		for (int i = 1; i <= pointsToCreate; i++)
		{
			Vector3 nextPoint = startPoint + distBetweenPoints * i * direction;
			_webRenderer.SetPosition(i, nextPoint);

			oldPositions[i] = nextPoint - (direction * distBetweenPoints * 0.5f);
			currPositions[i] = nextPoint;
			velocities[i] = direction * 10f;
		}
		pointsThusFar = pointsToCreate;
	}

	public void ExtendRope()
	{
		if (!isAttached || hasReached || pointsThusFar >= nbPoints) return;

		int pointsToAdd = Mathf.Min(unitCreatedPerFrame, nbPoints - pointsThusFar);

		// Décaler les anciens points
		for (int i = pointsThusFar; i >= 1; i--)
		{
			int newIndex = i + pointsToAdd;
			if (newIndex <= nbPoints)
			{
				currPositions[newIndex] = currPositions[i] + direction * distBetweenPoints * pointsToAdd;
				oldPositions[newIndex] = currPositions[i];
				velocities[newIndex] = velocities[i];
			}
		}

		// Créer les nouveaux points
		for (int i = 1; i <= pointsToAdd; i++)
		{
			Vector3 nextPoint = startPoint + distBetweenPoints * i * direction;
			currPositions[i] = nextPoint;
			oldPositions[i] = startPoint;
			velocities[i] = direction * 10f;
		}

		pointsThusFar = Mathf.Min(pointsThusFar + pointsToAdd, nbPoints);
		if (pointsThusFar >= nbPoints) CheckEndCollision();
	}

	private void WebMove()
	{
		if (currPositions == null || pointsThusFar == 0) return;

		float ts = Time.deltaTime / nIterations;
		if (ts <= 0f) return;

		for (int n = 0; n < nIterations; n++)
		{
			for (int i = 0; i <= pointsThusFar; i++) ApplyGravity(i, ts);
			//for (int i = 0; i < pointsThusFar; i++) SolveDistanceConstraint(i, i + 1, ts);
			for (int i = 0; i < pointsThusFar; i++) SolveDistanceConstraint(i, i + 1);
			for (int i = 0; i <= pointsThusFar; i++) SolveCollisionConstraint(i);

			AdaptVelocities(ts);
		}
	}

	public void ApplyGravity(int i, float ts)
	{
		// we don't move the end point when it's attached to the building
		if (i == nbPoints && hasReached)
		{
			currPositions[i] = oldPositions[i] = currPositions[i];
			return;
		}

		// we don't move the start point when it's attached to the player on the ground
		if (i == 0 && isAttached)
		{
			oldPositions[0] = currPositions[0];
			currPositions[0] = player.transform.position;
			velocities[i] = player.GetVelocity();
			return;
		}

		float damping = 0.98f;
		Vector3 start = currPositions[i];
		Vector3 vel = velocities[i] + ts * gravityScale;
		vel *= damping;
		Vector3 nextPos = start + ts * vel;

		velocities[i] = vel;
		oldPositions[i] = start;
		currPositions[i] = nextPos;
	}

	public void SolveDistanceConstraint(int i, int j)
	{
		if (i >= currPositions.Length || j >= currPositions.Length) return;

		Vector3 delta = currPositions[j] - currPositions[i];
		float dist = delta.magnitude;
		if (dist <= 0.0001f) return;


		if (i == 0 && dist > distBetweenPoints * 1.5f)
		{
			//passedLimit = true;
			Vector3 p0 = currPositions[0];
			Vector3 p1 = currPositions[1];
			corrFirstPoint = p1 + (p0 - p1).normalized * distBetweenPoints;
			//Debug.Log("first point doesn't respect the limit, with " + dist);
		}


		float change = 0f;
		if (dist > distBetweenPoints * 1.5f) change = distBetweenPoints * 1.5f;
		else change = distBetweenPoints;
		//float change = distBetweenPoints;

		float diff = (dist - change) / dist;

		float w1 = GetWeight(i);
		float w2 = GetWeight(j);
		float wSum = w1 + w2;
		if (wSum <= 0f) return;

		// corrige proportionnellement aux poids
		currPositions[i] += delta * (w1 / wSum) * diff;
		currPositions[j] -= delta * (w2 / wSum) * diff;
	}

	public void SolveCollisionConstraint(int i)
	{
		if (i >= currPositions.Length) return;

		Vector3 point = currPositions[i];
		Collider[] colliders = Physics.OverlapSphere(point, collRadius, envMask);

		foreach (Collider c in colliders)
		{
			Vector3 closestPoint = c.ClosestPoint(point);
			float dist = (closestPoint - point).magnitude;

			if (dist < collRadius)
			{
				Vector3 collNormal = (point - closestPoint).normalized;
				point += collNormal * (collRadius - dist) * 1.01f;
				currPositions[i] = point;
			}
		}
	}

	public float GetWeight(int i)
	{
		float m = 0.1f;
		if (i == 0 && isAttached) m = playerWeight;
		else if (i == pointsThusFar && hasReached) m = Mathf.Infinity;

		return 1f / m;
	}

	public void AdaptVelocities(float ts)
	{
		if (ts <= 0f) return;

		for (int i = 0; i <= pointsThusFar; i++)
			velocities[i] = (currPositions[i] - oldPositions[i]) / ts;
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

	public void CheckEndCollision()
	{
		if (pointsThusFar <= 0 || hasReached) return;

		Vector3 lastPoint = currPositions[pointsThusFar];
		Collider[] hitColliders = Physics.OverlapSphere(lastPoint, collRadius, envMask);

		if (hitColliders.Length > 0)
		{
			hasReached = true;
			for (int i = 0; i <= pointsThusFar; i++)
			{
				oldPositions[i] = currPositions[i];
				velocities[i] = Vector3.zero;
			}
		}
	}

	void OnDrawGizmos()
	{
		if (!enableDebug || currPositions == null || pointsThusFar == 0) return;

		Gizmos.color = Color.yellow;
		for (int i = 0; i <= pointsThusFar; i++)
		{
			if (velocities[i].magnitude > 0.1f)
				Gizmos.DrawRay(currPositions[i], velocities[i] * 0.1f);
		}

		Gizmos.color = Color.red;
		for (int i = 0; i <= pointsThusFar; i++)
		{
			if (velocities[i].magnitude > 20f)
				Gizmos.DrawWireSphere(currPositions[i], 1f);
		}

		Gizmos.color = Color.blue;
		for (int i = 0; i <= pointsThusFar; i++)
		{
			Gizmos.DrawWireSphere(currPositions[i], 0.2f);
		}

		Gizmos.color = Color.green;
		for (int i = 0; i < pointsThusFar; i++)
			Gizmos.DrawLine(currPositions[i], currPositions[i + 1]);
	}
}
