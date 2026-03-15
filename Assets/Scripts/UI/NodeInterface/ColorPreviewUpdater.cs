using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ColorPreviewUpdater : MonoBehaviour
{
    public TMP_InputField rInput;
    public TMP_InputField gInput;
    public TMP_InputField bInput;

    void Awake()
    {
        rInput.onValueChanged.AddListener(delegate { UpdateColorPreview(); });
        gInput.onValueChanged.AddListener(delegate { UpdateColorPreview(); });
        bInput.onValueChanged.AddListener(delegate { UpdateColorPreview(); });
    }

    public void UpdateColorPreview()
    {
        int r = int.TryParse(rInput.text, out r) ? r : 0;
        int g = int.TryParse(gInput.text, out g) ? g : 0;
        int b = int.TryParse(bInput.text, out b) ? b : 0;

        r = Mathf.Clamp(r, 0, 255);
        g = Mathf.Clamp(g, 0, 255);
        b = Mathf.Clamp(b, 0, 255);

        GetComponent<RawImage>().color = new Color(r / 255f, g / 255f, b / 255f);
    }
}
