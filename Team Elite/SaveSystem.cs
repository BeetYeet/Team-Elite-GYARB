using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Team_Elite
{
    static class SaveSystem
    {
        static readonly string basePath = Directory.GetCurrentDirectory() + "/SavedData/";
        public static void Read<T>(ref T output, string denominator) where T : ISaveable
        {
            using (BinaryReader reader = new BinaryReader(File.Open(basePath + denominator + ".savedata", FileMode.Open)))
            {
                output.CreateFromBinaryStream(reader);
            }
        }

        public static void Write<T>(T data, string denominator) where T : ISaveable
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            using (BinaryWriter writer = new BinaryWriter(File.Open(basePath + denominator + ".savedata", FileMode.Create)))
            {
                data.WriteToBinaryStream(writer);
            }
        }
        public static void Read<T>(ref T output, BinaryReader reader) where T : ISaveable
        {
            output.CreateFromBinaryStream(reader);
        }

        public static void Write<T>(T data, BinaryWriter writer) where T : ISaveable
        {
            data.WriteToBinaryStream(writer);
        }

        public static void SaveBalancedNumberList(List<BalancedNumber> balancedNumbers, string denominator)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            using (BinaryWriter writer = new BinaryWriter(File.Open(basePath + denominator + ".savedata", FileMode.Create)))
            {
                writer.Write((ushort)balancedNumbers.Count);
                foreach (BalancedNumber bn in balancedNumbers)
                {
                    Write(bn, writer);
                }
            }

        }
        public static List<BalancedNumber> LoadBalancedNumberList(string denominator)
        {
            List<BalancedNumber> result = new List<BalancedNumber>();
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(basePath + denominator + ".savedata", FileMode.Open)))
                {
                    ushort length = reader.ReadUInt16();
                    for (int i = 0; i < length; i++)
                    {
                        BalancedNumber bn = new BalancedNumber();
                        Read(ref bn, reader);
                        result.Add(bn);
                    }
                }
            }
            catch (Exception e)
            {
                return new List<BalancedNumber>();
            }
            return result;
        }
    }
}
