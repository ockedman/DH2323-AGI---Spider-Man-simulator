using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

public class Analyser : MonoBehaviour
{
    private List<float> distConstraint = new List<float>();
    private List<float> maxDistConstraint = new List<float>();
    private List<float> oscillations = new List<float>();
    private List<float> maxOscillations = new List<float>();
    private List<float> cpuResources = new List<float>();

    public Stopwatch stopwatch;

    public static Analyser instance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        stopwatch = new Stopwatch();
    }

    public List<float> GetCPUResources() => cpuResources;

    public void EvaluateIteration(RopeController rope)
    {
        ConstraintViolation(rope);
        EvaluateOscillations(rope);
    }

    public void EvaluateOscillations(RopeController rope)
    {
        if (rope.GetPointsThusFar() < 1) return;

        float totalOsc = 0f;
        float maxOsc = 0f;
        Vector3[] oldPos = rope.GetTrueOldPositions();
        Vector3[] currPos = rope.GetCurrPositions();
        for (int i = 0; i < rope.GetPointsThusFar(); i++)
        {
            Vector3 p = currPos[i];
            Vector3 x = oldPos[i];
            float osc = Vector3.Distance(p, x);
            totalOsc += Mathf.Pow(osc, 2);

            if (osc > maxOsc) maxOsc = osc;
        }

        oscillations.Add(totalOsc / rope.GetPointsThusFar());
        maxOscillations.Add(maxOsc);
    }

    public void ConstraintViolation(RopeController rope)
    {
        if (rope.GetPointsThusFar() < 1) return;

        float totalDiff = 0f;
        float maxDiff = 0f;
        float supposedDistance = rope.GetActualDistance();
        Vector3[] currPos = rope.GetCurrPositions();
        for (int i = 0; i < rope.GetPointsThusFar(); i++)
        {
            Vector3 pi = currPos[i];
            Vector3 pj = currPos[i + 1];
            float dij = Vector3.Distance(pi, pj);
            totalDiff += Mathf.Pow(dij - supposedDistance, 2);

            if (dij > maxDiff) maxDiff = dij;
        }

        distConstraint.Add(totalDiff / rope.GetPointsThusFar());
        maxDistConstraint.Add(maxDiff);
    }

    public void SaveResults()
    {
        UnityEngine.Debug.Log("saving the metrics");

        MetricsData data = new MetricsData();
        data.distError = distConstraint.ToArray();
        data.maxDistError = maxDistConstraint.ToArray();
        data.cpu = cpuResources.ToArray();
        data.oscillations = oscillations.ToArray();
        data.maxOscillations = oscillations.ToArray();

        data.method = GlobalParameters.instance.methode.ToString();
        data.integration = GlobalParameters.instance.integration.ToString();
        data.dt = Time.fixedDeltaTime;
        data.iterations = GlobalParameters.instance.nIterations;

        data.nPoints = GlobalParameters.instance.numberPoints;
        data.compliance = GlobalParameters.instance.compliance;
        data.playerMass = GlobalParameters.instance.playerMass;
        data.pointMass = GlobalParameters.instance.ropePointMass;

        string sceneName = SceneManager.GetActiveScene().name;
        string fileName = "rope_metrics.json";
        fileName = sceneName + "_" + data.method + "_" + data.integration + "_" + fileName;

        string json = JsonUtility.ToJson(data, true);
        string projectRoot = Directory.GetParent(Application.dataPath).FullName + "\\Data";
        string path = Path.Combine(projectRoot, fileName);

        File.WriteAllText(path, json);
    }


    [System.Serializable]
    public class MetricsData
    {
        public float[] distError;
        public float[] maxDistError;
        public float[] oscillations;
        public float[] maxOscillations;
        public float[] cpu;
        public string method;
        public string integration;
        public float dt;
        public int iterations;
        public int nPoints;
        public float compliance;
        public float playerMass;
        public float pointMass;
    }
}