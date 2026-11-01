using System;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    internal sealed class RouteEditRequestTicket { }

    internal sealed class RouteEditRequestGate
    {
        private readonly object m_sync = new object();
        private RouteEditRequestTicket m_active;
        public bool IsBusy { get { lock (m_sync) { return m_active != null; } } }

        public bool TryBegin(out RouteEditRequestTicket ticket)
        {
            lock (m_sync)
            {
                ticket = null;
                if (m_active != null) { return false; }
                ticket = m_active = new RouteEditRequestTicket();
                return true;
            }
        }

        public bool IsOwner(RouteEditRequestTicket ticket)
        {
            lock (m_sync) { return ticket != null && object.ReferenceEquals(m_active, ticket); }
        }

        // Hold ownership across only short state publications, never game API calls.
        public bool TryPublish(RouteEditRequestTicket ticket, Action action)
        {
            lock (m_sync)
            {
                if (ticket == null || !object.ReferenceEquals(m_active, ticket)) { return false; }
                action();
                return true;
            }
        }

        public bool Complete(RouteEditRequestTicket ticket)
        {
            lock (m_sync)
            {
                if (ticket == null || !object.ReferenceEquals(m_active, ticket)) { return false; }
                m_active = null;
                return true;
            }
        }

        // Signature: OpenAI-Codex_Automated_Public_Transport_Planning
        // Work may call native APIs; only short error/cleanup publications hold the lock.
        public void RunOwned(RouteEditRequestTicket ticket, Action work, Action<Exception> failed, Action finished)
        {
            try
            {
                if (IsOwner(ticket)) { work(); }
            }
            catch (Exception error)
            {
                TryPublish(ticket, delegate { failed(error); });
            }
            finally
            {
                TryPublish(ticket, finished);
            }
        }

        public void Reset(Action clearState)
        {
            lock (m_sync) { m_active = null; clearState(); }
        }
    }
}
