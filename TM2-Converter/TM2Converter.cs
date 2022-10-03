using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

namespace TM2_Converter
{
    internal static class TM2Converter
    {
        private static byte[] DDSHeader = new byte[]{
            0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x0F, 0x10, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00,
            0x41, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00,
            0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x10, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        private struct TM2Image
        {
            public byte[] Magic;
            public byte[] Unk;
            public int DataLen;
            public int PaletteLen;
            public int ImgDataLen;
            public short HeaderLen;
            public short ColorCount;
            public byte ImgFormat;
            public byte MipmapCount;
            public byte CLUTFormat;
            public byte Bpp;
            public short ImgWidth;
            public short ImgHeight;
            public byte[] GsTEX0;
            public byte[] GsTEX1;
            public byte[] GsRegs;
            public byte[] GsTexClut;
            public byte[] ImgData;
            public List<uint> Palettes;
        }
        private static TM2Image ReadTM2(ref BinaryReader br)
        {
            var result = new TM2Image();
            br.BaseStream.Position = 0;
            result.Magic = br.ReadBytes(4);
            result.Unk = br.ReadBytes(0xC);
            result.DataLen = br.ReadInt32();
            result.PaletteLen = br.ReadInt32();
            result.ImgDataLen = br.ReadInt32();
            result.HeaderLen = br.ReadInt16();
            result.ColorCount = br.ReadInt16();
            result.ImgFormat = br.ReadByte();
            result.MipmapCount = br.ReadByte();
            result.CLUTFormat = br.ReadByte();
            result.Bpp = br.ReadByte();
            result.ImgWidth = br.ReadInt16();
            result.ImgHeight = br.ReadInt16();
            result.GsTEX0 = br.ReadBytes(8);
            result.GsTEX1 = br.ReadBytes(8);
            result.GsRegs = br.ReadBytes(4);
            result.GsTexClut = br.ReadBytes(4);
            result.ImgData = br.ReadBytes(result.ImgDataLen);
            result.Palettes = new List<uint>();
            for (int i = 0; i < result.ColorCount; i++)
            {
                uint color = br.ReadUInt32();
                result.Palettes.Add(color);
            }
            return result;
        }
        public static byte[] ToDDS(string input)
        {
            using (var stream = File.OpenRead(input))
            {
                var br = new BinaryReader(stream);
                var img = ReadTM2(ref br);
                br.Close();
                if (img.Bpp != 4) throw new Exception("Unsupported format.");
                var result = new MemoryStream();
                using (var bw = new BinaryWriter(result))
                {
                    bw.Write(DDSHeader);
                    foreach (var b in img.ImgData)
                    {
                        var low = b & 0x0F;
                        var high = b >> 4;
                        bw.Write(img.Palettes[low]);
                        bw.Write(img.Palettes[high]);
                    }
                    bw.BaseStream.Seek(0xC, SeekOrigin.Begin);
                    bw.Write((int)img.ImgHeight);
                    bw.Write((int)img.ImgWidth);
                }
                return result.ToArray();
            }
        }
        public static byte[] ToTM2(string tm2, string dds)
        {
            using (var tm2Stream = File.OpenRead(tm2))
            {
                var br = new BinaryReader(tm2Stream);
                var img = ReadTM2(ref br);
                using (var result = new MemoryStream())
                {
                    br.BaseStream.Position = 0;
                    using (var bw = new BinaryWriter(result))
                    {
                        bw.Write(br.ReadBytes(img.HeaderLen + 0x10));
                        using (var imgData = new MemoryStream())
                        {
                            using (var bitBw = new BitHandle.BinaryWriter(imgData))
                            {
                                using (var ddsStream = File.OpenRead(dds))
                                {
                                    using (var ddsBr = new BinaryReader(ddsStream))
                                    {
                                        ddsBr.BaseStream.Position = 0x80;
                                        while (ddsBr.BaseStream.Position < ddsBr.BaseStream.Length)
                                        {
                                            var pixel = ddsBr.ReadUInt32();
                                            var index = img.Palettes.FindIndex(x => x == pixel);
                                            if (index < 0)
                                            {
                                                index = img.Palettes.Select((x, i) => new { Item = x, Index = i })
                                                    .Aggregate((x, y) => Math.Abs(x.Item - pixel) < Math.Abs(y.Item - pixel) ? x : y).Index;
                                            }
                                            var ba = new BitArray(new byte[] { (byte)index });
                                            for (int i = 0; i < 4; i++)
                                            {
                                                bitBw.Write(ba[i]);
                                            }
                                        }
                                    }
                                }
                            }
                            bw.Write(imgData.ToArray());
                        }
                        br.BaseStream.Position = br.BaseStream.Length - img.PaletteLen;
                        bw.Write(br.ReadBytes(img.PaletteLen));
                    }
                    br.Close();
                    return result.ToArray();
                }
            }
        }
    }
}
