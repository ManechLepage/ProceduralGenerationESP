using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ConnectionColorUpdater : MonoBehaviour
{
    public List<Image> connectionImages;
    private List<Color> initialColors;

    public void Start()
    {
        initialColors = new List<Color>();
        foreach (var image in connectionImages)
        {
            initialColors.Add(image.color);
        }
    }

    public void Disable()
    {
        for (int i = 0; i < connectionImages.Count; i++)
        {
            Color color = Color.gray;
            color.a = connectionImages[i].color.a;
            connectionImages[i].color = color;
        }
    }

    public void Enable()
    {
        for (int i = 0; i < connectionImages.Count; i++)
        {
            connectionImages[i].color = initialColors[i];
        }
    }
}
