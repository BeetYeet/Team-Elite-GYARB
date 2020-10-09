using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Extreme.Mathematics;

namespace Team_Elite
{
    public class BalancedNumber : IComparable
    {
        public BigInteger number { get; private set; }
        public BigInteger sideSum { get; private set; }
        public BigInteger k { get; private set; }
        public BigFloat KFactor
        {
            get
            {
                if (kFactor == 0)
                {
                    GetKFactor();
                }
                return kFactor;
            }
        }
        private BigFloat kFactor = 0;
        public BigRational kFactorRational { get; private set; }

        public FactorSet PrimeFactors
        {
            get
            {
                if (primeFactors == null)
                {
                    GeneratePrimeFactors();
                }
                return primeFactors;
            }
        }

        private void GeneratePrimeFactors()
        {
            List<BigInteger> numberFactors = number.Factorize();
            List<BigInteger> kPrimeFactors = k.Factorize();
            List<BigInteger> sideSumFactors = sideSum.Factorize();
            List<BigInteger> kFactorNumeratorFactors = kFactorRational.Numerator.Factorize();
            List<BigInteger> kFactorDenominatorFactors = kFactorRational.Denominator.Factorize();
            primeFactors = new FactorSet(numberFactors, kPrimeFactors, sideSumFactors, kFactorNumeratorFactors, kFactorDenominatorFactors);
        }

        public Task CalculatePrimeFactors()
        {
            if (primeFactors == null)
            {
                return Task.Factory.StartNew(GeneratePrimeFactors);
            }
            return null;
        }

        public Task RecalculatePrimeFactors()
        {
            return Task.Factory.StartNew(GeneratePrimeFactors);
        }

        private FactorSet primeFactors = null;

        public bool fullyComputed { get; private set; } = false;

        public BalancedNumber(BigInteger number, BigInteger sideSum, BigInteger k)
        {
            this.number = number;
            this.sideSum = sideSum;
            this.k = k;
            GetKFactorRational();
            GetKFactor();
        }
        public BalancedNumber(BigInteger number, BigInteger sideSum, BigInteger k, FactorSet factors)
        {
            this.number = number;
            this.sideSum = sideSum;
            this.k = k;
            GetKFactorRational();
            GetKFactor();
            this.primeFactors = factors;
        }

        private void GetKFactorRational()
        {
            kFactorRational = new BigRational(k, number);
        }

        private void GetKFactor()
        {
            kFactor = BigFloat.Divide(kFactorRational.Numerator, kFactorRational.Denominator, AccuracyGoal.Absolute(kFactorRational.Numerator.BitCount + 10), RoundingMode.TowardsNegativeInfinity);
        }


        public int CompareTo(object obj)
        {
            return number.CompareTo(((BalancedNumber)obj).number);
        }

        public static BalancedNumber CreateFromBinaryStream(BinaryReader reader)
        {
            BigInteger number = BigInteger.Parse(reader.ReadString());
            BigInteger sideSum = BigInteger.Parse(reader.ReadString());
            BigInteger k = BigInteger.Parse(reader.ReadString());
            FactorSet factors = ReadPrimeFactors(reader);
            return new BalancedNumber(number, sideSum, k, factors);
        }

        public void WriteToBinaryStream(BinaryWriter writer)
        {
            writer.Write(number.ToString());
            writer.Write(sideSum.ToString());
            writer.Write(k.ToString());
            WritePrimeFactors(writer);
        }

        private void WritePrimeFactors(BinaryWriter writer)
        {
            WriteFactors(PrimeFactors.number, writer);
            WriteFactors(PrimeFactors.k, writer);
            WriteFactors(PrimeFactors.sideSum, writer);
            WriteFactors(PrimeFactors.kFactorNumerator, writer);
            WriteFactors(PrimeFactors.kFactorDenominator, writer);
        }

        private void WriteFactors(List<BigInteger> factors, BinaryWriter writer)
        {
            writer.Write(factors.Count);
            foreach (BigInteger primeFactor in factors)
            {
                primeFactor.Write(writer);
            }
        }

        private static FactorSet ReadPrimeFactors(BinaryReader reader)
        {
            List<BigInteger> numberFactors = ReadFactors(reader);
            List<BigInteger> kPrimeFactors = ReadFactors(reader);
            List<BigInteger> sideSumFactors = ReadFactors(reader);
            List<BigInteger> kFactorNumeratorFactors = ReadFactors(reader);
            List<BigInteger> kFactorDenominatorFactors = ReadFactors(reader);

            return new FactorSet(numberFactors, kPrimeFactors, sideSumFactors, kFactorNumeratorFactors, kFactorDenominatorFactors);
        }

        private static List<BigInteger> ReadFactors(BinaryReader reader)
        {
            List<BigInteger> factors = new List<BigInteger>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                factors.Add(MathExtras.ReadBigInteger(reader));
            }
            return factors;
        }
    }


    class BalancedNumberEqualityComparer : IEqualityComparer<BalancedNumber>
    {
        public bool Equals(BalancedNumber x, BalancedNumber y)
        {
            // Two items are equal if their keys are equal.
            return x.number == y.number;
        }

        public int GetHashCode(BalancedNumber obj)
        {
            return obj.number.GetHashCode();
        }
    }

    public class FactorSet
    {
        public FactorSet(List<BigInteger> number, List<BigInteger> k, List<BigInteger> sideSum, List<BigInteger> kFactorNumerator, List<BigInteger> kFactorDenominator)
        {
            this.number = number;
            this.k = k;
            this.sideSum = sideSum;
            this.kFactorNumerator = kFactorNumerator;
            this.kFactorDenominator = kFactorDenominator;
        }

        public List<BigInteger> number { get; internal set; }
        public List<BigInteger> k { get; internal set; }
        public List<BigInteger> sideSum { get; internal set; }
        public List<BigInteger> kFactorNumerator { get; internal set; }
        public List<BigInteger> kFactorDenominator { get; internal set; }
    }
}