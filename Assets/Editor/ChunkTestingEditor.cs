using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChunkTesting))]
public class ChunkTestingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChunkTesting myTarget = (ChunkTesting)target;

        if (GUILayout.Button("Reload Chunks"))
        {
            myTarget.ReloadChunks();
        }
    }
}
