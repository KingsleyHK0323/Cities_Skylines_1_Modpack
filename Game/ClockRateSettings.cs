// Signature: OpenAI-Codex_rate_settings
using System;
using System.Globalization;

namespace RealisticCityTime.Game
{
    // This interface is a boundary only; no game, file, or platform store is attached here.
    // A conforming provider must perform no writes in Read, return null only for confirmed
    // absence, and report read errors or undecodable content distinctly. No real provider has
    // been verified against this contract. Completed means the operation has terminated and
    // cannot write later; it does not itself prove that data is durable. Unknown includes
    // unfinished, unverified, and unrecognized outcomes.
    internal enum ClockRateStoreWriteStatus { Unknown = 0, Completed = 1 }

    internal interface IClockRateStore
    {
        string Read();
        ClockRateStoreWriteStatus Write(string value);
    }

    internal enum ClockRateLoadStatus { Loaded, Missing, InvalidStoredValue, StorageFailure, Busy, Quarantined }
    internal enum ClockRateUpdateStatus { Applied, Unchanged, InvalidInput, StorageFailure, Busy, ReviewRequired, WriteUnknown }

    /// <summary>
    /// Provisional nonlocalized rate input and persistence boundary. The instance starts at 1x;
    /// loading is required before a store write, and only an explicit valid value change writes.
    /// This is a single-instance boundary; it cannot verify other writers, original bytes, durable
    /// storage, or an ambiguous write. A write exception or unknown outcome quarantines this instance;
    /// another instance is not proof that an earlier unresolved write has terminated.
    /// It does not control a game clock or define a save format.
    /// </summary>
    internal sealed class ClockRateSettings
    {
        private readonly object gate = new object();
        private readonly IClockRateStore store;
        private decimal multiplier = 1m;
        private bool busy;
        private bool canWrite;
        private bool quarantined;

        internal ClockRateSettings(IClockRateStore store)
        {
            if (store == null) throw new ArgumentNullException("store");
            this.store = store;
        }

        internal decimal Multiplier { get { lock (gate) { return multiplier; } } }
        internal bool NeedsReview { get { lock (gate) { return !canWrite; } } }
        internal bool IsQuarantined { get { lock (gate) { return quarantined; } } }

        // This boundary does not call Write for missing, malformed, or failed reads.
        // Whether a provider's Read is truly side-effect free depends on its own contract.
        internal ClockRateLoadStatus Load()
        {
            lock (gate)
            {
                if (busy) return ClockRateLoadStatus.Busy;
                if (quarantined) return ClockRateLoadStatus.Quarantined;
                busy = true;
                canWrite = false;
            }
            try
            {
                string text;
                try { text = store.Read(); }
                catch (Exception) { return ClockRateLoadStatus.StorageFailure; }
                if (text == null)
                {
                    lock (gate) { canWrite = true; }
                    return ClockRateLoadStatus.Missing;
                }
                decimal value;
                if (!TryParse(text, out value)) return ClockRateLoadStatus.InvalidStoredValue;
                lock (gate) { multiplier = value; canWrite = true; }
                return ClockRateLoadStatus.Loaded;
            }
            finally { lock (gate) { busy = false; } }
        }

        // A failed store may already have changed its backing value; only memory is guaranteed unchanged.
        // Applied means Write reported a terminated operation and this instance adopted the value,
        // not that the value was made durable. An unrecognized result is treated as unknown.
        internal ClockRateUpdateStatus Apply(string text)
        {
            decimal value;
            if (!TryParse(text, out value)) return ClockRateUpdateStatus.InvalidInput;
            lock (gate)
            {
                if (busy) return ClockRateUpdateStatus.Busy;
                if (!canWrite) return ClockRateUpdateStatus.ReviewRequired;
                if (multiplier == value) return ClockRateUpdateStatus.Unchanged;
                busy = true;
                canWrite = false;
            }
            bool writeStarted = false;
            bool adopted = false;
            try
            {
                string stored = value.ToString("0.00", CultureInfo.InvariantCulture);
                ClockRateStoreWriteStatus outcome;
                try { writeStarted = true; outcome = store.Write(stored); }
                catch (Exception) { return ClockRateUpdateStatus.StorageFailure; }
                if (outcome != ClockRateStoreWriteStatus.Completed) return ClockRateUpdateStatus.WriteUnknown;
                lock (gate) { multiplier = value; canWrite = true; adopted = true; }
                return ClockRateUpdateStatus.Applied;
            }
            finally
            {
                lock (gate)
                {
                    if (writeStarted && !adopted) { quarantined = true; canWrite = false; }
                    busy = false;
                }
            }
        }

        // The provisional policy limits literal fractional digits, including trailing zeros.
        internal static bool TryParse(string text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrEmpty(text)) return false;
            int point = -1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.' && point < 0) { point = i; continue; }
                if (c < '0' || c > '9') return false;
            }
            if (point == 0 || point == text.Length - 1 || (point >= 0 && text.Length - point - 1 > 2))
                return false;
            decimal parsed;
            if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsed)
                || parsed < 1m || parsed > 10m) return false;
            value = parsed;
            return true;
        }
    }
}
