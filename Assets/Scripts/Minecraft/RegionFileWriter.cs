using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using fNbt;

/*
RegionFileWriter.cs

Écrit un fichier région .mca (format Anvil) à partir de chunks NBT déjà construits par
ChunkBuilder. fNbt ne gère que l'arbre NBT, pas ce conteneur binaire -- tout est fait ici
à la main.

Portée actuelle (test uniquement) :
 - Compression zlib uniquement (type 2), pas de gzip/uncompressed/LZ4
 - Pas de fusion avec un fichier région existant : on écrase à chaque fois
 - Pensé pour un seul chunk au départ, mais fonctionne pour n'importe quel nombre <= 1024

Compression : DeflateStream (dispo partout) + reconstruction manuelle de l'enveloppe zlib
(header 2 octets + trailer Adler-32), parce que ZLibStream n'est pas accessible sur tous
les runtimes Unity et que DeflateStream seul ne produit que du deflate brut, pas du zlib.

Format (à vérifier contre un vrai fichier région si quelque chose ne charge pas) :
 - 4096 octets : table d'offsets (1024 x 4 octets : 3 octets secteur + 1 octet nb secteurs)
 - 4096 octets : table de timestamps (peu importe la valeur pour nous)
 - par chunk ensuite : 4 octets longueur (big-endian) + 1 octet type compression + données,
   le tout aligné sur des secteurs de 4096 octets
*/

public class RegionFileWriter
{
    private const int SectorSize = 4096;
    private const int ChunksPerRegion = 32;

    // chunks : clé = coordonnées locales (0-31, 0-31) dans la région, valeur = chunk NBT (root, sans nom)
    public static void WriteRegionFile(string path, Dictionary<(int x, int z), NbtCompound> chunks)
    {
        var locations = new int[ChunksPerRegion * ChunksPerRegion]; // valeur packée (offset<<8 | count)
        var orderedPayloads = new List<byte[]>(); // dans le même ordre que les secteurs assignés

        int nextSector = 2; // secteurs 0-1 réservés au header

        foreach (var kvp in chunks)
        {
            int localX = kvp.Key.x;
            int localZ = kvp.Key.z;
            if (localX < 0 || localX >= ChunksPerRegion || localZ < 0 || localZ >= ChunksPerRegion)
                throw new ArgumentException($"Coordonnées de chunk locales hors limites : ({localX},{localZ})");

            byte[] compressed = CompressChunk(kvp.Value);

            // 4 octets longueur + 1 octet type compression + données compressées
            byte[] entry = new byte[4 + 1 + compressed.Length];
            int payloadLength = compressed.Length + 1; // +1 pour l'octet de type
            WriteBigEndianInt(entry, 0, payloadLength);
            entry[4] = 2; // 2 = zlib
            Buffer.BlockCopy(compressed, 0, entry, 5, compressed.Length);

            int sectorCount = (int)Math.Ceiling(entry.Length / (double)SectorSize);
            int index = localZ * ChunksPerRegion + localX;

            locations[index] = (nextSector << 8) | (sectorCount & 0xFF);
            orderedPayloads.Add(entry);

            nextSector += sectorCount;
        }

        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            for (int i = 0; i < locations.Length; i++)
                WriteBigEndianIntToStream(stream, locations[i]);

            for (int i = 0; i < locations.Length; i++)
                WriteBigEndianIntToStream(stream, 0); // timestamps, valeur arbitraire

            foreach (byte[] entry in orderedPayloads)
            {
                stream.Write(entry, 0, entry.Length);
                int padding = PaddingToSector(entry.Length);
                if (padding > 0)
                    stream.Write(new byte[padding], 0, padding);
            }
        }
    }

    private static byte[] CompressChunk(NbtCompound chunk)
    {
        var file = new NbtFile(chunk);
        byte[] raw;
        using (var rawStream = new MemoryStream())
        {
            file.SaveToStream(rawStream, NbtCompression.None);
            raw = rawStream.ToArray();
        }

        byte[] deflated;
        using (var deflateOutput = new MemoryStream())
        {
            using (var deflate = new DeflateStream(deflateOutput, CompressionLevel.Optimal, leaveOpen: true))
                deflate.Write(raw, 0, raw.Length);
            deflated = deflateOutput.ToArray();
        }

        // zlib = en-tête 2 octets + données deflate + Adler-32 du contenu non compressé (4 octets, big-endian)
        using (var zlibOutput = new MemoryStream())
        {
            zlibOutput.WriteByte(0x78);
            zlibOutput.WriteByte(0x9C); // compression "par défaut"
            zlibOutput.Write(deflated, 0, deflated.Length);

            uint adler = Adler32(raw);
            zlibOutput.WriteByte((byte)(adler >> 24));
            zlibOutput.WriteByte((byte)(adler >> 16));
            zlibOutput.WriteByte((byte)(adler >> 8));
            zlibOutput.WriteByte((byte)adler);

            return zlibOutput.ToArray();
        }
    }

    private static uint Adler32(byte[] data)
    {
        const uint modAdler = 65521;
        uint a = 1, b = 0;
        foreach (byte by in data)
        {
            a = (a + by) % modAdler;
            b = (b + a) % modAdler;
        }
        return (b << 16) | a;
    }

    private static int PaddingToSector(int length)
    {
        int remainder = length % SectorSize;
        return remainder == 0 ? 0 : SectorSize - remainder;
    }

    private static void WriteBigEndianInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static void WriteBigEndianIntToStream(Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }
}
