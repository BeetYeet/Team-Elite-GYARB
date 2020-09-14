using System;
using System.Numerics;

namespace Team_Elite
{
    public class BalancedNumber: IComparable
    {
        public readonly BigInteger number;
        public readonly BigInteger sideSum;
        public readonly BigInteger k;

        public BalancedNumber(BigInteger number, BigInteger sideSum, BigInteger k)
        {
            this.number = number;
            this.sideSum = sideSum;
            this.k = k;
        }

        public int CompareTo(object obj)
        {
            if (obj.GetType() == typeof(BalancedNumber))
                return number.CompareTo(((BalancedNumber)obj).number);
            return number.CompareTo(obj);
        }
    }
}