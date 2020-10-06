using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Extreme.Mathematics;

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

        public static void SaveBalancedNumberList(List<BalancedNumber> balancedNumbers)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            using (BinaryWriter writer = new BinaryWriter(File.Open(basePath + "BalancedNumberList.savedata", FileMode.Create)))
            {
                writer.Write((ushort)balancedNumbers.Count);
                foreach (BalancedNumber bn in balancedNumbers)
                {
                    Write(bn, writer);
                }
            }
        }
        public static List<BalancedNumber> LoadBalancedNumberList()
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            List<BalancedNumber> result = new List<BalancedNumber>();
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(basePath + "BalancedNumberList.savedata", FileMode.Open)))
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
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Error loading list");
                return new List<BalancedNumber>();
            }
            return result;
        }

        public static void WriteToTxt(List<BalancedNumber> numbers)
        {
            StreamWriter writer = new StreamWriter(File.Open(basePath + "Balanced Numbers.txt", FileMode.OpenOrCreate));
            int i = 0;
            foreach (BalancedNumber bn in numbers)
            {
                writer.WriteLine("Balanced number #{0}", i);

                writer.WriteLine("\tNumber:  {0}", bn.number.ToString());
                writer.WriteLine("\tK:       {0}", bn.k.ToString());
                writer.WriteLine("\tSideNum: {0}", bn.sideSum.ToString());
                writer.WriteLine("\tKfactor: {0}\n\r", bn.kFactor.ToString());
                if (i < numbers.Count - 1)
                {
                    BigFloat floatn = bn.number;
                    writer.WriteLine("\tnFactor to next: {0}\n\r", BigFloat.Divide(numbers[i + 1].number, floatn, AccuracyGoal.Absolute(floatn.BinaryPrecision), RoundingMode.TowardsNearest).ToString());
                }
                i++;
            }
        }

        public static void SaveLast(BigInteger last)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            using (BinaryWriter writer = new BinaryWriter(File.Open(basePath + "LastNumber.savedata", FileMode.Create)))
            {
                writer.Write(last.ToString());
            }
        }
        public static BigInteger LoadLast()
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(basePath + "LastNumber.savedata", FileMode.Open)))
                {
                    return BigInteger.Parse(reader.ReadString());
                }
            }
            catch (FileNotFoundException e)
            {
                return new BigInteger(2);
            }
        }
    }
}
