using UnityEngine;

public class AlgorithmHelpers : MonoBehaviour
{
    public bool EqualAnimationCurves(AnimationCurve a, AnimationCurve b)
    {
        if (a.length != b.length) return false;
        for (int i = 0; i < a.length; i++)
        {
            if (a.keys[i].time != b.keys[i].time || a.keys[i].value != b.keys[i].value)
                return false;
        }
        return true;
    }
}
