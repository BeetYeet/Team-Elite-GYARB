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

        public static void WriteToTxt(List<BalancedNumber> numbers, bool extended)
        {
            StreamWriter writer = new StreamWriter(File.Open(basePath + (extended ? "Balanced Numbers Extended.txt" : "Balanced Numbers.txt"), FileMode.OpenOrCreate));
            int i = 0;
            foreach (BalancedNumber bn in numbers)
            {
                Dictionary<int, int> primeFactors = new Dictionary<int, int>();
                int totalFactors = 0;
                if (extended)
                {
                    primeFactors = bn.number.FactorizeUnique();
                    foreach (int factorCount in primeFactors.Values)
                    {
                        totalFactors += factorCount;
                    }
                }
                writer.WriteLine("Balanced number #{0}", i + 1);
                writer.WriteLine("\tNumber:          {0}", bn.number.ToString());
                if (extended)
                {
                    writer.WriteLine("\t  Digit Count:   {0}", GetDigits(bn.number).ToString());
                    writer.WriteLine("\t  Prime Factors: {0}", totalFactors);
                    writer.Write("\t\t");
                    foreach (var factor in primeFactors)
                    {
                        if (factor.Value == 1)
                            writer.Write("{0}    ", factor.Key);
                        else
                            writer.Write("{0}^{1}  ", factor.Key, factor.Value);
                    }
                    writer.WriteLine();
                }


                writer.WriteLine("\tSideNum:         {0}", bn.sideSum.ToString());
                if (extended)
                    writer.WriteLine("\t  Digit Count:   {0}", GetDigits(bn.sideSum).ToString());
                writer.WriteLine("\tK:               {0}", bn.k.ToString());
                if (extended)
                    writer.WriteLine("\t  Digit Count:   {0}", GetDigits(bn.k).ToString());
                writer.WriteLine("\tKfactor:         {0}", bn.kFactor.ToString());
                if (extended)
                    TestRational(bn.k, bn.number, writer);
                if (i < numbers.Count - 1)
                {
                    BigFloat floatn = bn.number;
                    BigFloat quoitent = BigFloat.Divide(numbers[i + 1].number, floatn, AccuracyGoal.Absolute(floatn.BinaryPrecision), RoundingMode.TowardsNearest);
                    writer.WriteLine("\tnFactor to next: {0}", quoitent.ToString());
                    if (extended)
                    {
                        // check if it can be represented as a rational
                        TestRational(numbers[i + 1].number, bn.number, writer);
                    }
                    writer.WriteLine();
                }
                else
                    writer.WriteLine("\tnFactor to next: N/A\n");
                i++;
            }
            writer.WriteLine("\n\n");
        }
        /// <summary>
        /// tests if a/b can be represented more effectively as a rational
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="writer"></param>
        private static void TestRational(BigInteger a, BigInteger b, StreamWriter writer)
        {
            BigRational rational = new BigRational(a, b);
            if (rational.Denominator != b)
            {
                // they share prime factors
                writer.WriteLine("\t  Can be represented as following rational:");
                writer.WriteLine("\t\t{0} divided by", rational.Numerator.ToString());
                writer.WriteLine("\t\t{0}", rational.Denominator.ToString());
            }
        }

        public static void SavePrimes(List<int> primes)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            using (BinaryWriter writer = new BinaryWriter(File.Open(basePath + "PrimeDatabase.savedata", FileMode.Create)))
            {
                writer.Write(primes.Count);
                foreach (int integer in primes)
                {
                    writer.Write(integer);
                }
            }
        }

        public static List<int> LoadPrimes()
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            List<int> result = new List<int>();
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(basePath + "PrimeDatabase.savedata", FileMode.Open)))
                {
                    int length = reader.ReadInt32();
                    for (int i = 0; i < length; i++)
                    {
                        result.Add(reader.ReadInt32());
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Error loading list");
                return new List<int>();
            }
            return result;
        }

        public static int GetDigits(BigInteger number)
        {
            return number.ToString().Count();
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
