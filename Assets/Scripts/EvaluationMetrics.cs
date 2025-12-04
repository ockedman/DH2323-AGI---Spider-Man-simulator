using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;

public class EvaluationMetrics : MonoBehaviour
{
    public RopeController rope;

    // stockage des mesures 
    private List<float> distErrorHistory = new List<float>();
    private List<float> maxStretchHistory = new List<float>();
    private List<float> energyHistory = new List<float>();
    private List<float> cpuHistory = new List<float>();

    private Stopwatch stopwatch;

    void Start()
    {
        stopwatch = new Stopwatch();
    }

    void FixedUpdate()
    {
        stopwatch.Restart();

        if (rope == null || rope.GetCurrPositions() == null) return;

        RecordDistanceError();
        RecordEnergy();
        
        stopwatch.Stop();
        cpuHistory.Add((float)stopwatch.Elapsed.TotalMilliseconds);
    }

    void RecordDistanceError()
    {
        Vector3[] pos = rope.GetCurrPositions();
        int n = rope.GetPointsThusFar();
        float d = rope.distBetweenPoints;

        float totalError = 0f;
        float maxStretch = 0f;

        for (int i=0; i < n; i++)
        {
            float dist = (pos[i+1] - pos[i]).magnitude;
            float err = Mathf.Abs(dist - d);

            totalError += err;
            if (err > maxStretch) maxStretch = err;
        }

        distErrorHistory.Add(totalError / n);
        maxStretchHistory.Add(maxStretch);
    }

    void RecordEnergy()
    {
        Vector3[] pos = rope.GetCurrPositions();
        Vector3[] vel = rope.GetOldPositions(); // ou rope.velocities si tu préfères
        int n = rope.GetPointsThusFar();

        float totalEnergy = 0f;

        for (int i = 0; i <= n; i++)
        {
            float m = GlobalParameters.instance.ropePointMass;

            float kinetic = 0.5f * m * vel[i].sqrMagnitude;
            float potential = m * -rope.gravityScale.y * pos[i].y;

            totalEnergy += kinetic + potential;
        }

        energyHistory.Add(totalEnergy);
    }

    // Export pour ton rapport
    public float[] GetDistError() => distErrorHistory.ToArray();
    public float[] GetMaxStretch() => maxStretchHistory.ToArray();
    public float[] GetEnergy() => energyHistory.ToArray();
    public float[] GetCPU() => cpuHistory.ToArray();
}
