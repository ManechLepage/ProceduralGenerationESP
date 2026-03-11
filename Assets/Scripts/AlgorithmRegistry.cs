using UnityEngine;
using System.Collections.Generic;

public class AlgorithmRegistry : MonoBehaviour
{
    /*
    Fichier gérant la sauvegarde des algorithmes utilisés pour ensuite créer des graphiques
    contenant le nom de l'algorithme ainsi que le temps de génération.    
    */
    
    public static AlgorithmRegistry Instance { get; private set; }
    public List<string> activeAlgorithms = new List<string>();
    public AlgorithmRegistry registry;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // S'assurer qu'il n'y a qu'une seule instance de AlgorithmRegistry en tout temps
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(string name)
    {
        /*
        Ajouter le nom d'un algorithme à la liste des algorithmes actifs pour le suivi des performances.
         - 'name': nom de l'algorithme à enregistrer
         - return: void
        */

        if (!activeAlgorithms.Contains(name))
            activeAlgorithms.Add(name);
    }

    public void Unregister(string name)
    {
        /*
        Retirer le nom d'un algorithme de la liste des algorithmes actifs.
         - 'name': nom de l'algorithme à désenregistrer
         - return: void
        */

        activeAlgorithms.Remove(name);
    }

    public List<string> GetAlgorithmList()
    {
        /*
        Obtenir la liste des algorithmes actifs.
         - return: liste des noms des algorithmes actifs
        */

        if (registry == null || registry.activeAlgorithms == null)
        {
            Debug.LogWarning("AlgoData: No registry found.");
            return new List<string>();
        }
        return new List<string>(registry.activeAlgorithms);
    }
}