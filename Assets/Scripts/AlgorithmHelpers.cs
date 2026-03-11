using UnityEngine;

public class AlgorithmHelpers : MonoBehaviour
{
    /*
    Pour le moment, cette classe ne sert qu'à comparer un paramètre de génération du FBM
    en comparant la similitude entre des AnimationCurves (gèrent la pente du terrain).
    */
    
    public bool EqualAnimationCurves(AnimationCurve a, AnimationCurve b)
    {
        /*
        Compare deux AnimationCurves pour vérifier leur égalité en regardant
        la hauteur à chaque point dans le temps.
         - 'a': première courbe à comparer
         - 'b': deuxième courbe à comparer
         - return: true si les courbes sont identiques, false sinon
        */
        
        if (a.length != b.length) return false;
        for (int i = 0; i < a.length; i++)
        {
            if (a.keys[i].time != b.keys[i].time || a.keys[i].value != b.keys[i].value)
                return false;
        }
        return true;
    }
}
