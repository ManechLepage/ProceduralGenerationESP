using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MinecraftExportNode : NodeBehaviour
{
    public BlockPalette blockPalette;
    public MinecraftConverter converter;

    public override Variant OnFire()
    {
        return new Variant();
    }

    public void Export()
    {
        MinecraftConverterSettings settings = GetSettings();
        string path = GetInputValue("path").GetValue<string>();
        List<List<float>> heightMap = GetInputValue("heightmap").GetValue<List<List<float>>>();

        if (heightMap == null || heightMap.Count == 0) { return; }
        
        string schematicPath = $"Assets/Schematics/{path}.schem";

        converter.SaveToSchem(heightMap, schematicPath, settings);
        UnityEditor.AssetDatabase.Refresh();
    }

    public MinecraftConverterSettings GetSettings()
    {
        MinecraftConverterSettings settings = new MinecraftConverterSettings();

        Vector2 size = GetInputValue("size").GetValue<Vector2>();
        settings.size = new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));

        settings.height = GetInputValue("height").GetValue<int>();
        settings.blockPalette = blockPalette;

        return settings;
    }
}
