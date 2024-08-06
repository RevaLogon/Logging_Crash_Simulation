using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Diagnostics;


class Program
{
    private static readonly string filePath = "/home/kuzu/Desktop/output.txt";
    private static readonly int numberOfWrites = 100000;
    private static readonly int numberOfThreads = 10;
    private static readonly object fileLock = new object();

    private static ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private static AutoResetEvent logEvent = new AutoResetEvent(false);
    private static volatile bool isWritingComplete = false;

    static void Main()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var stopwatch = Stopwatch.StartNew();

        Thread[] threads = new Thread[numberOfThreads];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(Worker);
            threads[i].Start(i);
        }

        Thread fileWriter = new Thread(FileWriter);
        fileWriter.Start();

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        stopwatch.Stop();
        Console.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds} ms");

        isWritingComplete = true;
        logEvent.Set();
        fileWriter.Join();

        Console.WriteLine("All threads have finished executing. Timestamp written to file.");
    }

    static void Worker(object threadIndexObj)
    {
        int threadIndex = (int)threadIndexObj;

        for (int i = 0; i < numberOfWrites; i++)
        {
            long ThreadId = AppDomain.GetCurrentThreadId();
            logQueue.Enqueue($"Thread {threadIndex} :: {ThreadId} - Write {i + 1} - : {DateTime.Now}\n");
            logEvent.Set();
        }

        Console.WriteLine($"Thread {threadIndex} completed its work.");
    }

    static void FileWriter()
    {
        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            while (true)
            {

                if (!logQueue.IsEmpty || !isWritingComplete)
                {
                    logEvent.WaitOne();
                }

                while (logQueue.TryDequeue(out string logMessage))
                {
                    try
                    {
                        lock (fileLock)
                        {

                            if (logMessage.Contains("Thread 5"))
                            {
                                throw new InvalidOperationException("Simulated crash during writing.");
                            }
                            writer.Write(logMessage);
                        }
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine($"Exception caught: {ex.Message}");

                    }
                }


                if (isWritingComplete && logQueue.IsEmpty)
                {
                    break;
                }
            }
        }
    }
}

//serilog ile de dene