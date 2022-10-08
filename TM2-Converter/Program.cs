using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TM2_Converter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (Path.GetExtension(arg) == ".tm2")
                {
                    byte[] data = TM2Converter.ToDDS(arg);
                    File.WriteAllBytes(arg + ".dds", data);
                }
                else if (Path.GetExtension(arg) == ".dds")
                {
                    string tm2path = Path.Combine(Path.GetDirectoryName(arg), Path.GetFileNameWithoutExtension(arg));
                    byte[] data = TM2Converter.ToTM2(tm2path, arg);
                    File.WriteAllBytes(tm2path, data);
                } 
            }
            Console.ReadKey();
        }
    }
}
