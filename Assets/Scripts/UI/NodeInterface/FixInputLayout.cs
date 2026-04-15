using UnityEngine;
using UnityEngine.UI;

public class FixInputLayout : MonoBehaviour
{
    void Start()
    {
        Reload();
    }

    public void Reload()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            GetComponent<RectTransform>()
        );
    }
}
