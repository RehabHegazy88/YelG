using System.Buffers.Binary;

namespace YelG.WebSite.Infrastructure.Media
{
    /// <summary>Reads the pixel size from the header of a JPEG, PNG or WebP file, without decoding it.</summary>
    public static class ImageSize
    {
        public static (int Width, int Height)? Read(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                return Read(stream);
            }
            catch (IOException)
            {
                return null;
            }
        }

        public static (int Width, int Height)? Read(Stream stream)
        {
            Span<byte> head = stackalloc byte[30];
            if (stream.Read(head) < 26) return null;

            // PNG: signature, then the IHDR chunk holds width and height
            if (head[0] == 0x89 && head[1] == (byte)'P' && head[2] == (byte)'N' && head[3] == (byte)'G')
                return (BinaryPrimitives.ReadInt32BigEndian(head[16..]), BinaryPrimitives.ReadInt32BigEndian(head[20..]));

            // WebP: RIFF....WEBP then a VP8 / VP8L / VP8X chunk
            if (head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8))
            {
                var chunk = head[12..16];
                if (chunk.SequenceEqual("VP8X"u8))
                    return (1 + (head[24] | head[25] << 8 | head[26] << 16), 1 + (head[27] | head[28] << 8 | head[29] << 16));
                if (chunk.SequenceEqual("VP8L"u8))
                {
                    var bits = BinaryPrimitives.ReadUInt32LittleEndian(head[21..]);
                    return ((int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
                }
                if (chunk.SequenceEqual("VP8 "u8))
                    return (BinaryPrimitives.ReadUInt16LittleEndian(head[26..]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(head[28..]) & 0x3FFF);
                return null;
            }

            // JPEG: walk the markers to the first start-of-frame
            if (head[0] != 0xFF || head[1] != 0xD8) return null;
            stream.Position = 2;
            Span<byte> seg = stackalloc byte[9];
            while (stream.Read(seg[..4]) == 4)
            {
                if (seg[0] != 0xFF) return null;
                var marker = seg[1];
                var length = BinaryPrimitives.ReadUInt16BigEndian(seg[2..]);
                var isFrame = marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC;
                if (isFrame)
                {
                    if (stream.Read(seg[..5]) != 5) return null;
                    return (BinaryPrimitives.ReadUInt16BigEndian(seg[3..]), BinaryPrimitives.ReadUInt16BigEndian(seg[1..]));
                }
                stream.Position += length - 2;
            }
            return null;
        }
    }
}
