using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class MinecraftWorldExportNode : NodeBehaviour
{
    public List<BlockPaletteItem> blockPalettes = new List<BlockPaletteItem>();

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
        
        string worldPath = $"Assets/Worlds/{path}";

        string biome_name = (await GetInputValue("biome")).GetValue<string>();
        string biome = "minecraft:" + biome_name.ToLower().Replace(" ", "_");

        GraphManager graphManager = GraphManager.Instance;
        graphManager.ShowNextButton(true);
        graphManager.SendNextButtonToFront();
        graphManager.SetNextButtonTitle("Exporting...");
        graphManager.SetNextFlagText("MC World Exporter");

        Debug.Log("Exporting Minecraft world...");

        ShowLoadingIcon(true);

        var progress = new Progress<float>(value =>
        {
            graphManager.SetNextButtonSliderValue(value);
        });

        MinecraftBlockStateConverter minecraftBlockStateConverter = new MinecraftBlockStateConverter();
        await WorldExporter.ExportWorldThreading(worldPath, settings.size.x, settings.height, settings.size.y, 
            (chunkX, chunkZ) => minecraftBlockStateConverter.CreateChunkBlockState(heightMap, settings, chunkX, chunkZ), 
            worldMinY: 0, worldName: path, biome: biome, waterLevel: settings.waterLevel, progress: progress);

        UnityEditor.AssetDatabase.Refresh();

        float endTime = Time.realtimeSinceStartup;
        float dt = endTime - startTime;

        Debug.Log($"Exported Minecraft world to {worldPath} in {dt:F3} seconds.");

        graphManager.ShowNextButton(false);
        graphManager.SendNextButtonToBack();

        ShowLoadingIcon(false);
    }

    public async Task ExportCallback(int step, int totalSteps)
    {
        if (step % 10 != 0 && step != totalSteps) return;
        GraphManager graphManager = GraphManager.Instance;
        graphManager.SetNextButtonSliderValue((float)step / totalSteps);
        
        await Task.Yield();
    }

    public async Task<MinecraftConverterSettings> GetSettings()
    {
        MinecraftConverterSettings settings = new MinecraftConverterSettings();

        Vector2 size = (await GetInputValue("size")).GetValue<Vector2>();
        settings.size = new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));

        string block_palette = (await GetInputValue("block_palette")).GetValue<string>();

        settings.height = (await GetInputValue("height")).GetValue<int>();
        settings.waterLevel = (await GetInputValue("water_level")).GetValue<int>();
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
