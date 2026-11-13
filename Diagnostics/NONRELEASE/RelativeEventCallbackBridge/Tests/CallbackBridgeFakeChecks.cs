// Signature: OpenAI-Codex_clock_surface
// The initial check uses reflection and does not require bridge sources to compile.
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using RealisticCityTime.Diagnostics.NonReleaseCallbackBridge;
using RealisticCityTime.Diagnostics.RelativeEventTrace;

namespace RealisticCityTime.Diagnostics.NonReleaseCallbackBridge.Tests
{
    internal static class CallbackBridgeFakeChecks
    {
        private const string TargetTypeName =
            "RealisticCityTime.Diagnostics.NonReleaseCallbackBridge.ThreadingCallbacks";

        public static int Main()
        {
            try
            {
                Type target = typeof(CallbackBridgeFakeChecks).Assembly.GetType(TargetTypeName, false);
                if (target == null)
                {
                    Console.Error.WriteLine("FAIL: ThreadingCallbacks is missing from the test assembly.");
                    return 1;
                }

                if (!typeof(ICities.IThreadingExtension).IsAssignableFrom(target))
                {
                    Console.Error.WriteLine("FAIL: ThreadingCallbacks does not implement the fake IThreadingExtension.");
                    return 1;
                }

                ArgumentAudit audit = AuditCallbackArguments(target);
                if (audit == ArgumentAudit.Accessed)
                {
                    Console.Error.WriteLine("FAIL: A callback loads or addresses a prohibited parameter.");
                    return 1;
                }

                try
                {
                    RunTypedChecks();
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("FAIL: Typed fake callback checks did not complete.");
                    return 1;
                }

                Console.WriteLine("PASS: ThreadingCallbacks implements the fake IThreadingExtension.");
                if (audit == ArgumentAudit.Unverified)
                {
                    Console.Error.WriteLine("UNVERIFIED: Could not decode both fake callback method bodies.");
                    return 2;
                }
                Console.WriteLine("PASS: Fake callback mapping, ticket checks, and direct argument audit.");
                return 0;
            }
            catch (Exception)
            {
                Console.Error.WriteLine("FAIL: Could not inspect the test assembly.");
                return 1;
            }
        }

        private sealed class FakeThreading : ICities.IThreading
        {
        }

        private static void Check(bool condition, string description)
        {
            if (condition) return;
            Console.Error.WriteLine("FAIL: " + description);
            throw new InvalidOperationException();
        }

        private static int ReadInt(ref int value)
        {
            return Interlocked.CompareExchange(ref value, 0, 0);
        }

        private static bool HasIssue(CallbackSnapshot snapshot, CallbackIssue issue)
        {
            return (snapshot.Issues & issue) != 0;
        }

        private static CallbackSnapshot Finish(CallbackSessionGate gate)
        {
            CallbackState state = gate.RequestStop();
            for (int i = 0; i < 8 && state != CallbackState.Stopped; i++)
                state = gate.TryCompleteStop();
            Check(state == CallbackState.Stopped, "The callback session did not finish stopping.");
            CallbackSnapshot snapshot = gate.ReadSnapshot();
            Check(snapshot.State == CallbackState.Stopped, "The final snapshot is not stopped.");
            return snapshot;
        }

        private static void RunTypedChecks()
        {
            CheckMappingAndDisabledState();
            CheckStartedEmptyComplete();
            CheckStopBeforeStart();
            CheckReleaseIsOneShot();
            CheckStartReleaseRace();
            CheckPendingUntilTicketEnds();
            CheckReleasePendingUntilTicketEnds();
            CheckSameThreadReentrancy();
            CheckEndPinDelaysStop();
            CheckConcurrentTickets();
            CheckWrongThreadEnd();
            CheckForeignTicketWhileBothHeld();
            CheckForeignTicketWithoutReceiverTicket();
            CheckNullTicketWhileHeld();
            CheckNullTicketWithoutReceiverTicket();
            CheckInvalidTicketsWhileClosing();
            CheckEndedForeignTicket();
            CheckInactiveGateRejectsWithoutRetroactiveIssue();
            CheckTicketWithoutOwner();
            CheckRecorderCapacityIsIncomplete();
            CheckThreadCapacityIsIncomplete();
            CheckLateOldCallbackIsIsolated();
        }

        private static void CheckMappingAndDisabledState()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(32, 4);
            ICities.IThreadingExtension callbacks = bridge;
            CallbackSessionGate gate = bridge.Session;
            Check(gate.ReadSnapshot().State == CallbackState.Disabled, "A new bridge must be disabled.");
            callbacks.OnCreated(new FakeThreading());
            callbacks.OnBeforeSimulationTick();
            CallbackSnapshot disabled = gate.ReadSnapshot();
            Check(disabled.State == CallbackState.Disabled && disabled.Events.Length == 0 && !disabled.IsComplete,
                "OnCreated or a disabled callback recorded an event.");
            Check(gate.TryStart(), "The fake callback session did not start.");
            callbacks.OnCreated(new FakeThreading());

            callbacks.OnBeforeSimulationTick();
            callbacks.OnBeforeSimulationFrame();
            callbacks.OnAfterSimulationFrame();
            callbacks.OnAfterSimulationTick();
            callbacks.OnUpdate(float.NaN, float.PositiveInfinity);

            CallbackSnapshot snapshot = Finish(gate);
            Check(snapshot.IsComplete, "The sequential fake callback snapshot is incomplete.");
            EventRecord[] events = snapshot.Events;
            EventKind[] expected = new EventKind[]
            {
                EventKind.BeforeSimulationTick,
                EventKind.BeforeSimulationFrame,
                EventKind.AfterSimulationFrame,
                EventKind.AfterSimulationTick,
                EventKind.Update
            };
            Check(events.Length == expected.Length * 2, "The fake callback event count is incorrect.");
            int tag = events[0].ThreadTag;
            Check(tag > 0, "The fake callback thread tag is missing.");
            for (int i = 0; i < expected.Length; i++)
            {
                EventRecord enter = events[i * 2];
                EventRecord exit = events[i * 2 + 1];
                Check(enter.Kind == expected[i] && enter.Point == EventPoint.Enter,
                    "A fake callback Enter mapping is incorrect.");
                Check(exit.Kind == expected[i] && exit.Point == EventPoint.Exit,
                    "A fake callback Exit mapping is incorrect.");
                Check(enter.Order == i * 2 + 1 && exit.Order == i * 2 + 2,
                    "The fake callback recorder order is incorrect.");
                Check(enter.ThreadTag == tag && exit.ThreadTag == tag,
                    "Sequential fake callbacks did not retain one local tag.");
            }

            events[0] = default(EventRecord);
            Check(snapshot.Events[0].Kind == expected[0], "A snapshot exposed its event buffer.");
            Check(!gate.TryStart(), "A stopped callback session restarted.");
            callbacks.OnAfterSimulationTick();
            Check(gate.ReadSnapshot().Events.Length == expected.Length * 2,
                "A callback changed a stopped snapshot.");
        }

        private static void CheckStartedEmptyComplete()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            Check(bridge.Session.TryStart(), "The empty fake session did not start.");
            CallbackSnapshot snapshot = Finish(bridge.Session);
            Check(snapshot.IsComplete && snapshot.Events.Length == 0,
                "A started session without callbacks should have an empty complete snapshot.");
        }

        private static void CheckStopBeforeStart()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSnapshot snapshot = Finish(bridge.Session);
            Check(HasIssue(snapshot, CallbackIssue.NotStarted) && !snapshot.IsComplete
                && snapshot.Events.Length == 0 && !bridge.Session.TryStart(),
                "Stop before start did not remain empty, incomplete, and terminal.");
        }

        private static void CheckReleaseIsOneShot()
        {
            ThreadingCallbacks beforeStart = new ThreadingCallbacks(16, 4);
            ICities.IThreadingExtension callbacks = beforeStart;
            callbacks.OnCreated(null);
            callbacks.OnReleased();
            Check(!beforeStart.Session.TryStart(), "Release before start allowed a later start.");
            CallbackSnapshot before = Finish(beforeStart.Session);
            Check(!before.IsComplete && before.Events.Length == 0,
                "Release before start produced a complete or nonempty snapshot.");

            ThreadingCallbacks afterStart = new ThreadingCallbacks(16, 4);
            Check(afterStart.Session.TryStart(), "The release test session did not start.");
            ((ICities.IThreadingExtension)afterStart).OnReleased();
            Finish(afterStart.Session);
            Check(!afterStart.Session.TryStart(), "Release allowed a session to restart.");
        }

        private static void CheckStartReleaseRace()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            int failed = 0;
            using (ManualResetEvent proceed = new ManualResetEvent(false))
            {
                Thread start = new Thread(delegate()
                {
                    try
                    {
                        if (!proceed.WaitOne(10000)) { Interlocked.Exchange(ref failed, 1); return; }
                        bridge.Session.TryStart();
                    }
                    catch (Exception) { Interlocked.Exchange(ref failed, 1); }
                });
                Thread release = new Thread(delegate()
                {
                    try
                    {
                        if (!proceed.WaitOne(10000)) { Interlocked.Exchange(ref failed, 1); return; }
                        ((ICities.IThreadingExtension)bridge).OnReleased();
                    }
                    catch (Exception) { Interlocked.Exchange(ref failed, 1); }
                });
                start.IsBackground = true;
                release.IsBackground = true;
                start.Start();
                release.Start();
                proceed.Set();
                Check(start.Join(10000) && release.Join(10000) && ReadInt(ref failed) == 0,
                    "The fake Start and Release race did not finish cleanly.");
            }
            CallbackSnapshot snapshot = Finish(bridge.Session);
            Check(snapshot.Events.Length == 0 && !bridge.Session.TryStart(),
                "The fake Start and Release race reopened or recorded a session.");
        }

        private sealed class HeldWorker : IDisposable
        {
            private readonly CallbackSessionGate gate;
            private readonly EventKind kind;
            private readonly Thread worker;
            private int failed;
            internal readonly ManualResetEvent Entered = new ManualResetEvent(false);
            internal readonly ManualResetEvent AllowEnd = new ManualResetEvent(false);

            internal HeldWorker(CallbackSessionGate gate, EventKind kind)
            {
                this.gate = gate;
                this.kind = kind;
                worker = new Thread(new ThreadStart(Run));
                worker.IsBackground = true;
            }

            internal void Start() { worker.Start(); }
            internal bool Join() { return worker.Join(10000); }
            internal bool Failed { get { return ReadInt(ref failed) != 0; } }
            private void Fail() { Interlocked.Exchange(ref failed, 1); }

            private void Run()
            {
                CallbackTicket ticket = default(CallbackTicket);
                bool held = false;
                bool endAttempted = false;
                try
                {
                    if (!gate.TryBegin(kind, out ticket)) { Fail(); return; }
                    held = true;
                    Entered.Set();
                    if (!AllowEnd.WaitOne(10000)) { Fail(); return; }
                    endAttempted = true;
                    if (!ticket.End()) Fail();
                }
                catch (Exception)
                {
                    Fail();
                }
                finally
                {
                    if (held && !endAttempted)
                    {
                        try { if (!ticket.End()) Fail(); }
                        catch (Exception) { Fail(); }
                    }
                    try { Entered.Set(); }
                    catch (Exception) { Fail(); }
                }
            }

            public void Dispose()
            {
                if (!worker.IsAlive)
                {
                    Entered.Dispose();
                    AllowEnd.Dispose();
                }
            }
        }

        private static void CheckPendingUntilTicketEnds()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The held-ticket session did not start.");
            using (HeldWorker worker = new HeldWorker(gate, EventKind.Update))
            {
                worker.Start();
                try
                {
                    Check(worker.Entered.WaitOne(10000) && !worker.Failed,
                        "The fake worker did not acquire a callback ticket.");
                    Check(gate.RequestStop() == CallbackState.Closing,
                        "Stop completed while a callback ticket was held.");
                    CallbackSnapshot pending = gate.ReadSnapshot();
                    Check(pending.State == CallbackState.Closing && HasIssue(pending, CallbackIssue.Pending)
                        && !pending.IsComplete && pending.Events.Length == 0,
                        "A held callback did not leave an empty pending snapshot.");
                    CallbackTicket denied = default(CallbackTicket);
                    bool admitted = gate.TryBegin(EventKind.Update, out denied);
                    if (admitted) denied.End();
                    Check(!admitted, "Stop admitted a new callback ticket.");
                }
                finally { worker.AllowEnd.Set(); }

                Check(worker.Join() && !worker.Failed, "The held callback did not end cleanly.");
            }
            CallbackSnapshot snapshot = Finish(gate);
            Check(snapshot.IsComplete, "The drained sequential callback should be complete.");
            EventRecord[] events = snapshot.Events;
            Check(events.Length == 2 && events[0].Kind == EventKind.Update
                && events[0].Point == EventPoint.Enter && events[1].Kind == EventKind.Update
                && events[1].Point == EventPoint.Exit && events[0].ThreadTag == events[1].ThreadTag,
                "Stopping did not preserve the held callback Enter and Exit.");
            Check(!gate.TryStart(), "A drained stopped session restarted.");
        }

        private static void CheckReleasePendingUntilTicketEnds()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The held-release session did not start.");
            using (HeldWorker worker = new HeldWorker(gate, EventKind.Update))
            {
                worker.Start();
                try
                {
                    Check(worker.Entered.WaitOne(10000) && !worker.Failed,
                        "The held-release worker did not acquire a callback ticket.");
                    ((ICities.IThreadingExtension)bridge).OnReleased();
                    CallbackSnapshot pending = gate.ReadSnapshot();
                    Check(pending.State == CallbackState.Closing && HasIssue(pending, CallbackIssue.Pending)
                        && !pending.IsComplete && pending.Events.Length == 0,
                        "Release did not remain pending while a callback ticket was held.");
                    Check(!gate.TryStart(), "Release allowed another start while draining.");
                }
                finally { worker.AllowEnd.Set(); }
                Check(worker.Join() && !worker.Failed, "The held-release callback did not end cleanly.");
            }
            Finish(gate);
            Check(!gate.TryStart(), "A released callback session restarted.");
        }

        private static void CheckSameThreadReentrancy()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The reentrancy session did not start.");
            CallbackTicket outer = default(CallbackTicket);
            Check(gate.TryBegin(EventKind.Update, out outer), "The outer callback was refused.");
            CallbackTicket nested = default(CallbackTicket);
            bool admitted = gate.TryBegin(EventKind.BeforeSimulationTick, out nested);
            if (admitted) nested.End();
            Check(!admitted, "A same-thread nested callback was admitted.");
            Check(outer.End(), "The original callback ticket did not end.");
            Check(!outer.End(), "A callback ticket ended twice.");
            CallbackSnapshot snapshot = Finish(gate);
            Check(HasIssue(snapshot, CallbackIssue.Reentrancy) && !snapshot.IsComplete,
                "Same-thread reentrancy was not retained as incomplete.");
            EventRecord[] events = snapshot.Events;
            Check(events.Length == 2 && events[0].Point == EventPoint.Enter
                && events[1].Point == EventPoint.Exit,
                "Reentrancy or a repeated End changed the event pair.");
        }

        private static void CheckEndPinDelaysStop()
        {
            Assembly fakeAssembly = typeof(CallbackBridgeFakeChecks).Assembly;
            Type gateType = typeof(CallbackSessionGate);
            Check(gateType.Assembly == fakeAssembly, "The pin check is not using the fake assembly.");
            MethodInfo tryPin = gateType.GetMethod("TryPinEnd", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo releasePin = gateType.GetMethod("ReleaseEndPin", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(tryPin != null && tryPin.ReturnType == typeof(bool) && tryPin.GetParameters().Length == 0
                && releasePin != null && releasePin.ReturnType == typeof(void) && releasePin.GetParameters().Length == 0,
                "The fake gate does not expose the expected private pin methods.");

            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The end-pin session did not start.");
            CallbackTicket ticket = default(CallbackTicket);
            Check(gate.TryBegin(EventKind.Update, out ticket), "The end-pin callback was refused.");
            bool pinHeld = false;
            try
            {
                pinHeld = (bool)tryPin.Invoke(gate, null);
                Check(pinHeld, "The fake end pin was refused.");
                Check(ticket.End(), "The final callback ticket did not finish.");
                Check(!ticket.End(), "The final callback ticket ended more than once.");
                Check(gate.RequestStop() == CallbackState.Closing
                    && gate.TryCompleteStop() == CallbackState.Closing,
                    "Stop completed while the fake end pin was held.");
                CallbackSnapshot pending = gate.ReadSnapshot();
                Check(pending.State == CallbackState.Closing && HasIssue(pending, CallbackIssue.Pending)
                    && !pending.IsComplete && pending.Events.Length == 0,
                    "The held end pin exposed a non-pending snapshot.");
            }
            finally
            {
                if (pinHeld) releasePin.Invoke(gate, null);
            }

            CallbackSnapshot snapshot = Finish(gate);
            Check(snapshot.IsComplete && snapshot.Events.Length == 2,
                "The callback session did not finish after releasing the fake end pin.");
        }

        private static void CheckConcurrentTickets()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(32, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The concurrent callback session did not start.");
            using (HeldWorker first = new HeldWorker(gate, EventKind.BeforeSimulationTick))
            using (HeldWorker second = new HeldWorker(gate, EventKind.AfterSimulationTick))
            {
                first.Start();
                try
                {
                    Check(first.Entered.WaitOne(10000) && !first.Failed, "The first fake callback was refused.");
                    second.Start();
                    Check(second.Entered.WaitOne(10000) && !second.Failed, "The second fake callback was refused.");
                    Check(gate.RequestStop() == CallbackState.Closing,
                        "Stop completed while two callback tickets were held.");
                    CallbackSnapshot pending = gate.ReadSnapshot();
                    Check(HasIssue(pending, CallbackIssue.Pending) && pending.Events.Length == 0,
                        "Concurrent held callbacks exposed a non-pending snapshot.");
                    first.AllowEnd.Set();
                    Check(first.Join() && !first.Failed, "The first fake callback did not finish.");
                }
                finally
                {
                    first.AllowEnd.Set();
                    second.AllowEnd.Set();
                }
                Check(second.Join() && !first.Failed && !second.Failed,
                    "The concurrent fake callback tickets did not end cleanly.");
            }
            CallbackSnapshot snapshot = Finish(gate);
            Check(HasIssue(snapshot, CallbackIssue.ConcurrentCallbacks) && !snapshot.IsComplete,
                "Overlapping callback tickets were not reported as incomplete.");
            EventRecord[] events = snapshot.Events;
            Check(events.Length == 4, "Concurrent fake callbacks did not produce two event pairs.");
            int firstTag = CheckPair(events, EventKind.BeforeSimulationTick);
            int secondTag = CheckPair(events, EventKind.AfterSimulationTick);
            Check(firstTag != secondTag, "Different fake workers shared a session-local tag.");
        }

        private static int CheckPair(EventRecord[] events, EventKind kind)
        {
            int enterCount = 0;
            int exitCount = 0;
            int enterOrder = 0;
            int exitOrder = 0;
            int enterTag = 0;
            int exitTag = 0;
            foreach (EventRecord item in events)
            {
                if (item.Kind != kind) continue;
                if (item.Point == EventPoint.Enter)
                {
                    enterCount++;
                    enterOrder = item.Order;
                    enterTag = item.ThreadTag;
                }
                else if (item.Point == EventPoint.Exit)
                {
                    exitCount++;
                    exitOrder = item.Order;
                    exitTag = item.ThreadTag;
                }
            }
            Check(enterCount == 1 && exitCount == 1 && enterOrder < exitOrder
                && enterTag > 0 && enterTag == exitTag,
                "A fake callback pair is missing, reordered, or has inconsistent tags.");
            return enterTag;
        }

        private static void CheckWrongThreadEnd()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The wrong-thread session did not start.");
            CallbackTicket ticket = default(CallbackTicket);
            Check(gate.TryBegin(EventKind.Update, out ticket), "The owner callback was refused.");
            int unexpected = 0;
            Thread worker = new Thread(delegate()
            {
                try { if (ticket.End()) Interlocked.Exchange(ref unexpected, 1); }
                catch (Exception) { Interlocked.Exchange(ref unexpected, 1); }
            });
            worker.IsBackground = true;
            worker.Start();
            Check(worker.Join(10000) && ReadInt(ref unexpected) == 0,
                "A wrong-thread End succeeded or threw.");
            Check(ticket.End(), "The owning fake thread could not finish its ticket.");
            CallbackSnapshot snapshot = Finish(gate);
            Check(HasIssue(snapshot, CallbackIssue.WrongThread) && !snapshot.IsComplete,
                "A wrong-thread End was not retained as incomplete.");
            Check(snapshot.Events.Length == 2, "Wrong-thread End altered the owner's event pair.");
        }

        private static void CheckForeignTicketWhileBothHeld()
        {
            ThreadingCallbacks firstBridge = new ThreadingCallbacks(16, 4);
            ThreadingCallbacks secondBridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate first = firstBridge.Session;
            CallbackSessionGate second = secondBridge.Session;
            Check(first.TryStart() && second.TryStart(), "The foreign-ticket sessions did not start.");
            CallbackTicket firstTicket = default(CallbackTicket);
            Check(first.TryBegin(EventKind.BeforeSimulationTick, out firstTicket),
                "The first fake gate did not admit its ticket.");
            bool firstEndAttempted = false;
            using (HeldWorker secondWorker = new HeldWorker(second, EventKind.AfterSimulationTick))
            {
                bool started = false;
                bool joined = false;
                try
                {
                    secondWorker.Start();
                    started = true;
                    Check(secondWorker.Entered.WaitOne(10000) && !secondWorker.Failed,
                        "The second fake gate did not admit its ticket.");
                    bool threw = false;
                    bool accepted = false;
                    try { accepted = second.End(firstTicket); }
                    catch (Exception) { threw = true; }
                    Check(!threw && !accepted, "A foreign ticket was not safely rejected.");
                    Check(first.RequestStop() == CallbackState.Closing
                        && second.RequestStop() == CallbackState.Closing,
                        "A foreign End prematurely drained a held fake gate.");
                    CheckPendingEmpty(first, "The first held gate was not pending after foreign End.");
                    CheckPendingEmpty(second, "The second held gate was not pending after foreign End.");

                    firstEndAttempted = true;
                    Check(firstTicket.End(), "The foreign End consumed the first gate's ticket.");
                    CallbackSnapshot firstSnapshot = Finish(first);
                    Check(firstSnapshot.IsComplete && firstSnapshot.Events.Length == 2,
                        "The foreign End changed the first gate's completeness or event pair.");
                    CheckPair(firstSnapshot.Events, EventKind.BeforeSimulationTick);
                    Check(second.TryCompleteStop() == CallbackState.Closing,
                        "Ending the first ticket prematurely drained the second gate.");
                    CheckPendingEmpty(second, "The second gate exposed events while its ticket remained held.");
                }
                finally
                {
                    if (!firstEndAttempted)
                    {
                        try { firstTicket.End(); }
                        catch (Exception) { }
                    }
                    secondWorker.AllowEnd.Set();
                    if (started) joined = secondWorker.Join();
                }
                Check(joined && !secondWorker.Failed, "The second gate's own ticket did not end cleanly.");
            }
            CallbackSnapshot secondSnapshot = Finish(second);
            Check(!secondSnapshot.IsComplete && HasIssue(secondSnapshot, CallbackIssue.Failure)
                && secondSnapshot.Events.Length == 2,
                "The foreign End did not leave the second gate incomplete with its own event pair.");
            CheckPair(secondSnapshot.Events, EventKind.AfterSimulationTick);
        }

        private static void CheckForeignTicketWithoutReceiverTicket()
        {
            ThreadingCallbacks firstBridge = new ThreadingCallbacks(16, 4);
            ThreadingCallbacks secondBridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate first = firstBridge.Session;
            CallbackSessionGate second = secondBridge.Session;
            Check(first.TryStart() && second.TryStart(), "The empty-receiver sessions did not start.");
            CallbackTicket firstTicket = default(CallbackTicket);
            Check(first.TryBegin(EventKind.Update, out firstTicket), "The owner ticket was refused.");
            bool endAttempted = false;
            try
            {
                bool threw = false;
                bool accepted = false;
                try { accepted = second.End(firstTicket); }
                catch (Exception) { threw = true; }
                Check(!threw && !accepted, "An empty receiver did not safely reject the foreign ticket.");
                CallbackSnapshot secondSnapshot = Finish(second);
                Check(!secondSnapshot.IsComplete && HasIssue(secondSnapshot, CallbackIssue.Failure)
                    && secondSnapshot.Events.Length == 0,
                    "A foreign ticket changed the empty receiver's events or completeness.");
                Check(first.RequestStop() == CallbackState.Closing,
                    "An empty receiver consumed the owner's held ticket.");
                CheckPendingEmpty(first, "The owner gate was not pending after foreign rejection.");
                endAttempted = true;
                Check(firstTicket.End(), "The owner could not end its original ticket.");
                CallbackSnapshot firstSnapshot = Finish(first);
                Check(firstSnapshot.IsComplete && firstSnapshot.Events.Length == 2,
                    "A foreign ticket changed the owner's completeness or event pair.");
                CheckPair(firstSnapshot.Events, EventKind.Update);
            }
            finally
            {
                if (!endAttempted)
                {
                    try { firstTicket.End(); }
                    catch (Exception) { }
                }
            }
        }

        private static void CheckNullTicketWhileHeld()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The held null-ticket session did not start.");
            CallbackTicket ticket = default(CallbackTicket);
            Check(gate.TryBegin(EventKind.Update, out ticket), "The held null-ticket session refused its ticket.");
            bool endAttempted = false;
            try
            {
                bool threw = false;
                bool accepted = false;
                try { accepted = gate.End(null); }
                catch (Exception) { threw = true; }
                Check(!threw && !accepted, "A null ticket was not safely rejected while a ticket was held.");
                Check(gate.RequestStop() == CallbackState.Closing, "A null End drained the held ticket.");
                CheckPendingEmpty(gate, "A held ticket did not remain pending after null End.");
                endAttempted = true;
                Check(ticket.End(), "The owner could not end its ticket after null rejection.");
                CallbackSnapshot snapshot = Finish(gate);
                Check(!snapshot.IsComplete && HasIssue(snapshot, CallbackIssue.Failure)
                    && snapshot.Events.Length == 2,
                    "A null End did not leave an incomplete session with its own event pair.");
                CheckPair(snapshot.Events, EventKind.Update);
            }
            finally
            {
                if (!endAttempted)
                {
                    try { ticket.End(); }
                    catch (Exception) { }
                }
            }
        }

        private static void CheckNullTicketWithoutReceiverTicket()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate gate = bridge.Session;
            Check(gate.TryStart(), "The empty null-ticket session did not start.");
            bool threw = false;
            bool accepted = false;
            try { accepted = gate.End(null); }
            catch (Exception) { threw = true; }
            Check(!threw && !accepted, "An empty gate did not safely reject a null ticket.");
            CallbackSnapshot snapshot = Finish(gate);
            Check(!snapshot.IsComplete && HasIssue(snapshot, CallbackIssue.Failure)
                && snapshot.Events.Length == 0,
                "A null ticket changed the empty gate's events or completeness.");
        }

        private static void CheckInvalidTicketsWhileClosing()
        {
            ThreadingCallbacks firstBridge = new ThreadingCallbacks(16, 4);
            ThreadingCallbacks secondBridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate first = firstBridge.Session;
            CallbackSessionGate second = secondBridge.Session;
            Check(first.TryStart() && second.TryStart(), "The closing-receiver sessions did not start.");
            CallbackTicket firstTicket = default(CallbackTicket);
            Check(first.TryBegin(EventKind.BeforeSimulationTick, out firstTicket),
                "The closing-receiver owner ticket was refused.");
            bool firstEndAttempted = false;
            using (HeldWorker secondWorker = new HeldWorker(second, EventKind.AfterSimulationTick))
            {
                bool started = false;
                bool joined = false;
                try
                {
                    secondWorker.Start();
                    started = true;
                    Check(secondWorker.Entered.WaitOne(10000) && !secondWorker.Failed,
                        "The closing receiver did not hold its own ticket.");
                    Check(second.RequestStop() == CallbackState.Closing,
                        "The held receiver did not enter Closing.");
                    bool threw = false;
                    bool nullAccepted = false;
                    bool foreignAccepted = false;
                    try
                    {
                        nullAccepted = second.End(null);
                        foreignAccepted = second.End(firstTicket);
                    }
                    catch (Exception) { threw = true; }
                    Check(!threw && !nullAccepted && !foreignAccepted,
                        "The closing receiver did not safely reject invalid tickets.");
                    CheckPendingEmpty(second, "The closing receiver prematurely drained its own ticket.");
                    Check(first.RequestStop() == CallbackState.Closing,
                        "The closing receiver consumed the first gate's ticket.");
                    firstEndAttempted = true;
                    Check(firstTicket.End(), "The first gate could not end its original ticket.");
                    CallbackSnapshot firstSnapshot = Finish(first);
                    Check(firstSnapshot.IsComplete && firstSnapshot.Events.Length == 2,
                        "The closing receiver changed the first gate's completeness.");
                    CheckPair(firstSnapshot.Events, EventKind.BeforeSimulationTick);
                    Check(second.TryCompleteStop() == CallbackState.Closing,
                        "The closing receiver stopped before its own ticket ended.");
                }
                finally
                {
                    if (!firstEndAttempted)
                    {
                        try { firstTicket.End(); }
                        catch (Exception) { }
                    }
                    secondWorker.AllowEnd.Set();
                    if (started) joined = secondWorker.Join();
                }
                Check(joined && !secondWorker.Failed, "The closing receiver's ticket did not end cleanly.");
            }
            CallbackSnapshot secondSnapshot = Finish(second);
            Check(!secondSnapshot.IsComplete && HasIssue(secondSnapshot, CallbackIssue.Failure)
                && secondSnapshot.Events.Length == 2,
                "The closing receiver did not retain Failure and its own event pair.");
            CheckPair(secondSnapshot.Events, EventKind.AfterSimulationTick);
        }

        private static void CheckEndedForeignTicket()
        {
            ThreadingCallbacks firstBridge = new ThreadingCallbacks(16, 4);
            ThreadingCallbacks secondBridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate first = firstBridge.Session;
            CallbackSessionGate second = secondBridge.Session;
            Check(first.TryStart() && second.TryStart(), "The ended-foreign sessions did not start.");
            CallbackTicket ticket = default(CallbackTicket);
            Check(first.TryBegin(EventKind.Update, out ticket), "The ended-foreign owner ticket was refused.");
            Check(ticket.End(), "The ended-foreign owner ticket did not finish.");
            bool threw = false;
            bool accepted = false;
            try { accepted = second.End(ticket); }
            catch (Exception) { threw = true; }
            Check(!threw && !accepted, "An ended foreign ticket was not safely rejected.");
            CallbackSnapshot secondSnapshot = Finish(second);
            Check(!secondSnapshot.IsComplete && HasIssue(secondSnapshot, CallbackIssue.Failure)
                && secondSnapshot.Events.Length == 0,
                "An ended foreign ticket did not mark the active receiver incomplete.");
            CallbackSnapshot firstSnapshot = Finish(first);
            Check(firstSnapshot.IsComplete && firstSnapshot.Events.Length == 2,
                "An ended foreign ticket changed its owner's completeness.");
            CheckPair(firstSnapshot.Events, EventKind.Update);
        }

        private static void CheckInactiveGateRejectsWithoutRetroactiveIssue()
        {
            ThreadingCallbacks firstBridge = new ThreadingCallbacks(16, 4);
            ThreadingCallbacks secondBridge = new ThreadingCallbacks(16, 4);
            CallbackSessionGate first = firstBridge.Session;
            CallbackSessionGate second = secondBridge.Session;
            Check(first.TryStart(), "The inactive-receiver owner session did not start.");
            CallbackTicket ticket = default(CallbackTicket);
            Check(first.TryBegin(EventKind.Update, out ticket), "The inactive-receiver owner ticket was refused.");
            bool endAttempted = false;
            try
            {
                CallbackSnapshot disabledBefore = second.ReadSnapshot();
                bool threw = false;
                bool nullAccepted = false;
                bool foreignAccepted = false;
                try
                {
                    nullAccepted = second.End(null);
                    foreignAccepted = second.End(ticket);
                }
                catch (Exception) { threw = true; }
                Check(!threw && !nullAccepted && !foreignAccepted,
                    "A disabled gate did not safely reject invalid tickets.");
                CallbackSnapshot disabledAfter = second.ReadSnapshot();
                Check(disabledBefore.State == CallbackState.Disabled && disabledAfter.State == CallbackState.Disabled
                    && disabledBefore.Issues == disabledAfter.Issues && disabledAfter.Events.Length == 0,
                    "Invalid End changed a disabled gate's state or issues.");
                Check(second.TryStart(), "Invalid End prevented the disabled gate from starting.");
                CallbackSnapshot stoppedBefore = Finish(second);
                Check(stoppedBefore.IsComplete && stoppedBefore.Events.Length == 0,
                    "The started empty receiver did not stop completely.");
                threw = false;
                try
                {
                    nullAccepted = second.End(null);
                    foreignAccepted = second.End(ticket);
                }
                catch (Exception) { threw = true; }
                Check(!threw && !nullAccepted && !foreignAccepted,
                    "A stopped gate did not safely reject invalid tickets.");
                CallbackSnapshot stoppedAfter = second.ReadSnapshot();
                Check(stoppedAfter.State == CallbackState.Stopped && stoppedAfter.Issues == stoppedBefore.Issues
                    && stoppedAfter.IsComplete && stoppedAfter.Events.Length == 0,
                    "Invalid End retroactively changed a stopped gate.");
                endAttempted = true;
                Check(ticket.End(), "An inactive receiver consumed the owner's ticket.");
                CallbackSnapshot firstSnapshot = Finish(first);
                Check(firstSnapshot.IsComplete && firstSnapshot.Events.Length == 2,
                    "An inactive receiver changed the owner's completeness.");
                CheckPair(firstSnapshot.Events, EventKind.Update);
            }
            finally
            {
                if (!endAttempted)
                {
                    try { ticket.End(); }
                    catch (Exception) { }
                }
            }
        }

        private static void CheckTicketWithoutOwner()
        {
            CallbackTicket ticket = new CallbackTicket(null, EventKind.Update);
            bool threw = false;
            bool accepted = false;
            try { accepted = ticket.End(); }
            catch (Exception) { threw = true; }
            Check(!threw && !accepted, "A ticket without an owner did not safely reject End.");
        }

        private static void CheckPendingEmpty(CallbackSessionGate gate, string description)
        {
            CallbackSnapshot snapshot = gate.ReadSnapshot();
            Check(snapshot.State == CallbackState.Closing && HasIssue(snapshot, CallbackIssue.Pending)
                && !snapshot.IsComplete && snapshot.Events.Length == 0, description);
        }

        private static void CheckRecorderCapacityIsIncomplete()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(1, 4);
            Check(bridge.Session.TryStart(), "The capacity session did not start.");
            ((ICities.IThreadingExtension)bridge).OnBeforeSimulationTick();
            CallbackSnapshot snapshot = Finish(bridge.Session);
            Check(!snapshot.IsComplete, "A full recorder was reported complete.");
            Check(snapshot.Events.Length <= 1, "The fake event capacity was exceeded.");
        }

        private static void CheckThreadCapacityIsIncomplete()
        {
            ThreadingCallbacks bridge = new ThreadingCallbacks(16, 1);
            Check(bridge.Session.TryStart(), "The thread-capacity session did not start.");
            ((ICities.IThreadingExtension)bridge).OnUpdate(float.NaN, float.NegativeInfinity);
            CallbackSnapshot snapshot = Finish(bridge.Session);
            Check(!snapshot.IsComplete, "A full fake thread capacity was reported complete.");
        }

        private static void CheckLateOldCallbackIsIsolated()
        {
            ThreadingCallbacks oldBridge = new ThreadingCallbacks(16, 4);
            Check(oldBridge.Session.TryStart(), "The old fake session did not start.");
            ICities.IThreadingExtension oldCallbacks = oldBridge;
            oldCallbacks.OnBeforeSimulationTick();
            oldCallbacks.OnReleased();
            Finish(oldBridge.Session);

            ThreadingCallbacks newBridge = new ThreadingCallbacks(16, 4);
            Check(newBridge.Session.TryStart(), "The new fake session did not start.");
            oldCallbacks.OnAfterSimulationTick();
            ((ICities.IThreadingExtension)newBridge).OnUpdate(float.NaN, float.NegativeInfinity);
            CallbackSnapshot snapshot = Finish(newBridge.Session);
            EventRecord[] events = snapshot.Events;
            Check(events.Length == 2 && events[0].Kind == EventKind.Update
                && events[0].Point == EventPoint.Enter && events[1].Kind == EventKind.Update
                && events[1].Point == EventPoint.Exit,
                "A delayed old callback polluted the new fake session.");
        }

        private enum ArgumentAudit { Verified, Accessed, Unverified }

        // Unsupported instructions are reported as unverified; operand bytes are never scanned as opcodes.
        private static readonly OpCode[] SupportedOpcodes = new OpCode[]
        {
            OpCodes.Nop, OpCodes.Break, OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3,
            OpCodes.Ldarg_S, OpCodes.Ldarga_S, OpCodes.Starg_S, OpCodes.Ldarg, OpCodes.Ldarga, OpCodes.Starg,
            OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3,
            OpCodes.Stloc_0, OpCodes.Stloc_1, OpCodes.Stloc_2, OpCodes.Stloc_3,
            OpCodes.Ldloc_S, OpCodes.Ldloca_S, OpCodes.Stloc_S, OpCodes.Ldloc, OpCodes.Ldloca, OpCodes.Stloc,
            OpCodes.Ldnull, OpCodes.Ldc_I4_M1, OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3, OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8, OpCodes.Ldc_I4_S, OpCodes.Ldc_I4, OpCodes.Ldc_I8, OpCodes.Ldc_R4, OpCodes.Ldc_R8,
            OpCodes.Dup, OpCodes.Pop, OpCodes.Jmp, OpCodes.Call, OpCodes.Calli, OpCodes.Callvirt, OpCodes.Ret,
            OpCodes.Br_S, OpCodes.Brfalse_S, OpCodes.Brtrue_S, OpCodes.Beq_S, OpCodes.Bge_S, OpCodes.Bgt_S,
            OpCodes.Ble_S, OpCodes.Blt_S, OpCodes.Bne_Un_S, OpCodes.Bge_Un_S, OpCodes.Bgt_Un_S,
            OpCodes.Ble_Un_S, OpCodes.Blt_Un_S, OpCodes.Br, OpCodes.Brfalse, OpCodes.Brtrue, OpCodes.Beq,
            OpCodes.Bge, OpCodes.Bgt, OpCodes.Ble, OpCodes.Blt, OpCodes.Bne_Un, OpCodes.Bge_Un,
            OpCodes.Bgt_Un, OpCodes.Ble_Un, OpCodes.Blt_Un, OpCodes.Switch, OpCodes.Leave, OpCodes.Leave_S,
            OpCodes.Add, OpCodes.Sub, OpCodes.Mul, OpCodes.Div, OpCodes.Div_Un, OpCodes.Rem, OpCodes.Rem_Un,
            OpCodes.And, OpCodes.Or, OpCodes.Xor, OpCodes.Shl, OpCodes.Shr, OpCodes.Shr_Un, OpCodes.Neg,
            OpCodes.Not, OpCodes.Ceq, OpCodes.Cgt, OpCodes.Cgt_Un, OpCodes.Clt, OpCodes.Clt_Un,
            OpCodes.Ldstr, OpCodes.Newobj, OpCodes.Castclass, OpCodes.Isinst, OpCodes.Box, OpCodes.Unbox,
            OpCodes.Unbox_Any, OpCodes.Throw, OpCodes.Rethrow, OpCodes.Endfinally, OpCodes.Endfilter,
            OpCodes.Ldfld, OpCodes.Ldflda, OpCodes.Stfld, OpCodes.Ldsfld, OpCodes.Ldsflda, OpCodes.Stsfld,
            OpCodes.Ldobj, OpCodes.Stobj, OpCodes.Initobj, OpCodes.Cpobj, OpCodes.Newarr, OpCodes.Ldlen,
            OpCodes.Ldelema, OpCodes.Ldelem_Ref, OpCodes.Stelem_Ref, OpCodes.Ldind_Ref, OpCodes.Stind_Ref,
            OpCodes.Ldtoken, OpCodes.Ldftn, OpCodes.Ldvirtftn, OpCodes.Constrained, OpCodes.Volatile,
            OpCodes.Unaligned, OpCodes.Tailcall, OpCodes.Readonly, OpCodes.Sizeof
        };

        private static ArgumentAudit AuditCallbackArguments(Type target)
        {
            try
            {
                Assembly fakeAssembly = typeof(CallbackBridgeFakeChecks).Assembly;
                if (target.Assembly != fakeAssembly || typeof(ICities.IThreadingExtension).Assembly != fakeAssembly)
                    return ArgumentAudit.Unverified;
                InterfaceMapping mapping = target.GetInterfaceMap(typeof(ICities.IThreadingExtension));
                MethodInfo onCreated = null;
                MethodInfo onUpdate = null;
                for (int i = 0; i < mapping.InterfaceMethods.Length; i++)
                {
                    string name = mapping.InterfaceMethods[i].Name;
                    if (name == "OnCreated")
                    {
                        if (onCreated != null) return ArgumentAudit.Unverified;
                        onCreated = mapping.TargetMethods[i];
                    }
                    else if (name == "OnUpdate")
                    {
                        if (onUpdate != null) return ArgumentAudit.Unverified;
                        onUpdate = mapping.TargetMethods[i];
                    }
                }
                if (onCreated == null || onUpdate == null || onCreated.Module.Assembly != fakeAssembly
                    || onUpdate.Module.Assembly != fakeAssembly) return ArgumentAudit.Unverified;
                ArgumentAudit createdAudit = AuditMethodArguments(onCreated, 1);
                ArgumentAudit updateAudit = AuditMethodArguments(onUpdate, 2);
                if (createdAudit == ArgumentAudit.Accessed || updateAudit == ArgumentAudit.Accessed)
                    return ArgumentAudit.Accessed;
                if (createdAudit != ArgumentAudit.Verified || updateAudit != ArgumentAudit.Verified)
                    return ArgumentAudit.Unverified;
                return ArgumentAudit.Verified;
            }
            catch (Exception)
            {
                return ArgumentAudit.Unverified;
            }
        }

        private static bool TryFindOpcode(ushort value, out OpCode result)
        {
            foreach (OpCode candidate in SupportedOpcodes)
            {
                if (unchecked((ushort)candidate.Value) == value)
                {
                    result = candidate;
                    return true;
                }
            }
            result = default(OpCode);
            return false;
        }

        private static bool IsArgumentVariableOpcode(OpCode opcode)
        {
            short value = opcode.Value;
            return value == OpCodes.Ldarg_S.Value || value == OpCodes.Ldarga_S.Value
                || value == OpCodes.Starg_S.Value || value == OpCodes.Ldarg.Value
                || value == OpCodes.Ldarga.Value || value == OpCodes.Starg.Value;
        }

        private static ArgumentAudit AuditMethodArguments(MethodInfo method, int protectedArguments)
        {
            try
            {
                if (method.IsStatic || method.ReturnType != typeof(void)
                    || method.GetParameters().Length != protectedArguments) return ArgumentAudit.Unverified;
                MethodBody body = method.GetMethodBody();
                if (body == null) return ArgumentAudit.Unverified;
                byte[] bytes = body.GetILAsByteArray();
                if (bytes == null || bytes.Length == 0) return ArgumentAudit.Unverified;
                int position = 0;
                while (position < bytes.Length)
                {
                    ushort value = bytes[position++];
                    if (value == 0xFE)
                    {
                        if (position >= bytes.Length) return ArgumentAudit.Unverified;
                        value = (ushort)(0xFE00 | bytes[position++]);
                    }
                    OpCode opcode;
                    if (!TryFindOpcode(value, out opcode)) return ArgumentAudit.Unverified;
                    // A jump forwards the current method arguments without explicit ldarg instructions.
                    if (opcode.Value == OpCodes.Jmp.Value) return ArgumentAudit.Accessed;

                    int operandStart = position;
                    int operandLength;
                    switch (opcode.OperandType)
                    {
                        case OperandType.InlineNone:
                            operandLength = 0;
                            break;
                        case OperandType.ShortInlineBrTarget:
                        case OperandType.ShortInlineI:
                        case OperandType.ShortInlineVar:
                            operandLength = 1;
                            break;
                        case OperandType.InlineVar:
                            operandLength = 2;
                            break;
                        case OperandType.InlineBrTarget:
                        case OperandType.InlineField:
                        case OperandType.InlineI:
                        case OperandType.InlineMethod:
                        case OperandType.InlineSig:
                        case OperandType.InlineString:
                        case OperandType.InlineTok:
                        case OperandType.InlineType:
                        case OperandType.ShortInlineR:
                            operandLength = 4;
                            break;
                        case OperandType.InlineI8:
                        case OperandType.InlineR:
                            operandLength = 8;
                            break;
                        case OperandType.InlineSwitch:
                            if (position > bytes.Length - 4) return ArgumentAudit.Unverified;
                            int count = bytes[position] | (bytes[position + 1] << 8)
                                | (bytes[position + 2] << 16) | (bytes[position + 3] << 24);
                            if (count < 0 || count > (bytes.Length - position - 4) / 4)
                                return ArgumentAudit.Unverified;
                            operandLength = 4 + count * 4;
                            break;
                        default:
                            return ArgumentAudit.Unverified;
                    }
                    if (operandLength > bytes.Length - position) return ArgumentAudit.Unverified;

                    int argument = -1;
                    if (opcode.Value == OpCodes.Ldarg_1.Value) argument = 1;
                    else if (opcode.Value == OpCodes.Ldarg_2.Value) argument = 2;
                    else if (opcode.Value == OpCodes.Ldarg_3.Value) argument = 3;
                    else if (IsArgumentVariableOpcode(opcode))
                    {
                        if (opcode.OperandType == OperandType.ShortInlineVar) argument = bytes[operandStart];
                        else if (opcode.OperandType == OperandType.InlineVar)
                            argument = bytes[operandStart] | (bytes[operandStart + 1] << 8);
                        else return ArgumentAudit.Unverified;
                    }
                    if (argument >= 1 && argument <= protectedArguments) return ArgumentAudit.Accessed;
                    position += operandLength;
                }
                return ArgumentAudit.Verified;
            }
            catch (Exception)
            {
                return ArgumentAudit.Unverified;
            }
        }
    }
}
