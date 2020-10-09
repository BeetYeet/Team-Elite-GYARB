using Extreme.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

        /// <summary>
        /// returns true for a prime number n, otherwise false
        /// </summary>
        public static bool MillerTest(BigInteger n)
        {
            if (n <= 1)
                throw new ArgumentOutOfRangeException();
            BigInteger d = n - 1;
            int r = 0;
            while (d % 2 == 0)
            {
                r++;
                d /= 2;
            }
            // n = 2^r * d - 1

            BigInteger limit = BigInteger.Min(n - 2, (BigInteger)BigFloat.Ceiling(2 * BigFloat.Pow(BigFloat.Log(n), 2)));

            for (int a = 2; a < limit; a++)
            {
                BigInteger A = a;
                BigInteger x = BigInteger.ModularPow(A, d, n);
                if (x == 1 || x == n - 1)
                    continue;
                bool ret = false;
                for (int count = 0; count < r - 1; count++)
                {
                    x = BigInteger.ModularPow(x, 2, n);
                    if (x == n - 1)
                    {
                        ret = true;
                        break;
                    }
                }
                if (ret)
                    continue;
                return false;
            }
            return true;
        }
    }

    public static class Extensions
    {
        public static void CalculatePrimeFactors(this List<BalancedNumber> numbers)
        {
            List<Task> calculations = new List<Task>();
            numbers.ForEach(x =>
            {
                Task calc = x.CalculatePrimeFactors();
                if (calc != null)
                    calculations.Add(calc);
            });
            Task.WaitAll(calculations.ToArray());
        }

        public static void RecalculatePrimeFactors(this List<BalancedNumber> numbers)
        {
            List<Task> recalculations = new List<Task>();
            numbers.ForEach(x =>
            {
                Task recalc = x.RecalculatePrimeFactors();
                if (recalc != null)
                    recalculations.Add(recalc);
            });
            Task.WaitAll(recalculations.ToArray());
        }

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
            if (number.BitCount > 10 && MathExtras.MillerTest(number))
            {
                // it's prime already!
                return new List<BigInteger>();
            }
            List<BigInteger> factors = new List<BigInteger>();
            int next = 0;
            while (number > 1)
            {
                if (Program.primes.Count <= next)
                {
                    // out of primes
                    if (MathExtras.MillerTest(number))
                    {
                        // prime!
                        factors.Add(number);
                        break;
                    }
                    factors.Add(-number);
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