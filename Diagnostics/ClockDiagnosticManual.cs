// Signature: OpenAI-Codex_diagnostic_impl
// Standalone managed control and presentation. No game extension or UI host is registered.
using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace RealisticCityTime.Diagnostics
{
    internal sealed class ClockDiagnosticManualController
    {
        private readonly ClockDiagnosticRecorder recorder;
        private readonly ClockDiagnosticPageLease lease = new ClockDiagnosticPageLease();
        private readonly bool ownsLease;
        private readonly object presentationGate = new object();
        private bool presentationInProgress;

        internal ClockDiagnosticManualController(ClockDiagnosticRecorder recorder)
        {
            if (recorder == null) throw new ArgumentNullException("recorder");
            this.recorder = recorder;
            ownsLease = recorder.TryBindManualLease(lease);
        }

        // These names identify intended caller actions; they do not authenticate a human or a city.
        internal bool StartFromManualAction(long generation) { return ownsLease && recorder.StartManual(generation, lease); }
        internal bool StopFromManualAction() { return ownsLease && recorder.StopManual(lease); }
        internal ClockDiagnosticReadOnlySnapshot TryReadSnapshot() { return ownsLease ? recorder.ReadManualSnapshot(lease) : null; }
        internal ClockDiagnosticGameAdapter CreateAdapter() { return new ClockDiagnosticGameAdapter(recorder, lease); }

        internal bool IsRevoked { get { return lease.IsRevoked; } }

        // The latch is permanent even when either non-blocking confirmation is busy.
        internal bool RevokeFromManualAction()
        {
            lease.Revoke();
            if (!Monitor.TryEnter(presentationGate)) return false;
            try
            {
                if (presentationInProgress) return false;
                return recorder.StopRevoked(lease);
            }
            finally { Monitor.Exit(presentationGate); }
        }

        // Shared by all presenters and views using this controller. No recorder gate is held
        // while a caller touches UI, and another thread never waits on this gate.
        internal bool TryEnterPresentation()
        {
            if (!ownsLease || lease.IsRevoked || !Monitor.TryEnter(presentationGate)) return false;
            if (lease.IsRevoked || presentationInProgress)
            {
                Monitor.Exit(presentationGate);
                return false;
            }
            presentationInProgress = true;
            return true;
        }

        internal bool IsPresentationOwnedByCurrentThread()
        {
            if (!Monitor.TryEnter(presentationGate)) return false;
            try { return presentationInProgress; }
            finally { Monitor.Exit(presentationGate); }
        }

        internal void ExitPresentation()
        {
            presentationInProgress = false;
            Monitor.Exit(presentationGate);
        }
    }

    // The caller supplies the UI and is responsible for calling on its own UI thread.
    // This object only compares managed thread IDs. IDs can be reused and do not authenticate
    // the actual thread or establish that it is the Unity UI thread.
    internal sealed class ClockDiagnosticPanelPresenter
    {
        private readonly ClockDiagnosticManualController controller;
        private readonly int ownerThread;

        internal ClockDiagnosticPanelPresenter(ClockDiagnosticManualController controller)
        {
            if (controller == null) throw new ArgumentNullException("controller");
            this.controller = controller;
            ownerThread = Thread.CurrentThread.ManagedThreadId;
        }

        private bool IsOwnerThread { get { return Thread.CurrentThread.ManagedThreadId == ownerThread; } }

        internal bool StartClicked(long generation)
        {
            return IsOwnerThread && controller.StartFromManualAction(generation);
        }

        internal bool StopClicked()
        {
            return IsOwnerThread && controller.StopFromManualAction();
        }

        internal string RefreshClicked()
        {
            if (!TryBeginViewPresentation()) return null;
            try { return RefreshWithinPresentation(); }
            finally { EndViewPresentation(); }
        }

        internal bool TryBeginViewPresentation()
        {
            return IsOwnerThread && controller.TryEnterPresentation();
        }

        internal void EndViewPresentation() { controller.ExitPresentation(); }

        internal bool CanContinueViewPresentation
        {
            get { return IsOwnerThread && !controller.IsRevoked && controller.IsPresentationOwnedByCurrentThread(); }
        }

        // Host invalidation is not game unload detection and can be called without touching UI.
        internal bool RevokeFromHost() { return controller.RevokeFromManualAction(); }

        internal string RefreshWithinPresentation()
        {
            if (!CanContinueViewPresentation) return null;
            ClockDiagnosticReadOnlySnapshot snapshot = controller.TryReadSnapshot();
            if (!CanContinueViewPresentation) return null;
            if (snapshot == null) return "Capture not started, still active, or busy; stop and retry.";

            StringBuilder text = new StringBuilder();
            text.AppendLine("Endpoint differences are game state; a future UI, host, or extension may retain or transmit copies.");
            text.AppendLine("LifecycleContinuityVerified=False CityIdentityVerified=False ConcurrentCallbacksExcluded=False PhaseMayWrapOrJump=True");
            text.AppendLine("ElapsedCounterTicks are raw Stopwatch counter differences, not seconds; Frequency is not recorded.");
            for (int i = 0; i < snapshot.FrameCount; i++)
            {
                ClockDiagnosticFrameRecord frame = snapshot.FrameAt(i);
                text.Append("Frame[").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] ");
                text.Append("FrameDelta=").Append(frame.FrameDelta.ToString(CultureInfo.InvariantCulture));
                text.Append(" PhaseDelta=").Append(frame.PhaseDelta.ToString(CultureInfo.InvariantCulture));
                text.Append(" PerFrameDeltaTicks=").Append(frame.PerFrameDeltaTicks.ToString(CultureInfo.InvariantCulture));
                text.Append(" ElapsedCounterTicks=").Append(frame.ElapsedCounterTicks.ToString(CultureInfo.InvariantCulture));
                text.Append(" PhaseMayWrapOrJump=").Append(frame.PhaseMayWrapOrJump);
                text.Append(" LifecycleContinuityVerified=").Append(frame.LifecycleContinuityVerified);
                text.Append(" CityIdentityVerified=").Append(frame.CityIdentityVerified);
                text.Append(" ConcurrentCallbacksExcluded=").Append(frame.ConcurrentCallbacksExcluded).AppendLine();
            }
            for (int i = 0; i < snapshot.DisplayCount; i++)
            {
                ClockDiagnosticDisplayRecord display = snapshot.DisplayAt(i);
                text.Append("Display[").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] ");
                text.Append("HasDelta=").Append(display.HasDelta);
                text.Append(" ReferenceFrameDelta=").Append(display.ReferenceFrameDelta.ToString(CultureInfo.InvariantCulture));
                text.Append(" ReferenceTimerDelta=").Append(display.ReferenceTimerDelta.ToString("R", CultureInfo.InvariantCulture));
                text.Append(" PhaseHourDelta=").Append(display.PhaseHourDelta.ToString("R", CultureInfo.InvariantCulture));
                text.Append(" PerFrameDeltaTicks=").Append(display.PerFrameDeltaTicks.ToString(CultureInfo.InvariantCulture));
                text.Append(" ElapsedCounterTicks=").Append(display.ElapsedCounterTicks.ToString(CultureInfo.InvariantCulture));
                text.Append(" PhaseMayWrapOrJump=").Append(display.PhaseMayWrapOrJump);
                text.Append(" LifecycleContinuityVerified=").Append(display.LifecycleContinuityVerified);
                text.Append(" CityIdentityVerified=").Append(display.CityIdentityVerified);
                text.Append(" ConcurrentCallbacksExcluded=").Append(display.ConcurrentCallbacksExcluded).AppendLine();
            }
            string result = text.ToString();
            return CanContinueViewPresentation ? result : null;
        }
    }
}
