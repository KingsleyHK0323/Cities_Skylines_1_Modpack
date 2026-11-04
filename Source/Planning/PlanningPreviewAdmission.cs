using System;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Owns only admission state; no game API may run while the admission lock is held.
    // Signature: OpenAI-Codex_aptp_release_boundary.
    internal sealed class PlanningPreviewAdmission
    {
        private readonly object m_sync = new object();
        private volatile bool m_busy;
        private bool m_pending;
        private bool m_cancelPending;

        public bool IsBusy { get { return m_busy; } }

        public bool TryQueue(Func<bool> prepare)
        {
            lock (m_sync)
            {
                if (m_busy || m_pending || m_cancelPending) { return false; }
                if (!prepare()) { return false; }
                m_busy = true;
                m_pending = true;
                return true;
            }
        }

        public bool TryTake()
        {
            lock (m_sync)
            {
                if (m_cancelPending || !m_pending) { return false; }
                m_pending = false;
                return true;
            }
        }

        public void Cancel(Action invalidate)
        {
            lock (m_sync)
            {
                m_cancelPending = true;
                m_pending = false;
                invalidate();
            }
        }

        public bool TryResetCancelled(Action reset)
        {
            lock (m_sync)
            {
                if (!m_cancelPending) { return false; }
                reset();
                m_busy = false;
                m_cancelPending = false;
                return true;
            }
        }

        // Call only after the owning request is validated or from the simulation thread.
        // This does not take m_sync, preventing lock inversion with the worker ticket gate.
        public void Complete() { m_busy = false; }
    }
}
