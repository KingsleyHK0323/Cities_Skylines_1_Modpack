// Signature: OpenAI-Codex_rate_settings
using System;
using System.Globalization;

namespace RealisticCityTime.Game
{
    /// <summary>
    /// In-memory draft and explicit-submit boundary. No UI, game clock, or platform storage
    /// is attached. Cancel uses a snapshot of the effective value and provides no notification
    /// or synchronization for an independent caller changing settings directly afterward.
    /// </summary>
    internal sealed class ClockRateInput
    {
        private readonly object gate = new object();
        private readonly ClockRateSettings settings;
        private string draft;
        private object revision = new object();
        private bool submitting;

        internal ClockRateInput(ClockRateSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            this.settings = settings;
            draft = Format(settings.Multiplier);
        }

        internal string Draft { get { lock (gate) { return draft; } } }

        // Draft changes are local and never call the setting store.
        internal void Edit(string text)
        {
            object nextRevision = new object();
            lock (gate) { draft = text; revision = nextRevision; }
        }

        // The setting boundary is called outside the draft lock. Completion never rewrites the draft.
        internal ClockRateUpdateStatus Submit()
        {
            object startRevision = new object();
            object endRevision = new object();
            string submitted;
            lock (gate)
            {
                if (submitting) return ClockRateUpdateStatus.Busy;
                submitted = draft;
                submitting = true;
                revision = startRevision;
            }
            try { return settings.Apply(submitted); }
            finally
            {
                lock (gate) { submitting = false; revision = endRevision; }
            }
        }

        // Submit the caller's immutable snapshot without replacing any current or newer draft.
        internal ClockRateUpdateStatus SubmitText(string immutableText)
        {
            object startRevision = new object();
            object endRevision = new object();
            lock (gate)
            {
                if (submitting) return ClockRateUpdateStatus.Busy;
                submitting = true;
                revision = startRevision;
            }
            try { return settings.Apply(immutableText); }
            finally
            {
                lock (gate) { submitting = false; revision = endRevision; }
            }
        }

        // False means a submission or another draft change raced this attempt; no text was replaced.
        internal bool TryCancel()
        {
            object observedRevision;
            lock (gate)
            {
                if (submitting) return false;
                observedRevision = revision;
            }
            string restored = Format(settings.Multiplier);
            object nextRevision = new object();
            lock (gate)
            {
                if (submitting || !object.ReferenceEquals(revision, observedRevision)) return false;
                draft = restored;
                revision = nextRevision;
                return true;
            }
        }

        private static string Format(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
