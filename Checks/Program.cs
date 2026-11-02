// Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population
// Updated by: OpenAI-Codex_Realistic_Urban_Population
using System;
using System.Threading;
using RealisticUrbanPopulation.Demand;

internal static class Program
{
    private static int assertions;
    private static void Check(bool condition, string message)
    { Interlocked.Increment(ref assertions); if (!condition) throw new Exception(message); }
    private static void Equal<T>(T expected, T actual, string message)
    { Check(object.Equals(expected, actual), message + ": expected=" + expected + ", actual=" + actual); }
    private static CategoryPolicy Native()
    {
        CategoryPolicy policy;
        Check(DemandPolicy.TryCreate((int)DemandMode.Native, null, out policy), "Native policy accepted");
        return policy;
    }
    private static CategoryPolicy Fixed(int target)
    {
        CategoryPolicy policy;
        Check(DemandPolicy.TryCreate((int)DemandMode.FixedTarget, target, out policy), "Fixed policy accepted");
        return policy;
    }
    private static DemandSettings All(int target)
    { return new DemandSettings(Fixed(target), Fixed(target), Fixed(target)); }
    private static long Ready(DemandSession session)
    {
        long epoch = session.BeginCity();
        Check(session.CompleteCityLoad(epoch), "City load completes");
        Check(session.SetControlPermission(epoch, ControlPermission.Allowed), "Control permission accepted");
        return epoch;
    }

    private static void NativePassThrough()
    {
        CategoryPolicy native = Native();
        Equal(DemandMode.Native, native.Mode, "Native mode");
        Equal<int?>(null, native.Target, "No unapproved initial edit target");
        foreach (int input in new int[] { int.MinValue, -5, 0, 37, 150, int.MaxValue })
            Equal(input, DemandPolicy.Evaluate(input, native), "Native is pass-through");
        Equal(23, DemandPolicy.Evaluate(23, null), "Missing policy fails closed");
    }

    private static void FixedBounds()
    {
        foreach (int target in new int[] { 0, 1, 50, 99, 100 })
        {
            CategoryPolicy policy = Fixed(target);
            Equal(DemandMode.FixedTarget, policy.Mode, "Fixed mode");
            Equal<int?>(target, policy.Target, "Fixed target retained");
            foreach (int input in new int[] { -5, 20, 150 })
                Equal(target, DemandPolicy.Evaluate(input, policy), "Fixed output");
        }
    }

    private static void InvalidPoliciesAndNativeDefaults()
    {
        CategoryPolicy rejected;
        foreach (int target in new int[] { int.MinValue, -1, 101, int.MaxValue })
        {
            Check(!DemandPolicy.TryCreate(1, target, out rejected), "Out-of-range rejected");
            Check(rejected == null, "No invalid policy returned");
        }
        Check(!DemandPolicy.TryCreate(1, null, out rejected), "Fixed requires explicit target");
        Check(!DemandPolicy.TryCreate(2, 50, out rejected), "Unknown mode rejected");
        Check(!DemandPolicy.TryCreate(-1, null, out rejected), "Negative mode rejected");
        Check(!DemandPolicy.TryCreate(0, 50, out rejected), "No hidden Native edit value");
        DemandSettings defaults = DemandSettings.Native();
        foreach (CategoryPolicy policy in new CategoryPolicy[] { defaults.Residential, defaults.Commercial, defaults.Workplace })
        {
            Equal(DemandMode.Native, policy.Mode, "Confirmed default is Native");
            Equal<int?>(null, policy.Target, "Default has no numeric edit value");
        }
        bool rejectedNull = false;
        try { new DemandSettings(null, Native(), Native()); }
        catch (ArgumentNullException) { rejectedNull = true; }
        Check(rejectedNull, "Partial settings rejected");
    }

    private static void ExplicitChannels()
    {
        DemandSession session = new DemandSession();
        long epoch = Ready(session);
        foreach (int shared in new int[] { 0, 50, 100 })
        {
            Check(session.TryApply(epoch, new DemandSettings(Fixed(shared), Fixed((shared + 1) % 101), Native())), "Apply channel policies");
            Equal(shared, session.Evaluate(epoch, DemandChannel.Residential, 50), "R selected explicitly");
            Equal((shared + 1) % 101, session.Evaluate(epoch, DemandChannel.Commercial, 50), "C selected explicitly");
            Equal(50, session.Evaluate(epoch, DemandChannel.Workplace, 50), "Workplace remains Native");
        }
        Check(session.TryApply(epoch, All(50)), "Equal targets accepted");
        Equal(50, session.Evaluate(epoch, DemandChannel.Workplace, 90), "Equal targets do not confuse channel");
        Equal(41, session.Evaluate(epoch, (DemandChannel)99, 41), "Unknown channel fails closed");
    }

    private static void DraftAndAtomicSettings()
    {
        DemandSession session = new DemandSession();
        long epoch = Ready(session);
        DemandSettings applied = new DemandSettings(Fixed(80), Native(), Fixed(0));
        Check(session.TryApply(epoch, applied), "Initial Apply");
        DemandSessionState before = session.ReadState();
        DemandSettings draft = All(100);
        Equal(80, session.Evaluate(epoch, DemandChannel.Residential, 20), "Draft does not change R");
        Equal(40, session.Evaluate(epoch, DemandChannel.Commercial, 40), "C passes current input");
        Equal(0, session.Evaluate(epoch, DemandChannel.Workplace, 60), "Workplace independent policy");
        Check(!session.TryApply(epoch, null), "Invalid Apply rejected");
        Equal(before.Revision, session.ReadState().Revision, "Rejected Apply preserves revision");
        Check(session.TryApply(epoch, draft), "Explicit Apply commits draft");
        DemandSessionState after = session.ReadState();
        Check(after.Revision > before.Revision, "Successful Apply advances revision");
        Equal<int?>(80, before.Settings.Residential.Target, "Previous snapshot immutable");
        Equal<int?>(100, after.Settings.Residential.Target, "R in new snapshot");
        Equal<int?>(100, after.Settings.Commercial.Target, "C in same snapshot");
        Equal<int?>(100, after.Settings.Workplace.Target, "W in same snapshot");
    }

    private static void LoadingAndPermissionGates()
    {
        DemandSession session = new DemandSession();
        Check(!session.TryApply(0, All(80)), "No city cannot Apply");
        Equal(31, session.Evaluate(0, DemandChannel.Residential, 31), "No city pass-through");
        long epoch = session.BeginCity();
        Equal(DemandMode.Native, session.ReadState().Settings.Residential.Mode, "City defaults Native");
        Check(session.SetControlPermission(epoch, ControlPermission.Allowed), "Permission before load");
        Check(!session.TryApply(epoch, All(80)), "Loading cannot Apply");
        Check(session.CompleteCityLoad(epoch), "Loaded");
        Equal(32, session.Evaluate(epoch, DemandChannel.Residential, 32), "Loaded but not applied");
        foreach (ControlPermission permission in new ControlPermission[] { ControlPermission.Unknown, ControlPermission.Conflict })
        {
            Check(session.SetControlPermission(epoch, permission), "Set gated permission");
            Check(!session.TryApply(epoch, All(80)), "Direct Apply obeys guard");
            Equal(33, session.Evaluate(epoch, DemandChannel.Residential, 33), "Gate pass-through");
        }
        Check(session.SetControlPermission(epoch, ControlPermission.Allowed), "Allow");
        Check(session.TryApply(epoch, All(80)), "Loaded and Allowed can Apply");
    }

    private static void RevocationAndRestore()
    {
        foreach (ControlPermission revoked in new ControlPermission[] { ControlPermission.Unknown, ControlPermission.Conflict, (ControlPermission)99 })
        {
            DemandSession session = new DemandSession();
            long epoch = Ready(session);
            Check(session.TryApply(epoch, All(80)), "Apply before revocation");
            Check(session.SetControlPermission(epoch, revoked), "Revoke (invalid values fail closed)");
            Equal(42, session.Evaluate(epoch, DemandChannel.Residential, 42), "Revocation stops control");
            Check(!session.ReadState().Armed, "Revoked session disarmed");
            Check(session.SetControlPermission(epoch, ControlPermission.Allowed), "Allowed again");
            Equal(43, session.Evaluate(epoch, DemandChannel.Residential, 43), "Allowed does not re-arm");
            Check(session.TryApply(epoch, All(80)), "Explicit re-Apply");
            Check(session.RestoreNative(epoch), "Restore");
            Equal(44, session.Evaluate(epoch, DemandChannel.Residential, 44), "Restore uses current input");
            Equal(DemandMode.Native, session.ReadState().Settings.Workplace.Mode, "Restore all Native");
            Check(!session.ReadState().Armed, "Restore disarms");
            Check(session.TryApply(epoch, All(80)), "Apply again");
            Check(session.CompleteCityLoad(epoch), "Duplicate load notification accepted");
            Equal(80, session.Evaluate(epoch, DemandChannel.Residential, 1), "Duplicate load does not reset settings");
        }
    }

    private static void CityEpochIsolation()
    {
        DemandSession session = new DemandSession();
        long a = Ready(session);
        Check(session.TryApply(a, All(80)), "A Apply");
        long b = Ready(session);
        Check(b > a, "Epoch increases");
        Check(!session.ReadState().Armed, "B starts without inherited active policy");
        Check(!session.TryApply(a, All(100)), "Reject stale Apply");
        Check(!session.CompleteCityLoad(a), "Reject stale load");
        Check(!session.SetControlPermission(a, ControlPermission.Conflict), "Reject stale permission");
        Check(!session.RestoreNative(a), "Reject stale Restore");
        Check(!session.EndCity(a), "Reject stale EndCity");
        Equal(b, session.ReadState().Epoch, "B remains current");
        Check(session.TryApply(b, All(60)), "B Apply");
        Equal(60, session.Evaluate(b, DemandChannel.Workplace, 1), "B unaffected");
        Equal(45, session.Evaluate(a, DemandChannel.Workplace, 45), "Stale Evaluate pass-through");
        Check(session.EndCity(b), "End B");
        Check(!session.EndCity(b), "Duplicate End rejected");
        Check(!session.TryApply(b, All(100)), "Closed epoch cannot Apply");
        Equal(46, session.Evaluate(b, DemandChannel.Residential, 46), "Closed pass-through");
    }

    private static void RunOrdered(Action first, Action second)
    {
        using (ManualResetEvent completed = new ManualResetEvent(false))
        {
            Exception failure = null;
            Thread worker = new Thread(delegate()
            { try { first(); } catch (Exception error) { failure = error; } finally { completed.Set(); } });
            worker.IsBackground = true;
            worker.Start();
            Check(completed.WaitOne(10000), "Ordered worker completes");
            Check(worker.Join(10000), "Ordered worker exits");
            if (failure != null) throw failure;
            second();
        }
    }

    private static void OrderedTransitionsAndCoherentSnapshots()
    {
        foreach (bool applyFirst in new bool[] { true, false })
        {
            DemandSession session = new DemandSession();
            long epoch = Ready(session);
            Action apply = delegate { bool accepted = session.TryApply(epoch, All(80)); Equal(applyFirst, accepted, "Apply ordering"); };
            Action close = delegate { Check(session.EndCity(epoch), "EndCity wins permanently"); };
            RunOrdered(applyFirst ? apply : close, applyFirst ? close : apply);
            Equal(17, session.Evaluate(epoch, DemandChannel.Residential, 17), "No re-arm after EndCity");

            session = new DemandSession();
            epoch = Ready(session);
            apply = delegate { bool accepted = session.TryApply(epoch, All(80)); Equal(applyFirst, accepted, "Permission ordering"); };
            Action revoke = delegate { Check(session.SetControlPermission(epoch, ControlPermission.Conflict), "Revoke"); };
            RunOrdered(applyFirst ? apply : revoke, applyFirst ? revoke : apply);
            Equal(18, session.Evaluate(epoch, DemandChannel.Residential, 18), "No re-arm after conflict");
        }

        // These races complement the ordered cases; passing cannot prove every possible schedule.
        foreach (bool closeCity in new bool[] { true, false })
        {
            for (int round = 0; round < 100; round++)
            {
                DemandSession racing = new DemandSession();
                long racingEpoch = Ready(racing);
                DemandSettings candidate = All(80);
                using (ManualResetEvent startRace = new ManualResetEvent(false))
                using (ManualResetEvent applyReady = new ManualResetEvent(false))
                using (ManualResetEvent revokeReady = new ManualResetEvent(false))
                {
                    Exception applyError = null;
                    Exception revokeError = null;
                    bool revoked = false;
                    Thread applier = new Thread(delegate()
                    {
                        try { applyReady.Set(); startRace.WaitOne(); racing.TryApply(racingEpoch, candidate); }
                        catch (Exception error) { applyError = error; }
                    });
                    Thread revoker = new Thread(delegate()
                    {
                        try
                        {
                            revokeReady.Set(); startRace.WaitOne();
                            revoked = closeCity ? racing.EndCity(racingEpoch) :
                                racing.SetControlPermission(racingEpoch, ControlPermission.Conflict);
                        }
                        catch (Exception error) { revokeError = error; }
                    });
                    applier.IsBackground = true;
                    revoker.IsBackground = true;
                    applier.Start(); revoker.Start();
                    bool bothReady = applyReady.WaitOne(10000) && revokeReady.WaitOne(10000);
                    startRace.Set();
                    bool applyExited = applier.Join(10000);
                    bool revokeExited = revoker.Join(10000);
                    Check(bothReady && applyExited && revokeExited, "Both racing operations finish");
                    if (applyError != null) throw applyError;
                    if (revokeError != null) throw revokeError;
                    Check(revoked, "Revocation accepted during race");
                    Check(!racing.ReadState().Armed, "Race cannot re-arm after revocation");
                    Check(!racing.TryApply(racingEpoch, candidate), "Post-revocation Apply rejected");
                    Equal(19, racing.Evaluate(racingEpoch, DemandChannel.Residential, 19), "Racing revocation keeps pass-through");
                }
            }
        }

        DemandSession concurrent = new DemandSession();
        long city = Ready(concurrent);
        Check(concurrent.TryApply(city, All(0)), "Seed consistent snapshot");
        using (ManualResetEvent start = new ManualResetEvent(false))
        {
            Exception failure = null;
            Thread writer = new Thread(delegate()
            {
                try
                {
                    start.WaitOne();
                    for (int i = 0; i < 2000; i++)
                        if (!concurrent.TryApply(city, All(i % 101))) throw new Exception("Concurrent Apply rejected");
                }
                catch (Exception error) { failure = error; }
            });
            writer.IsBackground = true;
            writer.Start();
            start.Set();
            long revision = 0;
            for (int i = 0; i < 2000; i++)
            {
                DemandSessionState state = concurrent.ReadState();
                Check(state.Revision >= revision, "Snapshot revision monotonic");
                revision = state.Revision;
                Equal(state.Settings.Residential.Target, state.Settings.Commercial.Target, "Coherent R/C");
                Equal(state.Settings.Residential.Target, state.Settings.Workplace.Target, "Coherent R/W");
            }
            Check(writer.Join(10000), "Snapshot writer exits");
            if (failure != null) throw failure;
        }
    }

    private static void PipelineAndCurrentInputBoundary()
    {
        DemandSession session = new DemandSession();
        long epoch = Ready(session);
        Check(session.TryApply(epoch, All(80)), "Apply pipeline policy");
        int returned = session.Evaluate(epoch, DemandChannel.Residential, 20);
        Equal(80, returned, "Policy controls only its return");
        Equal(40, returned / 2, "Synthetic downstream can change final result");
        Check(session.RestoreNative(epoch), "Stop override");
        Equal(63, session.Evaluate(epoch, DemandChannel.Residential, 63), "No rollback to captured input 20");
        Check(session.EndCity(epoch), "End pipeline city");
        Equal(71, session.Evaluate(epoch, DemandChannel.Residential, 71), "Closed session keeps new input");
    }

    public static int Main()
    {
        Action[] groups = new Action[] { NativePassThrough, FixedBounds, InvalidPoliciesAndNativeDefaults,
            ExplicitChannels, DraftAndAtomicSettings, LoadingAndPermissionGates, RevocationAndRestore,
            CityEpochIsolation, OrderedTransitionsAndCoherentSnapshots, PipelineAndCurrentInputBoundary,
            SnapshotChecks.ReadOnlyBoundary, LifecycleChecks.InactiveLifecycle, LifecycleChecks.CallbackErrorBoundary };
        int failed = 0;
        foreach (Action group in groups)
        {
            try { group(); Console.WriteLine("PASS " + group.Method.Name); }
            catch (Exception error) { failed++; Console.WriteLine("FAIL " + group.Method.Name + ": " + error); }
        }
        Console.WriteLine("Pure model only: " + (groups.Length - failed) + "/" + groups.Length +
            " groups passed; " + (assertions + SnapshotChecks.AssertionCount + LifecycleChecks.AssertionCount) +
            " assertions; no game adapter, game DLL build, save or release.");
        return failed == 0 ? 0 : 1;
    }
}
