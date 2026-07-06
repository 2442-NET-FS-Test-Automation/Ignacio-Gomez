using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DsaThreading;

Console.WriteLine("Hello, World!");

ThreadingDemo();

static async Task ThreadingDemo()
{
    // Lets take a look at how c# manages threads (OS threads not CPU threads.)
    // In C# Threads are an object - like everything else. Typically the're managed
    // by the runtime behind the scenes

    Console.WriteLine($"Main runs on thread #{Environment.CurrentManagedThreadId}");
    // It takes a delegate (we can define with lambda or pass it some prewritten method) to run
    var workerThread = new Thread(() =>
    {
       Console.WriteLine($"Hello from Thread #{Environment.CurrentManagedThreadId}"); 
    });
    // Once we have a thread setup - we have to manually start it

    Console.WriteLine($"Before start() call, is Alive = {workerThread.IsAlive}");

    workerThread.Start();//Thread is now running 
    //Console.WriteLine($"Before start() call, is Alive = {workerThread.IsAlive}");
    workerThread.Join();

    //Console.WriteLine($"After Join() call, is Alive = {workerThread.IsAlive}");

    //Parallelism vs concurrency
    //Interleaving - Below even the runtime the actual OS scheduler (the thing the kernel uses to map)
    // OS threads to CPU threads) interlaeves the threads - switches them on and off CPU

    //Concurrency - task in progess (interleaved)
    // Parallelis - task executing at the same time
    //Threads gives us concurrency, true parallelism depends on the hardware
    // var threads = new List<Thread>();
    // for (int i = 1; i <= 5; i++)
    // {
    //     int id = 1;
    //     var th = new Thread(() =>
    //     {
    //        Thread.Sleep(Random.Shared.Next(5, 40));
    //        Console.WriteLine($"Worker {id} finished on thread #{Environment.CurrentManagedThreadId}"); 
    //     });
    //     threads.Add(th);
    //     th.Start();
    // }
    // foreach(Thread thread in threads) thread.Join();
    var counts = new ConcurrentDictionary<int, int>();

    var threadPool = new List<Thread>();

     for (int i = 1; i <= 8; i++)
    {
        int id = 1;
        var th = new Thread(() =>
        {
            for (int k = 0; k < 1000 ; k++)
            {
                counts.AddOrUpdate(id, 1, (_, prev) => prev + 1);

            }
        });
        threadPool.Add(th);
        th.Start();
    }
    foreach (var th in threadPool) th.Join();
    Console.WriteLine($"Recorded {counts.Values.Sum()} increments across {counts.Count} threads");
    // When working with threads, it's common to not manually create the threads ourselves
    // For short work items like what we did above, we can use the ThreadPool
    // The threadpool is just a runtime managed set of background threads that we dont have to
    // create or destroy - they're already there we can just borrwo one

    // Lets make a concurrentQueue for FIFO work, well just have it store ints
    var done = new ConcurrentQueue<int>();
    for (int i = 0; i < 5; i++)
    {
        int n = 1;
        ThreadPool.QueueUserWorkItem(_ => done.Enqueue(n * n));

    }

    // Because we dont actually have the threads themselves at our disposal - we'll
    // do like a crude await
    while (done.Count < 5) Thread.Sleep(5); // await - but way dumber
    Console.WriteLine($"Threadpool finished. {string.Join(", ", done.OrderBy(x => x))}");
    ParallelSum();

    static void ParallelSum()
    {
        int[] data =  Enumerable.Range(1, 800000).ToArray();
        // First - lets do this totally sequentially
        var sw = Stopwatch.StartNew();
        long sequential = SumRange(data, 0, data.Length);
        sw.Stop();
        Console.WriteLine($"Sequential sum = {sequential}. {sw.ElapsedTicks} ticks, 1 thread");

        //Before we parallelize this, lets play with task
        Task<long> half1 = Task.Run(() => SumRange(data, 0, data.Length / 2));
        Task<long> half2 = Task.Run(() => SumRange(data, data.Length / 2, data.Length));

        long total = half1.Result + half2.Result;
        Console.WriteLine($"Two task sum: {total}");

        long parallelTotal = 0;

        sw.Restart();
        
        Parallel.For(0, data.Length,
            () => 0L,
            //body for each loop iteration on a given thread do something
            (i, _, Local) => Local + data[i],
            //LocalFinally:After a thread finishes all its assigned items this is called
            // Adds the thread local sum 
            Local => Interlocked.Add(ref parallelTotal, Local)//combine per theard sums to the outer variable

        );
        sw.Stop();
        Console.WriteLine($"Parallel sum = {parallelTotal}. {sw.ElapsedTicks} ticks, multi-thread");




    }
    static long SumRange(int[] a, int start, int end)
    {
        long sum = 0;
        for (int i = start; i < end; i++)
        {
            sum += a[i];
        }
        return sum;
    }
    RaceDemo();
    static void RaceDemo()
    {
        var bank = new Bank();
        Parallel.For(0, 100000, _ => bank.DepositUnsafe(1));
        Console.WriteLine($"Unsafe balance = {bank.Balance} (expected 100000)");
    }

    SafeDemo();
    static void SafeDemo()
    {
        var bank = new Bank();
        Parallel.For(0, 100000, _ => bank.DepositSafe(1));
        Console.WriteLine($"safe balance = {bank.Balance} (expected 100000)");
    }

    InterLockedDemo();

    static void InterLockedDemo()
    {
        long counter = 0;
        Parallel.For(0, 100000, _ => Interlocked.Increment(ref counter));
        Console.WriteLine($"Interlocked = {counter} (excepted 10000)");

    }

    //Deadlock - if two tasks create locks on resources the other ends up needing
    // They can deadlock. In this case they never resolve - our console app
    // would be waiting forever

    //Starvation - a Thread gets blocked by another threads work - and stays alive
    // but cannot progress. Different from a deadlock - becuase the other thread is able to resolve
    // This starved thread persists - potentially starving the ThreadPool

    CancellationDemo();

    static void CancellationDemo()
    {
        // Calling for a cancellationtoken, having it auto cancel after 100ms
        // Side not using: Once we exit the scope the variable created with using
        // lives in - dispose of it
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        CancellationToken token = cts.Token;
        var work = Task.Run(() =>
        {
           for (long i = 0; ; i++)
            {
                token.ThrowIfCancellationRequested();
                if (i % 500000 == 0){}
            }
        }, token);
        try
        {
            work.Wait();
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
            Console.WriteLine("Work was cancelled cooperatively");
        }
    }

    ExceptionDemo();

    static void ExceptionDemo()
    {
        var t = Task.Run(() => throw new InvalidOperationException("oops - but in a task"));
        try
        {
            t.Wait();
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"Caught: {ex.InnerException!.Message}");
        }
    }

    await AsyncDemo();

    static async Task AsyncDemo()
    {
        Console.WriteLine($"Before await on thread # {Environment.CurrentManagedThreadId}");
        await Task.Delay(50);
        Console.WriteLine($"After await on thread #{Environment.CurrentManagedThreadId}");
    } 
    

} 



