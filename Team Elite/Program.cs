using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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

        private static List<BalancedNumber> savedBalancedNumbers = new List<BalancedNumber>();

        const bool checkNumbersAtStartup = false;

        /// <summary>
        /// Should we try to guess k
        /// </summary>
        const bool guessk = true;

        private static List<BalancedNumber> kFactors = new List<BalancedNumber>();

        /// <summary>
        /// How close to the expected k do we dare to guess.
        /// If current n is greater than the n that generated kFactor this can safely be 1
        /// </summary>
        const double kGuessRatio = .999;

        static void Main(string[] args)
        {
            // Define how many threads we can have
            allowedThreads = lowPowerMode ? Environment.ProcessorCount - 5 : Environment.ProcessorCount - 1;

            savedBalancedNumbers = SaveSystem.LoadBalancedNumberList();
            kFactors = new List<BalancedNumber>(savedBalancedNumbers);

            foreach (BalancedNumber balanced in savedBalancedNumbers)
            {
                if (checkNumbersAtStartup)
                {
                    if (AddativeInoptimized_CheckNumber(balanced.number) == null)
                    {
                        Console.WriteLine("Not Balanced Number: ", balanced.number);
                    }
                }
                else
                {
                    Console.WriteLine(balanced.number);
                }
            }
            CultureInfo customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = customCulture;
            for (int i = 3; i < savedBalancedNumbers.Count; i++)
            {
                BigInteger expected = GetNextExpected(savedBalancedNumbers[i - 3].number, savedBalancedNumbers[i - 2].number, savedBalancedNumbers[i - 1].number);
                Console.WriteLine("Expected {0} and it was actually {1}", expected, savedBalancedNumbers[i].number);
            }


            //Console.ReadLine();

            // Debug that the startup has completed
            Console.WriteLine("Startup complete!");

            // Prepare data and storage space for calculations
            Chunk domain = new Chunk(9000000000, infinity);
            List<BalancedNumber> output = new List<BalancedNumber>();
            AsyncChunkDealer(AddativeOptimizedSearch_superior, ref output, domain, 10000000);

            // Save the numbers
            Console.WriteLine("Done! Saving...");
            Purge(ref savedBalancedNumbers);
            SaveSystem.SaveBalancedNumberList(savedBalancedNumbers);
            Console.WriteLine("Saved {0} balanced numbers", savedBalancedNumbers.Count);

            // Algorithm has finished, await user input
            Console.ReadLine();
        }



        /// <summary>
        /// How much bigger is k likely to be relative to n.
        /// Increases with n
        /// </summary>
        /// <returns>
        /// Largest known kFactor for a number lower than n
        /// </returns>
        static double GetKFactor(BigInteger number)
        {
            foreach (BalancedNumber balancedNumber in kFactors)
            {
                if (number > balancedNumber.number)
                    return balancedNumber.kFactor;
            }
            return 1.3;
        }
        /// <summary>
        /// Removes duplicates and sorts the given list
        /// </summary>
        /// <param name="balancedNumbers">The list to be purged</param>
        static void Purge(ref List<BalancedNumber> balancedNumbers)
        {
            List<BalancedNumber> distinct = balancedNumbers.Distinct(new BalancedNumberEqualityComparer()).ToList();
            distinct.Sort();
            balancedNumbers = distinct;
        }

        private static void Benchmark(ParameterizedThreadStart algorithm, Chunk domain, out List<BalancedNumber> output, bool debugNumbers = false)
        {
            output = new List<BalancedNumber>();

            Stopwatch sw = Stopwatch.StartNew();
            AsyncChunkDealer(algorithm, ref output, domain, (domain.end - domain.start) / 24);
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

            double secondsTaken = sw.ElapsedTicks / (double)Stopwatch.Frequency;
            double numbersPerSecond = (double)(domain.end - domain.start) / secondsTaken;

            Console.WriteLine("Calculations took {0:0.00}s, for an average of {1:0.0}n/s", secondsTaken, numbersPerSecond);
        }

        static BigInteger SyncChunkDealer(Algorithm algorithm, ref List<BalancedNumber> output, Chunk domain, BigInteger chunkSize)
        {
            BigInteger next = domain.start;
            if (next < 2)
                next = 2; // start somewhere resonable
            BigInteger last = 0;
            while (next + chunkSize < domain.end)
            {
                // we can fill a whole chunk
                Chunk chunk = new Chunk(next, next + chunkSize);
                algorithm(chunk, ref output);
                next += chunkSize;
                if (last < next)
                    last = next;
                //Thread.Sleep(1000);
            }
            // last chunk was too large
            if (next < domain.end)
            {
                // we do have space for some more though
                Chunk chunk = new Chunk(next, domain.end);
                algorithm(chunk, ref output);
                if (last < next)
                    last = domain.end;
            }
            return last;
        }
        static BigInteger AsyncChunkDealer(ParameterizedThreadStart algorithm, ref List<BalancedNumber> output, Chunk domain, BigInteger chunkSize)
        {
            List<Thread> threads = new List<Thread>();
            BigInteger next = domain.start;
            if (next < 2)
                next = 2; // start somewhere resonable
            int threadId = 0;
            BigInteger last = 0;

            while (next + chunkSize < domain.end && !Console.KeyAvailable)
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
                if (last < next)
                    last = next;
                //Thread.Sleep(1000);
            }
            // last chunk was too large
            if (next < domain.end && !Console.KeyAvailable)
            {
                // we do have space for some more though
                Chunk chunk = new Chunk(next, domain.end);
                Thread t = CreateThread(algorithm, string.Format("Thread #{0}", threadId), chunk, ref output);
                threads.Add(t);
                if (last < next)
                    last = domain.end;
            }
            Console.WriteLine("No more chunks to deal, now waiting for the remaining threads to complete");
            Purge(ref output);
            int outputCount = output.Count;
            while (threads.Count != 0)
            {
                if (output.Count != outputCount)
                {
                    Purge(ref output);
                    outputCount = output.Count;
                }
                List<Thread> toRemove = new List<Thread>();
                threads.ForEach(thread => { if (!thread.IsAlive) toRemove.Add(thread); });
                toRemove.ForEach(thread => threads.Remove(thread));
            }
            return last;
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

        #region AddativeOptimized_superior
        const ulong domainCutoff = ulong.MaxValue / 10 * 6;

        /// <summary>
        /// Algortithm that juggles data types to optimize calculation times
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static void AddativeOptimizedSearch_superior(Chunk chunk, ref List<BalancedNumber> output)
        {
            // safe
            ulong n = (ulong)chunk.start, k = n;
            BigInteger sumBeforeS = n * (n - 1) / 2, sumAfterS = 0;

            // volatile
            ulong sumBeforeV = 0, sumAfterV = 0;

            if (guessk)
            {
                k = (ulong)(GetKFactor(n) * kGuessRatio * n);
                sumAfterS = k * (k + 1) / 2 - sumBeforeS - n;
            }

            // fast forward
            while (sumBeforeS + sumBeforeV > sumAfterS + sumAfterV)
            {
                if (sumAfterV > domainCutoff)
                {
                    sumAfterS += sumAfterV;
                    sumAfterV = 0;
                }
                if (sumBeforeV > domainCutoff)
                {
                    sumBeforeS += sumBeforeV;
                    sumBeforeV = 0;
                }

                k++;
                sumAfterV += k;
            }

            // algorithm itself
            while (n<=chunk.end)
            {
                // sb>sa
                if(sumBeforeS + sumBeforeV > sumAfterS + sumAfterV)
                {
                    if (sumAfterV > domainCutoff)
                    {
                        sumAfterS += sumAfterV;
                        sumAfterV = 0;
                    }

                    k++;
                    sumAfterV += k;
                    continue;
                }
                
                if(sumBeforeS + sumBeforeV == sumAfterS + sumAfterV)
                {
                    HandleBalancedNumber(ref output, n, sumBeforeV + sumBeforeS, k);
                }

                // sa>sb  or  sa=sb but we continue
                sumBeforeV += n;
                n++;
                sumAfterV -= n;
                if (sumBeforeV > domainCutoff)
                {
                    sumBeforeS += sumBeforeV;
                    sumBeforeV = 0;
                }
            }
        }
        static void AddativeOptimizedSearch_superior(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch_superior(data.chunk, ref data.output);
        }
        #endregion

        public static BigInteger GetNextExpected(BigInteger m3, BigInteger m2, BigInteger last)
        {
            double diff1 = BigInteger.Log10(m3) - BigInteger.Log10(m2);
            double diff2 = BigInteger.Log10(m2) - BigInteger.Log10(last);
            // the diffrence between diff1 and diff2 decreases as n goes up

            double diffdiff = diff1 - diff2;
            return new BigInteger(Math.Pow(10, BigInteger.Log10(last) + 2 * diff2 - diff1));
        }

        private static void HandleBalancedNumber(ref List<BalancedNumber> output, BigInteger n, BigInteger sum, BigInteger k)
        {
            Console.WriteLine("Possible Balanced Number: {0}, Checking validity", n);
            BalancedNumber bn = AddativeInoptimized_CheckNumber(n);
            if (bn != null)
            {
                new BalancedNumber(n, sum, k);
                output.Add(bn);
                savedBalancedNumbers.Add(bn);
                kFactors.Add(bn);
                return;
            }
            Console.WriteLine("FALSE POSITIVE: {0}", n);
        }

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
                k = (ulong)(n * GetKFactor(n) * kGuessRatio);
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
                    HandleBalancedNumber(ref output, n, sumBefore, k);
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
                k = new BigInteger((double)n * GetKFactor(n) * kGuessRatio);
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
                    HandleBalancedNumber(ref output, n, sumBefore, k);
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
                    savedBalancedNumbers.Add(bn);
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
            BigInteger k = n + 1;
            if (guessk)
            {
                k = new BigInteger((double)n * GetKFactor(n) * kGuessRatio);
                sumAfter = (k * (k - 1) / 2) - (n * (n + 1) / 2);
            }
            while (sumAfter < sumBefore)
            {
                sumAfter += k;
                if (sumBefore == sumAfter)
                {
                    Console.WriteLine("Balanced Number: {0}", n);
                    BalancedNumber bn = new BalancedNumber(n, sumBefore, k);
                    return bn;
                }
                k++;
            }
            return null;
        }
        #endregion
    }
}
