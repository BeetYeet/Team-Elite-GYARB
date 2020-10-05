using System;
using System.Collections.Generic;
using System.IO;
using Extreme.Mathematics;

namespace Team_Elite
{
    public class BalancedNumber : IComparable, ISaveable
    {
        public BigInteger number { get; private set; }
        public BigInteger sideSum { get; private set; }
        public BigInteger k { get; private set; }
        public BigFloat kFactor { get; private set; }

        public BalancedNumber(BigInteger number, BigInteger sideSum, BigInteger k)
        {
            this.number = number;
            this.sideSum = sideSum;
            this.k = k;
            kFactor = BigFloat.Divide(k, number, AccuracyGoal.Absolute(k.BitCount + 100), RoundingMode.TowardsNegativeInfinity);
        }
        public BalancedNumber() { }

        public int CompareTo(object obj)
        {
            return number.CompareTo(((BalancedNumber)obj).number);
        }

        public void CreateFromBinaryStream(BinaryReader reader)
        {
            number = BigInteger.Parse(reader.ReadString());
            sideSum = BigInteger.Parse(reader.ReadString());
            k = BigInteger.Parse(reader.ReadString());
            kFactor = MathIO.ReadBigFloat(reader);
        }

        public void WriteToBinaryStream(BinaryWriter writer)
        {
            writer.Write(number.ToString());
            writer.Write(sideSum.ToString());
            writer.Write(k.ToString());
            kFactor.Write(writer);
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
}