using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteInEditMode] [RequireComponent(typeof(TextMeshProUGUI))]
public class TimeValueColorManager : MonoBehaviour
{
    public Gradient colorGradient;
    private TextMeshProUGUI timeText;

    void Awake()
    {
        timeText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (timeText.text != "")
        {
            if (int.TryParse(timeText.text, out int timeValue))
            {
                timeText.color = GetColorFromTime((float)timeValue);
            }
        }
    }

    Color GetColorFromTime(float time)
    {
        float timeMsToGradient = time / 5000f;
        return colorGradient.Evaluate(timeMsToGradient);
    }
}
