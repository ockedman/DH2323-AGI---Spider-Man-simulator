using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RotatingText : MonoBehaviour
{
    public CameraPOV cameraPOV;
    private TextMeshProUGUI textValue;
    private string baseText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textValue = GetComponent<TextMeshProUGUI>();
        baseText = textValue.text;
    }

    // Update is called once per frame
    void Update()
    {
        textValue.text = baseText + cameraPOV.GetCanRotate();
    }
}
