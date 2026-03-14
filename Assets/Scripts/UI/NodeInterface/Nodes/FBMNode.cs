using UnityEngine;

public class FBMNode : NodeBehaviour
{
    public FBMAlgorithm fbmAlgorithm;

    public override void Start()
    {
        base.Start();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    public override Variant Fire()
    {
        float scale = GetInputValue("scale").GetValue<float>();
        int octaves = GetInputValue("octaves").GetValue<int>();
        float persistence = GetInputValue("persistance").GetValue<float>();
        int seed = GetInputValue("seed").GetValue<int>();
        Vector2 offset = GetInputValue("offset").GetValue<Vector2>();

        Debug.Log("Firing FBMNode - Scale: " + scale + ", Octaves: " + octaves + ", Persistance: " + persistence + ", Seed: " + seed + ", Offset: " + offset);
        return new Variant();
    }
}
