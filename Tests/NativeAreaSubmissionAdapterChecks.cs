// OpenAI-Codex_build_review
#if FULLMAP_NATIVE_AREA_SUBMISSION_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using ICities;
using FullMapNativeResearch;

internal static class NativeAreaSubmissionAdapterChecks
{
    private static int checks;
    private static int failed;
    private sealed class CheckFailed : Exception { public CheckFailed(string message) : base(message) { } }
    private sealed class FakeWriterException : Exception { }
    private sealed class FakeQueueException : Exception { }

    private static void Check(bool condition, string label)
    {
        Interlocked.Increment(ref checks);
        if (!condition) throw new CheckFailed(label);
    }

    private static void Run(string name, Action test)
    {
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex); }
    }

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (Stream stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    private sealed class FakeAreas : IAreas
    {
        internal int Reads;
        internal int Writes;
        internal int X = -1;
        internal int Z = -1;
        internal bool RequireMoney = true;
        internal bool ThrowAfterWrite;
        internal bool Thrown;
        internal bool Live = true;
        internal bool LiveAtEntry;
        internal bool ProbeLock;
        internal bool ProbeSucceeded;
        internal NativeAreaSubmissionAdapter Owner;
        internal bool BlockAtEntry;
        internal readonly ManualResetEvent Entered = new ManualResetEvent(false);
        internal readonly ManualResetEvent Continue = new ManualResetEvent(false);
        public int unlockedAreaCount { get { Interlocked.Increment(ref Reads); return 1; } }
        public bool IsAreaUnlocked(int x, int z) { Interlocked.Increment(ref Reads); return false; }
        public void UnlockArea(int x, int z, bool requireMoney)
        {
            Interlocked.Increment(ref Writes);
            X = x; Z = z; RequireMoney = requireMoney; LiveAtEntry = Live;
            if (ProbeLock) ProbeSucceeded = CheckStatusAccess(Owner);
            Entered.Set();
            if (BlockAtEntry && !Continue.WaitOne(5000)) throw new Exception("writer wait timed out");
            if (ThrowAfterWrite) { Thrown = true; throw new FakeWriterException(); }
        }
    }

    private sealed class FakeThreading : IThreading
    {
        internal int Reads;
        internal int Calls;
        internal int Deliveries;
        internal bool ThrowBefore;
        internal bool ThrowAfter;
        internal Exception ThrownBefore;
        internal Exception ThrownAfter;
        internal bool Inline;
        internal bool ProbeLock;
        internal bool ProbeSucceeded;
        internal Action Saved;
        internal Action BeforeReturn;
        internal NativeAreaSubmissionAdapter Owner;
        public bool simulationPaused { get { Interlocked.Increment(ref Reads); return false; } }
        public void QueueSimulationThread(Action action)
        {
            if (action == null || action.GetType() != typeof(Action) ||
                typeof(IThreading).GetMethod("QueueSimulationThread").GetParameters()[0].ParameterType != action.GetType())
                throw new Exception("fake callback Action type mismatch");
            Console.WriteLine("FAKE_CALLBACK_TYPE=" + action.GetType().AssemblyQualifiedName);
            Interlocked.Increment(ref Calls);
            if (ProbeLock) ProbeSucceeded = CheckStatusAccess(Owner);
            if (ThrowBefore) { ThrownBefore = new FakeQueueException(); throw ThrownBefore; }
            Saved = action;
            if (Inline) Deliver();
            if (BeforeReturn != null) BeforeReturn();
            if (ThrowAfter) { ThrownAfter = new FakeQueueException(); throw ThrownAfter; }
        }
        internal void Deliver()
        {
            Check(Saved != null, "the same saved action must exist");
            Interlocked.Increment(ref Deliveries);
            Saved();
        }
    }

    private static bool CheckStatusAccess(NativeAreaSubmissionAdapter adapter)
    {
        ManualResetEvent completed = new ManualResetEvent(false);
        Thread worker = new Thread(delegate() { NativeAreaOutcome ignored = adapter.Status; completed.Set(); });
        worker.IsBackground = true;
        worker.Start();
        return completed.WaitOne(3000);
    }

    private static NativeAreaOutcome Core(NativeAreaSubmissionAdapter adapter, FakeAreas areas, FakeThreading threading, int x, int z)
    {
        MethodInfo method = typeof(NativeAreaSubmissionAdapter).GetMethod("Core", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(method != null && method.IsPrivate, "private Core must exist");
        return (NativeAreaOutcome)method.Invoke(adapter, new object[] { areas, threading, x, z });
    }

    private static void PublicSubmit()
    {
        NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
        FakeAreas areas = new FakeAreas();
        FakeThreading threading = new FakeThreading();
        Check(adapter.Submit(areas, threading, 2, 3) == NativeAreaOutcome.Unavailable, "plausible public Submit must be unavailable");
        Check(adapter.Submit(null, null, 2, 3) == NativeAreaOutcome.Unavailable, "null public Submit must be unavailable");
        Check(areas.Reads == 0 && areas.Writes == 0 && threading.Reads == 0 && threading.Calls == 0, "public Submit must not touch fake providers");
        Check(adapter.Status == NativeAreaOutcome.Unavailable, "public Submit must not start a ticket");
    }

    private static void InvalidCoordinates()
    {
        int[,] invalid = { { -1, 0 }, { 5, 0 }, { 0, -1 }, { 0, 5 }, { int.MinValue, int.MaxValue } };
        for (int i = 0; i < invalid.GetLength(0); i++)
        {
            NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
            FakeAreas areas = new FakeAreas();
            FakeThreading threading = new FakeThreading();
            Check(Core(adapter, areas, threading, invalid[i, 0], invalid[i, 1]) == NativeAreaOutcome.Unavailable, "invalid coordinate unavailable");
            Check(threading.Calls == 0 && areas.Writes == 0 && areas.Reads == 0, "invalid coordinate has no provider calls");
        }
    }

    private static void NormalAndReplay()
    {
        NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
        FakeAreas areas = new FakeAreas(); areas.Owner = adapter; areas.ProbeLock = true;
        FakeThreading threading = new FakeThreading(); threading.Owner = adapter; threading.ProbeLock = true;
        Check(areas.X == -1 && areas.Z == -1 && areas.RequireMoney, "fake writer argument sentinels are distinct");
        Check(Core(adapter, areas, threading, 4, 3) == NativeAreaOutcome.Pending, "stored action must remain pending");
        Check(threading.ProbeSucceeded, "scheduler must be called without shared state lock");
        Check(threading.Calls == 1 && areas.Writes == 0, "queue once and wait");
        Check(Core(adapter, areas, threading, 1, 1) == NativeAreaOutcome.Pending && threading.Calls == 1, "one ticket per instance");
        Action original = threading.Saved;
        threading.Deliver();
        Check(areas.ProbeSucceeded, "writer must be called without shared state lock");
        Check(areas.Writes == 1 && areas.X == 4 && areas.Z == 3 && !areas.RequireMoney, "fake writer receives exact x, z, false");
        Check(adapter.Status == NativeAreaOutcome.Unknown, "void fake writer return is unknown");
        threading.Deliver();
        Check(Object.ReferenceEquals(original, threading.Saved) && threading.Deliveries == 2 && areas.Writes == 1, "same action actually replays without second write");
        Check(Core(adapter, areas, threading, 4, 3) == NativeAreaOutcome.Unknown && threading.Calls == 1, "unknown cannot retry");
    }

    private static void ConcurrentRequests()
    {
        NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
        FakeAreas areas = new FakeAreas();
        FakeThreading threading = new FakeThreading();
        NativeAreaOutcome[] results = new NativeAreaOutcome[2];
        Exception[] errors = new Exception[2];
        Thread[] workers = new Thread[2];
        ManualResetEvent bothReady = new ManualResetEvent(false);
        ManualResetEvent start = new ManualResetEvent(false);
        int ready = 0;
        for (int i = 0; i < workers.Length; i++)
        {
            int index = i;
            workers[i] = new Thread(delegate() {
                if (Interlocked.Increment(ref ready) == 2) bothReady.Set();
                if (!start.WaitOne(5000)) { errors[index] = new Exception("start gate timed out"); return; }
                try { results[index] = Core(adapter, areas, threading, index + 1, index + 2); }
                catch (Exception ex) { errors[index] = ex; }
            });
            workers[i].IsBackground = true;
            workers[i].Start();
        }
        bool readyInTime = bothReady.WaitOne(5000);
        start.Set();
        bool firstDone = workers[0].Join(5000);
        bool secondDone = workers[1].Join(5000);
        Check(readyInTime && firstDone && secondDone, "both concurrent callers reached the gate and completed");
        Check(errors[0] == null && errors[1] == null, "both concurrent callers returned without error");
        Check(results[0] == NativeAreaOutcome.Pending && results[1] == NativeAreaOutcome.Pending, "both callers observe one pending ticket");
        Check(threading.Calls == 1 && threading.Saved != null && areas.Writes == 0, "one scheduler call and no premature writer");
        threading.Deliver();
        Check(areas.Writes == 1 && !areas.RequireMoney && ((areas.X == 1 && areas.Z == 2) || (areas.X == 2 && areas.Z == 3)), "one winner's fake arguments reach writer");
        Check(adapter.Status == NativeAreaOutcome.Unknown, "concurrent request outcome remains unknown");
    }
    private static void QueueFailures()
    {
        for (int scenario = 0; scenario < 3; scenario++)
        {
            NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
            FakeAreas areas = new FakeAreas();
            FakeThreading threading = new FakeThreading();
            threading.ThrowBefore = scenario == 0;
            threading.ThrowAfter = scenario != 0;
            threading.Inline = scenario == 2;
            Check(Core(adapter, areas, threading, 2, 4) == NativeAreaOutcome.Unknown, "queue exception is unknown");
            Check(threading.Calls == 1 && areas.Writes == (scenario == 2 ? 1 : 0), "queue failure stage observed");
            Check(scenario == 0 ? threading.ThrownBefore != null && threading.ThrownBefore.GetType() == typeof(FakeQueueException) && threading.ThrownAfter == null : threading.ThrownBefore == null && threading.ThrownAfter != null && threading.ThrownAfter.GetType() == typeof(FakeQueueException), "the designated fake queue exception was thrown");
            Check((scenario == 0) == (threading.Saved == null), "queue failure stage saved action as specified");
            if (scenario != 0)
            {
                Action original = threading.Saved;
                threading.Deliver();
                Check(Object.ReferenceEquals(original, threading.Saved) && threading.Deliveries == (scenario == 2 ? 2 : 1), "saved callback actually delivered");
                Check(areas.Writes == (scenario == 2 ? 1 : 0), "late or replayed callback cannot start another fake write");
            }
            Check(Core(adapter, areas, threading, 2, 4) == NativeAreaOutcome.Unknown && threading.Calls == 1, "queue exception has no retry");
        }
    }

    private static void WriterFailure()
    {
        NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
        FakeAreas areas = new FakeAreas(); areas.ThrowAfterWrite = true;
        FakeThreading threading = new FakeThreading();
        Check(Core(adapter, areas, threading, 3, 2) == NativeAreaOutcome.Pending, "writer case queued");
        threading.Deliver();
        Check(areas.Writes == 1 && areas.Thrown, "controlled fake writer recorded then actually threw");
        Check(adapter.Status == NativeAreaOutcome.Unknown, "writer exception is unknown");
        threading.Deliver();
        Check(threading.Deliveries == 2 && areas.Writes == 1, "writer exception action replay has no second write");
        Check(Core(adapter, areas, threading, 3, 2) == NativeAreaOutcome.Unknown && threading.Calls == 1, "writer exception has no retry");
    }

    private static void UnloadBoundary()
    {
        NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
        FakeAreas areas = new FakeAreas();
        FakeThreading threading = new FakeThreading();
        Check(Core(adapter, areas, threading, 1, 2) == NativeAreaOutcome.Pending, "unload case queued");
        areas.Live = false;
        threading.Deliver();
        Check(areas.Writes == 1 && !areas.LiveAtEntry, "fake writer observes invalidated fake provider at entry");
        Check(adapter.Status == NativeAreaOutcome.Unknown, "invalidated fake provider outcome remains unknown");
    }

    private static void InFlightQueueFailure()
    {
        NativeAreaSubmissionAdapter adapter = new NativeAreaSubmissionAdapter();
        FakeAreas areas = new FakeAreas(); areas.BlockAtEntry = true;
        FakeThreading threading = new FakeThreading(); threading.ThrowAfter = true;
        Thread worker = null;
        Exception callbackFailure = null;
        bool enteredBeforeQueueThrow = false;
        threading.BeforeReturn = delegate() {
            worker = new Thread(delegate() { try { threading.Deliver(); } catch (Exception ex) { callbackFailure = ex; } });
            worker.IsBackground = true;
            worker.Start();
            enteredBeforeQueueThrow = areas.Entered.WaitOne(5000);
            if (!enteredBeforeQueueThrow) throw new Exception("fake writer did not enter");
        };
        try
        {
            Check(Core(adapter, areas, threading, 4, 4) == NativeAreaOutcome.Unknown, "throw after writer entry is unknown");
            Check(enteredBeforeQueueThrow, "writer entry wait succeeded before queue throw");
            Check(threading.ThrownAfter != null && threading.ThrownAfter.GetType() == typeof(FakeQueueException), "designated queue exception actually thrown after writer entry");
            Check(areas.Writes == 1 && adapter.Status == NativeAreaOutcome.Unknown, "writer already entered before queue failure");
        }
        finally { areas.Continue.Set(); }
        Check(worker != null && worker.Join(5000), "fake in-flight callback completed");
        Check(callbackFailure == null && adapter.Status == NativeAreaOutcome.Unknown, "late callback cannot change unknown");
        threading.Deliver();
        Check(threading.Deliveries == 2 && areas.Writes == 1, "in-flight action replay cannot write twice");
        Check(Core(adapter, areas, threading, 4, 4) == NativeAreaOutcome.Unknown && threading.Calls == 1, "in-flight unknown has no retry");
    }

    private static int Main(string[] args)
    {
        if (args.Length != 2) { Console.WriteLine("SETUP expected surrogate and candidate hashes"); return 2; }
        Assembly surrogate = typeof(IAreas).Assembly;
        Assembly candidate = typeof(NativeAreaSubmissionAdapter).Assembly;
        Console.WriteLine("CLR=" + Environment.Version + " BITS=" + (IntPtr.Size * 8));
        Console.WriteLine("SURROGATE_ID=" + surrogate.GetName().FullName);
        Console.WriteLine("SURROGATE_PATH=" + surrogate.Location);
        Console.WriteLine("SURROGATE_SHA256=" + Hash(surrogate.Location));
        Console.WriteLine("CANDIDATE_ID=" + candidate.GetName().FullName);
        Console.WriteLine("CANDIDATE_PATH=" + candidate.Location);
        Console.WriteLine("CANDIDATE_SHA256=" + Hash(candidate.Location));
        if (surrogate.GetName().Name != "ICities" || surrogate.GetName().Version.ToString() != "1.17.0.0" ||
            (surrogate.GetName().GetPublicKeyToken() != null && surrogate.GetName().GetPublicKeyToken().Length != 0) ||
            !String.Equals(Path.GetFullPath(surrogate.Location), Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ICities.dll")), StringComparison.OrdinalIgnoreCase) ||
            !String.Equals(Path.GetFullPath(candidate.Location), Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NativeAreaSubmissionAdapter.dll")), StringComparison.OrdinalIgnoreCase) ||
            !String.Equals(Hash(surrogate.Location), args[0], StringComparison.OrdinalIgnoreCase) ||
            !String.Equals(Hash(candidate.Location), args[1], StringComparison.OrdinalIgnoreCase))
        { Console.WriteLine("SETUP identity or hash mismatch"); return 2; }
        Run("public submit", PublicSubmit);
        Run("invalid coordinates", InvalidCoordinates);
        Run("pending, one writer, and replay", NormalAndReplay);
        Run("concurrent private requests", ConcurrentRequests);
        Run("queue exceptions", QueueFailures);
        Run("fake writer exception", WriterFailure);
        Run("fake unload boundary", UnloadBoundary);
        Run("in-flight callback and queue exception", InFlightQueueFailure);
        Console.WriteLine("CHECKS=" + checks + " FAILED=" + failed);
        return failed == 0 ? 0 : 1;
    }
}
#endif