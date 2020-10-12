using Extreme.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Team_Elite
{
    public static class MathExtras
    {
        public static BigInteger gcd(BigInteger a, BigInteger b)
        {
            BigInteger remainder;
            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }
            return a;
        }

        public static PrimeFactorizationResult PollardsRho(BigInteger n)
        {
            if (MillerTest(n))
                return new PrimeFactorizationResult(PrimeFactorizationResultEvaluation.Prime);
            BigInteger x = 2, x_fixed = 2, count, size = 2, factor, loop = 1;
            Stopwatch sw = Stopwatch.StartNew();

            BigInteger rnd = 1; //BigInteger.Random(new Random(), n.BitCount + 10) % (n / 2);
            do
            {
                //Console.WriteLine("Loop {0}", loop);
                count = size;
                do
                {
                    x = (x * x + rnd) % n;
                    factor = gcd(BigInteger.Abs(x - x_fixed), n);
                    //Console.WriteLine("count = {0}  x = {1}  factor = {2}\n", size - count + 1, x, factor);
                }
                while ((count -= 1) > 0 && factor == 1);
                size *= 2;
                x_fixed = x;
                loop += 1;
            }
            while (factor == 1 && sw.Elapsed.TotalMinutes < 30);
            //Console.WriteLine("Factor is {0}", factor);
            if (factor != n)
            {
                return new PrimeFactorizationResult(factor);
            }
            else
            {
                if (factor == n)
                    return new PrimeFactorizationResult(PrimeFactorizationResultEvaluation.Faliure);

                // took too long
                return new PrimeFactorizationResult(PrimeFactorizationResultEvaluation.Faliure);
            }
        }

        public static PrimeFactorizationResult PollardsBrentRho(BigInteger n)
        {
            if (MillerTest(n))
                return new PrimeFactorizationResult(PrimeFactorizationResultEvaluation.Prime);
            BigInteger x = 2, x_fixed = 2, count, size = 2, factor, loop = 1, rnd = BigInteger.Random(new Random(), n.BitCount + 10) % (n / 2);
            do
            {
                //Console.WriteLine("Loop {0}", loop);
                count = size;
                do
                {
                    BigInteger term = 1;
                    for (int i = 0; i < 100; i++)
                    {
                        x = (x * x + rnd) % n;
                        term *= x - x_fixed;
                    }
                    factor = gcd(BigInteger.Abs(term), n);
                    //Console.WriteLine("count = {0}  x = {1}  factor = {2}\n", size - count + 1, x, factor);
                }
                while ((count -= 1) > 0 && factor == 1);
                size *= 2;
                x_fixed = x;
                loop += 1;
            }
            while (factor == 1);
            //Console.WriteLine("Factor is {0}", factor);
            if (factor != n && n % factor == 0)
            {
                return new PrimeFactorizationResult(factor);
            }
            else
            {
                return new PrimeFactorizationResult(PrimeFactorizationResultEvaluation.Faliure, factor);
            }
        }

        public struct PrimeFactorizationResult
        {
            public readonly PrimeFactorizationResultEvaluation result;
            public readonly BigInteger primeFactor;

            internal PrimeFactorizationResult(BigInteger primeFactor) : this()
            {
                this.primeFactor = primeFactor;
                result = PrimeFactorizationResultEvaluation.FactorFound;
            }

            internal PrimeFactorizationResult(PrimeFactorizationResultEvaluation result) : this()
            {
                if (result == PrimeFactorizationResultEvaluation.FactorFound)
                    throw new ArgumentException();
                this.result = result;
            }
            internal PrimeFactorizationResult(PrimeFactorizationResultEvaluation result, BigInteger primeFactor) : this()
            {
                if (result == PrimeFactorizationResultEvaluation.FactorFound)
                    throw new ArgumentException();
                this.result = result;
                this.primeFactor = primeFactor;
            }
        }

        public enum PrimeFactorizationResultEvaluation
        {
            Prime,
            FactorFound,
            Faliure
        }

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
                writer.Write("0");
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
            // n = 2^r * d + 1

            BigInteger limit = BigInteger.Min(n - 2, (BigInteger)BigFloat.Ceiling(2 * BigFloat.Pow(BigFloat.Log(n), 2)));

            for (int a = 2; a < limit; a++)
            {
                BigInteger x = BigInteger.ModularPow(a, d, n);
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

                    // Calculations concluded that number number is prime
                    if (MathExtras.MillerTest(number))
                    {
                        factors.Add(number);
                        return factors;
                    }

                    BigInteger sqrt = BigInteger.Sqrt(number);
                    if (sqrt * sqrt == number)
                    {
                        // Calculations concluded that number has two factors of sqrt
                        if (MathExtras.MillerTest(sqrt))
                        {
                            // and they are prime
                            factors.Add(sqrt);
                            factors.Add(sqrt);
                        }
                        else
                        {
                            // and they are composite
                            factors.Add(-sqrt);
                            factors.Add(-sqrt);
                        }
                        continue;
                    }

                    // no more factors can be found
                    factors.Add(-number);
                    return factors;
                    /*
                    MathExtras.PrimeFactorizationResult res = MathExtras.PollardsRho(number);
                    switch (res.result)
                    {
                        case MathExtras.PrimeFactorizationResultEvaluation.FactorFound:
                            // Calculations concluded that number has a factor of res.primeFactor
                            factors.Add(res.primeFactor);
                            number /= res.primeFactor;
                            continue;

                        case MathExtras.PrimeFactorizationResultEvaluation.Prime:
                            // Calculations concluded that number number is prime
                            factors.Add(number);
                            return factors;

                        default:
                            // There was an issue factorizing
                            factors.Add(-number);
                            return factors;
                    }
                    */
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