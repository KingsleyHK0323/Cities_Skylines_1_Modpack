// Signature: OpenAI-Codex_boundary_fake
// These checks use only synthetic callbacks and anonymous recorder-assigned tags.
using System;
using System.Reflection;
using System.Threading;

namespace RealisticCityTime.Diagnostics.RelativeEventTrace
{
    internal static class RelativeEventRecorderBehaviorChecks
    {
        private const int WaitLimitMilliseconds = 10000;
        private static int assertions;

        internal static int Run()
        {
            assertions = 0;
            CheckDisabledAndOneShot();
            CheckOrderAndAnonymousTags();
            CheckInvalidEvent((EventKind)0, EventPoint.Enter, "invalid kind");
            CheckInvalidEvent(EventKind.Update, (EventPoint)0, "invalid point");
            CheckEventCapacity();
            CheckThreadCapacity();
            CheckThreadSlotIsolationAndReuse();
            CheckSnapshotIsolation();
            CheckContentionWhenMonitorGateAvailable();
            CheckSimulatedReentrancyWhenFlagAvailable();
            CheckSimulatedAdmissionContention();
            CheckSimulatedPendingAdmission();
            CheckSimulatedAdmissionLimit();
            CheckStopBeforeStart();
            CheckSyntheticSaveDataEvents();
            CheckRejectedConstructorCapacity(0, 1, "zero event capacity");
            CheckRejectedConstructorCapacity(4097, 1, "excess event capacity");
            CheckRejectedConstructorCapacity(1, 0, "zero thread capacity");
            CheckRejectedConstructorCapacity(1, 129, "excess thread capacity");
            return assertions;
        }

        private static void Check(bool condition, string name)
        {
            assertions++;
            if (!condition)
            {
                throw new InvalidOperationException("Relative event behavior check failed: " + name);
            }
        }

        private static bool HasIssue(RecorderSnapshot snapshot, RecorderIssue issue)
        {
            return (snapshot.Issues & issue) == issue;
        }

        private static RecorderSnapshot StopAndRead(RelativeEventRecorder recorder)
        {
            recorder.RequestStop();
            RecorderState result = recorder.TryCompleteStop();
            Check(result == RecorderState.Stopped, "stop completes");
            Check(recorder.State == RecorderState.Stopped, "state is stopped");
            RecorderSnapshot snapshot = recorder.ReadSnapshot();
            Check(snapshot.State == RecorderState.Stopped, "snapshot is stopped");
            Check(snapshot.Events != null, "snapshot has an event array");
            return snapshot;
        }

        private static void Close(RelativeEventRecorder recorder)
        {
            try
            {
                recorder.RequestStop();
            }
            finally
            {
                recorder.TryCompleteStop();
            }
        }

        private static void CheckDisabledAndOneShot()
        {
            var recorder = new RelativeEventRecorder(4, 2);
            try
            {
                Check(recorder.State == RecorderState.Disabled, "initially disabled");
                Check(recorder.Observe(EventKind.Update, EventPoint.Enter) == RecordResult.NotRecording,
                    "callback before start is rejected");
                Check(recorder.TryStart(), "first start succeeds");
                Check(recorder.State == RecorderState.Recording, "state is recording");
                Check(!recorder.TryStart(), "second start while recording fails");

                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 0, "pre-start callback was not stored");
                Check(recorder.Observe(EventKind.Update, EventPoint.Exit) != RecordResult.Recorded,
                    "callback after stop is rejected");
                Check(!recorder.TryStart(), "stopped recorder cannot restart");
                RecorderSnapshot after = recorder.ReadSnapshot();
                Check(after.Events.Length == 0, "post-stop callback was not stored");
            }
            finally
            {
                Close(recorder);
            }
        }

        private static void CheckOrderAndAnonymousTags()
        {
            var recorder = new RelativeEventRecorder(8, 2 + 1); // Keep one thread slot unused.
            var firstDone = new ManualResetEvent(false);
            var secondDone = new ManualResetEvent(false);
            Thread first = null;
            Thread second = null;
            Exception firstFailure = null;
            Exception secondFailure = null;
            RecordResult firstResult = RecordResult.NotRecording;
            RecordResult secondResult = RecordResult.NotRecording;
            RecordResult thirdResult = RecordResult.NotRecording;
            try
            {
                Check(recorder.TryStart(), "order recorder starts");
                first = new Thread(delegate()
                {
                    try
                    {
                        firstResult = recorder.Observe(EventKind.BeforeSimulationFrame, EventPoint.Enter);
                        firstDone.Set();
                        if (!secondDone.WaitOne(WaitLimitMilliseconds))
                        {
                            throw new InvalidOperationException("Second synthetic callback did not finish.");
                        }
                        thirdResult = recorder.Observe(EventKind.AfterSimulationFrame, EventPoint.Exit);
                    }
                    catch (Exception error)
                    {
                        firstFailure = error;
                        firstDone.Set();
                        secondDone.Set();
                    }
                });
                second = new Thread(delegate()
                {
                    try
                    {
                        if (!firstDone.WaitOne(WaitLimitMilliseconds))
                        {
                            throw new InvalidOperationException("First synthetic callback did not finish.");
                        }
                        secondResult = recorder.Observe(EventKind.Update, EventPoint.Enter);
                    }
                    catch (Exception error)
                    {
                        secondFailure = error;
                    }
                    finally
                    {
                        secondDone.Set();
                    }
                });
                first.IsBackground = true;
                second.IsBackground = true;
                first.Start();
                second.Start();
                bool firstJoined = first.Join(WaitLimitMilliseconds);
                bool secondJoined = second.Join(WaitLimitMilliseconds);
                Check(firstJoined && secondJoined, "order callback threads join");
                Check(firstFailure == null && secondFailure == null, "order callbacks do not throw");
                Check(firstResult == RecordResult.Recorded && secondResult == RecordResult.Recorded &&
                    thirdResult == RecordResult.Recorded, "three callbacks are recorded");

                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 3, "three ordered records");
                Check(snapshot.Events[0].Kind == EventKind.BeforeSimulationFrame &&
                    snapshot.Events[0].Point == EventPoint.Enter, "first record values");
                Check(snapshot.Events[1].Kind == EventKind.Update &&
                    snapshot.Events[1].Point == EventPoint.Enter, "second record values");
                Check(snapshot.Events[2].Kind == EventKind.AfterSimulationFrame &&
                    snapshot.Events[2].Point == EventPoint.Exit, "third record values");
                Check(snapshot.Events[0].Order < snapshot.Events[1].Order &&
                    snapshot.Events[1].Order < snapshot.Events[2].Order, "record order increases");
                Check(snapshot.Events[0].ThreadTag == snapshot.Events[2].ThreadTag &&
                    snapshot.Events[0].ThreadTag != snapshot.Events[1].ThreadTag,
                    "anonymous tags follow first, second, first");
                Check(snapshot.Issues == RecorderIssue.None && snapshot.IsComplete,
                    "undropped ordered trace is complete");
            }
            finally
            {
                firstDone.Set();
                secondDone.Set();
                if (first != null && first.IsAlive) first.Join(WaitLimitMilliseconds);
                if (second != null && second.IsAlive) second.Join(WaitLimitMilliseconds);
                Close(recorder);
                if ((first == null || !first.IsAlive) && (second == null || !second.IsAlive))
                {
                    firstDone.Close();
                    secondDone.Close();
                }
            }
        }

        private static void CheckInvalidEvent(EventKind kind, EventPoint point, string name)
        {
            var recorder = new RelativeEventRecorder(4, 2);
            try
            {
                Check(recorder.TryStart(), name + " recorder starts");
                Check(recorder.Observe(kind, point) == RecordResult.DroppedInvalid, name + " rejected");
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 0, name + " not stored");
                Check(HasIssue(snapshot, RecorderIssue.InvalidEvent) && !snapshot.IsComplete,
                    name + " marks incomplete trace");
            }
            finally
            {
                Close(recorder);
            }
        }

        private static void CheckEventCapacity()
        {
            var recorder = new RelativeEventRecorder(2, 2);
            try
            {
                Check(recorder.TryStart(), "capacity recorder starts");
                Check(recorder.Observe(EventKind.Update, EventPoint.Enter) == RecordResult.Recorded,
                    "first event fits");
                Check(recorder.Observe(EventKind.Update, EventPoint.Exit) == RecordResult.Recorded,
                    "last event fits");
                Check(recorder.State != RecorderState.Recording, "full event capacity stops admission");
                Check(recorder.Observe(EventKind.Update, EventPoint.Enter) != RecordResult.Recorded,
                    "callback beyond event capacity rejected");
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 2, "event count remains within capacity");
                Check(HasIssue(snapshot, RecorderIssue.CapacityReached) && !snapshot.IsComplete,
                    "event capacity marks incomplete trace");
            }
            finally
            {
                Close(recorder);
            }
        }

        private static void CheckThreadCapacity()
        {
            var recorder = new RelativeEventRecorder(4, 1);
            Thread second = null;
            Exception failure = null;
            RecordResult result = RecordResult.NotRecording;
            try
            {
                Check(recorder.TryStart(), "thread capacity recorder starts");
                Check(recorder.Observe(EventKind.Update, EventPoint.Enter) == RecordResult.Recorded,
                    "first anonymous thread fits");
                Check(recorder.State != RecorderState.Recording,
                    "allocating the last thread slot stops admission immediately");
                second = new Thread(delegate()
                {
                    try
                    {
                        result = recorder.Observe(EventKind.Update, EventPoint.Exit);
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                });
                second.IsBackground = true;
                second.Start();
                Check(second.Join(WaitLimitMilliseconds), "thread capacity callback joins");
                Check(failure == null && result != RecordResult.Recorded,
                    "additional anonymous thread rejected");
                Check(recorder.State != RecorderState.Recording, "thread limit stops admission");
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 1, "thread limit preserves first event only");
                Check(HasIssue(snapshot, RecorderIssue.ThreadLimitReached) && !snapshot.IsComplete,
                    "thread limit marks incomplete trace");
            }
            finally
            {
                if (second != null && second.IsAlive) second.Join(WaitLimitMilliseconds);
                Close(recorder);
            }
        }

        private static void CheckThreadSlotIsolationAndReuse()
        {
            var first = new RelativeEventRecorder(4, 2);
            var second = new RelativeEventRecorder(4, 2);
            var later = new RelativeEventRecorder(4, 2);
            try
            {
                Check(first.TryStart() && second.TryStart(), "parallel recorders start");
                Check(first.Observe(EventKind.BeforeSimulationTick, EventPoint.Enter) == RecordResult.Recorded,
                    "first recorder owns the caller slot");
                Check(second.Observe(EventKind.Update, EventPoint.Enter) == RecordResult.DroppedThreadConflict,
                    "second recorder rejects the occupied caller slot");
                RecorderSnapshot secondSnapshot = StopAndRead(second);
                Check(secondSnapshot.Events.Length == 0 &&
                    HasIssue(secondSnapshot, RecorderIssue.ThreadConflict) && !secondSnapshot.IsComplete,
                    "conflict is confined to second recorder");
                Check(first.Observe(EventKind.AfterSimulationTick, EventPoint.Exit) == RecordResult.Recorded,
                    "first recorder still accepts its caller");
                RecorderSnapshot firstSnapshot = StopAndRead(first);
                Check(firstSnapshot.Events.Length == 2 && firstSnapshot.Issues == RecorderIssue.None &&
                    firstSnapshot.IsComplete, "first recorder remains complete");
                Check(firstSnapshot.Events[0].ThreadTag == firstSnapshot.Events[1].ThreadTag,
                    "first recorder retains its anonymous tag");

                Check(later.TryStart(), "later recorder starts");
                Check(later.Observe(EventKind.Update, EventPoint.Enter) == RecordResult.Recorded,
                    "caller slot can be reused after stop");
                RecorderSnapshot laterSnapshot = StopAndRead(later);
                Check(laterSnapshot.Events.Length == 1 && laterSnapshot.Issues == RecorderIssue.None &&
                    laterSnapshot.IsComplete, "later session is complete");
            }
            finally
            {
                Close(second);
                Close(first);
                Close(later);
            }
        }

        private static void CheckSnapshotIsolation()
        {
            var recorder = new RelativeEventRecorder(4, 2);
            try
            {
                Check(recorder.TryStart(), "snapshot recorder starts");
                Check(recorder.Observe(EventKind.Update, EventPoint.Enter) == RecordResult.Recorded,
                    "snapshot source event recorded");
                RecorderSnapshot first = StopAndRead(recorder);
                Check(first.Events.Length == 1, "first snapshot contains event");
                first.Events[0] = default(EventRecord);
                RecorderSnapshot second = recorder.ReadSnapshot();
                Check(second.Events.Length == 1 && second.Events[0].Kind == EventKind.Update &&
                    second.Events[0].Point == EventPoint.Enter, "snapshot array cannot mutate recorder");
            }
            finally
            {
                Close(recorder);
            }
        }

        // Optional white-box check: a reference-type _gate may be a Monitor gate.
        // Holding it is not evidence that a real callback is in flight.
        private static void CheckContentionWhenMonitorGateAvailable()
        {
            FieldInfo field = typeof(RelativeEventRecorder).GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType.IsValueType)
            {
                return;
            }

            var recorder = new RelativeEventRecorder(4, 2);
            var held = new ManualResetEvent(false);
            var release = new ManualResetEvent(false);
            var attempted = new ManualResetEvent(false);
            var finished = new ManualResetEvent(false);
            Thread holder = null;
            Thread callback = null;
            Exception holderFailure = null;
            Exception callbackFailure = null;
            RecordResult result = RecordResult.NotRecording;
            try
            {
                Check(recorder.TryStart(), "contention recorder starts");
                object gate = field.GetValue(recorder);
                Check(gate != null, "monitor gate exists");
                holder = new Thread(delegate()
                {
                    try
                    {
                        lock (gate)
                        {
                            held.Set();
                            if (!release.WaitOne(WaitLimitMilliseconds))
                            {
                                throw new InvalidOperationException("Gate release was not signaled.");
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        holderFailure = error;
                        held.Set();
                    }
                });
                callback = new Thread(delegate()
                {
                    try
                    {
                        attempted.Set();
                        result = recorder.Observe(EventKind.Update, EventPoint.Enter);
                    }
                    catch (Exception error)
                    {
                        callbackFailure = error;
                    }
                    finally
                    {
                        finished.Set();
                    }
                });
                holder.IsBackground = true;
                callback.IsBackground = true;
                holder.Start();
                Check(held.WaitOne(WaitLimitMilliseconds), "monitor gate acquired");
                callback.Start();
                Check(attempted.WaitOne(WaitLimitMilliseconds), "contended callback started");
                bool finishedWhileHeld = finished.WaitOne(WaitLimitMilliseconds / 2);
                release.Set();
                bool holderJoined = holder.Join(WaitLimitMilliseconds);
                bool callbackJoined = callback.Join(WaitLimitMilliseconds);
                Check(holderJoined && callbackJoined, "contention threads join");
                Check(holderFailure == null && callbackFailure == null, "contention threads do not throw");
                Check(finishedWhileHeld && result == RecordResult.DroppedContention,
                    "contended callback is rejected without waiting for gate");
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 0 && HasIssue(snapshot, RecorderIssue.Contention) &&
                    !snapshot.IsComplete, "contention marks incomplete trace");
            }
            finally
            {
                release.Set();
                if (holder != null && holder.IsAlive) holder.Join(WaitLimitMilliseconds);
                if (callback != null && callback.IsAlive) callback.Join(WaitLimitMilliseconds);
                Close(recorder);
                if ((holder == null || !holder.IsAlive) && (callback == null || !callback.IsAlive))
                {
                    held.Close();
                    release.Close();
                    attempted.Close();
                    finished.Close();
                }
            }
        }

        // Optional white-box simulation; it does not produce a nested callback.
        private static void CheckSimulatedReentrancyWhenFlagAvailable()
        {
            FieldInfo field = typeof(RelativeEventRecorder).GetField("_insideObserver", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(bool) ||
                !field.IsDefined(typeof(ThreadStaticAttribute), false))
            {
                return;
            }

            var recorder = new RelativeEventRecorder(4, 2);
            Thread callback = null;
            Exception failure = null;
            RecordResult result = RecordResult.NotRecording;
            try
            {
                Check(recorder.TryStart(), "simulated reentrancy recorder starts");
                callback = new Thread(delegate()
                {
                    try
                    {
                        object original = field.GetValue(null);
                        try
                        {
                            field.SetValue(null, true);
                            result = recorder.Observe(EventKind.Update, EventPoint.Enter);
                        }
                        finally
                        {
                            field.SetValue(null, original);
                        }
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                });
                callback.IsBackground = true;
                callback.Start();
                Check(callback.Join(WaitLimitMilliseconds), "simulated reentrancy callback joins");
                Check(failure == null && result == RecordResult.DroppedReentrancy,
                    "simulated reentrancy is rejected");
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 0 && HasIssue(snapshot, RecorderIssue.Reentrancy) &&
                    !snapshot.IsComplete, "simulated reentrancy marks incomplete trace");
            }
            finally
            {
                if (callback != null && callback.IsAlive) callback.Join(WaitLimitMilliseconds);
                Close(recorder);
            }
        }

        // These white-box checks simulate control state in one thread. They do not
        // demonstrate real callback overlap or the draining of a real callback.
        private const long SyntheticAdmissionUnit = 1L << 16;

        private static FieldInfo GetSyntheticAdmissionControl()
        {
            FieldInfo field = typeof(RelativeEventRecorder).GetField("control", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(field != null && field.FieldType == typeof(long), "synthetic admission control exists");
            return field;
        }

        private static void AddSyntheticAdmission(RelativeEventRecorder recorder, FieldInfo field)
        {
            long value = (long)field.GetValue(recorder);
            Check((value >> 16) == 0, "no actual admissions are outstanding before simulation");
            field.SetValue(recorder, value + SyntheticAdmissionUnit);
        }

        private static void RemoveSyntheticAdmission(RelativeEventRecorder recorder, FieldInfo field)
        {
            long value = (long)field.GetValue(recorder);
            if ((value >> 16) > 0)
            {
                field.SetValue(recorder, value - SyntheticAdmissionUnit);
            }
        }

        private static void CheckSimulatedAdmissionContention()
        {
            FieldInfo field = GetSyntheticAdmissionControl();
            var recorder = new RelativeEventRecorder(4, 2);
            bool syntheticAdmission = false;
            try
            {
                Check(recorder.TryStart(), "simulated admission contention recorder starts");
                AddSyntheticAdmission(recorder, field);
                syntheticAdmission = true;
                try
                {
                    recorder.Observe(EventKind.Update, EventPoint.Enter);
                    Check(((long)field.GetValue(recorder) >> 16) == 1,
                        "actual callback releases its own admission");
                }
                finally
                {
                    RemoveSyntheticAdmission(recorder, field);
                    syntheticAdmission = false;
                }
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(HasIssue(snapshot, RecorderIssue.Contention),
                    "a pre-existing simulated admission marks contention");
                Check(!snapshot.IsComplete, "simulated admission contention is incomplete");
            }
            finally
            {
                if (syntheticAdmission) RemoveSyntheticAdmission(recorder, field);
                Close(recorder);
            }
        }

        private static void CheckSimulatedPendingAdmission()
        {
            FieldInfo field = GetSyntheticAdmissionControl();
            var recorder = new RelativeEventRecorder(4, 2);
            bool syntheticAdmission = false;
            try
            {
                Check(recorder.TryStart(), "simulated pending recorder starts");
                Check(recorder.Observe(EventKind.Update, EventPoint.Enter) == RecordResult.Recorded,
                    "pending snapshot has a source event");
                AddSyntheticAdmission(recorder, field);
                syntheticAdmission = true;
                Check(recorder.RequestStop() == RecorderState.Stopping,
                    "stop waits while a simulated admission is outstanding");
                Check(recorder.TryCompleteStop() == RecorderState.Stopping,
                    "completion remains pending with a simulated admission");
                RecorderSnapshot pending = recorder.ReadSnapshot();
                Check(pending.State == RecorderState.Stopping && !pending.IsComplete &&
                    HasIssue(pending, RecorderIssue.Pending), "pending snapshot is incomplete");
                Check(pending.Events != null && pending.Events.Length == 0,
                    "pending snapshot does not expose source events");
                Check(((long)field.GetValue(recorder) >> 16) == 1,
                    "simulated pending admission remains outstanding");
                RemoveSyntheticAdmission(recorder, field);
                syntheticAdmission = false;
                Check(recorder.TryCompleteStop() == RecorderState.Stopped,
                    "stop completes after simulated admission is released");
                RecorderSnapshot stopped = recorder.ReadSnapshot();
                Check(stopped.State == RecorderState.Stopped && stopped.Events.Length == 1 &&
                    stopped.Events[0].Kind == EventKind.Update, "stopped snapshot exposes the source event");
                Check(!HasIssue(stopped, RecorderIssue.Pending), "stopped snapshot is no longer pending");
            }
            finally
            {
                if (syntheticAdmission) RemoveSyntheticAdmission(recorder, field);
                Close(recorder);
            }
        }

        private static void CheckStopBeforeStart()
        {
            var recorder = new RelativeEventRecorder(4, 2);
            try
            {
                Check(recorder.State == RecorderState.Disabled, "pre-start stop begins disabled");
                Check(recorder.RequestStop() == RecorderState.Stopped, "pre-start stop completes");
                Check(recorder.State == RecorderState.Stopped, "pre-start stop closes recorder");
                Check(!recorder.TryStart(), "pre-start stop prevents a delayed start");
                Check(recorder.Observe(EventKind.Update, EventPoint.Enter) != RecordResult.Recorded,
                    "pre-start stop rejects callbacks");
                RecorderSnapshot snapshot = recorder.ReadSnapshot();
                Check(snapshot.State == RecorderState.Stopped && snapshot.Events != null &&
                    snapshot.Events.Length == 0, "pre-start stopped snapshot contains no events");
                Check(Enum.IsDefined(typeof(RecorderIssue), "NotStarted"), "not-started issue exists");
                RecorderIssue notStarted = (RecorderIssue)Enum.Parse(typeof(RecorderIssue), "NotStarted");
                Check((int)notStarted == 1024, "not-started issue uses the specified flag");
                Check(HasIssue(snapshot, notStarted) && !snapshot.IsComplete,
                    "never-started snapshot is incomplete for the stated reason");
            }
            finally
            {
                Close(recorder);
            }
        }

        // This simulates outstanding admissions, not 1,024 real threads or callbacks.
        private static void CheckSimulatedAdmissionLimit()
        {
            const long syntheticCount = 1023;
            FieldInfo field = GetSyntheticAdmissionControl();
            var recorder = new RelativeEventRecorder(4, 2);
            bool syntheticAdmissions = false;
            try
            {
                Check(recorder.TryStart(), "simulated admission limit recorder starts");
                long initial = (long)field.GetValue(recorder);
                Check((initial >> 16) == 0, "no actual admissions exist before limit simulation");
                field.SetValue(recorder, initial + syntheticCount * SyntheticAdmissionUnit);
                syntheticAdmissions = true;
                try
                {
                    RecordResult outcome = recorder.Observe(EventKind.Update, EventPoint.Enter);
                    Check(outcome == RecordResult.DroppedAdmissionLimit,
                        "capacity-reaching callback reports the admission limit");
                    Check(((long)field.GetValue(recorder) >> 16) == syntheticCount,
                        "limit callback releases its own admission");
                }
                finally
                {
                    RemoveSimulatedAdmissions(recorder, field, syntheticCount);
                    syntheticAdmissions = false;
                }
                Check(recorder.State != RecorderState.Recording,
                    "admission stops upon reaching its capacity");
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(HasIssue(snapshot, RecorderIssue.AdmissionLimit) && !snapshot.IsComplete,
                    "admission capacity marks the stopped trace incomplete");
            }
            finally
            {
                if (syntheticAdmissions) RemoveSimulatedAdmissions(recorder, field, syntheticCount);
                Close(recorder);
            }
        }

        private static void RemoveSimulatedAdmissions(RelativeEventRecorder recorder, FieldInfo field, long count)
        {
            long value = (long)field.GetValue(recorder);
            long remaining = value >> 16;
            long remove = Math.Min(remaining, count);
            if (remove > 0)
            {
                field.SetValue(recorder, value - remove * SyntheticAdmissionUnit);
            }
        }

        // These are synthetic enum inputs; they do not access or trigger a save.
        private static void CheckSyntheticSaveDataEvents()
        {
            var recorder = new RelativeEventRecorder(3, 2);
            try
            {
                Check(recorder.TryStart(), "synthetic save event recorder starts");
                Check(recorder.Observe(EventKind.SaveData, EventPoint.Enter) == RecordResult.Recorded,
                    "synthetic save enter recorded");
                Check(recorder.Observe(EventKind.SaveData, EventPoint.Exit) == RecordResult.Recorded,
                    "synthetic save exit recorded");
                RecorderSnapshot snapshot = StopAndRead(recorder);
                Check(snapshot.Events.Length == 2, "two synthetic save events stored");
                Check(snapshot.Events[0].Kind == EventKind.SaveData &&
                    snapshot.Events[0].Point == EventPoint.Enter, "synthetic save enter values");
                Check(snapshot.Events[1].Kind == EventKind.SaveData &&
                    snapshot.Events[1].Point == EventPoint.Exit, "synthetic save exit values");
                Check(snapshot.Events[1].Order == snapshot.Events[0].Order + 1,
                    "synthetic save event orders are consecutive");
                Check(snapshot.Issues == RecorderIssue.None && snapshot.IsComplete,
                    "synthetic save event trace is complete");
            }
            finally
            {
                Close(recorder);
            }
        }

        private static void CheckRejectedConstructorCapacity(int eventCapacity, int threadCapacity, string name)
        {
            RelativeEventRecorder recorder = null;
            bool rejected = false;
            try
            {
                try
                {
                    recorder = new RelativeEventRecorder(eventCapacity, threadCapacity);
                }
                catch (ArgumentOutOfRangeException)
                {
                    rejected = true;
                }
                Check(rejected, name + " is rejected with ArgumentOutOfRangeException");
            }
            finally
            {
                if (recorder != null) Close(recorder);
            }
        }
    }
}
