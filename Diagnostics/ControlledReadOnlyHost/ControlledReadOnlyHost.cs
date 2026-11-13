// Signature: OpenAI-Codex_lifecycle_scope_review
// One cooperative, in-memory diagnostic session. This does not authenticate a city.
using System;
using System.Threading;

namespace RealisticCityTime.Diagnostics
{
    internal interface IExistingManagerSource
    {
        // Called only by an explicit Start attempt, never by an observation callback.
        bool TryGetExisting(out SimulationManager simulation, out LoadingManager loading);
    }

    internal enum ControlledHostState
    {
        Unavailable,
        Ready,
        Starting,
        Recording,
        StopPending,
        Stopped,
        RevokePending,
        Revoked
    }

    internal sealed class ControlledReadOnlyHost
    {
        private const long LocalGeneration = 1;
        private readonly IExistingManagerSource managers;
        private readonly ClockDiagnosticManualController controller;
        private readonly ClockDiagnosticGameAdapter adapter;
        private readonly object viewGate = new object();
        private int state = (int)ControlledHostState.Unavailable;
        private int terminal;
        private int acceptedLoad;
        // High bit closes admission permanently; the remaining bits count admitted work.
        private int admissions;
        private int viewEpoch;
        private int uiThreadId;
        private SimulationManager simulation;
        private LoadingManager loading;

        internal ControlledReadOnlyHost(IExistingManagerSource managers)
        {
            if (managers == null) throw new ArgumentNullException("managers");
            this.managers = managers;
            ClockDiagnosticRecorder recorder = new ClockDiagnosticRecorder();
            controller = new ClockDiagnosticManualController(recorder);
            adapter = controller.CreateAdapter();
        }

        private static int Read(ref int value)
        {
            return Interlocked.CompareExchange(ref value, 0, 0);
        }

        internal ControlledHostState State
        {
            get
            {
                int current = Read(ref state);
                if (Read(ref terminal) != 0 && current != (int)ControlledHostState.Revoked)
                    return ControlledHostState.RevokePending;
                return (ControlledHostState)current;
            }
        }

        // Counts manager discovery and outer callbacks, including work outside the recorder gate.
        internal int InFlightTickets { get { return Read(ref admissions) & Int32.MaxValue; } }

        private bool TryEnterTicket()
        {
            while (true)
            {
                int current = Read(ref admissions);
                if (current < 0 || current == Int32.MaxValue) return false;
                if (Interlocked.CompareExchange(ref admissions, current + 1, current) == current) return true;
            }
        }

        private void ExitTicket() { Interlocked.Decrement(ref admissions); }

        private void CloseAdmission()
        {
            while (true)
            {
                int current = Read(ref admissions);
                if (current < 0) return;
                if (Interlocked.CompareExchange(ref admissions, current | Int32.MinValue, current) == current) return;
            }
        }

        internal ControlledDiagnosticPresentation CreatePresentation()
        {
            return new ControlledDiagnosticPresentation(this, new ClockDiagnosticPanelPresenter(controller));
        }

        // Creating a replacement options page invalidates callbacks from every older page.
        internal int BeginView()
        {
            lock (viewGate)
            {
                int thread = Thread.CurrentThread.ManagedThreadId;
                if (uiThreadId == 0) uiThreadId = thread;
                if (thread != uiThreadId || viewEpoch == Int32.MaxValue)
                {
                    Revoke();
                    return 0;
                }
                return ++viewEpoch;
            }
        }

        private bool MatchesView(int view)
        {
            return view > 0 && viewEpoch == view && uiThreadId == Thread.CurrentThread.ManagedThreadId;
        }

        internal bool IsCurrentView(int view)
        {
            lock (viewGate) { return MatchesView(view); }
        }

        internal void OnLevelLoaded(bool gameplay)
        {
            if (Read(ref terminal) != 0) return;
            if (!gameplay)
            {
                if (Read(ref acceptedLoad) != 0) Revoke();
                return;
            }
            if (Interlocked.CompareExchange(ref acceptedLoad, 1, 0) != 0)
            {
                Revoke();
                return;
            }
            if (Read(ref terminal) == 0)
                Interlocked.CompareExchange(ref state, (int)ControlledHostState.Ready,
                    (int)ControlledHostState.Unavailable);
        }

        // A view token is a cooperative local control, not proof of a human or Unity thread.
        internal bool StartFromView(int view)
        {
            lock (viewGate)
            {
                if (!MatchesView(view) || Read(ref terminal) != 0 || Interlocked.CompareExchange(ref state,
                    (int)ControlledHostState.Starting, (int)ControlledHostState.Ready)
                    != (int)ControlledHostState.Ready) return false;
                if (!TryEnterTicket()) return false;
            }
            try { return StartWithTicket(view); }
            finally { ExitTicket(); }
        }

        private bool IsStartingFromView(int view)
        {
            lock (viewGate)
            {
                return MatchesView(view) && Read(ref terminal) == 0 && State == ControlledHostState.Starting;
            }
        }

        private bool StartWithTicket(int view)
        {
            SimulationManager candidateSimulation = null;
            LoadingManager candidateLoading = null;
            bool available = false;
            try
            {
                bool found = managers.TryGetExisting(out candidateSimulation, out candidateLoading);
                if (found && IsStartingFromView(view)
                    && candidateSimulation != null && candidateLoading != null
                    && IsStartingFromView(view))
                {
                    bool currentlyLoading = candidateLoading.m_currentlyLoading;
                    if (!currentlyLoading && IsStartingFromView(view))
                    {
                        bool loadingComplete = candidateLoading.m_loadingComplete;
                        available = loadingComplete && IsStartingFromView(view);
                    }
                }
            }
            catch (Exception) { available = false; }

            if (!available)
            {
                Interlocked.CompareExchange(ref state, (int)ControlledHostState.Ready,
                    (int)ControlledHostState.Starting);
                return false;
            }

            bool committed = false;
            bool revoke = false;
            lock (viewGate)
            {
                if (!MatchesView(view) || Read(ref terminal) != 0 || State != ControlledHostState.Starting
                    || !controller.StartFromManualAction(LocalGeneration))
                {
                    Interlocked.CompareExchange(ref state, (int)ControlledHostState.Ready,
                        (int)ControlledHostState.Starting);
                }
                else
                {
                    simulation = candidateSimulation;
                    loading = candidateLoading;
                    if (Read(ref terminal) != 0 || Interlocked.CompareExchange(ref state,
                        (int)ControlledHostState.Recording, (int)ControlledHostState.Starting)
                        != (int)ControlledHostState.Starting)
                    {
                        ClearManagerReferences();
                        revoke = true;
                    }
                    else committed = true;
                }
            }
            if (revoke) Revoke();
            if (!committed) return false;
            if (Read(ref terminal) != 0 || controller.IsRevoked)
            {
                Revoke();
                return false;
            }
            return true;
        }

        // Closing admission precedes the non-blocking stop. A false result is not confirmation.
        internal bool StopFromView(int view)
        {
            lock (viewGate)
            {
                if (!MatchesView(view) || Read(ref terminal) != 0) return false;
                if (State == ControlledHostState.Starting) { Revoke(); return false; }
                if (State == ControlledHostState.Recording)
                    Interlocked.CompareExchange(ref state, (int)ControlledHostState.StopPending,
                        (int)ControlledHostState.Recording);
                if (State == ControlledHostState.Stopped) return true;
                return RetryStop();
            }
        }

        // Automatic callers can only finish a Stop that a current view has already requested.
        internal bool RetryStop()
        {
            if (Read(ref terminal) != 0 || State != ControlledHostState.StopPending) return false;
            CloseAdmission();
            if (!controller.StopFromManualAction()) return false;
            if (Read(ref terminal) != 0 || InFlightTickets != 0) return false;
            return Interlocked.CompareExchange(ref state, (int)ControlledHostState.Stopped,
                (int)ControlledHostState.StopPending) == (int)ControlledHostState.StopPending;
        }

        internal bool CanRefreshFromView(int view)
        {
            lock (viewGate) { return MatchesView(view) && State == ControlledHostState.Stopped; }
        }

        internal ClockDiagnosticReadOnlySnapshot TryReadSnapshotFromView(int view)
        {
            lock (viewGate)
            {
                if (!MatchesView(view) || State != ControlledHostState.Stopped) return null;
                return controller.TryReadSnapshot();
            }
        }

        // Bounded managed snapshot and formatting only; no game lookup or UI call occurs here.
        internal string FormatSnapshotFromView(int view, ClockDiagnosticPanelPresenter presenter)
        {
            lock (viewGate)
            {
                if (!MatchesView(view) || State != ControlledHostState.Stopped || presenter == null) return null;
                return presenter.RefreshWithinPresentation();
            }
        }

        internal void OnLevelUnloading() { Revoke(); }
        internal void OnReleased() { Revoke(); }
        internal void RevokeAfterFailedView() { Revoke(); }

        // The lease latch is immediate, while successful quiescence remains a separate result.
        private void Revoke()
        {
            Interlocked.Exchange(ref terminal, 1);
            CloseAdmission();
            Interlocked.Exchange(ref state, (int)ControlledHostState.RevokePending);
            bool confirmed = controller.RevokeFromManualAction();
            ClearManagerReferences();
            if (confirmed && InFlightTickets == 0)
                Interlocked.CompareExchange(ref state, (int)ControlledHostState.Revoked,
                    (int)ControlledHostState.RevokePending);
        }

        internal bool RetryRevocation()
        {
            if (Read(ref terminal) == 0) return false;
            if (State == ControlledHostState.Revoked) return true;
            bool confirmed = controller.RevokeFromManualAction();
            if (!confirmed || InFlightTickets != 0) return false;
            ClearManagerReferences();
            Interlocked.CompareExchange(ref state, (int)ControlledHostState.Revoked,
                (int)ControlledHostState.RevokePending);
            return State == ControlledHostState.Revoked;
        }

        internal bool RetryRevocationFromView(int view)
        {
            lock (viewGate) { return MatchesView(view) && RetryRevocation(); }
        }

        private void ClearManagerReferences()
        {
            Interlocked.Exchange(ref simulation, null);
            Interlocked.Exchange(ref loading, null);
        }

        internal bool BeforeFrame() { return Capture(0); }
        internal bool AfterFrame() { return Capture(1); }
        internal bool DisplayUpdate() { return Capture(2); }

        private bool Capture(int kind)
        {
            if (State != ControlledHostState.Recording) return false;
            if (!TryEnterTicket()) return false;
            try
            {
                if (State != ControlledHostState.Recording) return false;
                SimulationManager s = simulation;
                LoadingManager l = loading;
                if (s == null || l == null || State != ControlledHostState.Recording) return false;
                if (kind == 0) return adapter.BeforeFrame(LocalGeneration, s, l);
                if (kind == 1) return adapter.AfterFrame(LocalGeneration, s, l);
                return adapter.DisplayUpdate(LocalGeneration, s, l);
            }
            finally { ExitTicket(); }
        }
    }

    // The caller never receives the presenter and therefore cannot use its Start or Stop methods.
    internal sealed class ControlledDiagnosticPresentation
    {
        private readonly ControlledReadOnlyHost host;
        private readonly ClockDiagnosticPanelPresenter presenter;

        internal ControlledDiagnosticPresentation(ControlledReadOnlyHost host, ClockDiagnosticPanelPresenter presenter)
        {
            this.host = host;
            this.presenter = presenter;
        }

        internal bool TryBegin(int view)
        {
            return host.IsCurrentView(view) && presenter.TryBeginViewPresentation();
        }

        internal bool CanContinue(int view)
        {
            return host.IsCurrentView(view) && presenter.CanContinueViewPresentation;
        }

        internal string Refresh(int view)
        {
            if (!host.CanRefreshFromView(view) || !CanContinue(view)) return null;
            string text = host.FormatSnapshotFromView(view, presenter);
            return host.CanRefreshFromView(view) && CanContinue(view) ? text : null;
        }

        internal void End() { presenter.EndViewPresentation(); }
    }
}
