// Signature: OpenAI-Codex_Realistic_Railway_Traffic_Control
using System;
using System.Runtime.CompilerServices;
using RealisticRailwayTrafficControl.Model;

[assembly: InternalsVisibleTo("RailwayModelChecks")]

namespace RealisticRailwayTrafficControl.Observation
{
    internal enum ObservationState { Disabled, NoSnapshot, Pending, SnapshotReceived }
    internal enum CaptureStartResult { Started, Inactive, Busy, InvalidScope, CounterExhausted }
    internal enum CaptureCompletion { Accepted, Stale, InvalidCapture }

    internal sealed class CaptureTicket
    {
        public long CityEpoch { get; private set; }
        public long CaptureSerial { get; private set; }
        public string TopologyToken { get; private set; }

        internal CaptureTicket(long epoch, long serial, string topologyToken)
        { CityEpoch = epoch; CaptureSerial = serial; TopologyToken = topologyToken; }
    }

    // Local callback lifetime only: no native game generation, occupancy acquisition, or game calls.
    // A future verified host must explicitly forward lifecycle/topology events and schedule reads.
    internal sealed class RailwayObservationSession
    {
        private readonly object sync = new object();
        private bool active;
        private long cityEpoch;
        private long captureSerial;
        private CaptureTicket pending;
        private RailwaySnapshot current;

        public ObservationState State
        {
            get
            {
                lock (sync)
                {
                    if (!active) return ObservationState.Disabled;
                    if (pending != null) return ObservationState.Pending;
                    return current == null ? ObservationState.NoSnapshot : ObservationState.SnapshotReceived;
                }
            }
        }

        public RailwaySnapshot CurrentSnapshot { get { lock (sync) { return current; } } }

        // Activation is explicit, not automatic game discovery. Epochs are local to this instance.
        public bool ActivateCity()
        {
            lock (sync)
            {
                pending = null;
                current = null;
                if (cityEpoch == Int64.MaxValue)
                {
                    active = false;
                    return false;
                }
                cityEpoch++;
                active = true;
                return true;
            }
        }

        public CaptureStartResult TryBeginCapture(string topologyToken, out CaptureTicket ticket)
        {
            lock (sync)
            {
                ticket = null;
                if (!active) return CaptureStartResult.Inactive;
                if (pending != null) return CaptureStartResult.Busy;
                if (topologyToken == null || topologyToken.Trim().Length == 0) return CaptureStartResult.InvalidScope;
                if (captureSerial == Int64.MaxValue) return CaptureStartResult.CounterExhausted;
                captureSerial++;
                ticket = new CaptureTicket(cityEpoch, captureSerial, topologyToken);
                pending = ticket;
                current = null;
                return CaptureStartResult.Started;
            }
        }

        // Accepted means matching delivery, never complete evidence or permission to move a train.
        public CaptureCompletion TryComplete(CaptureTicket ticket, RailwaySnapshot snapshot)
        {
            lock (sync)
            {
                if (!active || pending == null || !Object.ReferenceEquals(ticket, pending))
                    return CaptureCompletion.Stale;
                pending = null;
                current = null;
                if (snapshot == null || snapshot.CityEpoch != ticket.CityEpoch
                    || snapshot.CaptureSerial != ticket.CaptureSerial || snapshot.CapturedFrame < 0
                    || !String.Equals(snapshot.TopologyToken, ticket.TopologyToken, StringComparison.Ordinal))
                    return CaptureCompletion.InvalidCapture;
                current = snapshot;
                return CaptureCompletion.Accepted;
            }
        }

        public void Invalidate()
        {
            lock (sync) { pending = null; current = null; }
        }

        public void Disable()
        {
            lock (sync) { active = false; pending = null; current = null; }
        }
    }
}
