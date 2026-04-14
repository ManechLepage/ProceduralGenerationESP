using UnityEngine;
using fNbt;
using System.IO;
using System.Collections.Generic;

public class SchematicConverter : MonoBehaviour
{
    public static void Export(int[,,] blockMap, Dictionary<string, int> paletteDict, string filePath)
    {
        int W = blockMap.GetLength(0);
        int H = blockMap.GetLength(1);
        int L = blockMap.GetLength(2);

        var dataList = new List<byte>();
        for (int y = 0; y < H; y++)
        {
            for (int z = 0; z < L; z++)
            {
                for (int x = 0; x < W; x++)
                {
                    WriteVarint(dataList, blockMap[x, y, z]);
                }
            }
        }

        var paletteNbt = new NbtCompound("Palette");
        foreach (var kvp in paletteDict)
        {
            paletteNbt.Add(new NbtInt(kvp.Key, kvp.Value));
        }

        var schematic = new NbtCompound("Schematic") {
            new NbtInt("Version", 2),
            new NbtInt("DataVersion", 4125),
            new NbtShort("Width",  (short)W),
            new NbtShort("Height", (short)H),
            new NbtShort("Length", (short)L),
            new NbtIntArray("Offset", new int[] { 0, 0, 0 }),
            new NbtInt("PaletteMax", paletteDict.Count),
            paletteNbt,
            new NbtByteArray("BlockData", dataList.ToArray()),
            new NbtList("BlockEntities", NbtTagType.Compound)
        };

        var file = new NbtFile(schematic);
        file.SaveToFile(filePath, NbtCompression.GZip);
        Debug.Log($"Exporté : {filePath} ({W}×{H}×{L} blocs)");
    }

    static void WriteVarint(List<byte> buf, int value) {
        while ((value & 0xFFFFFF80) != 0) {
            buf.Add((byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }
        buf.Add((byte)(value & 0x7F));
    }
}
