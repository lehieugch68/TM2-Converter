using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Drawing;

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
            public List<RGBAColor> Palettes;
        }
        private struct RGBAColor
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;
            public byte[] ToBytes()
            {
                return new byte[] { this.R, this.G, this.B, this.A };
            }
            public bool Match(RGBAColor color)
            {
                return this.R == color.R 
                    && this.B == color.B 
                    && this.G == color.G 
                    && this.A == color.A;
            }
            /*public bool Compare(RGBAColor orginal, RGBAColor other)
            {
                var a = new int[] { this.R, this.G, this.B, this.A };
                var b = new int[] { other.R, other.G, other.B, other.A };
                var c = new int[] { orginal.R, orginal.G, orginal.B, orginal.A };
                return Utils.CosineSimilarity(a, c) >= Utils.CosineSimilarity(b, c);
            }*/
            public bool Compare(RGBAColor orginal, RGBAColor other)
            {
                var a = new int[] { this.R, this.G, this.B, this.A };
                var b = new int[] { other.R, other.G, other.B, other.A };
                var c = new int[] { orginal.R, orginal.G, orginal.B, orginal.A };
                return Utils.EuclideanDistance(a, c) <= Utils.EuclideanDistance(b, c);
            }
            public uint ToUInt()
            {
                return BitConverter.ToUInt32(new byte[] { this.R, this.G, this.B, this.A }, 0);
            }
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
            result.Palettes = new List<RGBAColor>();
            if (result.Bpp == 4)
            {
                for (int i = 0; i < result.ColorCount; i++)
                {
                    var color = new RGBAColor();
                    color.R = br.ReadByte();
                    color.G = br.ReadByte();
                    color.B = br.ReadByte();
                    color.A = br.ReadByte();
                    result.Palettes.Add(color);
                }
            } else if (result.Bpp == 5)
            {
                byte[] originalData = br.ReadBytes(result.PaletteLen);
                List<byte> reved = new List<byte>();
                int parts = result.PaletteLen / 128;
                int stripes = 2;
                int colors = 32;
                int blocks = 2;
                for (int part = 0; part < parts; part++)
                {
                    for (int block = 0; block < blocks; block++)
                    {
                        for (int stripe = 0; stripe < stripes; stripe++)
                        {
                            for (int color = 0; color < colors; color++)
                            {
                                reved.Add(originalData[part * colors * stripes * blocks + block * colors + stripe * stripes * colors + color]);
                            }
                        }
                    }
                }
                BinaryReader brms = new BinaryReader(new MemoryStream(reved.ToArray()));
                for (int i = 0; i < result.ColorCount; i++)
                {
                    var color = new RGBAColor();
                    color.R = brms.ReadByte();
                    color.G = brms.ReadByte();
                    color.B = brms.ReadByte();
                    color.A = brms.ReadByte();
                    result.Palettes.Add(color);
                }
            }
            return result;
        }
        public static byte[] ToDDS(string input)
        {
            using (var stream = File.OpenRead(input))
            {
                var br = new BinaryReader(stream);
                var img = ReadTM2(ref br);
                var result = new MemoryStream();
                using (var bw = new BinaryWriter(result))
                {
                    bw.Write(DDSHeader);
                    if (img.Bpp == 4) //4bit
                    {
                        foreach (var b in img.ImgData)
                        {
                            var low = b & 0x0F;
                            var high = b >> 4;
                            bw.Write(img.Palettes[low].ToBytes());
                            bw.Write(img.Palettes[high].ToBytes());
                        }
                    }
                    else if (img.Bpp == 5) //8bit
                    {
                        foreach (byte b in img.ImgData)
                        {
                            bw.Write(img.Palettes[b].ToBytes());
                        }
                    } 
                    else throw new Exception("Unsupported format.");
                    bw.BaseStream.Seek(0xC, SeekOrigin.Begin);
                    bw.Write((int)img.ImgHeight);
                    bw.Write((int)img.ImgWidth);
                }
                br.Close();
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
                            if (img.Bpp == 4)
                            {
                                using (var bitBw = new BitHandle.BinaryWriter(imgData))
                                {
                                    using (var ddsBr = new BinaryReader(File.OpenRead(dds)))
                                    {
                                        ddsBr.BaseStream.Position = 0x80;
                                        while (ddsBr.BaseStream.Position < ddsBr.BaseStream.Length)
                                        {
                                            var color = new RGBAColor();
                                            color.R = ddsBr.ReadByte();
                                            color.G = ddsBr.ReadByte();
                                            color.B = ddsBr.ReadByte();
                                            color.A = ddsBr.ReadByte();
                                            var index = img.Palettes.FindIndex(x => x.Match(color) );
                                            if (index < 0)
                                            {
                                                index = img.Palettes.Select((x, i) => new { Color = x, Index = i })
                                                    .Aggregate((x, y) => x.Color.Compare(color, y.Color) ? x : y).Index;
                                                /*Console.WriteLine("New Color (RGBA): {0} - {1} - {2} - {3}", color.R, color.G, color.B, color.A);
                                                Console.WriteLine("Nearest Color (RGBA): {0} - {1} - {2} - {3}\n", img.Palettes[index].R, img.Palettes[index].G, img.Palettes[index].B, img.Palettes[index].A);*/
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
                            else if (img.Bpp == 5)
                            {
                                using (var dataBw = new BinaryWriter(imgData))
                                {
                                    using (var ddsBr = new BinaryReader(File.OpenRead(dds)))
                                    {
                                        ddsBr.BaseStream.Position = 0x80;
                                        while (ddsBr.BaseStream.Position < ddsBr.BaseStream.Length)
                                        {
                                            var color = new RGBAColor();
                                            color.R = ddsBr.ReadByte();
                                            color.G = ddsBr.ReadByte();
                                            color.B = ddsBr.ReadByte();
                                            color.A = ddsBr.ReadByte();
                                            var index = img.Palettes.FindIndex(x => x.Match(color));
                                            if (index < 0)
                                            {
                                                index = img.Palettes.Select((x, i) => new { Color = x, Index = i })
                                                    .Aggregate((x, y) => x.Color.Compare(color, y.Color) ? x : y).Index;
                                                /*Console.WriteLine("New Color (RGBA): {0} - {1} - {2} - {3}", color.R, color.G, color.B, color.A);
                                                Console.WriteLine("Nearest Color (RGBA): {0} - {1} - {2} - {3}\n", img.Palettes[index].R, img.Palettes[index].G, img.Palettes[index].B, img.Palettes[index].A);*/
                                            }
                                            dataBw.Write((byte)index);
                                            
                                        }
                                    }
                                }
                            }
                            else throw new Exception("Unsupported format.");
                            bw.Write(imgData.ToArray());
                        }
                        br.BaseStream.Position = 0x10 + img.HeaderLen + img.ImgDataLen;
                        bw.Write(br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position)));
                    }
                    br.Close();
                    return result.ToArray();
                }
            }
        }
    }
}
