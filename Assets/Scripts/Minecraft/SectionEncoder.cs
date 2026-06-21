using System;
using System.Collections.Generic;
using fNbt;

/*
SectionEncoder.cs

Encode une section verticale de 16x16x16 blocs (et sa grille de biomes 4x4x4 associée)
au format "paletted container" utilisé par les chunks Minecraft depuis la 1.18 (donc
toujours valide en 1.21.x).

Une section contient deux conteneurs de ce type :
 - "block_states" : palette de BlockState (Name + Properties optionnelles) + indices
                     empaquetés sur les 4096 blocs (16x16x16)
 - "biomes"       : palette de biomes (juste des noms, pas de Properties) + indices
                     empaquetés sur 64 entrées (grille 4x4x4, résolution réduite)

À VALIDER contre un vrai chunk avant de faire confiance à 100% (voir le plan de match) :
 - l'ordre d'indexation (j'utilise index = (y*16 + z)*16 + x pour les blocs)
 - le nombre minimal de bits par entrée pour les biomes (MinBitsPerEntryBiomes ci-dessous,
   je ne suis pas certain de cette valeur)
*/

public class BlockState
{
    public string Name; // ex: "minecraft:oak_stairs"
    public Dictionary<string, string> Properties; // ex: {"facing", "north"} -- peut être null

    public BlockState(string name, Dictionary<string, string> properties = null)
    {
        Name = name;
        Properties = properties;
    }

    // Deux BlockState comptent comme la même entrée de palette seulement si Name ET
    // Properties sont identiques.
    public string PaletteKey()
    {
        if (Properties == null || Properties.Count == 0)
            return Name;

        var sortedProps = new List<string>();
        foreach (var kvp in Properties)
            sortedProps.Add($"{kvp.Key}={kvp.Value}");
        sortedProps.Sort();
        return Name + "[" + string.Join(",", sortedProps) + "]";
    }
}

public static class SectionEncoder
{
    public const int BlocksPerSection = 16 * 16 * 16; // 4096
    public const int BiomesPerSection = 4 * 4 * 4;    // 64

    private const int MinBitsPerEntryBlocks = 4; // minimum vanilla connu pour block_states
    private const int MinBitsPerEntryBiomes = 1; // à vérifier -- pas garanti exact

    // Construit le compound complet d'une section : Y + block_states + biomes
    public static NbtCompound EncodeSection(sbyte sectionY, BlockState[] blocks, string[] biomes)
    {
        if (blocks.Length != BlocksPerSection)
            throw new ArgumentException($"blocks doit contenir {BlocksPerSection} entrées (16x16x16), reçu {blocks.Length}");
        if (biomes.Length != BiomesPerSection)
            throw new ArgumentException($"biomes doit contenir {BiomesPerSection} entrées (4x4x4), reçu {biomes.Length}");

        return new NbtCompound
        {
            new NbtByte("Y", (byte)sectionY), // cast explicite : TAG_Byte est signé, le bit pattern reste correct (-4 -> 252)
            EncodeBlockStates(blocks),
            EncodeBiomes(biomes)
        };
    }

    // Index linéaire d'un bloc dans une section (convention y/z/x de Minecraft)
    public static int BlockIndex(int x, int y, int z) => (y * 16 + z) * 16 + x;

    // Idem pour les biomes (grille 4x4x4)
    public static int BiomeIndex(int bx, int by, int bz) => (by * 4 + bz) * 4 + bx;

    private static NbtCompound EncodeBlockStates(BlockState[] blocks)
    {
        var paletteIndexByKey = new Dictionary<string, int>();
        var paletteEntries = new List<BlockState>();
        var indices = new int[blocks.Length];

        for (int i = 0; i < blocks.Length; i++)
        {
            string key = blocks[i].PaletteKey();
            if (!paletteIndexByKey.TryGetValue(key, out int idx))
            {
                idx = paletteEntries.Count;
                paletteIndexByKey[key] = idx;
                paletteEntries.Add(blocks[i]);
            }
            indices[i] = idx;
        }

        var blockStates = new NbtCompound("block_states")
        {
            BuildBlockPaletteList(paletteEntries)
        };

        // Cas spécial vanilla : un seul bloc dans toute la section -> pas de "data" du tout
        if (paletteEntries.Count > 1)
        {
            long[] packed = PackIndices(indices, paletteEntries.Count, MinBitsPerEntryBlocks);
            blockStates.Add(new NbtLongArray("data", packed));
        }

        return blockStates;
    }

    private static NbtList BuildBlockPaletteList(List<BlockState> paletteEntries)
    {
        var list = new NbtList("palette", NbtTagType.Compound);
        foreach (var state in paletteEntries)
        {
            var entry = new NbtCompound { new NbtString("Name", state.Name) };
            if (state.Properties != null && state.Properties.Count > 0)
            {
                var props = new NbtCompound("Properties");
                foreach (var kvp in state.Properties)
                    props.Add(new NbtString(kvp.Key, kvp.Value));
                entry.Add(props);
            }
            list.Add(entry);
        }
        return list;
    }

    private static NbtCompound EncodeBiomes(string[] biomes)
    {
        var paletteIndexByName = new Dictionary<string, int>();
        var paletteEntries = new List<string>();
        var indices = new int[biomes.Length];

        for (int i = 0; i < biomes.Length; i++)
        {
            string name = biomes[i];
            if (!paletteIndexByName.TryGetValue(name, out int idx))
            {
                idx = paletteEntries.Count;
                paletteIndexByName[name] = idx;
                paletteEntries.Add(name);
            }
            indices[i] = idx;
        }

        var biomesCompound = new NbtCompound("biomes");

        var paletteList = new NbtList("palette", NbtTagType.String);
        foreach (string name in paletteEntries)
            paletteList.Add(new NbtString(name));
        biomesCompound.Add(paletteList);

        if (paletteEntries.Count > 1)
        {
            long[] packed = PackIndices(indices, paletteEntries.Count, MinBitsPerEntryBiomes);
            biomesCompound.Add(new NbtLongArray("data", packed));
        }

        return biomesCompound;
    }

    // Empaquette un tableau d'indices de palette en long[], format vanilla depuis la 1.16 :
    // une entrée ne chevauche jamais deux long (les bits restants en fin de long sont perdus).
    public static long[] PackIndices(int[] indices, int paletteSize, int minBitsPerEntry)
    {
        int bitsPerEntry = Math.Max(minBitsPerEntry, BitsNeeded(paletteSize));
        int valuesPerLong = 64 / bitsPerEntry;
        int longCount = (int)Math.Ceiling(indices.Length / (double)valuesPerLong);

        long[] data = new long[longCount];
        for (int i = 0; i < indices.Length; i++)
        {
            int longIndex = i / valuesPerLong;
            int bitOffset = (i % valuesPerLong) * bitsPerEntry;
            data[longIndex] |= ((long)indices[i] & ((1L << bitsPerEntry) - 1)) << bitOffset;
        }

        return data;
    }

    // Nombre de bits nécessaires pour représenter "count" valeurs distinctes (ceil(log2(count)))
    private static int BitsNeeded(int count)
    {
        if (count <= 1) return 0;
        int bits = 0;
        int max = count - 1;
        while (max > 0)
        {
            bits++;
            max >>= 1;
        }
        return bits;
    }
}
