using UnityEngine;
using UnityEngine.UI;

public class FixInputLayout : MonoBehaviour
{
    void Start()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            GetComponent<RectTransform>()
        );
    }
}
