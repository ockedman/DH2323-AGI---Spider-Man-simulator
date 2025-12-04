using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShowSliderValue : MonoBehaviour
{
    private TextMeshProUGUI textValue;
    public Slider slider;
    private string baseText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textValue = GetComponent<TextMeshProUGUI>();
        baseText = textValue.text + "\n(" + slider.minValue + "-" + slider.maxValue + "): ";
    }

    // Update is called once per frame
    void Update()
    {
        textValue.text = baseText + slider.value;
    }
}
