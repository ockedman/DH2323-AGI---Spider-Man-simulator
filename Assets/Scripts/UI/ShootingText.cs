using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShootingText : MonoBehaviour
{
    public WebShooter shooter;
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
        textValue.text = baseText + shooter.GetCanShoot();
    }
}
