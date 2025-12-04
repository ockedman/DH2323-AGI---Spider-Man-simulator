using UnityEngine;

public class GlobalParameters : MonoBehaviour
{
    [Header("Simulation Properties")]
    public int nIterations;

    [Header("Rope Properties")]
    public int numberPoints;
    public float distBetweenPoints;
    public float collRadius;
    public float ropePointMass;
    public float velDamping;

    [Header("Player Properties")]
    public float playerMass;
    public float playerRadius;

    [Header("XPBD Properties")]
    public float compliance;

    public enum Methods
    {
        PBD,
        XPBD,
    };

    public enum Integration
    {
        Forward,
        Backward
    };

    public Methods methode;
    public Integration integration;

    public static GlobalParameters instance;

    void Awake()
    {
        instance = this;
    }
}
