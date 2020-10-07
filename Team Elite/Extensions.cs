using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extreme.Mathematics;

namespace Team_Elite
{
    public static class MathExtras
    {
        public static BigInteger ReadBigInteger(BinaryReader reader)
        {
            BigInteger integer = BigInteger.Parse(reader.ReadString());
            return integer;
        }
        public static BigFloat ReadBigFloat(BinaryReader reader)
        {
            /// BigInteger mantissa
            /// int sign
            /// int exponent
            BigInteger mantissa = ReadBigInteger(reader);
            BigFloat number = new BigFloat(reader.ReadInt32(), mantissa, reader.ReadInt32());
            return number;
        }

        public static void WriteBigInteger(BigInteger number, BinaryWriter writer)
        {
            try
            {
                writer.Write(number.ToString());
            }
            catch (ArgumentOutOfRangeException e)
            {

            }
        }

        public static void WriteBigFloat(BigFloat number, BinaryWriter writer)
        {
            /// BigInteger mantissa
            /// int sign
            /// int exponent
            WriteBigInteger(number.Mantissa, writer);
            writer.Write(number.Sign);
            writer.Write(number.Exponent);
        }
    }

    public static class Extensions
    {
        public static void Write(this BigFloat number, BinaryWriter writer)
        {
            MathExtras.WriteBigFloat(number, writer);
        }
        public static void Write(this BigInteger number, BinaryWriter writer)
        {
            MathExtras.WriteBigInteger(number, writer);
        }

        public static List<BigInteger> Factorize(this BigInteger number)
        {
            List<BigInteger> factors = new List<BigInteger>();
            int next = 0;
            while (number > 1)
            {
                if (Program.primes.Count <= next)
                {
                    // out of primes
                    if (Program.primes.Count < 1 || number < Program.primes[Program.primes.Count - 1] * Program.primes[Program.primes.Count - 1])
                    {
                        // prime!
                        Program.primes.Add(-number);
                        number = 1;
                        break;
                    }
                    factors.Add(-number);
                    number = 1;
                    break;
                }
                else
                {
                    if (Program.primes[next] < 0)
                    {
                        // ran out of safe primes
                        next = Program.primes.Count;
                        continue;
                    }
                }
                BigInteger prime = Program.primes[next];
                if (number % prime == 0)
                {
                    factors.Add(prime);
                    number /= prime;
                }
                else
                {
                    next += 1;
                }
            }
            return factors;
        }
        public static Dictionary<BigInteger, int> GetUniqueFactors(this List<BigInteger> factors)
        {
            Dictionary<BigInteger, int> uniqueFactors = new Dictionary<BigInteger, int>();
            factors.ForEach(n =>
            {
                if (uniqueFactors.ContainsKey(n))
                {
                    uniqueFactors[n]++;
                }
                else
                {
                    uniqueFactors.Add(n, 1);
                }
            });
            return uniqueFactors;
        }
    }
}
