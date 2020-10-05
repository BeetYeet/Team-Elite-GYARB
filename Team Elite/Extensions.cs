using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extreme.Mathematics;

namespace Team_Elite
{
    public static class MathIO
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
            writer.Write(number.ToString());
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
            MathIO.WriteBigFloat(number, writer);
        }
        public static void Write(this BigInteger number, BinaryWriter writer)
        {
            MathIO.WriteBigInteger(number, writer);
        }
    }
}
