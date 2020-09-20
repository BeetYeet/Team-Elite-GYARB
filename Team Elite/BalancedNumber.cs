using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Team_Elite
{
    public class BalancedNumber : IComparable, ISaveable
    {
        public BigInteger number { get; private set; }
        public BigInteger sideSum { get; private set; }
        public BigInteger k { get; private set; }
        public double kFactor { get; private set; }

        public BalancedNumber(BigInteger number, BigInteger sideSum, BigInteger k)
        {
            this.number = number;
            this.sideSum = sideSum;
            this.k = k;
            kFactor = (double)k / (double)number;
        }
        public BalancedNumber() { }

        public int CompareTo(object obj)
        {
            if (obj.GetType() == typeof(BalancedNumber))
                return number.CompareTo(((BalancedNumber)obj).number);
            return number.CompareTo(obj);
        }

        public void CreateFromBinaryStream(BinaryReader reader)
        {
            number = BigInteger.Parse(reader.ReadString());
            sideSum = BigInteger.Parse(reader.ReadString());
            k = BigInteger.Parse(reader.ReadString());
            kFactor = reader.ReadDouble();
        }

        public void WriteToBinaryStream(BinaryWriter writer)
        {
            writer.Write(number.ToString());
            writer.Write(sideSum.ToString());
            writer.Write(k.ToString());
            writer.Write(kFactor);
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