using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading;

namespace Balanserade_Tal_Multiplication
{
    class Program
    {
        static void Main(string[] args)
        {
            BigInteger number = 2;
            BigInteger sumBefore = Factorial(number-1);
            BigInteger k = number + 1;
            BigInteger sumAfter = Factorial(number + 1, k);

            while (true)
            {
                if (number % 100 == 0)
                {
                    Console.Write("\rCurrent Number: {0}              ", number);
                }
                if (sumBefore > sumAfter)
                {
                    k++;
                    sumAfter *= k;
                    continue;
                }
                if(sumAfter == sumBefore)
                {
                    Console.WriteLine("\rBalanserat tal: {0}            ", number);
                }
                sumBefore *= number;
                number++;
                sumAfter /= number;
                continue;
            }

        }
        public static BigInteger Factorial(BigInteger n)
        {
            BigInteger result = 1;

            for (int i = 1; i <= n; i++)
            {
                result *= i;
            }

            return result;
        }
        public static BigInteger Factorial(BigInteger start, BigInteger end)
        {
            BigInteger result = 1;

            for (BigInteger i = start; i <= end; i++)
            {
                result *= i;
            }

            return result;
        }
    }
}
