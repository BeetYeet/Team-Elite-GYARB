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
        public static BalancedNumber Read(string denominator)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(basePath + denominator + ".savedata", FileMode.Open)))
            {
                return BalancedNumber.CreateFromBinaryStream(reader);
            }
        }

        public static void Write(BalancedNumber data, string denominator)
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
        public static BalancedNumber Read(BinaryReader reader)
        {
            return BalancedNumber.CreateFromBinaryStream(reader);
        }

        public static void Write(BalancedNumber data, BinaryWriter writer)
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
                    if (length == 0)
                        return new List<BalancedNumber>();
                    for (int i = 0; i < length; i++)
                    {
                        BalancedNumber bn = Read(reader);
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
                writer.WriteLine("Balanced number #{0}", i + 1);
                writer.WriteLine("\tNumber:          {0}", bn.number.ToString());
                if (extended)
                {
                    writer.WriteLine("\t  Digit Count:   {0}", GetDigits(bn.number).ToString());
                    WritePrimeFactors(bn.PrimeFactors.number, writer);
                }


                writer.WriteLine("\tSideNum:         {0}", bn.sideSum.ToString());
                if (extended)
                {
                    writer.WriteLine("\t  Digit Count:   {0}", GetDigits(bn.sideSum).ToString());
                    WritePrimeFactors(bn.PrimeFactors.sideSum, writer);
                }
                writer.WriteLine("\tK:               {0}", bn.k.ToString());
                if (extended)
                {
                    writer.WriteLine("\t  Digit Count:   {0}", GetDigits(bn.k).ToString());
                    WritePrimeFactors(bn.PrimeFactors.k, writer);
                }
                writer.WriteLine("\tKfactor:         {0}", bn.KFactor.ToString());
                if (extended)
                    TestRational(bn.k, bn.PrimeFactors.kFactorNumerator, bn.number, bn.PrimeFactors.kFactorDenominator, writer);
                if (i < numbers.Count - 1)
                {
                    BigFloat floatn = bn.number;
                    BigFloat quoitent = BigFloat.Divide(numbers[i + 1].number, floatn, AccuracyGoal.Absolute(floatn.BinaryPrecision), RoundingMode.TowardsNearest);
                    writer.WriteLine("\tnFactor to next: {0}", quoitent.ToString());
                    writer.WriteLine();
                }
                else
                    writer.WriteLine("\tnFactor to next: N/A\n");
                i++;
            }
            writer.Flush();
        }

        private static void WritePrimeFactors(List<BigInteger> factors, StreamWriter writer, string whiteSpace = "")
        {
            Dictionary<BigInteger, int> primeFactors = factors.GetUniqueFactors();
            int totalFactors = factors.Count;
            writer.WriteLine(whiteSpace + "\t  Prime Factors ({0}):", totalFactors);
            writer.Write(whiteSpace + "\t\t");
            foreach (var factor in primeFactors)
            {
                if (factor.Key < 0)
                {
                    // composite, as denoted by being negative
                    writer.Write("  and an additional factor of \"{0}\", which is composite", -factor.Key);
                }
                else
                {
                    // prime
                    if (factor.Value == 1)
                        writer.Write("{0}  ", factor.Key);
                    else
                        writer.Write("{0}^{1}  ", factor.Key, factor.Value);
                }
            }
            writer.WriteLine();
        }

        /// <summary>
        /// tests if a/b can be represented more effectively as a rational
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="writer"></param>
        private static void TestRational(BigInteger a, List<BigInteger> aPrimeFactors, BigInteger b, List<BigInteger> bPrimeFactors, StreamWriter writer)
        {
            BigRational rational = new BigRational(a, b);
            if (rational.Denominator != b)
            {
                // they share prime factors
                writer.WriteLine("\t  Can be represented as following rational:");
                writer.WriteLine("\t\t{0}", rational.Numerator.ToString());
                WritePrimeFactors(aPrimeFactors, writer, "\t");
                writer.WriteLine("\t\tdivided by");
                writer.WriteLine("\t\t{0}", rational.Denominator.ToString());
                WritePrimeFactors(bPrimeFactors, writer, "\t");
            }
        }

        public static void SavePrimes(List<BigInteger> primes)
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            using (BinaryWriter writer = new BinaryWriter(File.Open(basePath + "PrimeDatabase.savedata", FileMode.OpenOrCreate)))
            {
                writer.Write(primes.Count);
                foreach (BigInteger integer in primes)
                {
                    integer.Write(writer);
                }
            }
        }

        public static List<BigInteger> LoadPrimes()
        {
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            List<BigInteger> result = new List<BigInteger>();
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(basePath + "PrimeDatabase.savedata", FileMode.Open)))
                {
                    int length = reader.ReadInt32();
                    for (int i = 0; i < length; i++)
                    {
                        result.Add(MathExtras.ReadBigInteger(reader));
                    }
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Error loading Primes");
                return new List<BigInteger>();
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
                return new BigInteger(204);
            }
        }
    }
}
