// Signature: OpenAI-Codex_rate_settings
using System;
using System.Threading;
using System.Reflection;
using RealisticCityTime.Game;

internal static class ClockRateInputTests
{
    private sealed class FakeStore : IClockRateStore
    {
        internal string Stored;
        internal int Writes;
        internal bool ThrowAfterWrite;
        internal Action DuringWrite;

        public string Read() { return Stored; }
        public ClockRateStoreWriteStatus Write(string value)
        {
            Writes++;
            if (DuringWrite != null) DuringWrite();
            Stored = value;
            if (ThrowAfterWrite) throw new InvalidOperationException("synthetic write failure");
            return ClockRateStoreWriteStatus.Completed;
        }
    }

    private static void Check(bool condition, string scenario)
    {
        if (!condition) throw new Exception("ClockRateInput: " + scenario);
    }

    private static void EditSubmitAndCancel()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "initial input load confirms missing value");
        ClockRateInput input = new ClockRateInput(settings);
        Check(input.Draft == "1.00" && store.Writes == 0, "initial display uses current default without writing");
        input.Edit("2.5");
        Check(input.Draft == "2.5" && settings.Multiplier == 1m && store.Writes == 0, "editing only changes the draft");
        Check(input.Submit() == ClockRateUpdateStatus.Applied, "explicit submit applies a valid change");
        Check(settings.Multiplier == 2.5m && store.Stored == "2.50" && input.Draft == "2.5", "successful submit preserves submitted draft and writes canonical setting");
        input.Edit("1.250");
        Check(input.Submit() == ClockRateUpdateStatus.InvalidInput && input.Draft == "1.250", "invalid input remains visible");
        Check(settings.Multiplier == 2.5m && store.Writes == 1, "invalid submit does not write");
        Check(input.TryCancel() && input.Draft == "2.50" && store.Writes == 1, "cancel restores current valid value without writing");
        input.Edit(null);
        Check(input.Submit() == ClockRateUpdateStatus.InvalidInput && input.Draft == null, "null input remains an invalid draft");
        Check(input.TryCancel() && input.Draft == "2.50", "cancel restores a null draft");
        Check(settings.Apply("3.00") == ClockRateUpdateStatus.Applied, "external effective setting changed");
        input.Edit("9");
        int writes = store.Writes;
        Check(input.TryCancel() && input.Draft == "3.00" && store.Writes == writes, "cancel uses a fresh effective snapshot without writing");
    }

    private static void FailedStoreKeepsDraft()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "failure test confirms missing value");
        ClockRateInput input = new ClockRateInput(settings);
        store.ThrowAfterWrite = true;
        input.Edit("4.00");
        Check(input.Submit() == ClockRateUpdateStatus.StorageFailure, "write failure is visible to the caller");
        Check(input.Draft == "4.00" && settings.Multiplier == 1m, "write failure preserves draft and active value");
        Check(store.Stored == "4.00", "failed store may already have changed durable data");
        int writes = store.Writes;
        input.Edit("5.00");
        Check(input.Submit() == ClockRateUpdateStatus.ReviewRequired && input.Draft == "5.00" && store.Writes == writes,
            "presenter forwards review status and does not blindly retry an ambiguous write");
        Check(input.TryCancel() && input.Draft == "1.00" && store.Writes == writes, "cancel after failure does not attempt repair");
    }

    private static void ExplicitSnapshotDoesNotReplaceNewDraft()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "snapshot test confirms missing value");
        ClockRateInput input = new ClockRateInput(settings);
        input.Edit("2.00");
        string captured = input.Draft;
        input.Edit("3.00");
        Check(input.SubmitText(captured) == ClockRateUpdateStatus.Applied, "immutable captured text is submitted");
        Check(settings.Multiplier == 2m && store.Stored == "2.00" && input.Draft == "3.00", "older submitted snapshot does not replace newer draft");
        int writes = store.Writes;
        Check(input.SubmitText("1.250") == ClockRateUpdateStatus.InvalidInput && input.Draft == "3.00" && store.Writes == writes,
            "invalid explicit snapshot neither writes nor changes draft");
        Check(input.SubmitText(null) == ClockRateUpdateStatus.InvalidInput && input.Draft == "3.00" && store.Writes == writes,
            "null explicit snapshot leaves the draft untouched");
    }

    private static void CorruptStoreRequiresReview()
    {
        FakeStore store = new FakeStore();
        store.Stored = "1.250";
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.InvalidStoredValue, "corrupt stored text is detected");
        ClockRateInput input = new ClockRateInput(settings);
        input.Edit("2.00");
        Check(input.Submit() == ClockRateUpdateStatus.ReviewRequired, "presenter exposes corrupt-store review status");
        Check(input.Draft == "2.00" && store.Stored == "1.250" && store.Writes == 0, "corrupt original remains unchanged");
        Check(input.TryCancel() && input.Draft == "1.00" && store.Writes == 0, "cancel does not overwrite corrupt data");
    }

    private static void ReentrantDraftChange()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "reentry test confirms missing value");
        ClockRateInput input = new ClockRateInput(settings);
        ClockRateUpdateStatus nested = ClockRateUpdateStatus.Applied;
        bool cancelled = true;
        store.DuringWrite = delegate
        {
            input.Edit("7.77");
            nested = input.Submit();
            cancelled = input.TryCancel();
        };
        input.Edit("2.00");
        Check(input.Submit() == ClockRateUpdateStatus.Applied, "outer explicit submit succeeds");
        Check(nested == ClockRateUpdateStatus.Busy && !cancelled, "reentrant submit and cancel report busy");
        Check(input.Draft == "7.77" && settings.Multiplier == 2m && store.Stored == "2.00" && store.Writes == 1,
            "older submit does not overwrite the reentrant draft");
    }

    private static void ConcurrentDraftChange()
    {
        FakeStore store = new FakeStore();
        ClockRateSettings settings = new ClockRateSettings(store);
        Check(settings.Load() == ClockRateLoadStatus.Missing, "concurrent test confirms missing value");
        ClockRateInput input = new ClockRateInput(settings);
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        store.DuringWrite = delegate { entered.Set(); if (!release.WaitOne(5000)) throw new Exception("synthetic write timed out"); };
        input.Edit("2.00");
        ClockRateUpdateStatus result = ClockRateUpdateStatus.StorageFailure;
        Thread worker = new Thread(delegate() { result = input.Submit(); });
        worker.IsBackground = true;
        worker.Start();
        try
        {
            Check(entered.WaitOne(5000), "worker reached synthetic storage");
            input.Edit("3.50");
            Check(!input.TryCancel() && input.Draft == "3.50", "cancel refuses to overwrite the newer draft during submit");
            Check(input.Submit() == ClockRateUpdateStatus.Busy && store.Writes == 1, "concurrent submit is refused without another write");
            Check(input.SubmitText("9.00") == ClockRateUpdateStatus.Busy && store.Writes == 1,
                "concurrent immutable submit shares the same busy boundary");
        }
        finally
        {
            release.Set();
            Check(worker.Join(5000), "worker finishes");
            entered.Close(); release.Close();
        }
        Check(result == ClockRateUpdateStatus.Applied && settings.Multiplier == 2m, "original submitted value became effective");
        Check(input.Draft == "3.50", "original submit did not replace the newer draft");
        int writes = store.Writes;
        Check(input.TryCancel() && input.Draft == "2.00" && store.Writes == writes, "later explicit cancel restores effective value");
    }

    internal static void Run()
    {
        Type input = typeof(ClockRateInputTests).Assembly.GetType("RealisticCityTime.Game.ClockRateInput");
        if (input == null) throw new Exception("ClockRateInput is missing; no explicit-submit draft boundary exists.");
        if (input.GetMethod("SubmitText", BindingFlags.Instance | BindingFlags.NonPublic, null,
            new Type[] { typeof(string) }, null) == null)
            throw new Exception("ClockRateInput.SubmitText is missing; immutable submitted text is not supported.");
        EditSubmitAndCancel(); ExplicitSnapshotDoesNotReplaceNewDraft(); FailedStoreKeepsDraft(); CorruptStoreRequiresReview(); ReentrantDraftChange(); ConcurrentDraftChange();
        Console.WriteLine("PASS explicit-submit rate draft boundary using in-memory storage; no UI or game access");
    }
}
