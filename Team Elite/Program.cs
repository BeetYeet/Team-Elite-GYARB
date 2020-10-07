﻿using Extreme.Mathematics;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
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
        const double kGuessRatio = 1;

        public static List<BigInteger> primes;

        public static void GenerateBasicPrimes(out List<BigInteger> output, int upperBound)
        {
            bool[] table = new bool[upperBound];
            for (int a = 2; a < upperBound; a++)
            {
                table[a] = true;
            }

            int i = 2;
            while (i < upperBound)
            {
                for (int n = 2; i * n < upperBound; n++)
                {
                    table[i * n] = false;
                }

                i = GetNextPrime(i, upperBound, ref table);
                if (i == 0)
                    break; // no more primes
            }
            output = new List<BigInteger>();
            for (int m = 0; m < table.Length; m++)
            {
                if (table[m])
                    output.Add(m);
            }
        }

        private static int GetNextPrime(int current, int upperBound, ref bool[] table)
        {
            for (int i = current + 1; i < upperBound; i++)
            {
                if (table[i])
                {
                    return i;
                }
            }
            return 0;
        }

        static void Main(string[] args)
        {
            // Define how many threads we can have
            allowedThreads = lowPowerMode ? Environment.ProcessorCount / 2 : Environment.ProcessorCount - 1;

            savedBalancedNumbers = SaveSystem.LoadBalancedNumberList();

            savedBalancedNumbers.Add(KEquality_CheckNumber(6, 8));
            savedBalancedNumbers.Add(KEquality_CheckNumber(35, 49));
            Purge(ref savedBalancedNumbers);

            primes = SaveSystem.LoadPrimes();
            if (primes.Count < 100)
            {
                GenerateBasicPrimes(out primes, 400000);
                SaveSystem.SavePrimes(primes);
                Console.WriteLine("Saved {0} primes", primes.Count);
            }
            GenerateMorePrimes(10000000);
            SaveSystem.SavePrimes(primes);

            if (true) // set to true to just write the numbers to a file
            {
                WriteToFile();
                return;
            }

            kFactors = new List<BalancedNumber>(savedBalancedNumbers);
            CultureInfo customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = customCulture;

            // Debug that the startup has completed
            Console.WriteLine("Startup complete!");


            Chunk domain = new Chunk(0, SaveSystem.LoadLast());
            List<BalancedNumber> output = savedBalancedNumbers.GetRange(0, savedBalancedNumbers.Count);
            while (!Console.KeyAvailable)
            {
                SyncChunkDealer(AddativeGuessSearch, ref output, domain, infinity);
                savedBalancedNumbers.AddRange(output);
                Purge(ref savedBalancedNumbers);
                savedBalancedNumbers.Sort();
                // Save the numbers
                Console.WriteLine("Done! Saving...");
                SaveSystem.SaveLast(savedBalancedNumbers[savedBalancedNumbers.Count - 1].number);
                SaveSystem.SaveBalancedNumberList(savedBalancedNumbers);
                Console.WriteLine("Saved {0} balanced numbers", savedBalancedNumbers.Count);

                // Algorithm has finished, await user input
                //Console.ReadLine();
                domain = new Chunk(domain.end, domain.end * new BigInteger(10000000000000000));
            }
            Console.ReadLine();
            WriteToFile();
        }

        private static void GenerateMorePrimes(int limit)
        {
            for (int i = primes.Count; i < limit; i++)
            {
                bool prime = true;
                int rooti = (int) Math.Ceiling(Math.Sqrt(i));
                for (int j = 0; primes[j] < rooti; j++)
                {
                    if (i % primes[j] == 0)
                    {
                        prime = false;
                        break;
                    }
                }
                if (prime)
                {
                    primes.Add(i);
                    //Console.WriteLine(i);
                }
            }
            Console.WriteLine("All primes done");
        }

        private static void WriteToFile()
        {
            Console.WriteLine("Writing simple stats to file...");
            Stopwatch sw = Stopwatch.StartNew();
            SaveSystem.WriteToTxt(savedBalancedNumbers, false);
            sw.Stop();
            Console.WriteLine("Done! Took {0:0.000} ms", sw.Elapsed.TotalMilliseconds);

            Console.WriteLine("Writing full stats to file...");
            sw.Restart();
            SaveSystem.WriteToTxt(savedBalancedNumbers, true);
            sw.Stop();
            Console.WriteLine("Done! Took {0:0.000} ms", sw.Elapsed.TotalMilliseconds);
            Console.WriteLine("Done saving");
            Console.ReadLine();
        }

        #region framework

        /// <summary>
        /// How much bigger is k likely to be relative to n.
        /// Increases with n
        /// </summary>
        /// <returns>
        /// Largest known kFactor for a number lower than n
        /// </returns>
        static BigFloat GetKFactor(BigInteger number)
        {
            lock (kFactors)
            {
                BigFloat best = 1.3;
                foreach (BalancedNumber balancedNumber in kFactors)
                {
                    if (number > balancedNumber.number)
                    {
                        if (balancedNumber.KFactor > best)
                            best = balancedNumber.KFactor;
                    }
                }
                //Console.WriteLine("kFactor is {0}", best);
                return best;
            }
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
                algorithm(chunk, ref output, false);
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
                algorithm(chunk, ref output, false);
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
                threadId += 1;
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
            t.Start(new AlgorithmData(chunk, ref output, false));
            return t;
        }


        delegate bool Algorithm(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew);
        #endregion

        #region gen_1

        #region AddativeOptimized_old

        /// <summary>
        /// Old version of fast algorithm that has to run continusly
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static bool AddativeOptimizedSearch_old(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew)
        {
            bool returnValue = false;
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
                    k += 1;
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
                    k += 1;
                    sumAfter += k;
                }
            }


            while (n < chunk.end)
            {
                if (sumBefore > sumAfter)
                {
                    k += 1;
                    sumAfter += k;
                    continue;
                }
                if (sumBefore == sumAfter)
                {
                    if (HandleBalancedNumber(ref output, n))
                    {
                        if (returnOnNew)
                            return true;

                        returnValue = true;
                    }
                }
                sumBefore += n;
                n += 1;
                sumAfter -= n;
            }
            return returnValue;
        }
        static void AddativeOptimizedSearch_old(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch_old(data.chunk, ref data.output, data.returnOnNew);
        }
        #endregion

        #region AddativeOptimized
        /// <summary>
        /// Fast algorithm that has to run continusly
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static bool AddativeOptimizedSearch(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew)
        {
            bool returnValue = false;
            BigInteger n = chunk.start;
            BigInteger sumBefore = n * (n - 1) / 2;
            BigInteger sumAfter, k;
            if (guessk)
            {
                // Guess what k could be
                k = (BigInteger)((double)n * GetKFactor(n) * kGuessRatio);
                // Calculate the sumAfter for that k
                sumAfter = (k * (k + 1) / 2) - sumBefore - n;

                while (sumBefore > sumAfter)
                {
                    // final warmup the algorithm to quickly get it to the right numbers
                    k += 1;
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
                    k += 1;
                    sumAfter += k;
                }
            }
            while (n < chunk.end)
            {
                if (sumBefore > sumAfter)
                {
                    k += 1;
                    sumAfter += k;
                    continue;
                }
                if (sumBefore == sumAfter)
                {
                    if (HandleBalancedNumber(ref output, n))
                    {
                        if (returnOnNew)
                            return true;
                        returnValue = true;
                    }
                }
                sumBefore += n;
                n += 1;
                sumAfter -= n;
            }
            return returnValue;
        }
        static void AddativeOptimizedSearch(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch(data.chunk, ref data.output, data.returnOnNew);
        }
        #endregion

        #region AddativeInoptimized
        /// <summary>
        /// Baseline algorithm, should always be right
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static bool AddativeInoptimizedSearch(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew)
        {
            bool returnValue = false;
            for (BigInteger n = chunk.start; n <= chunk.end; n += 1)
            {
                BalancedNumber bn = AddativeInoptimized_CheckNumber(n);
                if (bn != null)
                {
                    output.Add(bn);
                    savedBalancedNumbers.Add(bn);
                    output.Sort();
                    if (returnOnNew)
                        return true;
                    returnValue = true;
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Baseline algorithm, should always be right
        /// </summary>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static void AddativeInoptimizedSearch(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeInoptimizedSearch(data.chunk, ref data.output, data.returnOnNew);
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
                k = (BigInteger)((double)n * GetKFactor(n) * kGuessRatio);
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
                k += 1;
            }
            return null;
        }
        #endregion
        #endregion

        #region gen_2


        #region HybridAlgorithm

        static bool aboveLimit = false;
        static void HybridSearch(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            if (!aboveLimit)
            {
                try
                {
                    AddativeOptimizedSearch_old(data.chunk, ref data.output, false);
                }
                catch (OverflowException e)
                {
                    aboveLimit = true;
                    Console.WriteLine("Reached limit of AddativeOptimizedSearch_old");
                    AddativeOptimizedSearch(data.chunk, ref data.output, false);
                }
            }
            else
                AddativeOptimizedSearch(data.chunk, ref data.output, false);

            Console.WriteLine("Chunk ended at {0}", data.chunk.end);
        }

        #endregion


        #region AddativeOptimized_superior
        const ulong domainCutoff = ulong.MaxValue / 10 * 6;
        /// <summary>
        /// Algortithm that juggles data types to optimize calculation times
        /// </summary>
        /// <remarks>
        /// Supports any size of data, but becomes inefficient when k is close to ulong.MaxValue
        /// </remarks>
        /// <param name="chunk">Domain of the search</param>
        /// <param name="output">Output buffer</param>
        static bool AddativeOptimizedSearch_superior(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew)
        {
            bool returnValue = false;
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

                k += 1;
                sumAfterV += k;
            }

            // algorithm itself
            while (n <= chunk.end)
            {
                // sb>sa
                if (sumBeforeS + sumBeforeV > sumAfterS + sumAfterV)
                {
                    if (sumAfterV > domainCutoff)
                    {
                        sumAfterS += sumAfterV;
                        sumAfterV = 0;
                    }

                    k += 1;
                    sumAfterV += k;
                    continue;
                }

                if (sumBeforeS + sumBeforeV == sumAfterS + sumAfterV)
                {
                    double percentComplete = (double)(n - chunk.start) / (double)(chunk.end - chunk.start + 1) * 100000;
                    Console.WriteLine("{0:0.000}%", percentComplete);
                    if (HandleBalancedNumber(ref output, n) && returnOnNew)
                    {
                        return true;
                    }
                    else
                    {
                        returnValue = true;
                    }
                }

                // sa>sb  or  sa=sb but we continue
                sumBeforeV += n;
                n += 1;
                sumAfterV -= n;
                if (sumBeforeV > domainCutoff)
                {
                    sumBeforeS += sumBeforeV;
                    sumBeforeV = 0;
                }
            }
            Console.WriteLine("Chunk done!");
            return returnValue;
        }
        static void AddativeOptimizedSearch_superior(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch_superior(data.chunk, ref data.output, data.returnOnNew);
        }

        #endregion

        #region AddativeOptimized_superior_volotile
        /// <summary>
        /// Works with numbers under 21 billion
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="output"></param>
        /// <param name="returnOnNew"></param>
        /// <returns></returns>
        static bool AddativeOptimizedSearch_superiorVolotile(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew)
        {
            bool returnValue = false;
            // safe
            ulong n = (ulong)chunk.start, k = n;
            BigInteger sum = 0;

            // volatile
            ulong sumBeforeV = 0, sumAfterV = 0;

            if (guessk)
            {
                k = (ulong)(GetKFactor(n) * kGuessRatio * n);

                BigInteger sumBeforeB = n * (n - 1) / 2;
                BigInteger sumAfterB = k * (k + 1) / 2 - sumBeforeB - n;
                double factor = 1;
                while (sumBeforeB > domainCutoff || sumAfterB < 0)
                {
                    while (sumBeforeB > domainCutoff)
                    {
                        BigInteger shift = new BigInteger((double)domainCutoff * factor);
                        sumBeforeB -= shift;
                        sumAfterB -= shift;
                        sum += shift;
                    }
                    factor /= 2;
                    while (sumAfterB < 0)
                    {
                        BigInteger shift = new BigInteger((double)domainCutoff * factor);
                        sumBeforeB += shift;
                        sumAfterB += shift;
                        sum -= shift;
                    }
                    factor /= 2;
                    if (factor < 0.000000001)
                        throw new OverflowException("Sums would overflow from fitting it into an ulong, use another algorithm");
                }
                sumBeforeV = (ulong)sumBeforeB;
                sumAfterV = (ulong)sumAfterB;
            }

            // fast forward
            while (sumBeforeV > sumAfterV)
            {
                k += 1;
                sumAfterV += k;
            }

            // algorithm itself
            while (n <= chunk.end)
            {
                // sb>sa
                if (sumBeforeV > sumAfterV)
                {
                    if (sumBeforeV > domainCutoff)
                    {
                        sum += sumBeforeV;
                        sumAfterV -= sumBeforeV;
                        sumBeforeV = 0;
                    }
                    k += 1;
                    sumAfterV += k;
                    continue;
                }

                if (sumBeforeV == sumAfterV)
                {
                    if (HandleBalancedNumber(ref output, n) && returnOnNew)
                        return true;
                    else
                        returnValue = true;
                }

                // sa>sb  or  sa=sb but we continue
                sumBeforeV += n;
                n += 1;
                sumAfterV -= n;
                if (sumBeforeV > domainCutoff)
                {
                    sum += sumBeforeV - n;
                    sumAfterV -= sumBeforeV - n;
                    sumBeforeV = n;
                }
            }
            return returnValue;
        }
        static void AddativeOptimizedSearch_superiorVolotile(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeOptimizedSearch_superiorVolotile(data.chunk, ref data.output, data.returnOnNew);
        }
        #endregion

        #endregion

        #region gen_3

        public static BigInteger GetNextExpected(BigInteger lastlast, BigInteger last)
        {
            return SquareDiff(lastlast, last);
        }

        private static BigInteger SquareDiff(BigInteger lastlast, BigInteger last)
        {
            return last * last / lastlast;
        }


        #region KEquality

        public static BalancedNumber KEquality_kDealer(kEqualityAlgorithm algorithm, BigInteger n, BigInteger stopAt)
        {
            int numTasks = allowedThreads * 10;
            Task[] tasks = new Task[numTasks];
            for (int i = 0; i < numTasks; i += 1)
            {
                tasks[i] = Task.Factory.StartNew((object obj) => { return algorithm(obj); }, new kEqualityData(n, numTasks, i, stopAt));
            }
            Task.WaitAll(tasks);
            BalancedNumber result = null;

            foreach (Task<object> t in tasks)
            {
                BalancedNumber number = t.Result as BalancedNumber;
                if (number != null)
                    result = number;
            }

            return result;
        }

        public delegate object kEqualityAlgorithm(object data);
        public struct kEqualityData
        {
            public BigInteger n, increase, offset, stopAt;

            public kEqualityData(BigInteger n, BigInteger increase, BigInteger offset, BigInteger stopAt)
            {
                this.n = n;
                this.increase = increase;
                this.offset = offset;
                this.stopAt = stopAt;
            }
        }

        public static BalancedNumber KEquality_CheckNumber_slow(object data)
        {
            kEqualityData info = (kEqualityData)data;
            return KEquality_CheckNumber_slow(info.n, info.increase, info.offset, info.stopAt);
        }
        public static BalancedNumber KEquality_CheckNumber_slow(BigInteger n, BigInteger increase, BigInteger offset, BigInteger stopAt)
        {
            BigInteger k = (BigInteger)((double)n * GetKFactor(n) * kGuessRatio) + offset;

            BigInteger sum = n * n * 2;
            while (k <= stopAt)
            {
                BigInteger kSum = k * (k + 1);
                if (kSum == sum)
                {
                    //Console.WriteLine("Sum is {0} for a k of {1}, which equals {2}!", kSum, k, sum);
                    return new BalancedNumber(n, n * (n - 1), k);
                }
                //Console.WriteLine("Sum is {0} for a k of {1}, which does not equal {2}", kSum, k, sum);
                k += increase;
            }
            return null;
        }
        public static BalancedNumber KEquality_CheckNumber_mod(object data)
        {
            kEqualityData info = (kEqualityData)data;
            return KEquality_CheckNumber_mod(info.n, info.increase, info.offset, info.stopAt);
        }
        public static BalancedNumber KEquality_CheckNumber_mod(BigInteger n, BigInteger increase, BigInteger offset, BigInteger stopAt)
        {
            BigInteger k = (BigInteger)((double)n * GetKFactor(n) * kGuessRatio) + offset;

            BigInteger val = n * n * 2;
            while (k <= stopAt)
            {
                if (val % k == 0)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    if (val == k * (k + 1))
                        return new BalancedNumber(n, n * (n - 1), k);
                    else
                    {
                        Console.WriteLine("FALSE POSITIVE: {0} for number {1}", k, n);
                    }
                    sw.Stop();
                    Console.WriteLine("    False positive took {0}ms to check\n", sw.ElapsedMilliseconds);
                    if (k * (k + 1) > val)
                    {
                        return null;
                    }
                }
                k += increase;
            }
            return null;
        }

        public static BalancedNumber KEquality_CheckNumber(BigInteger n, BigInteger k)
        {
            BigInteger value = n * n * 2;
            if (value == k * (k + 1))
            {
                return new BalancedNumber(n, n * (n - 1), k);
            }
            return null;
        }

        public struct KEqualityAlgrebreicData
        {
            public BigInteger n, stopAt;
            public KEqualityAlgrebreicData(BigInteger n, BigInteger stopAt)
            {
                this.n = n;
                this.stopAt = stopAt;
            }
        }
        public static BalancedNumber KEquality_CheckNumber_algebraic(object obj)
        {
            KEqualityAlgrebreicData data = (KEqualityAlgrebreicData)obj;
            return KEquality_CheckNumber_algebraic(data.n, data.stopAt);
        }

        public static BalancedNumber KEquality_CheckNumber_algebraic(BigInteger n, BigInteger stopAt)
        {
            BigInteger val = n * n * 2;
            // guess a bad k
            BigFloat k = (BigFloat)n * GetKFactor(n) * kGuessRatio;
            //Console.WriteLine("k starts as {0}", k);
            BigInteger stopAtBigFloat = stopAt;
            while (k < stopAtBigFloat)
            {
                BigFloat newk = BigFloat.Sqrt(val - k);
                k = newk;
                //Console.WriteLine("k is now {0}", k);
                if (BigFloat.Abs(newk - k) < .001)
                {
                    return KEquality_CheckNumber(n, (BigInteger)k);
                }
            }
            return null;
        }

        public static Task<BalancedNumber> KEquality_SweepForward(BigInteger n, BigInteger delta)
        {
            return Task.Factory.StartNew(() =>
            {
                Task[] tasks = new Task[(int)delta];
                for (int i = 0; i < delta; i += 1)
                {
                    tasks[i] = Task.Factory.StartNew((object obj) => { return KEquality_CheckNumber_algebraic(obj); }, new KEqualityAlgrebreicData(n + i, (n + i) * 3 / 2));
                }

                Task.WaitAll(tasks);

                foreach (Task<BalancedNumber> t in tasks)
                {
                    BalancedNumber result = t.Result;
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            });
        }

        public static Task<BalancedNumber> KEquality_SweepBackward(BigInteger n, BigInteger delta)
        {
            return Task.Factory.StartNew(() =>
            {
                Task[] tasks = new Task[((int)delta) - 1];
                for (int i = 1; i < delta; i += 1)
                {
                    tasks[i - 1] = Task.Factory.StartNew((object obj) => { return KEquality_CheckNumber_algebraic(obj); }, new KEqualityAlgrebreicData(n - i, (n - i) * 3 / 2));
                }
                Task.WaitAll(tasks);

                foreach (Task<BalancedNumber> t in tasks)
                {
                    BalancedNumber result = t.Result;
                    if (result != null)
                    {
                        return result;
                    }
                }
                return null;
            });
        }

        #endregion



        #region AddativeGuess

        static AccuracyGoal goal = AccuracyGoal.Absolute(1000);
        static BigFloat root2 = BigFloat.Sqrt(2, goal);

        static bool AddativeGuessSearch(Chunk chunk, ref List<BalancedNumber> output, bool returnOnNew)
        {
            bool returnValue = false;
            if (output.Count < 2)
                throw new ArgumentOutOfRangeException("Not enough numbers to get the next one");
            while (output[output.Count - 1].number < chunk.end && !Console.KeyAvailable)
            {
                BigInteger next = GetNextExpected(output[output.Count - 2].number, output[output.Count - 1].number);
                Console.WriteLine("Guessing next balanced number is {0}", next);
                BigFloat stopAtFloat = new BigFloat(next) * root2;
                BigInteger stopAt = BigInteger.Pow(stopAtFloat.Mantissa, stopAtFloat.Exponent) + 10;
                BalancedNumber balancedNumber = KEquality_CheckNumber_algebraic(next, stopAt);
                if (balancedNumber != null)
                {
                    output = HandleCorrectGuess(output, balancedNumber);
                    if (returnOnNew)
                    {
                        return true;
                    }
                    else
                    {
                        returnValue = true;
                        continue;
                    }
                }
                Task<BalancedNumber>[] calculations = new Task<BalancedNumber>[2];
                int count = 1;
                while (count < 100)
                {
                    Console.WriteLine("Now at extension {0}", count);
                    calculations[0] = KEquality_SweepForward(next + BigInteger.Pow(10, count - 1), BigInteger.Pow(10, count));
                    calculations[1] = KEquality_SweepBackward(next - BigInteger.Pow(10, count - 1) + 1, BigInteger.Pow(10, count));

                    while (!calculations[0].IsCompleted && !calculations[1].IsCompleted)
                    {
                        Thread.Sleep(1000);
                    }

                    if (calculations[0].Result != null)
                    {
                        balancedNumber = calculations[0].Result;
                    }
                    else
                    {
                        balancedNumber = calculations[1].Result;
                    }

                    if (balancedNumber != null)
                    {
                        output = HandleCorrectGuess(output, balancedNumber);
                        if (returnOnNew)
                        {
                            return true;
                        }
                        else
                        {
                            returnValue = true;
                            continue;
                        }
                    }

                    count += 1;
                }
            }
            return returnValue;
        }

        static void AddativeGuessSearch(object input)
        {
            AlgorithmData data = input as AlgorithmData;
            AddativeGuessSearch(data.chunk, ref data.output, data.returnOnNew);
        }

        private static List<BalancedNumber> HandleCorrectGuess(List<BalancedNumber> output, BalancedNumber balancedNumber)
        {
            output.Add(balancedNumber);
            Purge(ref output);
            output.Sort();
            kFactors.Add(balancedNumber);
            Purge(ref kFactors);
            kFactors.Sort();
            Console.WriteLine("New Balanced Number: {0}", balancedNumber.number);
            return output;
        }

        #endregion

        #endregion

        private static bool HandleBalancedNumber(ref List<BalancedNumber> output, BigInteger n)
        {
            Console.WriteLine("Possible Balanced Number: {0}, Checking validity", n);
            BalancedNumber bn = AddativeInoptimized_CheckNumber(n);
            if (bn != null)
            {
                output.Add(bn);
                savedBalancedNumbers.Add(bn);
                kFactors.Add(bn);
                return true;
            }
            Console.WriteLine("FALSE POSITIVE: {0}", n);
            return false;
        }
    }
}
