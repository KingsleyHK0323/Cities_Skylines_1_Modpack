// Signature: OpenAI-Codex_diagnostic_impl
// Standalone, disabled diagnostic candidate. It is not a game extension or a clock writer.
using System;
using System.Diagnostics;
using System.Threading;

namespace RealisticCityTime.Diagnostics
{
    // A local cooperative capability, not a security boundary against code in this process.
    internal sealed class ClockDiagnosticPageLease
    {
        private int revoked;
        internal bool IsRevoked { get { return Interlocked.CompareExchange(ref revoked, 0, 0) != 0; } }
        internal void Revoke() { Interlocked.Exchange(ref revoked, 1); }
    }

    // These are endpoint differences, not proof of continuity or of city identity.
    internal struct ClockDiagnosticFrameRecord
    {
        internal readonly long FrameDelta;
        internal readonly long PhaseDelta;
        internal readonly long PerFrameDeltaTicks;
        // Stopwatch counter difference only; Frequency is not stored, so this is not seconds or TimeSpan ticks.
        internal readonly long ElapsedCounterTicks;
        internal readonly bool PhaseMayWrapOrJump;
        internal readonly bool LifecycleContinuityVerified;
        internal readonly bool CityIdentityVerified;
        internal readonly bool ConcurrentCallbacksExcluded;

        internal ClockDiagnosticFrameRecord(long frameDelta, long phaseDelta, long durationDelta,
            long elapsed)
        {
            FrameDelta = frameDelta;
            PhaseDelta = phaseDelta;
            PerFrameDeltaTicks = durationDelta;
            ElapsedCounterTicks = elapsed;
            PhaseMayWrapOrJump = true;
            LifecycleContinuityVerified = false;
            CityIdentityVerified = false;
            ConcurrentCallbacksExcluded = false;
        }
    }

    internal struct ClockDiagnosticDisplayRecord
    {
        internal readonly bool HasDelta;
        internal readonly long ReferenceFrameDelta;
        internal readonly float ReferenceTimerDelta;
        internal readonly float PhaseHourDelta;
        internal readonly long PerFrameDeltaTicks;
        // Stopwatch counter difference only; Frequency is not stored, so this is not seconds or TimeSpan ticks.
        internal readonly long ElapsedCounterTicks;
        internal readonly bool PhaseMayWrapOrJump;
        internal readonly bool LifecycleContinuityVerified;
        internal readonly bool CityIdentityVerified;
        internal readonly bool ConcurrentCallbacksExcluded;

        internal ClockDiagnosticDisplayRecord(bool hasDelta, long frameDelta, float timerDelta,
            float hourDelta, long durationDelta, long elapsed)
        {
            HasDelta = hasDelta;
            ReferenceFrameDelta = frameDelta;
            ReferenceTimerDelta = timerDelta;
            PhaseHourDelta = hourDelta;
            PerFrameDeltaTicks = durationDelta;
            ElapsedCounterTicks = elapsed;
            PhaseMayWrapOrJump = true;
            LifecycleContinuityVerified = false;
            CityIdentityVerified = false;
            ConcurrentCallbacksExcluded = false;
        }
    }

    // A bounded copy for an explicit manual reader, available only after a confirmed stop.
    internal sealed class ClockDiagnosticReadOnlySnapshot
    {
        private readonly ClockDiagnosticFrameRecord[] frames;
        private readonly ClockDiagnosticDisplayRecord[] displays;

        internal ClockDiagnosticReadOnlySnapshot(ClockDiagnosticFrameRecord[] frames,
            ClockDiagnosticDisplayRecord[] displays)
        {
            this.frames = frames;
            this.displays = displays;
        }

        internal int FrameCount { get { return frames.Length; } }
        internal int DisplayCount { get { return displays.Length; } }
        internal ClockDiagnosticFrameRecord FrameAt(int index) { return frames[index]; }
        internal ClockDiagnosticDisplayRecord DisplayAt(int index) { return displays[index]; }
    }

#if CLOCK_DIAGNOSTIC_TESTS
    // A copy is exposed only to the isolated fake test assembly.
    internal sealed class ClockDiagnosticTestSnapshot
    {
        private readonly ClockDiagnosticFrameRecord[] frames;
        private readonly ClockDiagnosticDisplayRecord[] displays;

        internal ClockDiagnosticTestSnapshot(ClockDiagnosticFrameRecord[] frames,
            ClockDiagnosticDisplayRecord[] displays)
        {
            this.frames = frames;
            this.displays = displays;
        }

        internal int FrameCount { get { return frames.Length; } }
        internal int DisplayCount { get { return displays.Length; } }
        internal ClockDiagnosticFrameRecord FrameAt(int index) { return frames[index]; }
        internal ClockDiagnosticDisplayRecord DisplayAt(int index) { return displays[index]; }
    }
#endif

    internal sealed class ClockDiagnosticRecorder
    {
        private const int Limit = 8;
        private readonly object gate = new object();
        private readonly ClockDiagnosticFrameRecord[] frames = new ClockDiagnosticFrameRecord[Limit];
        private readonly ClockDiagnosticDisplayRecord[] displays = new ClockDiagnosticDisplayRecord[Limit];
        private volatile bool enabled;
        private long generation;
        private bool captureInProgress;
        private bool recordingFromAdapter;
        private int captureFrameCount;
        private int captureDisplayCount;
        private int invalidationRequested;
        private int frameCount;
        private int displayCount;
        private bool wasManuallyArmed;
        // Bound under gate before enabled becomes true and never rebound during this recorder's lifetime.
        private ClockDiagnosticPageLease ownerLease;

        // Baselines are temporary input needed to compute differences. They are not output records.
        private bool framePending;
        private uint previousFrame;
        private uint previousPhase;
        private long previousFrameDuration;
        private long previousFrameCounter;
        private int previousFrameThread;
        private bool displayBaseline;
        private uint previousReferenceFrame;
        private float previousReferenceTimer;
        private float previousDisplayHour;
        private long previousDisplayDuration;
        private long previousDisplayCounter;
        private int previousDisplayThread;

#if CLOCK_DIAGNOSTIC_TESTS
        private bool wasArmed;
        private bool armedForTest;
#endif

        internal ClockDiagnosticRecorder() { enabled = false; generation = 0; }
        internal bool Enabled { get { return enabled; } }

        private static bool Finite(float value)
        {
            return !Single.IsNaN(value) && !Single.IsInfinity(value);
        }

        private void ClearFrameBaseline()
        {
            framePending = false;
            previousFrame = 0;
            previousPhase = 0;
            previousFrameDuration = 0;
            previousFrameCounter = 0;
            previousFrameThread = 0;
        }

        private void ClearDisplayBaseline()
        {
            displayBaseline = false;
            previousReferenceFrame = 0;
            previousReferenceTimer = 0;
            previousDisplayHour = 0;
            previousDisplayDuration = 0;
            previousDisplayCounter = 0;
            previousDisplayThread = 0;
        }

        private void InvalidateBaselines()
        {
            ClearFrameBaseline();
            ClearDisplayBaseline();
        }

        private bool ConsumeInvalidation()
        {
            if (Interlocked.Exchange(ref invalidationRequested, 0) == 0) return false;
            InvalidateBaselines();
            return true;
        }

        private bool IsPotentialOwner(ClockDiagnosticPageLease lease)
        {
            ClockDiagnosticPageLease current = ownerLease;
            if (current != null) return Object.ReferenceEquals(current, lease) && !current.IsRevoked;
#if CLOCK_DIAGNOSTIC_TESTS
            return armedForTest && lease == null;
#else
            return false;
#endif
        }

        private bool EnterRecorder(ClockDiagnosticPageLease lease)
        {
            // A foreign lease must not poison the owner's pending sample, even on contention.
            if (!enabled || !IsPotentialOwner(lease)) return false;
            if (Monitor.TryEnter(gate)) return true;
            if (IsPotentialOwner(lease)) Interlocked.Exchange(ref invalidationRequested, 1);
            return false;
        }

        // The adapter retains this private gate while reading the supplied reference fields.
        // A competing attempt is refused, but its invalidation mark can arrive after an in-flight
        // record commits. An accepted record does not exclude overlapping callbacks or ABA.
        // No game lock is acquired.
        internal bool TryEnterCapture(ClockDiagnosticPageLease lease, long suppliedGeneration)
        {
            if (!EnterRecorder(lease)) return false;
            if (!IsPotentialOwner(lease)) { Monitor.Exit(gate); return false; }
            if (captureInProgress)
            {
                Interlocked.Exchange(ref invalidationRequested, 1);
                InvalidateBaselines();
                Monitor.Exit(gate);
                return false;
            }
            if (!enabled || !IsPotentialOwner(lease) || suppliedGeneration != generation || ConsumeInvalidation())
            {
                InvalidateBaselines();
                Monitor.Exit(gate);
                return false;
            }
            captureInProgress = true;
            captureFrameCount = frameCount;
            captureDisplayCount = displayCount;
            return true;
        }

        internal bool ExitCapture(ClockDiagnosticPageLease lease)
        {
            bool valid = Interlocked.Exchange(ref invalidationRequested, 0) == 0 && IsPotentialOwner(lease);
            if (!valid)
            {
                if (frameCount > captureFrameCount)
                {
                    frameCount = captureFrameCount;
                    frames[frameCount] = new ClockDiagnosticFrameRecord();
                }
                if (displayCount > captureDisplayCount)
                {
                    displayCount = captureDisplayCount;
                    displays[displayCount] = new ClockDiagnosticDisplayRecord();
                }
                InvalidateBaselines();
            }
            captureInProgress = false;
            Monitor.Exit(gate);
            return valid;
        }

        // These methods are called only while the adapter owns the diagnostic gate.
        internal bool MayReadBeforeFrame(ClockDiagnosticPageLease lease, long suppliedGeneration)
        {
            if (!enabled || !IsPotentialOwner(lease) || suppliedGeneration != generation || ConsumeInvalidation())
            {
                InvalidateBaselines();
                return false;
            }
            if (frameCount >= Limit) return false;
            if (!framePending) return true;
            ClearFrameBaseline();
            return false;
        }

        internal bool MayReadAfterFrame(ClockDiagnosticPageLease lease, long suppliedGeneration)
        {
            if (!enabled || !IsPotentialOwner(lease) || suppliedGeneration != generation || ConsumeInvalidation())
            {
                InvalidateBaselines();
                return false;
            }
            return frameCount < Limit && framePending;
        }

        internal bool MayReadDisplay(ClockDiagnosticPageLease lease, long suppliedGeneration)
        {
            if (!enabled || !IsPotentialOwner(lease) || suppliedGeneration != generation || ConsumeInvalidation())
            {
                InvalidateBaselines();
                return false;
            }
            return displayCount < Limit;
        }

        internal bool CaptureMayContinue(ClockDiagnosticPageLease lease)
        {
            return enabled && captureInProgress && IsPotentialOwner(lease);
        }

        internal void InvalidateCapture(ClockDiagnosticPageLease lease)
        {
            if (Object.ReferenceEquals(ownerLease, lease)
#if CLOCK_DIAGNOSTIC_TESTS
                || (armedForTest && lease == null)
#endif
                ) InvalidateBaselines();
        }

        private bool BeforeFrameLeased(ClockDiagnosticPageLease lease, long suppliedGeneration, uint frame, long durationTicks,
            uint phase, long counter, int threadId)
        {
            if (!EnterRecorder(lease)) return false;
            try
            {
                if (captureInProgress && !recordingFromAdapter)
                {
                    Interlocked.Exchange(ref invalidationRequested, 1);
                    InvalidateBaselines();
                    return false;
                }
                if (!enabled || !IsPotentialOwner(lease) || suppliedGeneration != generation || ConsumeInvalidation())
                {
                    InvalidateBaselines();
                    return false;
                }
                if (frameCount >= Limit) return false;
                if (framePending || durationTicks <= 0 || counter < 0 || threadId <= 0)
                {
                    ClearFrameBaseline();
                    return false;
                }
                previousFrame = frame;
                previousPhase = phase;
                previousFrameDuration = durationTicks;
                previousFrameCounter = counter;
                previousFrameThread = threadId;
                framePending = true;
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        private bool AfterFrameLeased(ClockDiagnosticPageLease lease, long suppliedGeneration, uint frame, long durationTicks,
            uint phase, long counter, int threadId)
        {
            if (!EnterRecorder(lease)) return false;
            try
            {
                if (captureInProgress && !recordingFromAdapter)
                {
                    Interlocked.Exchange(ref invalidationRequested, 1);
                    InvalidateBaselines();
                    return false;
                }
                if (!enabled || !IsPotentialOwner(lease) || suppliedGeneration != generation || ConsumeInvalidation())
                {
                    InvalidateBaselines();
                    return false;
                }
                if (frameCount >= Limit || !framePending) return false;
                if (threadId != previousFrameThread || durationTicks <= 0 || counter < previousFrameCounter
                    || previousFrame == UInt32.MaxValue || frame != previousFrame + 1)
                {
                    ClearFrameBaseline();
                    return false;
                }
                long phaseDifference = (long)phase - previousPhase;
                frames[frameCount++] = new ClockDiagnosticFrameRecord(1,
                    phaseDifference, durationTicks - previousFrameDuration,
                    counter - previousFrameCounter);
                ClearFrameBaseline();
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        private bool DisplayUpdateLeased(ClockDiagnosticPageLease lease, long suppliedGeneration, uint referenceFrame, float referenceTimer,
            float phaseHour, long durationTicks, long counter, int threadId)
        {
            if (!EnterRecorder(lease)) return false;
            try
            {
                if (captureInProgress && !recordingFromAdapter)
                {
                    Interlocked.Exchange(ref invalidationRequested, 1);
                    InvalidateBaselines();
                    return false;
                }
                if (!enabled || !IsPotentialOwner(lease) || suppliedGeneration != generation || ConsumeInvalidation())
                {
                    InvalidateBaselines();
                    return false;
                }
                if (displayCount >= Limit) return false;
                if (!Finite(referenceTimer) || !Finite(phaseHour) || phaseHour < 0 || phaseHour >= 24
                    || durationTicks <= 0 || counter < 0 || threadId <= 0)
                {
                    ClearDisplayBaseline();
                    return false;
                }
                if (!displayBaseline)
                {
                    displays[displayCount++] = new ClockDiagnosticDisplayRecord(false, 0, 0, 0, 0, 0);
                }
                else
                {
                    float timerDelta = referenceTimer - previousReferenceTimer;
                    float hourDelta = phaseHour - previousDisplayHour;
                    if (threadId != previousDisplayThread || referenceFrame < previousReferenceFrame
                        || counter < previousDisplayCounter || !Finite(timerDelta) || !Finite(hourDelta))
                    {
                        ClearDisplayBaseline();
                        return false;
                    }
                    displays[displayCount++] = new ClockDiagnosticDisplayRecord(true,
                        (long)referenceFrame - previousReferenceFrame, timerDelta,
                        hourDelta, durationTicks - previousDisplayDuration,
                        counter - previousDisplayCounter);
                }
                if (displayCount >= Limit) { ClearDisplayBaseline(); return true; }
                previousReferenceFrame = referenceFrame;
                previousReferenceTimer = referenceTimer;
                previousDisplayHour = phaseHour;
                previousDisplayDuration = durationTicks;
                previousDisplayCounter = counter;
                previousDisplayThread = threadId;
                displayBaseline = true;
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        // Called only by the adapter while it owns the diagnostic gate.
        internal bool RecordBeforeCaptured(ClockDiagnosticPageLease lease, long suppliedGeneration, uint frame, long durationTicks,
            uint phase, long counter, int threadId)
        {
            recordingFromAdapter = true;
            try { return BeforeFrameLeased(lease, suppliedGeneration, frame, durationTicks, phase, counter, threadId); }
            finally { recordingFromAdapter = false; }
        }

        internal bool RecordAfterCaptured(ClockDiagnosticPageLease lease, long suppliedGeneration, uint frame, long durationTicks,
            uint phase, long counter, int threadId)
        {
            recordingFromAdapter = true;
            try { return AfterFrameLeased(lease, suppliedGeneration, frame, durationTicks, phase, counter, threadId); }
            finally { recordingFromAdapter = false; }
        }

        internal bool RecordDisplayCaptured(ClockDiagnosticPageLease lease, long suppliedGeneration, uint referenceFrame, float referenceTimer,
            float phaseHour, long durationTicks, long counter, int threadId)
        {
            recordingFromAdapter = true;
            try { return DisplayUpdateLeased(lease, suppliedGeneration, referenceFrame, referenceTimer, phaseHour, durationTicks, counter, threadId); }
            finally { recordingFromAdapter = false; }
        }

#if CLOCK_DIAGNOSTIC_TESTS
        // These no-lease methods exist only for isolated fake tests that use ArmForTest.
        internal bool BeforeFrame(long suppliedGeneration, uint frame, long durationTicks,
            uint phase, long counter, int threadId)
        {
            return BeforeFrameLeased(null, suppliedGeneration, frame, durationTicks, phase, counter, threadId);
        }

        internal bool AfterFrame(long suppliedGeneration, uint frame, long durationTicks,
            uint phase, long counter, int threadId)
        {
            return AfterFrameLeased(null, suppliedGeneration, frame, durationTicks, phase, counter, threadId);
        }

        internal bool DisplayUpdate(long suppliedGeneration, uint referenceFrame, float referenceTimer,
            float phaseHour, long durationTicks, long counter, int threadId)
        {
            return DisplayUpdateLeased(null, suppliedGeneration, referenceFrame, referenceTimer,
                phaseHour, durationTicks, counter, threadId);
        }
#endif

        // The first successful non-blocking binding reserves this recorder for one controller.
        // It is a cooperative ownership check, not a security boundary within this process.
        internal bool TryBindManualLease(ClockDiagnosticPageLease lease)
        {
            if (lease == null || lease.IsRevoked || !Monitor.TryEnter(gate)) return false;
            try
            {
                if (lease.IsRevoked || enabled || wasManuallyArmed || captureInProgress || ownerLease != null) return false;
                ownerLease = lease;
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        // A caller must invoke this explicitly. The method name cannot authenticate a human action.
        // The one-shot instance retains its completed records and cannot be silently restarted.
        internal bool StartManual(long suppliedGeneration, ClockDiagnosticPageLease lease)
        {
            if (lease == null || lease.IsRevoked || suppliedGeneration <= 0 || !Monitor.TryEnter(gate)) return false;
            try
            {
                if (lease.IsRevoked || enabled || wasManuallyArmed || captureInProgress
                    || !Object.ReferenceEquals(ownerLease, lease)) return false;
#if CLOCK_DIAGNOSTIC_TESTS
                if (wasArmed) return false;
                wasArmed = true;
#endif
                generation = suppliedGeneration;
                wasManuallyArmed = true;
                enabled = true;
                if (lease.IsRevoked) { enabled = false; InvalidateBaselines(); return false; }
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        // False means stop is not confirmed, including busy, foreign, or revoked cases.
        // No automatic retry or later stop is scheduled.
        internal bool StopManual(ClockDiagnosticPageLease lease)
        {
            if (lease == null || lease.IsRevoked || !Monitor.TryEnter(gate)) return false;
            try
            {
                if (lease.IsRevoked) return false;
                if (!Object.ReferenceEquals(ownerLease, lease)) return false;
                if (captureInProgress) return false;
                enabled = false;
                InvalidateBaselines();
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        // A completed, bounded copy is available only after the one-shot capture has stopped.
        internal ClockDiagnosticReadOnlySnapshot ReadManualSnapshot(ClockDiagnosticPageLease lease)
        {
            if (lease == null || lease.IsRevoked || !Monitor.TryEnter(gate)) return null;
            try
            {
                if (lease.IsRevoked || !Object.ReferenceEquals(ownerLease, lease)
                    || !wasManuallyArmed || enabled || captureInProgress) return null;
                ClockDiagnosticFrameRecord[] frameCopy = new ClockDiagnosticFrameRecord[frameCount];
                ClockDiagnosticDisplayRecord[] displayCopy = new ClockDiagnosticDisplayRecord[displayCount];
                Array.Copy(frames, frameCopy, frameCount);
                Array.Copy(displays, displayCopy, displayCount);
                if (lease.IsRevoked) return null;
                return new ClockDiagnosticReadOnlySnapshot(frameCopy, displayCopy);
            }
            finally { Monitor.Exit(gate); }
        }

        // A foreign lease cannot stop its owner; it can confirm only that the recorder is idle.
        internal bool StopRevoked(ClockDiagnosticPageLease lease)
        {
            if (lease == null || !lease.IsRevoked || !Monitor.TryEnter(gate)) return false;
            try
            {
                if (captureInProgress) return false;
                if (ownerLease == null || !Object.ReferenceEquals(ownerLease, lease)) return !enabled;
                enabled = false;
                InvalidateBaselines();
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

#if CLOCK_DIAGNOSTIC_TESTS
        internal bool ArmForTest(long suppliedGeneration)
        {
            if (suppliedGeneration <= 0 || !Monitor.TryEnter(gate)) return false;
            try
            {
                if (enabled || wasArmed || wasManuallyArmed || captureInProgress || ownerLease != null) return false;
                generation = suppliedGeneration;
                wasArmed = true;
                wasManuallyArmed = true;
                armedForTest = true;
                enabled = true;
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        // False means busy. True means disabled and no adapter capture holds the gate.
        internal bool StopForTest()
        {
            if (!Monitor.TryEnter(gate)) return false;
            try
            {
                if (captureInProgress) return false;
                enabled = false;
                InvalidateBaselines();
                return true;
            }
            finally { Monitor.Exit(gate); }
        }

        internal ClockDiagnosticTestSnapshot SnapshotForTest()
        {
            if (!Monitor.TryEnter(gate)) return null;
            try
            {
                if (captureInProgress) return null;
                ClockDiagnosticFrameRecord[] frameCopy = new ClockDiagnosticFrameRecord[frameCount];
                ClockDiagnosticDisplayRecord[] displayCopy = new ClockDiagnosticDisplayRecord[displayCount];
                Array.Copy(frames, frameCopy, frameCount);
                Array.Copy(displays, displayCopy, displayCount);
                return new ClockDiagnosticTestSnapshot(frameCopy, displayCopy);
            }
            finally { Monitor.Exit(gate); }
        }
#endif
    }

    // Not an extension and never registered automatically. The caller must supply both existing
    // references and a local generation; neither authenticates a city or prevents an ABA race.
    internal sealed class ClockDiagnosticGameAdapter
    {
        private readonly ClockDiagnosticRecorder recorder;
        private readonly ClockDiagnosticPageLease lease;

        internal ClockDiagnosticGameAdapter(ClockDiagnosticRecorder recorder, ClockDiagnosticPageLease lease)
        {
            if (recorder == null) throw new ArgumentNullException("recorder");
            if (lease == null) throw new ArgumentNullException("lease");
            this.recorder = recorder;
            this.lease = lease;
        }

#if CLOCK_DIAGNOSTIC_TESTS
        // No-lease adapter exists only for an isolated fake recorder armed by ArmForTest.
        internal ClockDiagnosticGameAdapter(ClockDiagnosticRecorder recorder)
        {
            if (recorder == null) throw new ArgumentNullException("recorder");
            this.recorder = recorder;
            lease = null;
        }
#endif

        private bool Stable(LoadingManager loading)
        {
            if (loading == null || !recorder.CaptureMayContinue(lease)) return false;
            bool currentlyLoading = loading.m_currentlyLoading;
            if (!recorder.CaptureMayContinue(lease)) return false;
            bool loadingComplete = loading.m_loadingComplete;
            return !currentlyLoading && loadingComplete && recorder.CaptureMayContinue(lease);
        }

        internal bool BeforeFrame(long generation, SimulationManager simulation, LoadingManager loading)
        {
            if (!recorder.Enabled || !recorder.TryEnterCapture(lease, generation)) return false;
            bool accepted = false;
            try
            {
                if (!recorder.MayReadBeforeFrame(lease, generation)) return false;
                if (simulation == null || !Stable(loading)) { recorder.InvalidateCapture(lease); return false; }
                uint frame = simulation.m_currentFrameIndex;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                long duration = simulation.m_timePerFrame.Ticks;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                uint phase = simulation.m_dayTimeFrame;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                long counter = Stopwatch.GetTimestamp();
                int thread = Thread.CurrentThread.ManagedThreadId;
                if (!Stable(loading)) { recorder.InvalidateCapture(lease); return false; }
                accepted = recorder.RecordBeforeCaptured(lease, generation, frame, duration, phase, counter, thread);
            }
            catch (Exception) { recorder.InvalidateCapture(lease); }
            finally { if (!recorder.ExitCapture(lease)) accepted = false; }
            return accepted;
        }

        internal bool AfterFrame(long generation, SimulationManager simulation, LoadingManager loading)
        {
            if (!recorder.Enabled || !recorder.TryEnterCapture(lease, generation)) return false;
            bool accepted = false;
            try
            {
                if (!recorder.MayReadAfterFrame(lease, generation)) return false;
                if (simulation == null || !Stable(loading)) { recorder.InvalidateCapture(lease); return false; }
                uint frame = simulation.m_currentFrameIndex;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                long duration = simulation.m_timePerFrame.Ticks;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                uint phase = simulation.m_dayTimeFrame;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                long counter = Stopwatch.GetTimestamp();
                int thread = Thread.CurrentThread.ManagedThreadId;
                if (!Stable(loading)) { recorder.InvalidateCapture(lease); return false; }
                accepted = recorder.RecordAfterCaptured(lease, generation, frame, duration, phase, counter, thread);
            }
            catch (Exception) { recorder.InvalidateCapture(lease); }
            finally { if (!recorder.ExitCapture(lease)) accepted = false; }
            return accepted;
        }

        internal bool DisplayUpdate(long generation, SimulationManager simulation, LoadingManager loading)
        {
            if (!recorder.Enabled || !recorder.TryEnterCapture(lease, generation)) return false;
            bool accepted = false;
            try
            {
                if (!recorder.MayReadDisplay(lease, generation)) return false;
                if (simulation == null || !Stable(loading)) { recorder.InvalidateCapture(lease); return false; }
                uint referenceFrame = simulation.m_referenceFrameIndex;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                float referenceTimer = simulation.m_referenceTimer;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                float phaseHour = simulation.m_currentDayTimeHour;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                long duration = simulation.m_timePerFrame.Ticks;
                if (!recorder.CaptureMayContinue(lease)) { recorder.InvalidateCapture(lease); return false; }
                long counter = Stopwatch.GetTimestamp();
                int thread = Thread.CurrentThread.ManagedThreadId;
                if (!Stable(loading)) { recorder.InvalidateCapture(lease); return false; }
                accepted = recorder.RecordDisplayCaptured(lease, generation, referenceFrame, referenceTimer,
                    phaseHour, duration, counter, thread);
            }
            catch (Exception) { recorder.InvalidateCapture(lease); }
            finally { if (!recorder.ExitCapture(lease)) accepted = false; }
            return accepted;
        }
    }
}
