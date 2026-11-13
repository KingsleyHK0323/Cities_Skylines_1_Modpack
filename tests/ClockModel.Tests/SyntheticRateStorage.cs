// Signature: OpenAI-Codex_rate_transaction
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace RealisticCityTime.Tests
{
    // All capabilities in this file are in-memory test assumptions, not file or game APIs.
    internal enum SyntheticReadKind { Found, Missing, Unavailable }
    internal enum SyntheticCommitKind { Committed, Conflict, NotCommitted, Unknown, LeaseExpired }
    internal enum SyntheticCommitFault { None, NotCommitted, UnknownBeforeWithoutReceipt, UnknownBeforeWithReceipt, UnknownAfterCommit }
    internal enum SyntheticSessionState { Unloaded, Ready, Missing, NeedsMigration, Corrupt, FutureVersion, Unavailable, Pending, Unknown, Conflict, ChangedAfterCommit, NeedsReload }
    internal enum SyntheticStageFault { None, CreateFailure, WriteFailure, VerifyFailure }

    internal sealed class SyntheticVersion
    {
        internal readonly SyntheticRateStorage Store;
        internal readonly string Scope;
        internal readonly long Revision;

        internal SyntheticVersion(SyntheticRateStorage store, string scope, long revision)
        {
            Store = store; Scope = scope; Revision = revision;
        }

        internal bool SameAs(SyntheticVersion other)
        {
            return other != null && object.ReferenceEquals(Store, other.Store)
                && string.Equals(Scope, other.Scope, StringComparison.Ordinal) && Revision == other.Revision;
        }
    }

    internal sealed class SyntheticReadResult
    {
        private readonly byte[] raw;
        internal readonly SyntheticReadKind Kind;
        internal readonly SyntheticVersion Version;
        internal byte[] Raw { get { return raw == null ? null : (byte[])raw.Clone(); } }

        internal SyntheticReadResult(SyntheticReadKind kind, SyntheticVersion version, byte[] data)
        {
            Kind = kind; Version = version; raw = data == null ? null : (byte[])data.Clone();
        }
    }

    internal sealed class SyntheticLease
    {
        internal readonly SyntheticRateStorage Store;
        internal readonly object Owner;
        internal readonly string Scope;

        internal SyntheticLease(SyntheticRateStorage store, object owner, string scope)
        {
            Store = store; Owner = owner; Scope = scope;
        }
    }

    internal sealed class SyntheticReceipt
    {
        private readonly byte[] raw;
        internal readonly SyntheticCommitKind Kind;
        internal readonly string Scope;
        internal readonly string Operation;
        internal readonly object Owner;
        internal readonly SyntheticVersion Expected;
        internal readonly SyntheticVersion Version;
        internal byte[] Raw { get { return raw == null ? null : (byte[])raw.Clone(); } }

        internal SyntheticReceipt(SyntheticCommitKind kind, string scope, string operation, object owner,
            SyntheticVersion expected, SyntheticVersion version, byte[] data)
        {
            Kind = kind; Scope = scope; Operation = operation; Owner = owner; Expected = expected; Version = version;
            raw = data == null ? null : (byte[])data.Clone();
        }
    }

    internal sealed class SyntheticCommitResult
    {
        internal readonly SyntheticCommitKind Kind;
        internal readonly SyntheticLease Lease;
        internal readonly string Operation;
        internal readonly SyntheticVersion Version;

        internal SyntheticCommitResult(SyntheticCommitKind kind, SyntheticLease lease,
            string operation, SyntheticVersion version)
        {
            Kind = kind; Lease = lease; Operation = operation; Version = version;
        }
    }

    /// <summary>
    /// Fake store with one lock, scoped monotonic versions and durable in-memory receipts.
    /// ExternalSet/Delete model other writers or setup, not a real storage implementation.
    /// </summary>
    internal sealed class SyntheticRateStorage
    {
        private sealed class Slot { internal long Revision; internal byte[] Raw; }
        private sealed class Operation
        {
            internal readonly string Scope;
            internal readonly SyntheticVersion Expected;
            internal readonly byte[] Raw;
            internal Operation(string scope, SyntheticVersion expected, byte[] raw)
            {
                Scope = scope; Expected = expected; Raw = (byte[])raw.Clone();
            }
        }
        private readonly object gate = new object();
        private readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>(StringComparer.Ordinal);
        private readonly Dictionary<object, SyntheticLease> leases = new Dictionary<object, SyntheticLease>();
        private readonly Dictionary<string, SyntheticReceipt> receipts = new Dictionary<string, SyntheticReceipt>(StringComparer.Ordinal);
        private readonly Dictionary<string, Operation> operations = new Dictionary<string, Operation>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> fileDirectories = new Dictionary<string, string>(StringComparer.Ordinal);
        private int commitAttempts;
        private bool readUnavailable;
        internal ManualResetEvent CommitEntered;
        internal ManualResetEvent ContinueCommit;
        internal volatile bool CommitWaitTimedOut;

        internal int CommitAttempts { get { lock (gate) { return commitAttempts; } } }
        internal bool ReadUnavailable { get { lock (gate) { return readUnavailable; } } set { lock (gate) { readUnavailable = value; } } }

        // Fixture setup binds opaque identifiers; it does not select or access a real path.
        internal void BindFileDirectory(string scope, string directory)
        {
            if (string.IsNullOrEmpty(scope)) throw new ArgumentException("Scope is required.", "scope");
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Directory identity is required.", "directory");
            lock (gate)
            {
                string existing;
                if (fileDirectories.TryGetValue(scope, out existing))
                {
                    if (!string.Equals(existing, directory, StringComparison.Ordinal))
                        throw new InvalidOperationException("Scope is already bound to a different fake directory.");
                    return;
                }
                fileDirectories.Add(scope, directory);
            }
        }

        internal bool IsBoundFileDirectory(string scope, string directory)
        {
            lock (gate)
            {
                string existing;
                return scope != null && directory != null && fileDirectories.TryGetValue(scope, out existing)
                    && string.Equals(existing, directory, StringComparison.Ordinal);
            }
        }

        internal SyntheticLease BeginLease(object owner, string scope)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            if (string.IsNullOrEmpty(scope)) throw new ArgumentException("Scope is required.", "scope");
            lock (gate)
            {
                SyntheticLease next = new SyntheticLease(this, owner, scope);
                leases[owner] = next;
                return next;
            }
        }

        internal SyntheticReadResult Read(string scope)
        {
            if (string.IsNullOrEmpty(scope)) throw new ArgumentException("Scope is required.", "scope");
            lock (gate)
            {
                if (readUnavailable) return new SyntheticReadResult(SyntheticReadKind.Unavailable, null, null);
                Slot slot;
                if (!slots.TryGetValue(scope, out slot))
                    return new SyntheticReadResult(SyntheticReadKind.Missing, new SyntheticVersion(this, scope, 0), null);
                SyntheticVersion version = new SyntheticVersion(this, scope, slot.Revision);
                return slot.Raw == null
                    ? new SyntheticReadResult(SyntheticReadKind.Missing, version, null)
                    : new SyntheticReadResult(SyntheticReadKind.Found, version, slot.Raw);
            }
        }

        internal void ExternalSet(string scope, byte[] raw)
        {
            if (string.IsNullOrEmpty(scope)) throw new ArgumentException("Scope is required.", "scope");
            if (raw == null) throw new ArgumentNullException("raw");
            byte[] copied = (byte[])raw.Clone();
            lock (gate)
            {
                Slot slot = GetOrCreate(scope);
                long next = checked(slot.Revision + 1);
                slot.Raw = copied; slot.Revision = next;
            }
        }

        internal void ExternalDelete(string scope)
        {
            if (string.IsNullOrEmpty(scope)) throw new ArgumentException("Scope is required.", "scope");
            lock (gate)
            {
                Slot slot = GetOrCreate(scope);
                long next = checked(slot.Revision + 1);
                slot.Raw = null; slot.Revision = next;
            }
        }

        internal SyntheticCommitResult TryCommit(SyntheticLease lease, SyntheticVersion expected,
            string operation, byte[] raw, SyntheticCommitFault fault)
        {
            if (string.IsNullOrEmpty(operation)) throw new ArgumentException("Operation is required.", "operation");
            if (raw == null) throw new ArgumentNullException("raw");
            byte[] copied = (byte[])raw.Clone();
            if (CommitEntered != null)
            {
                CommitEntered.Set();
                if (!ContinueCommit.WaitOne(5000))
                {
                    CommitWaitTimedOut = true;
                    throw new InvalidOperationException("Synthetic commit barrier timed out.");
                }
            }
            lock (gate)
            {
                commitAttempts = checked(commitAttempts + 1);
                SyntheticLease currentLease;
                if (lease == null || !object.ReferenceEquals(lease.Store, this)
                    || !leases.TryGetValue(lease.Owner, out currentLease) || !object.ReferenceEquals(currentLease, lease))
                {
                    if (lease != null && object.ReferenceEquals(lease.Store, this) && !operations.ContainsKey(operation))
                    {
                        operations.Add(operation, new Operation(lease.Scope, expected, copied));
                        receipts.Add(operation, new SyntheticReceipt(SyntheticCommitKind.NotCommitted,
                            lease.Scope, operation, lease.Owner, expected, null, copied));
                    }
                    return new SyntheticCommitResult(SyntheticCommitKind.LeaseExpired, lease, operation, null);
                }
                Slot slot;
                bool found = slots.TryGetValue(lease.Scope, out slot);
                long actualRevision = found ? slot.Revision : 0;
                SyntheticVersion actual = new SyntheticVersion(this, lease.Scope, actualRevision);
                if (operations.ContainsKey(operation))
                    return new SyntheticCommitResult(SyntheticCommitKind.Conflict, lease, operation, null);
                operations.Add(operation, new Operation(lease.Scope, expected, copied));
                if (expected == null || !expected.SameAs(actual))
                {
                    receipts.Add(operation, new SyntheticReceipt(SyntheticCommitKind.NotCommitted,
                        lease.Scope, operation, lease.Owner, expected, null, copied));
                    return new SyntheticCommitResult(SyntheticCommitKind.Conflict, lease, operation, null);
                }
                if (fault == SyntheticCommitFault.UnknownBeforeWithoutReceipt)
                    return new SyntheticCommitResult(SyntheticCommitKind.Unknown, lease, operation, null);
                if (fault == SyntheticCommitFault.NotCommitted || fault == SyntheticCommitFault.UnknownBeforeWithReceipt)
                {
                    SyntheticReceipt notCommitted = new SyntheticReceipt(SyntheticCommitKind.NotCommitted,
                        lease.Scope, operation, lease.Owner, expected, null, copied);
                    receipts.Add(operation, notCommitted);
                    return new SyntheticCommitResult(fault == SyntheticCommitFault.NotCommitted
                        ? SyntheticCommitKind.NotCommitted : SyntheticCommitKind.Unknown, lease, operation, null);
                }
                if (!found) slot = GetOrCreate(lease.Scope);
                long nextRevision = checked(slot.Revision + 1);
                SyntheticVersion nextVersion = new SyntheticVersion(this, lease.Scope, nextRevision);
                SyntheticReceipt committed = new SyntheticReceipt(SyntheticCommitKind.Committed,
                    lease.Scope, operation, lease.Owner, expected, nextVersion, copied);
                receipts.Add(operation, committed);
                slot.Raw = copied; slot.Revision = nextRevision;
                return new SyntheticCommitResult(fault == SyntheticCommitFault.UnknownAfterCommit
                    ? SyntheticCommitKind.Unknown : SyntheticCommitKind.Committed, lease, operation,
                    fault == SyntheticCommitFault.UnknownAfterCommit ? null : nextVersion);
            }
        }

        internal SyntheticReceipt Resolve(string scope, string operation, object owner)
        {
            lock (gate)
            {
                SyntheticReceipt receipt;
                if (scope == null || operation == null || !receipts.TryGetValue(operation, out receipt)
                    || !object.ReferenceEquals(owner, receipt.Owner)
                    || !string.Equals(scope, receipt.Scope, StringComparison.Ordinal)) return null;
                return receipt;
            }
        }

        private Slot GetOrCreate(string scope)
        {
            Slot slot;
            if (!slots.TryGetValue(scope, out slot)) { slot = new Slot(); slots.Add(scope, slot); }
            return slot;
        }
    }

    internal sealed class SyntheticReadTicket
    {
        internal readonly SyntheticRateSession Owner;
        internal readonly long Epoch;
        internal readonly long Request;
        internal readonly string Scope;
        internal readonly SyntheticReadResult Result;
        internal SyntheticReadTicket(SyntheticRateSession owner, long epoch, long request, string scope, SyntheticReadResult result)
        {
            Owner = owner; Epoch = epoch; Request = request; Scope = scope; Result = result;
        }
    }

    internal sealed class SyntheticSavePlan
    {
        private readonly object planGate = new object();
        private readonly byte[] payload;
        private bool executed;
        private object stagingOwner;
        internal readonly SyntheticRateSession Owner;
        internal readonly SyntheticLease Lease;
        internal readonly SyntheticVersion Expected;
        internal readonly long Epoch;
        internal readonly string Scope;
        internal readonly string Operation;
        internal readonly decimal Rate;
        internal readonly decimal PriorRate;
        internal readonly SyntheticSessionState PriorState;
        internal readonly bool IsRestore;
        internal byte[] Payload { get { return (byte[])payload.Clone(); } }

        internal SyntheticSavePlan(SyntheticRateSession owner, SyntheticLease lease, SyntheticVersion expected,
            long epoch, string scope, string operation, decimal rate, decimal priorRate, byte[] data,
            SyntheticSessionState priorState, bool isRestore)
        {
            Owner = owner; Lease = lease; Expected = expected; Epoch = epoch; Scope = scope;
            Operation = operation; Rate = rate; PriorRate = priorRate; payload = (byte[])data.Clone(); PriorState = priorState;
            IsRestore = isRestore;
        }

        internal bool TryStart(object owner)
        {
            lock (planGate)
            {
                if (executed || !object.ReferenceEquals(stagingOwner, owner)) return false;
                executed = true;
                return true;
            }
        }

        internal bool TryReserve(object owner)
        {
            lock (planGate)
            {
                if (owner == null || executed || stagingOwner != null) return false;
                stagingOwner = owner;
                return true;
            }
        }

        internal bool TryCancel(object owner)
        {
            lock (planGate)
            {
                if (executed || !object.ReferenceEquals(stagingOwner, owner)) return false;
                executed = true;
                return true;
            }
        }

        internal bool TryCancelForScopeSwitch()
        {
            lock (planGate)
            {
                if (executed) return false;
                executed = true;
                return true;
            }
        }

        internal bool HasStagingCapability(object owner)
        {
            lock (planGate) { return owner != null && object.ReferenceEquals(stagingOwner, owner); }
        }
    }

    /// <summary>
    /// Session for test-only snapshot/commit semantics. Scope is opaque; no product save format is selected.
    /// </summary>
    internal sealed class SyntheticRateSession
    {
        private readonly object gate = new object();
        private readonly SyntheticRateStorage store;
        private readonly Dictionary<string, SyntheticSavePlan> unresolved = new Dictionary<string, SyntheticSavePlan>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> usedOperations = new Dictionary<string, bool>(StringComparer.Ordinal);
        private long epoch;
        private long latestReadRequest;
        private long acceptedReadRequest;
        private string scope;
        private SyntheticLease lease;
        private SyntheticReadResult snapshot;
        private SyntheticSavePlan pending;
        private string lastConfirmedOperation;
        private SyntheticVersion lastConfirmedVersion;
        private long lastConfirmedEpoch;
        private decimal current = 1m;
        private SyntheticSessionState state = SyntheticSessionState.Unloaded;

        internal SyntheticRateSession(SyntheticRateStorage store)
        {
            if (store == null) throw new ArgumentNullException("store");
            this.store = store;
        }

        // Historical value only; it is not a current/confirmed value unless State is Ready.
        internal decimal LastKnown { get { lock (gate) { return current; } } }
        internal SyntheticSessionState State { get { lock (gate) { return state; } } }

        // Ready is the last verified fake snapshot, not proof that no later writer exists.
        internal bool TryGetConfirmedCurrent(out decimal value)
        {
            lock (gate)
            {
                value = state == SyntheticSessionState.Ready ? current : 0m;
                return state == SyntheticSessionState.Ready;
            }
        }

        internal void SelectScope(string newScope)
        {
            if (string.IsNullOrEmpty(newScope)) throw new ArgumentException("Scope is required.", "newScope");
            lock (gate)
            {
                if (pending != null && (state == SyntheticSessionState.Pending || state == SyntheticSessionState.Unknown))
                {
                    // The plan lock is also used by ExecuteSaveCore before it enters the fake CAS.
                    // Only a plan proven not to have started can be canceled without a receipt.
                    if (!pending.TryCancelForScopeSwitch()) unresolved[scope] = pending;
                }
                long next = checked(epoch + 1);
                SyntheticLease nextLease = store.BeginLease(this, newScope);
                epoch = next; scope = newScope; lease = nextLease; snapshot = null; pending = null;
                lastConfirmedOperation = null; lastConfirmedVersion = null; lastConfirmedEpoch = 0;
                current = 1m; state = SyntheticSessionState.Unloaded;
                SyntheticSavePlan unresolvedPlan;
                if (unresolved.TryGetValue(newScope, out unresolvedPlan))
                {
                    pending = unresolvedPlan; current = unresolvedPlan.PriorRate; state = SyntheticSessionState.Unknown;
                }
            }
        }

        internal SyntheticReadTicket BeginRead()
        {
            long capturedEpoch;
            long capturedRequest;
            string capturedScope;
            lock (gate)
            {
                if (scope == null || state == SyntheticSessionState.Pending || state == SyntheticSessionState.Unknown) return null;
                capturedEpoch = epoch; capturedScope = scope;
                capturedRequest = checked(++latestReadRequest);
            }
            SyntheticReadResult result;
            try { result = store.Read(capturedScope); }
            catch (Exception) { result = new SyntheticReadResult(SyntheticReadKind.Unavailable, null, null); }
            return new SyntheticReadTicket(this, capturedEpoch, capturedRequest, capturedScope, result);
        }

        internal bool AcceptRead(SyntheticReadTicket ticket)
        {
            lock (gate)
            {
                if (ticket == null || !object.ReferenceEquals(ticket.Owner, this) || ticket.Epoch != epoch
                    || ticket.Request != latestReadRequest || ticket.Request <= acceptedReadRequest
                    || !string.Equals(scope, ticket.Scope, StringComparison.Ordinal)
                    || state == SyntheticSessionState.Pending || state == SyntheticSessionState.Unknown) return false;
                acceptedReadRequest = ticket.Request;
                SyntheticReadResult result = ticket.Result;
                if (result.Kind == SyntheticReadKind.Unavailable) { snapshot = null; state = SyntheticSessionState.Unavailable; return true; }
                SyntheticReadResult latest;
                try { latest = store.Read(scope); }
                catch (Exception) { latest = new SyntheticReadResult(SyntheticReadKind.Unavailable, null, null); }
                if (latest.Kind == SyntheticReadKind.Unavailable) { snapshot = null; state = SyntheticSessionState.Unavailable; return true; }
                if (result.Version == null || !result.Version.SameAs(latest.Version))
                {
                    snapshot = null; state = SyntheticSessionState.Conflict; return true;
                }
                result = latest;
                if (lastConfirmedVersion != null && !lastConfirmedVersion.SameAs(result.Version))
                {
                    lastConfirmedOperation = null; lastConfirmedVersion = null; lastConfirmedEpoch = 0;
                }
                snapshot = result;
                if (result.Kind == SyntheticReadKind.Missing) { state = SyntheticSessionState.Missing; return true; }
                decimal parsed;
                SyntheticSessionState parsedState = ParseRecord(result.Raw, out parsed);
                state = parsedState;
                if (parsedState == SyntheticSessionState.Ready || parsedState == SyntheticSessionState.NeedsMigration) current = parsed;
                return true;
            }
        }

        internal SyntheticSavePlan PrepareSave(string text, string operation)
        {
            decimal rate;
            if (string.IsNullOrEmpty(operation) || !TryParseRate(text, out rate)) return null;
            lock (gate)
            {
                if (usedOperations.ContainsKey(operation) || pending != null || snapshot == null || snapshot.Version == null
                    || (state != SyntheticSessionState.Ready && state != SyntheticSessionState.Missing
                        && state != SyntheticSessionState.NeedsMigration)) return null;
                byte[] data = Encoding.UTF8.GetBytes("v1:" + rate.ToString("0.00", CultureInfo.InvariantCulture));
                long nextReadRequest = checked(latestReadRequest + 1);
                SyntheticSavePlan plan = new SyntheticSavePlan(this, lease, snapshot.Version, epoch, scope, operation, rate, current, data, state, false);
                usedOperations.Add(operation, true);
                latestReadRequest = nextReadRequest;
                pending = plan; state = SyntheticSessionState.Pending;
                return plan;
            }
        }

        internal SyntheticCommitResult ExecuteSave(SyntheticSavePlan plan, SyntheticCommitFault fault)
        {
            return ExecuteSaveCore(plan, null, fault);
        }

        internal SyntheticCommitResult ExecuteStagedSave(SyntheticSavePlan plan, object stagingOwner, SyntheticCommitFault fault)
        {
            return ExecuteSaveCore(plan, stagingOwner, fault);
        }

        private SyntheticCommitResult ExecuteSaveCore(SyntheticSavePlan plan, object stagingOwner, SyntheticCommitFault fault)
        {
            if (plan == null || !object.ReferenceEquals(plan.Owner, this)) return null;
            if (!plan.TryStart(stagingOwner)) return null;
            try { return store.TryCommit(plan.Lease, plan.Expected, plan.Operation, plan.Payload, fault); }
            catch (Exception) { return new SyntheticCommitResult(SyntheticCommitKind.Unknown, plan.Lease, plan.Operation, null); }
        }

        // A fake failure before CAS may cancel a plan only while execution has not started.
        internal bool ReserveStaging(SyntheticSavePlan plan, object stagingOwner, SyntheticRateStorage expectedStore)
        {
            lock (gate)
            {
                if (stagingOwner == null || !object.ReferenceEquals(store, expectedStore)
                    || plan == null || !object.ReferenceEquals(plan, pending) || plan.Epoch != epoch
                    || state != SyntheticSessionState.Pending) return false;
                return plan.TryReserve(stagingOwner);
            }
        }

        internal bool CancelUnexecuted(SyntheticSavePlan plan, object stagingOwner)
        {
            return CancelUnexecuted(plan, stagingOwner, plan == null ? SyntheticSessionState.Unloaded : plan.PriorState);
        }

        internal bool CancelUnexecuted(SyntheticSavePlan plan, object stagingOwner, SyntheticSessionState afterCancel)
        {
            lock (gate)
            {
                if (plan == null || !object.ReferenceEquals(plan, pending) || plan.Epoch != epoch
                    || state != SyntheticSessionState.Pending) return false;
                if (!plan.TryCancel(stagingOwner)) return false;
                pending = null; state = afterCancel;
                if (afterCancel == SyntheticSessionState.Unavailable || afterCancel == SyntheticSessionState.Conflict)
                {
                    snapshot = null; lastConfirmedOperation = null; lastConfirmedVersion = null; lastConfirmedEpoch = 0;
                }
                return true;
            }
        }

        internal bool AcceptSave(SyntheticSavePlan plan, SyntheticCommitResult result)
        {
            lock (gate)
            {
                if (plan == null || result == null || !object.ReferenceEquals(plan, pending)
                    || plan.Epoch != epoch || !object.ReferenceEquals(plan.Owner, this)
                    || !object.ReferenceEquals(plan.Lease, result.Lease)
                    || !string.Equals(plan.Scope, scope, StringComparison.Ordinal)
                    || !string.Equals(plan.Operation, result.Operation, StringComparison.Ordinal)) return false;
                if (result.Kind == SyntheticCommitKind.Unknown) { state = SyntheticSessionState.Unknown; return true; }
                if (result.Kind == SyntheticCommitKind.Conflict || result.Kind == SyntheticCommitKind.LeaseExpired)
                {
                    // An unstarted plan must be canceled under the same lock as TryStart.
                    // An already-started plan needs a matching receipt before it can be cleared.
                    if (!plan.TryCancelForScopeSwitch())
                    {
                        SyntheticReceipt rejected = store.Resolve(scope, plan.Operation, this);
                        if (!ReceiptMatches(plan, rejected, SyntheticCommitKind.NotCommitted, null))
                        {
                            state = SyntheticSessionState.Unknown; return true;
                        }
                    }
                    pending = null; state = SyntheticSessionState.Conflict; return true;
                }
                if (result.Kind == SyntheticCommitKind.NotCommitted)
                {
                    SyntheticReceipt rejected = store.Resolve(scope, plan.Operation, this);
                    if (!ReceiptMatches(plan, rejected, SyntheticCommitKind.NotCommitted, null))
                    {
                        state = SyntheticSessionState.Unknown; return true;
                    }
                    // A not-committed receipt does not rule out another writer.
                    pending = null; snapshot = null; state = SyntheticSessionState.NeedsReload; return true;
                }
                SyntheticReceipt committed = store.Resolve(scope, plan.Operation, this);
                if (!ReceiptMatches(plan, committed, SyntheticCommitKind.Committed, result.Version))
                {
                    state = SyntheticSessionState.Unknown; return true;
                }
                FinishCommitted(plan, result.Version);
                return true;
            }
        }

        internal bool ResolvePending()
        {
            lock (gate)
            {
                if (state != SyntheticSessionState.Unknown || pending == null) return false;
                SyntheticReceipt receipt;
                try { receipt = store.Resolve(scope, pending.Operation, this); }
                catch (Exception) { return false; }
                if (receipt == null) return false;
                if (receipt.Expected == null || !receipt.Expected.SameAs(pending.Expected)
                    || !EqualBytes(receipt.Raw, pending.Payload)) return false;
                if (receipt.Kind == SyntheticCommitKind.NotCommitted)
                {
                    state = pending.Epoch == epoch ? SyntheticSessionState.NeedsReload : SyntheticSessionState.Unloaded;
                    pending = null; snapshot = null; unresolved.Remove(scope); return true;
                }
                if (receipt.Kind != SyntheticCommitKind.Committed) return false;
                return FinishCommitted(pending, receipt.Version);
            }
        }

        private bool FinishCommitted(SyntheticSavePlan plan, SyntheticVersion committedVersion)
        {
            SyntheticReadResult latest;
            try { latest = store.Read(scope); }
            catch (Exception) { latest = new SyntheticReadResult(SyntheticReadKind.Unavailable, null, null); }
            if (latest.Kind == SyntheticReadKind.Unavailable) { state = SyntheticSessionState.Unknown; return false; }
            if (latest.Kind != SyntheticReadKind.Found || committedVersion == null
                || !committedVersion.SameAs(latest.Version) || !EqualBytes(latest.Raw, plan.Payload))
            {
                pending = null; unresolved.Remove(scope); state = SyntheticSessionState.ChangedAfterCommit; return true;
            }
            if (plan.IsRestore)
            {
                pending = null; unresolved.Remove(scope); snapshot = null; state = SyntheticSessionState.NeedsReload;
                lastConfirmedOperation = null; lastConfirmedVersion = null; lastConfirmedEpoch = 0;
                return true;
            }
            current = plan.Rate; snapshot = latest; pending = null; unresolved.Remove(scope); state = SyntheticSessionState.Ready;
            lastConfirmedOperation = plan.Operation; lastConfirmedVersion = committedVersion; lastConfirmedEpoch = epoch;
            return true;
        }

        internal bool IsConfirmedCommit(SyntheticSavePlan plan, SyntheticVersion version)
        {
            lock (gate)
            {
                return plan != null && object.ReferenceEquals(plan.Owner, this) && plan.Epoch == epoch
                    && lastConfirmedEpoch == epoch && state == SyntheticSessionState.Ready
                    && string.Equals(scope, plan.Scope, StringComparison.Ordinal)
                    && string.Equals(lastConfirmedOperation, plan.Operation, StringComparison.Ordinal)
                    && lastConfirmedVersion != null && lastConfirmedVersion.SameAs(version)
                    && snapshot != null && snapshot.Version != null && snapshot.Version.SameAs(version);
            }
        }

        internal void RequireReloadAfterConfirmed(SyntheticSavePlan plan, SyntheticVersion version)
        {
            lock (gate)
            {
                if (!IsConfirmedCommit(plan, version)) return;
                snapshot = null; lastConfirmedOperation = null; lastConfirmedVersion = null; lastConfirmedEpoch = 0;
                if (latestReadRequest == long.MaxValue) state = SyntheticSessionState.Unavailable;
                else { latestReadRequest++; state = SyntheticSessionState.NeedsReload; }
            }
        }

        internal SyntheticSavePlan PrepareRestore(SyntheticSavePlan original, SyntheticVersion version,
            string operation, byte[] backup, object stagingCapability)
        {
            if (string.IsNullOrEmpty(operation) || backup == null) return null;
            lock (gate)
            {
                if (usedOperations.ContainsKey(operation) || pending != null || !IsConfirmedCommit(original, version)
                    || !original.HasStagingCapability(stagingCapability)) return null;
                SyntheticReceipt receipt = store.Resolve(scope, original.Operation, this);
                if (!ReceiptMatches(original, receipt, SyntheticCommitKind.Committed, version)) return null;
                long nextReadRequest = checked(latestReadRequest + 1);
                SyntheticSavePlan restore = new SyntheticSavePlan(this, lease, version, epoch, scope, operation,
                    current, current, backup, state, true);
                usedOperations.Add(operation, true);
                latestReadRequest = nextReadRequest; pending = restore; state = SyntheticSessionState.Pending;
                return restore;
            }
        }

        private static bool ReceiptMatches(SyntheticSavePlan plan, SyntheticReceipt receipt,
            SyntheticCommitKind kind, SyntheticVersion version)
        {
            if (plan == null || receipt == null || receipt.Kind != kind || receipt.Expected == null
                || !receipt.Expected.SameAs(plan.Expected) || !EqualBytes(receipt.Raw, plan.Payload)
                || !string.Equals(receipt.Scope, plan.Scope, StringComparison.Ordinal)
                || !string.Equals(receipt.Operation, plan.Operation, StringComparison.Ordinal)
                || !object.ReferenceEquals(receipt.Owner, plan.Owner)) return false;
            return kind != SyntheticCommitKind.Committed
                || (receipt.Version != null && receipt.Version.SameAs(version));
        }

        private static bool EqualBytes(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        private static SyntheticSessionState ParseRecord(byte[] raw, out decimal value)
        {
            value = 0m;
            if (raw == null) return SyntheticSessionState.Corrupt;
            string text;
            try { text = new UTF8Encoding(false, true).GetString(raw); }
            catch (DecoderFallbackException) { return SyntheticSessionState.Corrupt; }
            if (text.StartsWith("v1:", StringComparison.Ordinal))
                return TryParseRate(text.Substring(3), out value) ? SyntheticSessionState.Ready : SyntheticSessionState.Corrupt;
            if (text.StartsWith("v0:", StringComparison.Ordinal))
                return TryParseRate(text.Substring(3), out value) ? SyntheticSessionState.NeedsMigration : SyntheticSessionState.Corrupt;
            int colon = text.IndexOf(':');
            if (colon > 1 && text[0] == 'v')
            {
                bool digits = true;
                for (int i = 1; i < colon; i++) if (text[i] < '0' || text[i] > '9') digits = false;
                if (digits)
                {
                    int version;
                    if (!int.TryParse(text.Substring(1, colon - 1), NumberStyles.None, CultureInfo.InvariantCulture, out version)
                        || version > 1) return SyntheticSessionState.FutureVersion;
                }
            }
            return SyntheticSessionState.Corrupt;
        }

        private static bool TryParseRate(string text, out decimal value)
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
            if (point == 0 || point == text.Length - 1 || (point >= 0 && text.Length - point - 1 > 2)) return false;
            decimal parsed;
            if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsed)
                || parsed < 1m || parsed > 10m) return false;
            value = parsed;
            return true;
        }
    }

    /// <summary>
    /// Fake same-directory staging and backup. Names are opaque memory-only labels, not paths.
    /// The store's CAS and persistent receipts are assumed capabilities, not OS guarantees.
    /// </summary>
    internal sealed class SyntheticFileTransaction
    {
        private readonly object gate = new object();
        private readonly object stagingCapability = new object();
        private readonly SyntheticRateStorage store;
        private readonly SyntheticRateSession session;
        private readonly SyntheticSavePlan plan;
        private readonly string directory;
        private byte[] staged;
        private byte[] backup;
        private bool stageCreated;
        private bool stageVerified;
        private bool backupReady;
        private bool backupPresent;
        private bool finished;
        internal ManualResetEvent RestoreCommitted;
        internal ManualResetEvent ContinueRestore;
        internal volatile bool RestoreWaitTimedOut;

        internal SyntheticFileTransaction(SyntheticRateStorage store, SyntheticRateSession session,
            SyntheticSavePlan plan, string directory)
        {
            if (store == null) throw new ArgumentNullException("store");
            if (session == null) throw new ArgumentNullException("session");
            if (plan == null || !object.ReferenceEquals(plan.Owner, session)) throw new ArgumentException("Plan owner mismatch.", "plan");
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Directory label is required.", "directory");
            if (!store.IsBoundFileDirectory(plan.Scope, directory))
                throw new ArgumentException("Directory does not match the fake target scope.", "directory");
            this.store = store; this.session = session; this.plan = plan; this.directory = directory;
            if (!session.ReserveStaging(plan, stagingCapability, store)) throw new InvalidOperationException("Plan is not available for staging.");
        }

        internal string StageDirectory { get { lock (gate) { return stageCreated ? directory : null; } } }
        internal string BackupDirectory { get { lock (gate) { return backupReady ? directory : null; } } }
        internal byte[] StagedRaw { get { lock (gate) { return staged == null ? null : (byte[])staged.Clone(); } } }
        internal byte[] BackupRaw { get { lock (gate) { return backup == null ? null : (byte[])backup.Clone(); } } }
        internal bool BackupPresent { get { lock (gate) { return backupReady && backupPresent; } } }

        internal bool Stage(SyntheticStageFault fault)
        {
            lock (gate)
            {
            if (finished || stageCreated) return false;
            if (fault == SyntheticStageFault.CreateFailure) { FailBeforeCommit(); return false; }
            stageCreated = true;
            byte[] bytes = plan.Payload;
            if (fault == SyntheticStageFault.WriteFailure)
            {
                staged = new byte[] { bytes[0] };
                FailBeforeCommit(); return false;
            }
            staged = (byte[])bytes.Clone();
            if (fault == SyntheticStageFault.VerifyFailure) { FailBeforeCommit(); return false; }
            stageVerified = true;
            return true;
            }
        }

        internal bool MakeBackup(bool simulateFailure)
        {
            lock (gate)
            {
            if (finished || !stageVerified || backupReady) return false;
            SyntheticReadResult current;
            try { current = store.Read(plan.Scope); }
            catch (Exception) { FailBeforeCommit(SyntheticSessionState.Unavailable); return false; }
            if (current.Kind == SyntheticReadKind.Unavailable || current.Version == null)
            {
                FailBeforeCommit(SyntheticSessionState.Unavailable); return false;
            }
            if (!plan.Expected.SameAs(current.Version)) { FailBeforeCommit(SyntheticSessionState.Conflict); return false; }
            if (simulateFailure) { FailBeforeCommit(); return false; }
            backupPresent = current.Kind == SyntheticReadKind.Found;
            backup = current.Raw;
            backupReady = true;
            return true;
            }
        }

        internal bool AbortBeforeCommit()
        {
            lock (gate)
            {
                if (finished || !stageVerified || !backupReady) return false;
                return FailBeforeCommit();
            }
        }

        internal SyntheticCommitResult Commit(SyntheticCommitFault fault)
        {
            lock (gate)
            {
            if (finished || !stageVerified || !backupReady) return null;
            finished = true;
            SyntheticCommitResult result = session.ExecuteStagedSave(plan, stagingCapability, fault);
            if (result != null) session.AcceptSave(plan, result);
            return result;
            }
        }

        // Explicit fake restore uses CAS; it never silently overwrites a later version.
        internal SyntheticCommitResult Restore(SyntheticVersion expectedCurrent, string operation)
        {
            lock (gate)
            {
                if (!finished || !backupReady || !backupPresent || backup == null) return null;
                SyntheticReceipt receipt = store.Resolve(plan.Scope, plan.Operation, session);
                if (receipt == null || receipt.Kind != SyntheticCommitKind.Committed
                    || receipt.Version == null || receipt.Expected == null
                    || !receipt.Expected.SameAs(plan.Expected) || !SameBytes(receipt.Raw, plan.Payload)
                    || !session.IsConfirmedCommit(plan, receipt.Version)) return null;
                if (!receipt.Version.SameAs(expectedCurrent))
                {
                    session.RequireReloadAfterConfirmed(plan, receipt.Version);
                    return null;
                }
                SyntheticSavePlan restore = session.PrepareRestore(plan, receipt.Version, operation, backup, stagingCapability);
                if (restore == null) return null;
                SyntheticCommitResult result = session.ExecuteSave(restore, SyntheticCommitFault.None);
                try
                {
                    if (RestoreCommitted != null)
                    {
                        RestoreCommitted.Set();
                        if (!ContinueRestore.WaitOne(5000))
                        {
                            RestoreWaitTimedOut = true;
                            throw new InvalidOperationException("Synthetic restore barrier timed out.");
                        }
                    }
                }
                catch (Exception)
                {
                    session.AcceptSave(restore, new SyntheticCommitResult(SyntheticCommitKind.Unknown,
                        restore.Lease, restore.Operation, null));
                    throw;
                }
                if (result != null) session.AcceptSave(restore, result);
                return result;
            }
        }

        private static bool SameBytes(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        private bool FailBeforeCommit()
        {
            finished = true;
            return session.CancelUnexecuted(plan, stagingCapability);
        }

        private bool FailBeforeCommit(SyntheticSessionState outcome)
        {
            finished = true;
            return session.CancelUnexecuted(plan, stagingCapability, outcome);
        }
    }
}
