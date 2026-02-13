using UnityEngine;
using System.Collections.Generic;

public class AlgorithmTesting : MonoBehaviour
{
    public bool isEnabled = false;
    public Vector2 pixelSize = new Vector2(256, 256);
    public Vector2 physicalSize = new Vector2(16f, 16f);
    public float height = 50f;

    [Header("Settings")]
    public FBMSettings fbmSettings;

    [Header("Animation")]
    public bool autoUpdate = false;
    public float updateInterval = 0.1f;
    private float accumulatedTime = 0f;

    private FBMSettings lastSettings;
    private float lastHeight;

    void Start()
    {
        lastSettings = fbmSettings.GetCopy();
        lastHeight = height;
        if (isEnabled)
            GenerateFBM();
    }

    void Update()
    {
        if (isEnabled)
        {
            accumulatedTime += Time.deltaTime;
            if (autoUpdate && accumulatedTime >= updateInterval && (!fbmSettings.SameSettings(lastSettings) || lastHeight != height))
            {
                accumulatedTime = 0f;
                GenerateFBM();

                lastSettings = fbmSettings.GetCopy();
                lastHeight = height;
            }
            else if (Input.GetKeyDown(KeyCode.F))
                GenerateFBM();
        }
    }

    public void GenerateFBM()
    {
        List<List<float>> fbmHeightMap = GameManager.Instance.fbmGenerator.GetHeightMap(pixelSize, fbmSettings);
        Mesh mesh = GameManager.Instance.meshGenerator.HeightMapToMesh(fbmHeightMap, height / fbmSettings.scale, physicalSize, false);
        GameManager.Instance.meshGenerator.ShowMesh(mesh);
    }
}
