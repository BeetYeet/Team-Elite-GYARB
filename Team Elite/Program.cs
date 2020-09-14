using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Team_Elite
{
    class Program
    {
        static int allowedThreads;
        const bool lowPowerMode = false;
        /// <summary>
        /// Represents a practically infinite quantity. Used to mark Chunk to only end at infinity
        /// </summary>
        static readonly BigInteger infinity = BigInteger.Pow(new BigInteger(10), 100000);
        /// <summary>
        /// Should we try to guess k
        /// </summary>
        const bool guessk = true;
        /// <summary>
        /// How much bigger is k likely to be relative to n.
        /// Increases with n
        /// </summary>
        static decimal kFactor = 1.3M;
        /// <summary>
        /// How close to the expected k do we dare to guess.
        /// If current n is greater than the n that generated kFactor this can safely be 1
        /// </summary>
        const decimal kGuessRatio = 1M;
        static void Main(string[] args)
        {
            // Define how many threads we can have
            allowedThreads = lowPowerMode ? Environment.ProcessorCount - 5 : Environment.ProcessorCount - 1;
            /*
                        // Calculate a resonable kFactor to use as a basis for the calculations
                        List<BalancedNumber> KfactorReferenceNumber = new List<BalancedNumber>();
                        AddativeOptimizedSearch(new Chunk(new BigInteger(9228778025), new BigInteger(9228778027)), ref KfactorReferenceNumber);
                        if (KfactorReferenceNumber[0] != null)
                        {
                            kFactor = (decimal)KfactorReferenceNumber[0].k / (decimal)KfactorReferenceNumber[0].number;
                            Console.WriteLine("kFactor is {0:0.000000000}", kFactor);
                        }
                        */
            // Debug that the startup has completed
            Console.WriteLine("Startup complete!");

            // Prepare data and storage space for calculations
            Chunk domain = new Chunk(0, 1000000000);
            List<BalancedNumber> output = new List<BalancedNumber>();

            // Run the algorithm
            AsyncChunkDealer(AddativeOptimizedSearch_superior, ref output, domain, 100000000);

            output.Sort();

            output.ForEach(x =>
            {
                Console.WriteLine("k/n is {0}", (double)x.k / (double)x.number);
            });

            // Algorithm has finished, await user input
            Console.ReadLine();
        }

        private static void Benchmark(ParameterizedThreadStart algorithm, Chunk domain, out List<BalancedNumber> output, bool debugNumbers = false)
        {
            output = new List<BalancedNumber>();

            Stopwatch sw = Stopwatch.StartNew();
            AsyncChunkDealer(algorithm, ref output, domain, (domain.end - domain.start) / 100);
            sw.Stop();

            output.Sort();
            if (debugNumbers)
            {
                Console.WriteLine("\n{0} calculated there to be a total of {1} balanced numbers between {2} and {3}, those are:", algorithm.GetMethodInfo().Name, output.Count, domain.start, domain.end);
                foreach (BalancedNumber bn in output)
                {
                    Console.WriteLine(bn.number);
                }
            }
            else
            {
                Console.WriteLine("\n{0} calculated there to be a total of {1} balanced numbers between {2} and {3}", algorithm.GetMethodInfo().Name, output.Count, domain.start, domain.end);
            }

            decimal secondsTaken = sw.ElapsedTicks / (decimal)Stopwatch.Frequency;
            decimal numbersPerSecond = (decimal)(domain.end - domain.start) / secondsTaken;

            Console.WriteLine("Calculations took {0:0.00}s, for an average of {1:0.0}n/s", secondsTaken, numbersPerSecond);
        }

        static void SyncChunkDealer(Algorithm algorithm, ref List<BalancedNumber> output, Chunk domain, BigInteger chunkSize)
        {
            BigInteger next = domain.start;
            if (next < 2)
                next = 2; // start somewhere resonable
            while (next + chunkSize < domain.end)
            {
                // we can fill a whole chunk
                Chunk chunk = new Chunk(next, next + chunkSize);
                algorithm(chunk, ref output);
                next += chunkSize;
            }
            // last chunk was too large
            if (next < domain.end)
            {
                // we do have space for some more though
                Chunk chunk = new Chunk(next, domain.end);
                algorithm(chunk, ref output);
            }
        }
        static void AsyncChunkDealer(ParameterizedThreadStart algorithm, ref List<BalancedNumber> output, Chunk domain, BigInteger chunkSize)
        {
            List<Thread> threads = new List<Thread>();
            BigInteger next = domain.start;
            if (next < 2)
                next = 2; // start somewhere resonable
            int threadId = 0;

            while (next + chunkSize < domain.end)
            {
                if (threads.Count >= allowedThreads)
                {
                    List<Thread> toRemove = new List<Thread>();
                    threads.ForEach(thread => { if (!thread.IsAlive) toRemove.Add(thread); });
                    toRemove.ForEach(thread => threads.Remove(thread));
                    continue;
                }
                // we can fill a whole chunk
                Chunk chunk = new Chunk(next, next + chunkSize);
                Thread t = CreateThread(algorithm, string.Format("Thread #{0}", threadId), chunk, ref output);
                threads.Add(t);
                threadId++;
                Console.WriteLine("New chunk starting at {0}", next);
                next += chunkSize;
                Thread.Sleep(200);
            }
            // last chunk was too large
            if (next < domain.end)
            {
                // we do have space for some more though
                Chunk chunk = new Chunk(next, domain.end);
                Thread t = CreateThread(algorithm, string.Format("Thread #{0}", threadId), chunk, ref output);
                threads.Add(t);
            }
            while (threads.Count != 0)
            {
                List<Thread> toRemove = new List<Thread>();
                threads.ForEach(thread => { if (!thread.IsAlive) toRemove.Add(thread); });
                toRemove.ForEach(thread => threads.Remove(thread));
            }
        }

        static Thread CreateThread(ParameterizedThreadStart algorithm, string name, Chunk chunk, ref List<BalancedNumber> output)
        {
            Thread t = new Thread(algorithm);
            t.Name = name;
            t.Priority = ThreadPriority.AboveNormal;
            t.Start(new AlgorithmData(chunk, ref output));
            return t;
        }


        delegate void Algorithm(Chunk chunk, ref List<BalancedNumber> output);

        #region HybridAlgorithm

        static bool aboveLimit = false;
        static void HybridSearch(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            if (!aboveLimit)
            {
                try
                {
                    AddativeOptimizedSearch_old(data.chunk, ref data.output);
                }
                catch (OverflowException e)
                {
                    aboveLimit = true;
                    Console.WriteLine("Reached limit of AddativeOptimizedSearch_old");
                    AddativeOptimizedSearch(data.chunk, ref data.output);
                }
            }
            else
                AddativeOptimizedSearch(data.chunk, ref data.output);

            Console.WriteLine("Chunk ended at {0}", data.chunk.end);
        }

        #endregion

        #region AddativeOptimized_old_optimized
        const ulong domainCutoff = 10000;//ulong.MaxValue / 10 * 9;


        /// <summary>
        /// Algortithm that juggles data types to optimize calculation times
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static void AddativeOptimizedSearch_superior(Chunk chunk, ref List<BalancedNumber> output)
        {
            BigInteger nOverflow = (ulong)chunk.start;
            BigInteger sumBeforeBuffer = nOverflow * (nOverflow - 1) / 2;
            BigInteger sumOverflow = 0, kOverflow = 0;
            ulong sumBefore, k, sumAfter, n = 0;
            if (nOverflow < domainCutoff)
                k = (ulong)nOverflow;
            else
                kOverflow = nOverflow;
            if (sumBeforeBuffer > domainCutoff)
            {
                // Overflow! shift some into the overflow
                // any value will do that is less than the overflowing value, but prefer higher values for more efficiency
                ulong shift = (ulong)((double)sumBeforeBuffer * .9);
                sumOverflow += shift;
                sumBefore = (ulong)sumBeforeBuffer - shift;
            }
            else
            {
                sumBefore = (ulong)sumBeforeBuffer;
            }
            if (guessk)
            {
                // Guess what k could be
                BigInteger kBuffer = new BigInteger((decimal)(n+nOverflow) * kFactor * kGuessRatio);
                if (kBuffer > domainCutoff)
                {
                    ulong shift = (ulong)((double)kBuffer * .9);

                    k = (ulong)(kBuffer - shift);
                    kOverflow += shift;
                }
                else
                {
                    k = (ulong)kBuffer;
                }
                // Calculate the sumAfter for that k
                BigInteger sumUpToK = ((k + kOverflow) * (k + kOverflow + 1) / 2);
                BigInteger sumBeforePlusN = sumOverflow + sumBefore + n + nOverflow;
                sumAfter = (ulong)(sumUpToK - sumBeforePlusN);

                while (sumBefore > sumAfter)
                {
                    // final warmup the algorithm to quickly get it to the right numbers
                    k++;
                    BigInteger sumAfterBuffer = sumAfter + k + kOverflow;
                    if (sumAfterBuffer > domainCutoff)
                    {
                        ulong shift = (ulong)((double)sumAfterBuffer * .9);
                        sumOverflow += shift;
                        sumAfter = (ulong)(sumAfterBuffer - shift);
                    }
                    else
                        sumAfter = (ulong)sumAfterBuffer;

                    if (k > domainCutoff)
                    {
                        ulong shift = (ulong)((double)k * .9);
                        k -= shift;
                        kOverflow += shift;
                    }
                }
            }
            else
            {
                sumAfter = 0;
                k = n;
                kOverflow = nOverflow;
                while (sumBefore > sumAfter)
                {
                    // final warmup the algorithm to quickly get it to the right numbers
                    k++;
                    BigInteger sumAfterBuffer = sumAfter + k + kOverflow;
                    if (sumAfterBuffer > domainCutoff)
                    {
                        ulong shift = (ulong)((double)sumAfterBuffer * .9);
                        sumOverflow += shift;
                        sumAfter = (ulong)(sumAfterBuffer - shift);
                        sumBefore -= shift;
                    }
                    else
                        sumAfter = (ulong)sumAfterBuffer;

                    if (k > domainCutoff)
                    {
                        ulong shift = (ulong)((double)k * .9);
                        k -= shift;
                        kOverflow += shift;
                    }
                }
            }


            while (n + nOverflow < chunk.end)
            {
                if (sumBefore > sumAfter)
                {
                    k++;
                    BigInteger sumAfterBuffer = sumAfter + k + kOverflow;
                    if (sumAfterBuffer > domainCutoff)
                    {
                        ulong shift = (ulong)((double)sumAfterBuffer * .9);
                        sumOverflow += shift;
                        sumAfter = (ulong)(sumAfterBuffer - shift);
                        sumBefore -= shift;
                    }
                    else
                        sumAfter = (ulong)sumAfterBuffer;

                    if (k > domainCutoff)
                    {
                        ulong shift = (ulong)((double)k * .9);
                        k -= shift;
                        kOverflow += shift;
                    }
                    continue;
                }
                if (sumBefore == sumAfter)
                {
                    Console.WriteLine("Balanced Number: {0}", n + nOverflow);
                    BalancedNumber bn = new BalancedNumber(n + nOverflow, sumBefore + sumOverflow, k + kOverflow);
                    output.Add(bn);
                }
                sumBefore += (ulong)(n + nOverflow);
                n++;
                sumAfter -= (ulong)(n + nOverflow);
                if (n > domainCutoff)
                {
                    ulong shift = (ulong)((double)n * .9);
                    n -= shift;
                    nOverflow += shift;
                }
            }
        }
        static void AddativeOptimizedSearch_superior(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch_superior(data.chunk, ref data.output);
        }
        #endregion

        #region AddativeOptimized_old

        /// <summary>
        /// Old version of fast algorithm that has to run continusly
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static void AddativeOptimizedSearch_old(Chunk chunk, ref List<BalancedNumber> output)
        {
            if (chunk.end * chunk.end / 2 > ulong.MaxValue)
                throw new OverflowException("AddativeOptimizedSearch_old's ulongs cannot handle such large numbers, please use AddativeOptimizedSearch instead");

            ulong n = (ulong)chunk.start;
            ulong sumBefore = n * (n - 1) / 2;
            ulong k, sumAfter;
            if (guessk)
            {
                // Guess what k could be
                k = (ulong)(n * kFactor * kGuessRatio);
                // Calculate the sumAfter for that k
                sumAfter = (ulong)(k * (k + 1) / 2) - sumBefore - n;

                while (sumBefore > sumAfter)
                {
                    // final warmup the algorithm to quickly get it to the right numbers
                    k++;
                    sumAfter += k;
                }
            }
            else
            {
                sumAfter = 0;
                k = n;
                while (sumBefore > sumAfter)
                {
                    // warmup the algorithm to quickly get it to the right numbers
                    k++;
                    sumAfter += k;
                }
            }


            while (n < chunk.end)
            {
                if (sumBefore > sumAfter)
                {
                    k++;
                    sumAfter += k;
                    continue;
                }
                if (sumBefore == sumAfter)
                {
                    Console.WriteLine("Balanced Number: {0}", n);
                    BalancedNumber bn = new BalancedNumber(n, sumBefore, k);
                    output.Add(bn);
                }
                sumBefore += n;
                n++;
                sumAfter -= n;
            }
        }
        static void AddativeOptimizedSearch_old(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch_old(data.chunk, ref data.output);
        }
        #endregion

        #region AddativeOptimized
        /// <summary>
        /// Fast algorithm that has to run continusly
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static void AddativeOptimizedSearch(Chunk chunk, ref List<BalancedNumber> output)
        {
            BigInteger n = chunk.start;
            BigInteger sumBefore = n * (n - 1) / 2;
            BigInteger sumAfter, k;
            if (guessk)
            {
                // Guess what k could be
                k = new BigInteger((decimal)n * kFactor * kGuessRatio);
                // Calculate the sumAfter for that k
                sumAfter = (k * (k + 1) / 2) - sumBefore - n;

                while (sumBefore > sumAfter)
                {
                    // final warmup the algorithm to quickly get it to the right numbers
                    k++;
                    sumAfter += k;
                }
            }
            else
            {
                sumAfter = 0;
                k = n;
                while (sumBefore > sumAfter)
                {
                    // warmup the algorithm to quickly get it to the right numbers
                    k++;
                    sumAfter += k;
                }
            }
            while (n < chunk.end)
            {
                if (sumBefore > sumAfter)
                {
                    k++;
                    sumAfter += k;
                    continue;
                }
                if (sumBefore == sumAfter)
                {
                    Console.WriteLine("Balanced Number: {0}", n);
                    BalancedNumber bn = new BalancedNumber(n, sumBefore, k);
                    output.Add(bn);
                }
                sumBefore += n;
                n++;
                sumAfter -= n;
            }
        }
        static void AddativeOptimizedSearch(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch(data.chunk, ref data.output);
        }
        #endregion

        #region AddativeInoptimized
        /// <summary>
        /// Baseline algorithm, should always be right
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static void AddativeInoptimizedSearch(Chunk chunk, ref List<BalancedNumber> output)
        {
            for (BigInteger n = chunk.start; n <= chunk.end; n++)
            {
                BalancedNumber bn = AddativeInoptimized_CheckNumber(n);
                if (bn != null)
                {
                    output.Add(bn);
                    output.Sort();
                }
            }
        }
        /// <summary>
        /// Baseline algorithm, should always be right
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static void AddativeInoptimizedSearch(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeInoptimizedSearch(data.chunk, ref data.output);
        }
        /// <summary>
        /// Baseline algorithm, should always be right
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static BalancedNumber AddativeInoptimized_CheckNumber(BigInteger n)
        {
            BigInteger sumBefore = n * (n - 1) / 2;
            BigInteger sumAfter = 0;
            for (BigInteger k = n + 1; sumAfter < sumBefore; k++)
            {
                sumAfter += k;
                if (sumBefore == sumAfter)
                {
                    Console.WriteLine("Balanced Number: {0}", n);
                    BalancedNumber bn = new BalancedNumber(n, sumBefore, k);
                    return bn;
                }
            }
            return null;
        }
        #endregion
    }
}
