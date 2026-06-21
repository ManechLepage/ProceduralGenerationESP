using System;
using fNbt;

/*
ChunkBuilder.cs

Assemble les sections (SectionEncoder) en un chunk NBT complet, au format "flatten"
utilisé depuis la 1.18 (chunk root sans le compound "Level" historique).

Objectif actuel (minimal) : pouvoir produire UN chunk de test, par exemple rempli de
pierre, pour valider tout le pipeline avant de s'attaquer au fichier région .mca.

Champs inclus dans ce chunk minimal (DataVersion 4325 = 1.21.5) :
 DataVersion, xPos, zPos, yPos, Status, LastUpdate, InhabitedTime, isLightOn,
 sections, Heightmaps (MOTION_BLOCKING seulement)

À VALIDER contre un vrai chunk avant de faire confiance à 100% (pas pu confirmer par
recherche en ce moment) :
 - la valeur exacte de Status ("minecraft:full" vs "full")
 - si d'autres champs sont obligatoires (LastUpdate/InhabitedTime acceptent peut-être
   d'être absents, je les ai mis par prudence)
*/

public static class ChunkBuilder
{
    public const int ChunkSize = 16; // largeur/longueur en blocs
    public const int SectionHeight = 16;

    // Construit un chunk complet à partir d'un tableau de blocs [x, y, z] (y = index local,
    // 0 correspond à worldMinY) et d'un nom de biome unique appliqué partout.
    public static NbtCompound BuildChunk(
        int chunkX, int chunkZ,
        BlockState[,,] blocks,
        int worldMinY,
        string biomeName = "minecraft:plains",
        int dataVersion = 4325,
        int maxNonAirY = int.MaxValue)
    {
        int height = blocks.GetLength(1);
        if (height % SectionHeight != 0)
            throw new ArgumentException($"La hauteur du tableau ({height}) doit être un multiple de {SectionHeight}");
        if (worldMinY % SectionHeight != 0)
            throw new ArgumentException($"worldMinY ({worldMinY}) doit être un multiple de {SectionHeight}");

        int sectionCount = height / SectionHeight;
        int lowestSectionY = worldMinY / SectionHeight;

        var sectionsList = new NbtList("sections", NbtTagType.Compound);
        var heightmapValues = new int[ChunkSize * ChunkSize]; // hauteur du sommet, offset depuis worldMinY

        for (int s = 0; s < sectionCount; s++)
        {
            sbyte sectionY = (sbyte)(lowestSectionY + s);
            int yOffset = s * SectionHeight;

            if (yOffset > maxNonAirY)
            {
                sectionsList.Add(SectionEncoder.EncodeUniformSection((sbyte)(lowestSectionY + s), "minecraft:air"));
                continue;
            }

            var sectionBlocks = new BlockState[SectionEncoder.BlocksPerSection];
            for (int x = 0; x < ChunkSize; x++)
            for (int y = 0; y < SectionHeight; y++)
            for (int z = 0; z < ChunkSize; z++)
            {
                BlockState block = blocks[x, yOffset + y, z];
                sectionBlocks[SectionEncoder.BlockIndex(x, y, z)] = block;

                // heightmap approximative : position la plus haute non-air rencontrée
                if (block.Name != "minecraft:air")
                {
                    int idx = z * ChunkSize + x;
                    int topValue = yOffset + y + 1;
                    if (topValue > heightmapValues[idx])
                        heightmapValues[idx] = topValue;
                }
            }

            var sectionBiomes = new string[SectionEncoder.BiomesPerSection];
            for (int i = 0; i < sectionBiomes.Length; i++)
                sectionBiomes[i] = biomeName;

            sectionsList.Add(SectionEncoder.EncodeSection(sectionY, sectionBlocks, sectionBiomes));
        }

        var heightmaps = new NbtCompound("Heightmaps")
        {
            new NbtLongArray("MOTION_BLOCKING", SectionEncoder.PackIndices(heightmapValues, height + 1, 0))
        };

        return new NbtCompound("")
        {
            new NbtInt("DataVersion", dataVersion),
            new NbtInt("xPos", chunkX),
            new NbtInt("zPos", chunkZ),
            new NbtInt("yPos", lowestSectionY),
            new NbtString("Status", "minecraft:carvers"),   // au lieu de "minecraft:full"
            new NbtLong("LastUpdate", 0),
            new NbtLong("InhabitedTime", 0),
            new NbtByte("isLightOn", 0), // force le recalcul de la lumière au premier chargement
            sectionsList,
            heightmaps
        };
    }

    public static NbtCompound BuildChunkFromGrid(
        int chunkX, int chunkZ,
        BlockState[,,] fullGrid, int gridOffsetX, int gridOffsetZ,
        int worldMinY, string biomeName = "minecraft:plains", int dataVersion = 4325)
    {
        int height = fullGrid.GetLength(1);
        var blocks = new BlockState[ChunkSize, height, ChunkSize];
        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < height; y++)
        for (int z = 0; z < ChunkSize; z++)
            blocks[x, y, z] = fullGrid[gridOffsetX + x, y, gridOffsetZ + z];

        return BuildChunk(chunkX, chunkZ, blocks, worldMinY, biomeName, dataVersion);
    }

    // Raccourci pour un chunk de test 100% pierre sous Y=0, air au-dessus.
    // Pratique pour valider tout le pipeline avant d'écrire le fichier région.
    public static NbtCompound BuildTestStoneChunk(int chunkX, int chunkZ, int worldMinY = -64, int worldMaxY = 320)
    {
        int height = worldMaxY - worldMinY;
        var blocks = new BlockState[ChunkSize, height, ChunkSize];
        var stone = new BlockState("minecraft:stone");
        var air = new BlockState("minecraft:air");

        for (int x = 0; x < ChunkSize; x++)
        for (int y = 0; y < height; y++)
        for (int z = 0; z < ChunkSize; z++)
            blocks[x, y, z] = (worldMinY + y < 0) ? stone : air;

        return BuildChunk(chunkX, chunkZ, blocks, worldMinY);
    }
}
