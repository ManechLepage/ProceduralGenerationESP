using System;
using System.Collections.Generic;
using System.IO;
using fNbt;
using System.Threading.Tasks;

/*
WorldExporter.cs

Orchestrateur minimal pour produire un dossier de monde testable : un seul chunk
(ChunkBuilder.BuildTestStoneChunk) écrit dans un seul fichier région (RegionFileWriter),
plus un level.dat minimal.

Portée actuelle (test uniquement) :
 - Un seul chunk, à (0,0)
 - Pas de dimensions Nether/End, pas de poi/ ni entities/ (le jeu les recréera au besoin)
 - level.dat réduit au strict nécessaire pour charger le monde

ATTENTION : level.dat est la pièce où je suis le moins sûr -- je n'ai pas pu vérifier par
recherche les champs strictement obligatoires en 1.21.5. Si le monde n'apparaît pas dans le
menu "Monde solo" ou plante au chargement, c'est le premier endroit à comparer avec le
level.dat d'un vrai monde 1.21.5 fraîchement créé.
*/

public static class WorldExporter
{
    public delegate (BlockState[,,] Blocks, int MaxNonAirY) ChunkBlockGenerator(int chunkWorldX, int chunkWorldZ);

    async public static void ExportWorld(string worldFolderPath, int sizeX, int height, int sizeZ, ChunkBlockGenerator generateChunk, int worldMinY,
    string worldName = "Exported World", int originChunkX = 0, int originChunkZ = 0, string biome = "minecraft:plains", int waterLevel = -1, Func<int, int, Task> callback = null)
    {
        if (sizeX % ChunkBuilder.ChunkSize != 0 || sizeZ % ChunkBuilder.ChunkSize != 0)
            throw new ArgumentException("X et Z doivent être multiples de 16");

        Directory.CreateDirectory(worldFolderPath);
        Directory.CreateDirectory(Path.Combine(worldFolderPath, "region"));

        var regions = new Dictionary<(int, int), Dictionary<(int, int), NbtCompound>>();

        int totalSteps = (sizeX / ChunkBuilder.ChunkSize) * (sizeZ / ChunkBuilder.ChunkSize);
        int currentStep = 0;

        for (int cx = 0; cx < sizeX / ChunkBuilder.ChunkSize; cx++)
        for (int cz = 0; cz < sizeZ / ChunkBuilder.ChunkSize; cz++)
        {
            int chunkWorldX = originChunkX + cx, chunkWorldZ = originChunkZ + cz;
            var (chunkBlocks, maxNonAirY) = generateChunk(chunkWorldX, chunkWorldZ);
            var chunk = ChunkBuilder.BuildChunk(chunkWorldX, chunkWorldZ, chunkBlocks, worldMinY, biome, maxNonAirY: maxNonAirY, waterLevel: waterLevel);

            var regionKey = (chunkWorldX >> 5, chunkWorldZ >> 5);
            if (!regions.TryGetValue(regionKey, out var regionChunks))
                regions[regionKey] = regionChunks = new Dictionary<(int, int), NbtCompound>();
            regionChunks[(chunkWorldX & 31, chunkWorldZ & 31)] = chunk;

            currentStep++;

            if (callback != null)
                await callback(currentStep, totalSteps);
        }

        foreach (var kvp in regions)
            RegionFileWriter.WriteRegionFile(
                Path.Combine(worldFolderPath, "region", $"r.{kvp.Key.Item1}.{kvp.Key.Item2}.mca"),
                kvp.Value);

        WriteLevelDat(Path.Combine(worldFolderPath, "level.dat"), worldName, biome);
    }

    async public static Task ExportWorldThreading(string worldFolderPath, int sizeX, int height, int sizeZ, ChunkBlockGenerator generateChunk, int worldMinY,
    string worldName = "Exported World", int originChunkX = 0, int originChunkZ = 0, string biome = "minecraft:plains", int waterLevel = -1, IProgress<float> progress = null)
    {
        if (sizeX % ChunkBuilder.ChunkSize != 0 || sizeZ % ChunkBuilder.ChunkSize != 0)
            throw new ArgumentException("X et Z doivent être multiples de 16");

        Directory.CreateDirectory(worldFolderPath);
        Directory.CreateDirectory(Path.Combine(worldFolderPath, "region"));

        var regions = new Dictionary<(int, int), Dictionary<(int, int), NbtCompound>>();

        int totalChunksX = sizeX / ChunkBuilder.ChunkSize;
        int totalChunksZ = sizeZ / ChunkBuilder.ChunkSize;
        int totalSteps = totalChunksX * totalChunksZ;
        int currentStep = 0;

        await Task.Run(() =>
        {
            Parallel.For(0, totalChunksX, cx =>
            {
                for (int cz = 0; cz < totalChunksZ; cz++)
                {
                    int chunkWorldX = originChunkX + cx, chunkWorldZ = originChunkZ + cz;
                    var (chunkBlocks, maxNonAirY) = generateChunk(chunkWorldX, chunkWorldZ);
                    var chunk = ChunkBuilder.BuildChunk(chunkWorldX, chunkWorldZ, chunkBlocks, worldMinY, biome, maxNonAirY: maxNonAirY, waterLevel: waterLevel);

                    var regionKey = (chunkWorldX >> 5, chunkWorldZ >> 5);
                    lock (regions)
                    {
                        if (!regions.TryGetValue(regionKey, out var regionChunks))
                            regions[regionKey] = regionChunks = new Dictionary<(int, int), NbtCompound>();
                        regionChunks[(chunkWorldX & 31, chunkWorldZ & 31)] = chunk;
                    }

                    int step = System.Threading.Interlocked.Increment(ref currentStep);
                    progress?.Report((float)step / totalSteps);
                }
            });
        });

        foreach (var kvp in regions)
            RegionFileWriter.WriteRegionFile(
                Path.Combine(worldFolderPath, "region", $"r.{kvp.Key.Item1}.{kvp.Key.Item2}.mca"),
                kvp.Value);

        WriteLevelDat(Path.Combine(worldFolderPath, "level.dat"), worldName, biome);
    }

    public static void ExportTestWorld(string worldFolderPath, string worldName = "Test Stone World", string biome = "minecraft:plains")
    {
        Directory.CreateDirectory(worldFolderPath);
        Directory.CreateDirectory(Path.Combine(worldFolderPath, "region"));

        // 1. Le chunk de test
        NbtCompound chunk = ChunkBuilder.BuildTestStoneChunk(0, 0);

        // 2. Le fichier région contenant ce seul chunk : (0,0) -> coords locales (0,0) dans r.0.0.mca
        var chunks = new Dictionary<(int x, int z), NbtCompound> { { (0, 0), chunk } };
        string regionPath = Path.Combine(worldFolderPath, "region", "r.0.0.mca");
        RegionFileWriter.WriteRegionFile(regionPath, chunks);

        // 3. level.dat minimal
        WriteLevelDat(Path.Combine(worldFolderPath, "level.dat"), worldName, biome);
    }

    private static void WriteLevelDat(string path, string worldName, string biome = "minecraft:plains")
    {
        var data = new NbtCompound("Data")
        {
            new NbtInt("version", 19133),
            new NbtInt("DataVersion", 4325),
            new NbtCompound("Version")
            {
                new NbtInt("Id", 4325),
                new NbtString("Name", "1.21.5"),
                new NbtString("Series", "main"),
                new NbtByte("Snapshot", 0)
            },
            BuildWorldGenSettings(seed: 0, biome),
            new NbtString("LevelName", worldName),
            new NbtLong("LastPlayed", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            new NbtInt("SpawnX", 8),
            new NbtInt("SpawnY", 1),
            new NbtInt("SpawnZ", 8),
            new NbtLong("Time", 0),
            new NbtLong("DayTime", 0),
            new NbtByte("raining", 0),
            new NbtByte("thundering", 0),
            new NbtInt("GameType", 1), // 1 = créatif, pratique pour voler et inspecter le chunk de test
            new NbtByte("hardcore", 0),
            new NbtByte("Difficulty", 2),
            new NbtByte("allowCommands", 1),
            new NbtByte("initialized", 1),
            new NbtCompound("GameRules")
            {
                new NbtString("doDaylightCycle", "false")
            }
        };

        var root = new NbtCompound("") { data };
        var file = new NbtFile(root);
        file.SaveToFile(path, NbtCompression.GZip);
    }

    // Générateur "flat" pour les 3 dimensions : schéma plus simple et plus stable dans le temps
    // que "minecraft:noise", donc moins risqué à deviner sans pouvoir vérifier en ce moment.
    // Le contenu réel généré n'a pas d'importance ici puisque notre seul chunk a Status=full
    // (le jeu ne le régénère pas) -- ceci sert juste à satisfaire le codec de WorldGenSettings.
    private static NbtCompound BuildWorldGenSettings(long seed, string biome = "minecraft:plains")
    {
        return new NbtCompound("WorldGenSettings")
        {
            new NbtLong("seed", seed),
            new NbtByte("generate_features", 1),
            new NbtByte("bonus_chest", 0),
            new NbtCompound("dimensions")
            {
                FlatDimension("minecraft:overworld", seed, biome, true,
                    ("minecraft:bedrock", 1), ("minecraft:stone", 1)),
                FlatDimension("minecraft:the_nether", seed, "minecraft:nether_wastes", false,
                    ("minecraft:bedrock", 1), ("minecraft:netherrack", 1)),
                FlatDimension("minecraft:the_end", seed, "minecraft:the_end", false,
                    ("minecraft:end_stone", 1))
            }
        };
    }

    private static NbtCompound FlatDimension(string dimensionType, long seed, string biome, bool features, params (string block, int height)[] layers)
    {
        var layerList = new NbtList("layers", NbtTagType.Compound);
        foreach (var (block, height) in layers)
            layerList.Add(new NbtCompound { new NbtString("block", block), new NbtInt("height", height) });

        return new NbtCompound(dimensionType)
        {
            new NbtString("type", dimensionType),
            new NbtCompound("generator")
            {
                new NbtString("type", "minecraft:flat"),
                new NbtCompound("settings")
                {
                    new NbtString("biome", biome),
                    new NbtByte("features", (byte)(features ? 1 : 0)),
                    layerList
                }
            }
        };
    }
}
