using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Speedometer : MonoBehaviour
{
    [SerializeField] private Image track;
    [SerializeField] private Image redline;
    [SerializeField] private TMP_Text gearText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private RectTransform needle;
    [SerializeField] private float damping;
    
    [HideInInspector] public float rpm;
    [HideInInspector] public int speed;
    [HideInInspector] public float redline_rpm;
    [HideInInspector] public string current_gear;

    private float normalized_rpm;
    private float needle_rpm;

    private void FixedUpdate()
    {
        Tachometer();
    }

    public void SetRedline(float value)
    {
        redline_rpm = value;
        redline.fillAmount = (10000f - redline_rpm) / 14000f;
    }

    private void Tachometer()
    {
        rpm = Mathf.Clamp(rpm, 0, 10000);

        normalized_rpm = Mathf.Lerp(normalized_rpm, rpm / 13333.34f, Time.deltaTime * damping);
        track.fillAmount = normalized_rpm;

        speed = Mathf.Clamp(speed, 0, 999);
        speedText.text = speed.ToString();

        needle_rpm = (normalized_rpm - 0.375f) * -360;
        needle.localEulerAngles = new Vector3(0f, 0f, needle_rpm);

        gearText.text = current_gear;
    }
}
