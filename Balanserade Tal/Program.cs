using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Balanserade_tal
{
    class Program
    {
        public static ulong n = 3000000;
        public static bool keepRunning = true;
        private static int threadId = 0;
        private static List<int> openSlots;
        private static ulong[] slots = new ulong[Environment.ProcessorCount - 1];
        static void Main(string[] args)
        {
            SingleThreadSimpleAlgorithm();
        }

        private static void MultiThreadOptimizedAlgorithm()
        {
            int targetThreadCount = Environment.ProcessorCount - 1;
            List<Thread> threads = new List<Thread>();
            openSlots = new List<int>();
            for (int i = 0; i < targetThreadCount; i++)
            {
                openSlots.Add(i);
            }
            while (true)
            {
                List<Thread> toRemove = new List<Thread>();
                threads.ForEach(t => { if (!t.IsAlive) { toRemove.Add(t); } });
                toRemove.ForEach(t => { threads.Remove(t); });
                if (threads.Count < targetThreadCount)
                    CreateNewThread(threads);

                Console.Write("\rCurrent Numbers:  ");

                foreach(ulong i in slots)
                {
                    Console.Write( "{0}M  ", i );
                }
            }
        }

        private static void CreateNewThread(List<Thread> threads)
        {
            Thread t = new Thread(SmartAlgorithm);
            t.Name = "Calculation Thread #" + threadId;
            threadId++;
            t.Priority = ThreadPriority.AboveNormal;
            threads.Add(t);
            int slot;
            lock (openSlots)
            {
                slot = openSlots[0];
                openSlots.RemoveAt(0);
            }
            t.Start(GetNextBatch(slot));
        }

        private static ulong batchStart = 0;
        private static ulong batchIncrease = 1000000000;
        private static ulong batchStartMinimum = 2;
        private static ulong batchMaxStart = 100000;

        private static bool isReading = false;
        private static Batch GetNextBatch(int slot)
        {
            while (isReading)
            {
                Thread.Sleep(10);
            }
            isReading = true;

            Batch b = new Batch(batchStart, batchMaxStart, slot);
            if (b.start < batchStartMinimum)
            {
                b.start = batchStartMinimum;
            }
            batchStart = batchMaxStart;
            batchMaxStart += batchIncrease;

            isReading = false;
            return b;
        }

        struct Batch
        {
            public ulong start;
            public ulong end;
            public int slot;

            public Batch(ulong start, ulong end, int slot)
            {
                this.start = start;
                this.end = end;
                this.slot = slot;
            }
        }

        private static void SmartAlgorithm(object obj)
        {
            Batch batch = (Batch)obj;
            //Console.WriteLine("Batch begun at number: {0}", batch.start);
            ulong number = batch.start;
            ulong sumBefore = number * (number - 1) / 2;
            ulong sumAfter = 0;
            ulong k = number;

            while (sumBefore > sumAfter)
            {
                // move up after
                k++;
                sumAfter += k;
            }

            while (true)
            {
                if (number % 1000000 == 0)
                {
                    slots[batch.slot] = number/1000000;
                }
                if (number == batch.end)
                {
                    break;
                }
                if (sumBefore < sumAfter)
                {
                    sumBefore += number;
                    number++;
                    sumAfter -= number;
                    continue;
                }
                if (sumBefore == sumAfter)
                {
                    // balanced number
                    Console.WriteLine("\rNumber {0}: Balanced                                                                                                   ", number);
                }
                // sumBefore is greater than sumAfter
                // move up after
                k++;
                sumAfter += k;
            }
            openSlots.Add(batch.slot);
            //Console.WriteLine("Batch ended at number: {0}", number);
        }

        private static void SingleThreadSimpleAlgorithm()
        {
            Console.WriteLine("Hello World!");

            ulong number = 6741042300;
            ulong sumBefore = 1;
            ulong startAt = 2;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (number < startAt)
            {
                sumBefore += number;
                number++;
            }
            sw.Stop();
            Console.WriteLine("Startup took {0}ms", sw.ElapsedMilliseconds);

            Console.WriteLine("Started at number: {0}", number);

            while (!Console.KeyAvailable)
            {
                sumBefore += number;
                number++;

                const int testEvery = 100000;
                const int testOver = 10000;
                if (number % testEvery == 0)
                {
                    sw = new Stopwatch();
                    sw.Start();
                }


                {
                    ulong sumAfter = 0;
                    for (ulong k = number + 1; sumAfter < sumBefore; k++)
                    {
                        sumAfter += k;
                        if (sumAfter == sumBefore)
                            Console.WriteLine("Number {0}: Balanced", number);
                    }
                }

                if (number % testEvery == testOver)
                {
                    Console.WriteLine("Number {0} took {1}ms", number - testOver, ((double)sw.ElapsedMilliseconds) / testOver);
                }
            }
        }

        private static void MultiThreadSimpleAlgorithm()
        {
            Console.WriteLine("Hello World!");
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < Environment.ProcessorCount - 1; i++)
            {
                Thread t = new Thread(CheckNumbers);
                t.Name = "Calculation Thread #" + i;
                t.Priority = ThreadPriority.AboveNormal;
                threads.Add(t);
                t.Start();
            }

            Console.ReadLine();
            keepRunning = false;
        }

        private static ulong GetNextNumber()
        {
            n++;
            return n;
        }

        private static void CheckNumbers(object obj)
        {
            const int testEvery = 10000;
            const int testOver = 1000;
            bool isMeasuring = false;
            DateTime dt = DateTime.Now;
            while (keepRunning)
            {
                ulong number = GetNextNumber();
                if (number % testEvery == 0)
                {
                    dt = DateTime.Now;
                    isMeasuring = true;
                }
                CheckNumber(number);
                if (isMeasuring && number % testEvery > testOver)
                {
                    isMeasuring = false;
                    double milliSecondsPerNumber = (DateTime.Now.Subtract(dt).TotalMilliseconds) / (number % testEvery);
                    Console.WriteLine("Numbers {0} through {1} took on average {2:0.0000}ms, for a speed of {3:0.0} numbers/s", number - (number % testEvery), number, milliSecondsPerNumber, 1000 / milliSecondsPerNumber);
                }
            }
        }

        private static void CheckNumber(ulong number)
        {
            ulong sumBefore = number * (number - 1) / 2;
            ulong sumAfter = 0;
            for (ulong k = number + 1; sumAfter < sumBefore; k++)
            {
                sumAfter += k;
                if (sumAfter == sumBefore)
                {
                    Console.WriteLine("Number {0}: Balanced", number);
                    break;
                }
            }
        }
    }
}
