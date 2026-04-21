using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MinecraftExportNode : NodeBehaviour
{
    public List<BlockPaletteItem> blockPalettes = new List<BlockPaletteItem>();
    public MinecraftConverter converter;

    public override Task<Variant> OnFire()
    {
        return Task.FromResult(new Variant());
    }

    async public void Export()
    {
        float startTime = Time.realtimeSinceStartup;

        MinecraftConverterSettings settings = await GetSettings();
        string path = (await GetInputValue("path")).GetValue<string>();
        List<List<float>> heightMap = (await GetInputValue("heightmap", onlyIfModified: true)).GetValue<List<List<float>>>();

        if (heightMap == null || heightMap.Count == 0) { return; }
        
        string schematicPath = $"Assets/Schematics/{path}.schem";

        converter.SaveToSchem(heightMap, schematicPath, settings);
        UnityEditor.AssetDatabase.Refresh();

        float endTime = Time.realtimeSinceStartup;
        float dt = endTime - startTime;

        Debug.Log($"Exported Minecraft schematic to {schematicPath} in {dt:F3} seconds.");
    }

    public async Task<MinecraftConverterSettings> GetSettings()
    {
        MinecraftConverterSettings settings = new MinecraftConverterSettings();

        Vector2 size = (await GetInputValue("size")).GetValue<Vector2>();
        settings.size = new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));

        string block_palette = (await GetInputValue("block_palette")).GetValue<string>();

        settings.height = (await GetInputValue("height")).GetValue<int>();
        settings.onlySurface = (await GetInputValue("only_surface")).GetValue<bool>();
        settings.blockPalette = GetBlockPaletteByName(block_palette);

        return settings;
    }

    public BlockPalette GetBlockPaletteByName(string name)
    {
        foreach (BlockPaletteItem item in blockPalettes)
        {
            if (item.name == name)
            {
                return item.palette;
            }
        }
        return blockPalettes.Count > 0 ? blockPalettes[0].palette : null;
    }
}


[System.Serializable]
public class BlockPaletteItem
{
    public string name;
    public BlockPalette palette;
}
