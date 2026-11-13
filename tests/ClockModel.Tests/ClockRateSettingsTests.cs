// Signature: OpenAI-Codex_rate_settings
using System;
using System.Threading;
using RealisticCityTime.Game;

internal static class ClockRateSettingsTests
{
    private sealed class FakeStore : IClockRateStore
    {
        internal string Stored;
        internal int Reads;
        internal int Writes;
        internal bool ThrowRead;
        internal bool ThrowBeforeWrite;
        internal bool ThrowAfterWrite;
        internal bool ReturnWithoutWriting;
        internal bool DeferWriteThenThrow;
        internal bool DeferWriteThenReturn;
        internal bool ReturnUnrecognizedStatus;
        internal string PendingWrite;
        internal Action DuringRead;
        internal Action DuringWrite;

        public string Read()
        {
            Reads++;
            if (DuringRead != null) DuringRead();
            if (ThrowRead) throw new InvalidOperationException("synthetic read failure");
            return Stored;
        }

        public ClockRateStoreWriteStatus Write(string value)
        {
            Writes++;
            if (ThrowBeforeWrite) throw new InvalidOperationException("synthetic failure before write");
            if (DeferWriteThenReturn)
            {
                PendingWrite = value;
                return ClockRateStoreWriteStatus.Unknown;
            }
            if (ReturnUnrecognizedStatus) return (ClockRateStoreWriteStatus)123;
            if (ReturnWithoutWriting) return ClockRateStoreWriteStatus.Completed;
            if (DeferWriteThenThrow)
            {
                PendingWrite = value;
                throw new InvalidOperationException("synthetic write outcome still pending");
            }
            if (DuringWrite != null) DuringWrite();
            Stored = value;
            if (ThrowAfterWrite) throw new InvalidOperationException("synthetic failure after write");
            return ClockRateStoreWriteStatus.Completed;
        }

        internal void CompletePendingWrite()
        {
            Stored = PendingWrite;
            PendingWrite = null;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("ClockRateSettings: " + message);
    }

    private static void GuardUnknownAndCorruptStorage()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.NeedsReview, "new setting requires a verified load");
        Check(settings.Apply("2.00").ToString() == "ReviewRequired", "an unread store must block a write");
        Check(settings.Multiplier == 1m && store.Writes == 0, "unread store was not modified");
        Check(settings.Apply("1.250") == ClockRateUpdateStatus.InvalidInput && store.Writes == 0, "invalid input remains rejected while blocked");
        store.Stored = "corrupt original text";
        Check(settings.Load() == ClockRateLoadStatus.InvalidStoredValue, "corrupt stored text is detected");
        Check(settings.NeedsReview, "corrupt storage requires review");
        Check(settings.Apply("3.00").ToString() == "ReviewRequired", "corrupt store must block a write");
        Check(settings.Apply("1.00").ToString() == "ReviewRequired", "unchanged value still reports blocked storage");
        Check(store.Stored == "corrupt original text" && store.Writes == 0, "corrupt original text is preserved");
        store.ThrowRead = true;
        Check(settings.Load() == ClockRateLoadStatus.StorageFailure && settings.NeedsReview, "read failure requires review");
        Check(settings.Apply("2.00").ToString() == "ReviewRequired" && store.Writes == 0, "read failure blocks a write");
        store.ThrowRead = false;
        store.Stored = "1.50";
        Check(settings.Load() == ClockRateLoadStatus.Loaded && !settings.NeedsReview, "explicit valid reread clears review");
        Check(settings.Apply("2.00") == ClockRateUpdateStatus.Applied && store.Stored == "2.00", "new write requires a successful reread");
    }

    private static void DefaultsAndInput()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Multiplier == 1m && store.Reads == 0 && store.Writes == 0, "default without automatic storage access");
        Check(settings.Load() == ClockRateLoadStatus.Missing && settings.Multiplier == 1m && !settings.NeedsReview, "missing value keeps default and permits explicit changes");
        Check(store.Writes == 0, "missing value is not overwritten");
        Check(settings.Apply("1.00") == ClockRateUpdateStatus.Unchanged && store.Writes == 0, "unchanged value does not write");
        Check(settings.Apply("10.00") == ClockRateUpdateStatus.Applied && settings.Multiplier == 10m, "upper bound accepted");
        Check(store.Stored == "10.00", "upper bound persisted canonically");
        Check(settings.Apply("1.25") == ClockRateUpdateStatus.Applied && settings.Multiplier == 1.25m, "two-digit fraction accepted");
        Check(store.Stored == "1.25", "fraction persisted canonically");
        int writes = store.Writes;
        foreach (string invalid in new string[] { null, "", "0.99", "10.01", "1.001", "1.250", "1e0", "1,25", "+1", "-1", " 1", "1 ", "1.", ".5", "1..2", "１.２" })
        {
            Check(settings.Apply(invalid) == ClockRateUpdateStatus.InvalidInput, "reject input " + invalid);
            Check(settings.Multiplier == 1.25m && store.Writes == writes, "invalid input changes neither memory nor store");
        }
        Check(settings.Apply("1") == ClockRateUpdateStatus.Applied && store.Stored == "1.00", "lower bound and canonical text");
    }

    private static void DeferredWriteCannotBeUnlockedByOneRead()
    {
        FakeStore store = new FakeStore();
        store.Stored = "1.00";
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Loaded, "deferred write test starts from a confirmed value");
        store.DeferWriteThenThrow = true;
        Check(settings.Apply("2.00") == ClockRateUpdateStatus.StorageFailure, "deferred write reports an ambiguous failure");
        Check(settings.NeedsReview && settings.IsQuarantined && settings.Multiplier == 1m && store.Stored == "1.00" && store.PendingWrite == "2.00",
            "failed write may still be pending while storage reads the old value");
        int reads = store.Reads;
        ClockRateLoadStatus load = settings.Load();
        Check(settings.NeedsReview, "one read cannot unlock an unresolved asynchronous write");
        Check(load == ClockRateLoadStatus.Quarantined && store.Reads == reads, "quarantined instance does not call the provider again");
        Check(settings.Apply("3.00").ToString() == "ReviewRequired" && store.Writes == 1,
            "quarantine prevents a new write while the old write may finish");
        store.CompletePendingWrite();
        Check(store.Stored == "2.00" && settings.Multiplier == 1m, "old write can finish after the stale observation");
        Check(settings.Load() == ClockRateLoadStatus.Quarantined && settings.NeedsReview && settings.IsQuarantined, "quarantine cannot be cleared by another read");
        Check(settings.Apply("4.00").ToString() == "ReviewRequired" && store.Writes == 1,
            "the same instance stays quarantined even after the fake completion");
    }

    private static void NormalReturnWithPendingWriteIsUnknown()
    {
        FakeStore store = new FakeStore();
        store.Stored = "1.00";
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Loaded, "normal-return pending write starts from a known value");
        store.DeferWriteThenReturn = true;
        ClockRateUpdateStatus outcome = settings.Apply("2.00");
        Check(outcome == ClockRateUpdateStatus.WriteUnknown, "normal return with pending background write must not be adopted");
        Check(settings.IsQuarantined && settings.NeedsReview && settings.Multiplier == 1m,
            "unknown completion quarantines the instance without changing the active rate");
        Check(store.Stored == "1.00" && store.PendingWrite == "2.00", "old value can remain visible while a later write is pending");
        Check(settings.Apply("3.00") == ClockRateUpdateStatus.ReviewRequired && store.Writes == 1,
            "unknown completion blocks a subsequent write");
        store.CompletePendingWrite();
        Check(store.Stored == "2.00" && settings.Load() == ClockRateLoadStatus.Quarantined && settings.NeedsReview,
            "later completion cannot silently release the instance quarantine");
    }

    private static void UnrecognizedOutcomeFailsClosed()
    {
        Check(default(ClockRateStoreWriteStatus) == ClockRateStoreWriteStatus.Unknown,
            "default provider result is unknown");
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "unrecognized outcome starts from a known missing value");
        store.ReturnUnrecognizedStatus = true;
        Check(settings.Apply("2.00") == ClockRateUpdateStatus.WriteUnknown, "unrecognized result is not accepted");
        Check(settings.IsQuarantined && settings.NeedsReview && settings.Multiplier == 1m,
            "unrecognized result quarantines without changing the active rate");
        Check(settings.Load() == ClockRateLoadStatus.Quarantined && store.Writes == 1,
            "unrecognized result cannot be cleared by a later load");
    }

    private static void AppliedIsNotProofOfDurability()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "normal-return test starts from a confirmed missing value");
        store.ReturnWithoutWriting = true;
        Check(settings.Apply("2.00") == ClockRateUpdateStatus.Applied && settings.Multiplier == 2m,
            "normal provider return causes this instance to adopt the value");
        Check(store.Stored == null && store.Writes == 1, "Applied alone does not prove the provider made data durable");
    }

    private static void LoadingDoesNotRepairStorage()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        store.Stored = "2.75";
        Check(settings.Load() == ClockRateLoadStatus.Loaded && settings.Multiplier == 2.75m, "valid saved value loads");
        Check(store.Writes == 0, "load does not write");
        store.Stored = "1.250";
        Check(settings.Load() == ClockRateLoadStatus.InvalidStoredValue, "overprecision stored value is invalid");
        Check(settings.Multiplier == 2.75m && store.Stored == "1.250" && store.Writes == 0, "invalid stored value is preserved");
        Check(settings.NeedsReview && settings.Apply("5.00").ToString() == "ReviewRequired" && store.Writes == 0, "invalid stored value cannot be overwritten");
        store.Stored = null;
        Check(settings.Load() == ClockRateLoadStatus.Missing && settings.Multiplier == 2.75m, "later missing value preserves current setting");
        store.ThrowRead = true;
        Check(settings.Load() == ClockRateLoadStatus.StorageFailure && settings.Multiplier == 2.75m && settings.NeedsReview, "read failure preserves current setting and requires review");
        Check(store.Writes == 0, "failed reads do not write defaults");
    }

    private static void WriteFailuresAreAmbiguous()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "write test explicitly loads missing store");
        Check(settings.Apply("2.00") == ClockRateUpdateStatus.Applied, "write failure precondition");
        store.ThrowBeforeWrite = true;
        Check(settings.Apply("3.00") == ClockRateUpdateStatus.StorageFailure, "failure before write is reported");
        Check(settings.Multiplier == 2m && store.Stored == "2.00" && settings.NeedsReview && settings.IsQuarantined, "failure before write leaves active value and quarantines the instance");
        store.ThrowBeforeWrite = false;
        int writes = store.Writes;
        Check(settings.Apply("3.00").ToString() == "ReviewRequired" && store.Writes == writes, "failure before write cannot be blindly retried");
        Check(settings.Load() == ClockRateLoadStatus.Quarantined && settings.Multiplier == 2m && settings.NeedsReview,
            "even a pre-write fake failure cannot prove the provider has no pending work");

        FakeStore afterStore = new FakeStore();
        ClockRateSettings after = new ClockRateSettings(afterStore);
        Check(after.Load() == ClockRateLoadStatus.Missing && after.Apply("2.00") == ClockRateUpdateStatus.Applied,
            "post-write failure test starts from a confirmed value");
        afterStore.ThrowAfterWrite = true;
        Check(after.Apply("3.00") == ClockRateUpdateStatus.StorageFailure, "failure after write is reported");
        Check(after.Multiplier == 2m && afterStore.Stored == "3.00" && after.NeedsReview && after.IsQuarantined,
            "failed write can have changed storage without changing memory");
        afterStore.ThrowAfterWrite = false;
        writes = afterStore.Writes;
        int reads = afterStore.Reads;
        Check(after.Apply("3.00").ToString() == "ReviewRequired" && afterStore.Writes == writes, "failure after write cannot be blindly retried");
        Check(after.Load() == ClockRateLoadStatus.Quarantined && afterStore.Reads == reads && after.Multiplier == 2m,
            "quarantine does not use a reread to claim the ambiguous write has settled");
        Check(after.Apply("4.00").ToString() == "ReviewRequired" && afterStore.Writes == writes,
            "the quarantined instance cannot write a different value either");
    }

    private static void ReentryAndConcurrentRequests()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        ClockRateUpdateStatus nestedWrite = ClockRateUpdateStatus.Applied;
        ClockRateLoadStatus nestedLoad = ClockRateLoadStatus.Loaded;
        store.DuringRead = delegate { nestedWrite = settings.Apply("9.00"); };
        store.Stored = "1.50";
        Check(settings.Load() == ClockRateLoadStatus.Loaded && nestedWrite == ClockRateUpdateStatus.Busy, "reentrant update is refused");
        Check(settings.Multiplier == 1.5m && store.Writes == 0, "reentrant update has no storage effect");
        store.DuringRead = null;
        store.DuringWrite = delegate { nestedLoad = settings.Load(); };
        Check(settings.Apply("2.00") == ClockRateUpdateStatus.Applied && nestedLoad == ClockRateLoadStatus.Busy, "reentrant load is refused");
        Check(settings.Multiplier == 2m && store.Stored == "2.00", "outer update commits after successful write");

        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        ClockRateUpdateStatus result = ClockRateUpdateStatus.StorageFailure;
        store.DuringWrite = delegate { entered.Set(); if (!release.WaitOne(5000)) throw new Exception("synthetic writer timed out"); };
        Thread worker = new Thread(delegate() { result = settings.Apply("4.00"); });
        worker.IsBackground = true;
        worker.Start();
        try
        {
            Check(entered.WaitOne(5000), "concurrent write entered fake storage");
            Check(settings.Multiplier == 2m, "active value remains unchanged until storage returns");
            Check(settings.NeedsReview, "in-progress write is not treated as confirmed");
            Check(settings.Apply("5.00") == ClockRateUpdateStatus.Busy, "concurrent update is refused");
            Check(settings.Load() == ClockRateLoadStatus.Busy, "concurrent load is refused");
        }
        finally
        {
            release.Set();
            Check(worker.Join(5000), "concurrent writer completes");
            entered.Close(); release.Close();
        }
        Check(result == ClockRateUpdateStatus.Applied && settings.Multiplier == 4m && store.Stored == "4.00" && !settings.NeedsReview, "successful writer commits exactly its value");
    }

    internal static void Run()
    {
        Type settings = typeof(ClockRateSettingsTests).Assembly.GetType("RealisticCityTime.Game.ClockRateSettings");
        if (settings == null) throw new Exception("ClockRateSettings is missing; no rate persistence boundary exists.");
        GuardUnknownAndCorruptStorage(); NormalReturnWithPendingWriteIsUnknown(); UnrecognizedOutcomeFailsClosed(); DeferredWriteCannotBeUnlockedByOneRead(); DefaultsAndInput(); LoadingDoesNotRepairStorage(); WriteFailuresAreAmbiguous(); AppliedIsNotProofOfDurability(); ReentryAndConcurrentRequests();
        Console.WriteLine("PASS rate setting boundary with in-memory storage; no game or persistent storage accessed");
    }
}
