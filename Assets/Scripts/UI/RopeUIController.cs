using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Slider;
using TMPro;

public class RopeUIController : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider distanceSlider;
    public Slider collSlider;
    public Slider massSlider;
    public Slider gravitySlider;
    public Slider iterationsSlider;
    public Button restartButton;

    [Header("Ropes Data")]
    public GameObject ropeGroup;
    private RopeController[] allRopes;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        iterationsSlider.onValueChanged.AddListener(v =>
        {
            allRopes = ropeGroup.transform.GetComponentsInChildren<RopeController>();
            foreach (var rope in allRopes)
            {
                rope.nIterations = Mathf.RoundToInt(v);
            }
        });

        distanceSlider.onValueChanged.AddListener(v =>
        {
            allRopes = ropeGroup.transform.GetComponentsInChildren<RopeController>();
            foreach (var rope in allRopes)
            {
                rope.distBetweenPoints = v;
            }
        });

        collSlider.onValueChanged.AddListener(v =>
        {
            allRopes = ropeGroup.transform.GetComponentsInChildren<RopeController>();
            foreach (var rope in allRopes)
            {
                rope.collRadius = v;
            }
        });

        massSlider.onValueChanged.AddListener(v =>
        {
            allRopes = ropeGroup.transform.GetComponentsInChildren<RopeController>();
            foreach (var rope in allRopes)
            {
                rope.pointMass = v;
            }
        });

        gravitySlider.onValueChanged.AddListener(v =>
        {
            allRopes = ropeGroup.transform.GetComponentsInChildren<RopeController>();
            foreach (var rope in allRopes)
            {
                rope.gravityDamping = v;
            }
        });
    }
}