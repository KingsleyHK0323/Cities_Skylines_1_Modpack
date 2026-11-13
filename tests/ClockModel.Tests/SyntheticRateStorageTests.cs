// Signature: OpenAI-Codex_rate_transaction
using System;
using System.Text;
using System.Threading;
using System.Reflection;
using RealisticCityTime.Tests;

internal static class SyntheticRateStorageTests
{
    private static byte[] Bytes(string value) { return Encoding.UTF8.GetBytes(value); }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Synthetic rate transaction: " + message);
    }

    private static void EqualBytes(byte[] left, byte[] right, string message)
    {
        if (left == null || right == null || left.Length != right.Length) throw new Exception(message);
        for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) throw new Exception(message);
    }

    private static void Load(SyntheticRateSession session)
    {
        SyntheticReadTicket ticket = session.BeginRead();
        Check(ticket != null && session.AcceptRead(ticket), "read accepted in current scope");
    }

    private static SyntheticSavePlan Plan(SyntheticRateSession session, string value, string operation)
    {
        SyntheticSavePlan plan = session.PrepareSave(value, operation);
        Check(plan != null, "explicit save can be prepared");
        return plan;
    }

    private static SyntheticFileTransaction FileTransaction(SyntheticRateStorage store, SyntheticRateSession session,
        SyntheticSavePlan plan, string directory)
    {
        store.BindFileDirectory(plan.Scope, directory);
        return new SyntheticFileTransaction(store, session, plan, directory);
    }

    private static void Save(SyntheticRateSession session, string value, string operation)
    {
        SyntheticSavePlan plan = Plan(session, value, operation);
        SyntheticCommitResult result = session.ExecuteSave(plan, SyntheticCommitFault.None);
        Check(result.Kind == SyntheticCommitKind.Committed, "normal fake commit succeeds");
        Check(session.AcceptSave(plan, result), "normal fake commit result accepted");
        Check(session.State == SyntheticSessionState.Ready, "normal fake commit is current");
    }

    private static void MissingAndNoImplicitWrites()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("scope-A");
        Check(session.LastKnown == 1m && store.CommitAttempts == 0, "default has no write");
        Load(session);
        Check(session.State == SyntheticSessionState.Missing && store.CommitAttempts == 0, "missing read has no write");
        Check(session.PrepareSave("1.250", "invalid") == null && store.CommitAttempts == 0,
            "invalid input cannot initiate a write");
        SyntheticSavePlan plan = Plan(session, "1.00", "explicit-default");
        byte[] exposedPlan = plan.Payload;
        exposedPlan[0] = (byte)'X';
        Check(session.AcceptSave(plan, session.ExecuteSave(plan, SyntheticCommitFault.None)), "explicit default accepted");
        Check(session.State == SyntheticSessionState.Ready, "default save is current");
        Check(store.CommitAttempts == 1 && session.LastKnown == 1m, "explicit default save can create fixture data");
        EqualBytes(store.Read("scope-A").Raw, Bytes("v1:1.00"), "canonical fake record");
        Check(session.PrepareSave("2.00", "explicit-default") == null, "a session cannot reuse an operation id");
    }

    private static void CopiesAndVersions()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        byte[] input = Bytes("v1:1.25");
        store.ExternalSet("A", input);
        input[0] = (byte)'X';
        SyntheticReadResult first = store.Read("A");
        EqualBytes(first.Raw, Bytes("v1:1.25"), "storage copied input");
        byte[] exposed = first.Raw;
        exposed[0] = (byte)'X';
        EqualBytes(first.Raw, Bytes("v1:1.25"), "read snapshot returns copies");
        EqualBytes(store.Read("A").Raw, Bytes("v1:1.25"), "snapshot mutation cannot change store");
        store.ExternalSet("A", Bytes("v1:2.00"));
        store.ExternalSet("A", Bytes("v1:1.25"));
        Check(!first.Version.SameAs(store.Read("A").Version), "ABA changes opaque version");
        store.ExternalDelete("A");
        SyntheticReadResult absent = store.Read("A");
        Check(absent.Kind == SyntheticReadKind.Missing && !first.Version.SameAs(absent.Version), "deletion changes absence version");
        store.ExternalSet("A", Bytes("v1:1.25"));
        Check(!absent.Version.SameAs(store.Read("A").Version), "recreation changes version");
        object writer = new object();
        SyntheticLease lease = store.BeginLease(writer, "A");
        SyntheticCommitResult stale = store.TryCommit(lease, first.Version, "stale-aba", Bytes("v1:3.00"), SyntheticCommitFault.None);
        Check(stale.Kind == SyntheticCommitKind.Conflict, "old ABA token is rejected");
        SyntheticLease otherScope = store.BeginLease(new object(), "B");
        SyntheticCommitResult wrongScope = store.TryCommit(otherScope, store.Read("A").Version,
            "wrong-scope", Bytes("v1:3.00"), SyntheticCommitFault.None);
        Check(wrongScope.Kind == SyntheticCommitKind.Conflict, "cross-scope token is rejected");
        SyntheticRateStorage otherStore = new SyntheticRateStorage();
        SyntheticLease otherLease = otherStore.BeginLease(new object(), "A");
        Check(otherStore.TryCommit(otherLease, store.Read("A").Version, "wrong-store", Bytes("v1:3.00"),
            SyntheticCommitFault.None).Kind == SyntheticCommitKind.Conflict, "cross-store token is rejected");
        SyntheticLease uniqueLease = store.BeginLease(new object(), "A");
        SyntheticVersion uniqueVersion = store.Read("A").Version;
        Check(store.TryCommit(uniqueLease, uniqueVersion, "unique-operation", Bytes("v1:4.00"),
            SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed, "unique operation commits once");
        Check(store.TryCommit(uniqueLease, store.Read("A").Version, "unique-operation", Bytes("v1:4.00"),
            SyntheticCommitFault.None).Kind == SyntheticCommitKind.Conflict, "reusing an operation id with another version cannot commit");
        Check(store.TryCommit(uniqueLease, store.Read("A").Version, "unique-operation", Bytes("v1:9.00"),
            SyntheticCommitFault.None).Kind == SyntheticCommitKind.Conflict, "reusing an operation id with another payload cannot commit");
        Check(store.TryCommit(otherScope, store.Read("B").Version, "unique-operation", Bytes("v1:4.00"),
            SyntheticCommitFault.None).Kind == SyntheticCommitKind.Conflict, "reusing an operation id in another scope cannot commit");
    }

    private static void CorruptionAndMigration()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        byte[] corrupt = new byte[] { 0, 255, 10, 65 };
        store.ExternalSet("broken", corrupt);
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("broken"); Load(session);
        Check(session.State == SyntheticSessionState.Corrupt, "corrupt bytes are rejected");
        Check(session.PrepareSave("2.00", "broken-save") == null, "ordinary save cannot replace corrupt bytes");
        EqualBytes(store.Read("broken").Raw, corrupt, "corrupt bytes remain intact");
        store.ExternalSet("future", Bytes("v9:2.00"));
        session.SelectScope("future"); Load(session);
        Check(session.State == SyntheticSessionState.FutureVersion, "unknown newer format is distinct");
        Check(session.PrepareSave("2.00", "future-save") == null, "ordinary save cannot replace future format");
        EqualBytes(store.Read("future").Raw, Bytes("v9:2.00"), "future bytes remain intact");
        store.ExternalSet("legacy", Bytes("v0:1.50"));
        session.SelectScope("legacy"); Load(session);
        Check(session.State == SyntheticSessionState.NeedsMigration && session.LastKnown == 1.5m, "legacy parse is in memory only");
        EqualBytes(store.Read("legacy").Raw, Bytes("v0:1.50"), "reading does not migrate");
        Save(session, "1.50", "explicit-migration");
        EqualBytes(store.Read("legacy").Raw, Bytes("v1:1.50"), "explicit save migrates example format");
    }

    private static void FailedAndConflictingCommits()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        SyntheticRateSession left = new SyntheticRateSession(store);
        SyntheticRateSession right = new SyntheticRateSession(store);
        left.SelectScope("A"); right.SelectScope("A"); Load(left); Load(right);
        SyntheticSavePlan rejected = Plan(left, "2.00", "reject");
        Check(left.AcceptSave(rejected, left.ExecuteSave(rejected, SyntheticCommitFault.NotCommitted)), "not-committed result accepted");
        Check(left.LastKnown == 1.25m && left.State == SyntheticSessionState.NeedsReload,
            "failed write keeps the historical value pending a fresh read");
        EqualBytes(store.Read("A").Raw, Bytes("v1:1.25"), "failed write preserves raw value");
        Load(left);
        Check(left.State == SyntheticSessionState.Ready && left.LastKnown == 1.25m,
            "fresh read confirms the preserved value");
        Save(left, "2.00", "winner");
        SyntheticSavePlan stale = Plan(right, "3.00", "loser");
        Check(right.AcceptSave(stale, right.ExecuteSave(stale, SyntheticCommitFault.None)), "conflict result accepted");
        Check(right.State == SyntheticSessionState.Conflict && right.LastKnown == 1.25m, "stale session does not overwrite");
        EqualBytes(store.Read("A").Raw, Bytes("v1:2.00"), "winning bytes remain intact");
    }

    private static void UnknownNeedsReceiptAndCurrentVersion()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticSavePlan plan = Plan(session, "2.00", "lost-before");
        Check(session.AcceptSave(plan, session.ExecuteSave(plan, SyntheticCommitFault.UnknownBeforeWithoutReceipt)), "unknown result accepted");
        Check(session.State == SyntheticSessionState.Unknown && session.LastKnown == 1.25m, "unknown preserves current value");
        Check(session.PrepareSave("2.00", "blind-retry") == null, "unknown blocks a blind retry");
        store.ExternalSet("A", Bytes("v1:2.00"));
        Check(!session.ResolvePending() && session.State == SyntheticSessionState.Unknown,
            "same bytes without a receipt do not prove this operation committed");
        session.SelectScope("B"); Load(session);
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unknown && session.BeginRead() == null
            && session.PrepareSave("2.00", "switch-bypass") == null, "switching away and back cannot discard uncertainty");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        plan = Plan(session, "2.00", "lost-after");
        Check(session.AcceptSave(plan, session.ExecuteSave(plan, SyntheticCommitFault.UnknownAfterCommit)), "committed-but-unknown accepted");
        Check(session.State == SyntheticSessionState.Unknown && session.LastKnown == 1.25m, "uncertain success is not yet effective");
        store.ExternalSet("A", Bytes("v1:2.00"));
        Check(session.ResolvePending() && session.State == SyntheticSessionState.ChangedAfterCommit,
            "receipt plus changed version reports later overwrite even if bytes match");
        Check(session.LastKnown == 1.25m, "overwritten commit is not asserted current");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        plan = Plan(session, "2.00", "known-not-committed");
        Check(session.AcceptSave(plan, session.ExecuteSave(plan, SyntheticCommitFault.UnknownBeforeWithReceipt)), "unknown not-committed accepted");
        Check(session.ResolvePending() && session.State == SyntheticSessionState.NeedsReload && session.LastKnown == 1.25m,
            "definitive not-committed receipt requires a fresh read");
        Load(session);
        Check(session.State == SyntheticSessionState.Ready && session.LastKnown == 1.25m,
            "fresh read confirms the value after a not-committed receipt");
        plan = Plan(session, "3.00", "known-committed");
        Check(session.AcceptSave(plan, session.ExecuteSave(plan, SyntheticCommitFault.UnknownAfterCommit)), "unknown committed accepted");
        Check(store.Resolve("A", "known-committed", new object()) == null, "another session cannot claim the receipt");
        byte[] receiptCopy = store.Resolve("A", "known-committed", session).Raw;
        receiptCopy[0] = (byte)'X';
        Check(session.ResolvePending() && session.State == SyntheticSessionState.Ready && session.LastKnown == 3m,
            "receipt remains intact and matching current version confirms the fake commit");
    }

    private static void ScopeSwitchAndLateResults()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:2.00"));
        store.ExternalSet("B", Bytes("v1:3.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A");
        SyntheticReadTicket lateRead = session.BeginRead();
        session.SelectScope("B"); Load(session);
        Check(!session.AcceptRead(lateRead) && session.LastKnown == 3m, "old scope read cannot update the new scope");
        session.SelectScope("A"); Load(session);
        SyntheticSavePlan latePlan = Plan(session, "4.00", "late-after-switch");
        session.SelectScope("B"); Load(session);
        SyntheticCommitResult expired = session.ExecuteSave(latePlan, SyntheticCommitFault.None);
        Check(expired == null && !session.AcceptSave(latePlan, expired), "scope switch cancels an unstarted old plan");
        EqualBytes(store.Read("A").Raw, Bytes("v1:2.00"), "revoked plan does not commit old scope");
        Check(session.LastKnown == 3m, "revoked plan does not alter new scope");
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unloaded, "canceled unstarted plan needs only a fresh read");
        Load(session);
        SyntheticSavePlan beforeSwitch = Plan(session, "5.00", "commit-before-switch");
        SyntheticCommitResult completed = session.ExecuteSave(beforeSwitch, SyntheticCommitFault.None);
        session.SelectScope("B"); Load(session);
        Check(!session.AcceptSave(beforeSwitch, completed) && session.LastKnown == 3m,
            "late reply from an already committed old scope cannot alter new scope");
        EqualBytes(store.Read("A").Raw, Bytes("v1:5.00"), "commit before switch remains in its old fake scope");
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unknown && session.ResolvePending() && session.LastKnown == 5m,
            "returning to the old scope requires its receipt and current version");
    }

    private static void SameScopeReadOrdering()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A");
        SyntheticReadTicket old = session.BeginRead();
        store.ExternalSet("A", Bytes("v1:2.00"));
        SyntheticReadTicket latest = session.BeginRead();
        Check(session.AcceptRead(latest) && session.LastKnown == 2m, "newer read accepted");
        Check(!session.AcceptRead(old) && !session.AcceptRead(latest) && session.LastKnown == 2m,
            "older and duplicate read tickets are inert");
        SyntheticReadTicket beforeSave = session.BeginRead();
        Save(session, "3.00", "read-invalidation");
        Check(!session.AcceptRead(beforeSave) && session.LastKnown == 3m, "save invalidates older pending read");
    }

    private static void UnavailableReadPreservesCertaintyBoundary()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A");
        store.ReadUnavailable = true;
        Load(session);
        Check(session.State == SyntheticSessionState.Unavailable && session.PrepareSave("2.00", "no-read") == null,
            "unavailable read cannot authorize a write");
        store.ReadUnavailable = false;
        Load(session);
        SyntheticSavePlan plan = Plan(session, "2.00", "unavailable-after-commit");
        SyntheticCommitResult result = session.ExecuteSave(plan, SyntheticCommitFault.None);
        store.ReadUnavailable = true;
        Check(session.AcceptSave(plan, result) && session.State == SyntheticSessionState.Unknown && session.LastKnown == 1.25m,
            "unavailable current-version verification retains uncertainty");
        Check(!session.ResolvePending(), "receipt without readable current version cannot declare success");
        store.ReadUnavailable = false;
        Check(session.ResolvePending() && session.State == SyntheticSessionState.Ready && session.LastKnown == 2m,
            "receipt plus readable matching version resolves the fake commit");
    }

    private static void StagingFailuresDoNotTouchTarget()
    {
        foreach (SyntheticStageFault fault in new SyntheticStageFault[] {
            SyntheticStageFault.CreateFailure, SyntheticStageFault.WriteFailure, SyntheticStageFault.VerifyFailure })
        {
            SyntheticRateStorage store = new SyntheticRateStorage();
            store.ExternalSet("A", Bytes("v1:1.25"));
            SyntheticRateSession session = new SyntheticRateSession(store);
            session.SelectScope("A"); Load(session);
            SyntheticSavePlan plan = Plan(session, "2.00", "stage-failure");
            SyntheticFileTransaction transaction = FileTransaction(store, session, plan, "isolated-directory");
            Check(session.ExecuteSave(plan, SyntheticCommitFault.None) == null, "reserved plan cannot bypass staging");
            Check(!transaction.Stage(fault), "injected staging failure is reported");
            Check(session.State == SyntheticSessionState.Ready && session.LastKnown == 1.25m, "staging failure keeps confirmed rate");
            Check(store.CommitAttempts == 0 && transaction.Commit(SyntheticCommitFault.None) == null, "staging failure cannot commit");
            EqualBytes(store.Read("A").Raw, Bytes("v1:1.25"), "staging failure preserves original bytes");
        }

        SyntheticRateStorage backupStore = new SyntheticRateStorage();
        backupStore.ExternalSet("A", Bytes("v1:1.25"));
        SyntheticRateSession backupSession = new SyntheticRateSession(backupStore);
        backupSession.SelectScope("A"); Load(backupSession);
        SyntheticFileTransaction failedBackup = FileTransaction(backupStore, backupSession,
            Plan(backupSession, "2.00", "backup-failure"), "isolated-directory");
        Check(failedBackup.Stage(SyntheticStageFault.None) && !failedBackup.MakeBackup(true), "backup failure is reported");
        Check(backupStore.CommitAttempts == 0 && backupSession.LastKnown == 1.25m, "backup failure cannot commit");
        EqualBytes(backupStore.Read("A").Raw, Bytes("v1:1.25"), "backup failure preserves original bytes");

        SyntheticRateStorage abortStore = new SyntheticRateStorage();
        abortStore.ExternalSet("A", Bytes("v1:1.25"));
        SyntheticRateSession abortSession = new SyntheticRateSession(abortStore);
        abortSession.SelectScope("A"); Load(abortSession);
        SyntheticFileTransaction aborted = FileTransaction(abortStore, abortSession,
            Plan(abortSession, "2.00", "abort-before-commit"), "isolated-directory");
        Check(aborted.Stage(SyntheticStageFault.None) && aborted.MakeBackup(false) && aborted.AbortBeforeCommit(),
            "explicit precommit abort succeeds");
        Check(abortStore.CommitAttempts == 0 && abortSession.LastKnown == 1.25m, "precommit abort never changes target");
        EqualBytes(aborted.BackupRaw, Bytes("v1:1.25"), "backup remains available after abort");
        EqualBytes(abortStore.Read("A").Raw, Bytes("v1:1.25"), "abort preserves target");
    }

    private static void StagingMigrationBackupAndConflict()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v0:1.50"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "1.50", "staged-migration"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "migration stages and backs up");
        Check(transaction.StageDirectory == "module-directory" && transaction.BackupDirectory == "module-directory",
            "staging and backup use the same fake directory");
        byte[] stageCopy = transaction.StagedRaw;
        byte[] backupCopy = transaction.BackupRaw;
        stageCopy[0] = (byte)'X'; backupCopy[0] = (byte)'X';
        EqualBytes(transaction.StagedRaw, Bytes("v1:1.50"), "staging bytes are defensive copies");
        EqualBytes(transaction.BackupRaw, Bytes("v0:1.50"), "backup bytes are defensive copies");
        SyntheticCommitResult result = transaction.Commit(SyntheticCommitFault.None);
        Check(result != null && result.Kind == SyntheticCommitKind.Committed && session.State == SyntheticSessionState.Ready,
            "explicit fake migration commits");
        EqualBytes(store.Read("A").Raw, Bytes("v1:1.50"), "migration writes new example format");
        SyntheticVersion committedVersion = store.Read("A").Version;
        SyntheticCommitResult restored = transaction.Restore(committedVersion, "explicit-restore");
        Check(restored != null && restored.Kind == SyntheticCommitKind.Committed, "explicit fake backup restore uses current version");
        EqualBytes(store.Read("A").Raw, Bytes("v0:1.50"), "backup can restore original fixture bytes");
        EqualBytes(transaction.BackupRaw, Bytes("v0:1.50"), "restore does not consume the backup");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        transaction = FileTransaction(store, session,
            Plan(session, "2.00", "conflicting-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "conflict test stages and backs up");
        store.ExternalSet("A", Bytes("v1:3.00"));
        result = transaction.Commit(SyntheticCommitFault.None);
        Check(result.Kind == SyntheticCommitKind.Conflict && session.State == SyntheticSessionState.Conflict,
            "other writer after backup causes version conflict");
        EqualBytes(store.Read("A").Raw, Bytes("v1:3.00"), "failed commit does not overwrite another writer");
        EqualBytes(transaction.BackupRaw, Bytes("v1:1.25"), "preconflict backup is retained");
    }

    private static void StagingUnknownRetainsBackup()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.25"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "staged-unknown"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "unknown test stages and backs up");
        SyntheticCommitResult result = transaction.Commit(SyntheticCommitFault.UnknownAfterCommit);
        Check(result.Kind == SyntheticCommitKind.Unknown && session.State == SyntheticSessionState.Unknown && session.LastKnown == 1.25m,
            "unknown fake commit preserves last confirmed in-memory setting");
        EqualBytes(transaction.BackupRaw, Bytes("v1:1.25"), "unknown result retains backup");
        Check(session.ResolvePending() && session.LastKnown == 2m, "strong receipt and current version reconcile unknown commit");
    }

    internal static void RunRestoreRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "original-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "restore red stages and backs up");
        Check(transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed, "restore red establishes original commit");
        store.ExternalSet("A", Bytes("v1:9.00"));
        SyntheticVersion laterVersion = store.Read("A").Version;
        SyntheticCommitResult restore = transaction.Restore(laterVersion, "unsafe-restore");
        Check(restore == null || restore.Kind == SyntheticCommitKind.Conflict, "restore overwrote newer writer");
        EqualBytes(store.Read("A").Raw, Bytes("v1:9.00"), "newer writer must remain intact");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        transaction = FileTransaction(store, session,
            Plan(session, "2.00", "aborted-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false) && transaction.AbortBeforeCommit(),
            "restore red establishes aborted transaction");
        SyntheticVersion beforeRestore = store.Read("A").Version;
        Check(transaction.Restore(beforeRestore, "restore-after-abort") == null, "aborted transaction cannot restore");
        Check(beforeRestore.SameAs(store.Read("A").Version), "aborted transaction cannot alter target version");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        transaction = FileTransaction(store, session,
            Plan(session, "2.00", "unknown-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "unknown restore red stages and backs up");
        Check(transaction.Commit(SyntheticCommitFault.UnknownAfterCommit).Kind == SyntheticCommitKind.Unknown,
            "unknown restore red establishes uncertain result");
        Check(transaction.Restore(store.Read("A").Version, "restore-while-unknown") == null,
            "uncertain transaction cannot restore before receipt resolution");
        Check(session.ResolvePending(), "unknown commit is explicitly resolved");
        SyntheticCommitResult resolvedRestore = transaction.Restore(store.Read("A").Version, "restore-after-resolution");
        Check(resolvedRestore != null && resolvedRestore.Kind == SyntheticCommitKind.Committed,
            "resolved current commit permits explicit restore");
        EqualBytes(store.Read("A").Raw, Bytes("v1:1.00"), "resolved restore uses retained backup");
    }

    internal static void RunScopeCancellationRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:2.00"));
        store.ExternalSet("B", Bytes("v1:3.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticSavePlan unstarted = Plan(session, "4.00", "unstarted-on-switch");
        session.SelectScope("B"); Load(session);
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unloaded,
            "unstarted canceled plan remains stuck unknown after scope switch");
        Load(session);
        Check(session.ExecuteSave(unstarted, SyntheticCommitFault.None) == null,
            "canceled old plan can never execute after scope switch");
        EqualBytes(store.Read("A").Raw, Bytes("v1:2.00"), "unstarted cancellation preserves old scope");

        SyntheticSavePlan started = Plan(session, "5.00", "started-on-switch");
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent resume = new ManualResetEvent(false);
        store.CommitEntered = entered; store.ContinueCommit = resume;
        SyntheticCommitResult result = null;
        Thread worker = new Thread(delegate() { result = session.ExecuteSave(started, SyntheticCommitFault.None); });
        worker.IsBackground = true;
        worker.Start();
        try
        {
            Check(entered.WaitOne(5000), "started commit reached fake barrier");
            session.SelectScope("B");
        }
        finally
        {
            resume.Set();
            Check(worker.Join(5000), "started commit returned");
            store.CommitEntered = null; store.ContinueCommit = null;
            entered.Close(); resume.Close();
        }
        Check(!store.CommitWaitTimedOut && result != null && result.Kind == SyntheticCommitKind.LeaseExpired,
            "started commit is not misclassified by a fake barrier timeout");
        Load(session);
        Check(!session.AcceptSave(started, result) && session.LastKnown == 3m, "late result cannot update new scope");
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unknown && session.BeginRead() == null,
            "started old operation remains unknown until its receipt is resolved");
        Check(session.ResolvePending() && session.State == SyntheticSessionState.Unloaded,
            "revoked lease receipt proves the started operation did not commit");
        Load(session);
        Check(session.LastKnown == 2m, "fresh old-scope read restores the prior confirmed value");
    }

    internal static void RunRestoreStateRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "state-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "restore-state setup");
        Check(transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed && session.LastKnown == 2m,
            "restore-state original commit confirmed");
        SyntheticCommitResult restored = transaction.Restore(store.Read("A").Version, "state-restore");
        Check(restored != null && restored.Kind == SyntheticCommitKind.Committed, "restore-state operation committed");
        Check(session.State.ToString() == "NeedsReload", "successful restore leaves session claiming stale current value");
        Check(session.PrepareSave("3.00", "save-before-reread") == null, "restore blocks save until re-read");
        store.ReadUnavailable = true;
        Load(session);
        Check(session.State == SyntheticSessionState.Unavailable && session.PrepareSave("3.00", "save-after-failed-read") == null,
            "failed re-read does not declare restored state current");
        store.ReadUnavailable = false;
        Load(session);
        Check(session.State == SyntheticSessionState.Ready && session.LastKnown == 1m, "fresh re-read observes restored value");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        store.ExternalSet("B", Bytes("v1:3.00"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        transaction = FileTransaction(store, session,
            Plan(session, "2.00", "scope-restore-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "scope restore setup");
        Check(transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed, "scope restore initial commit");
        SyntheticVersion version = store.Read("A").Version;
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent resume = new ManualResetEvent(false);
        transaction.RestoreCommitted = entered; transaction.ContinueRestore = resume;
        SyntheticCommitResult delayed = null;
        Exception failure = null;
        SyntheticFileTransaction captured = transaction;
        Thread worker = new Thread(delegate()
        {
            try { delayed = captured.Restore(version, "scope-restore"); }
            catch (Exception ex) { failure = ex; }
        });
        worker.IsBackground = true;
        worker.Start();
        try
        {
            Check(entered.WaitOne(5000), "restore reached postcommit barrier");
            session.SelectScope("B"); Load(session);
        }
        finally
        {
            resume.Set();
            Check(worker.Join(5000), "delayed restore returned");
            transaction.RestoreCommitted = null; transaction.ContinueRestore = null;
            entered.Close(); resume.Close();
        }
        Check(failure == null && !transaction.RestoreWaitTimedOut && delayed != null
            && delayed.Kind == SyntheticCommitKind.Committed, "fake restore completed before scope switch");
        Check(session.State == SyntheticSessionState.Ready && session.LastKnown == 3m,
            "old scope restore cannot invalidate new scope session");
        EqualBytes(store.Read("A").Raw, Bytes("v1:1.00"), "old scope restore stays in old fake scope");
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unknown && session.ResolvePending()
            && session.State.ToString() == "NeedsReload", "returning to restored scope requires receipt and re-read");
        Load(session);
        Check(session.State == SyntheticSessionState.Ready && session.LastKnown == 1m,
            "returning scope observes restored value only after fresh read");
    }

    internal static void RunCrossDirectoryRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.BindFileDirectory("opaque-scope", "target-directory");
        store.ExternalSet("opaque-scope", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("opaque-scope"); Load(session);
        SyntheticSavePlan plan = Plan(session, "2.00", "cross-directory");
        bool rejected = false;
        try { new SyntheticFileTransaction(store, session, plan, "different-directory"); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "cross-directory staging accepted for target scope");
        Check(store.CommitAttempts == 0, "directory mismatch cannot attempt commit");
        EqualBytes(store.Read("opaque-scope").Raw, Bytes("v1:1.00"), "directory mismatch preserves target");
        bool rebindRejected = false;
        try { store.BindFileDirectory("opaque-scope", "different-directory"); }
        catch (InvalidOperationException) { rebindRejected = true; }
        Check(rebindRejected, "fake target directory binding cannot be replaced");
        SyntheticFileTransaction matching = new SyntheticFileTransaction(store, session, plan, "target-directory");
        Check(matching.Stage(SyntheticStageFault.None) && matching.MakeBackup(false), "bound target directory is accepted");
        Check(matching.StageDirectory == "target-directory" && matching.BackupDirectory == "target-directory",
            "stage and backup are tied to the target scope directory");
    }

    internal static void RunBackupConflictStateRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "backup-version-conflict"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None), "backup conflict stages data");
        store.ExternalSet("A", Bytes("v1:9.00"));
        Check(!transaction.MakeBackup(false), "backup detects changed target version");
        Check(session.State == SyntheticSessionState.Conflict, "backup version conflict leaves stale ready state");
        EqualBytes(store.Read("A").Raw, Bytes("v1:9.00"), "backup conflict preserves newer target");
    }

    internal static void RunBackupUnavailableStateRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "backup-read-unavailable"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None), "unavailable backup stages data");
        store.ReadUnavailable = true;
        Check(!transaction.MakeBackup(false), "unavailable backup fails without commit");
        Check(session.State == SyntheticSessionState.Unavailable, "unavailable backup leaves stale ready state");
        Check(session.PrepareSave("3.00", "unavailable-retry") == null, "unavailable backup blocks blind save");
    }

    internal static void RunRestoreVersionStateRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "restore-version-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "restore version setup");
        Check(transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed, "restore version committed");
        store.ExternalSet("A", Bytes("v1:9.00"));
        Check(transaction.Restore(store.Read("A").Version, "restore-newer-version") == null, "newer version is not restored over");
        Check(session.State.ToString() == "NeedsReload" || session.State == SyntheticSessionState.Conflict,
            "rejected restore leaves stale ready state");
        EqualBytes(store.Read("A").Raw, Bytes("v1:9.00"), "rejected restore preserves newer target");
    }

    internal static void RunPlanStagingProtectionRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticSavePlan plan = Plan(session, "2.00", "plan-staging-bypass");
        SyntheticFileTransaction transaction = FileTransaction(store, session, plan, "module-directory");
        FieldInfo field = typeof(SyntheticSavePlan).GetField("StagingOwner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && !field.IsPrivate) field.SetValue(plan, null);
        Check(session.ExecuteSave(plan, SyntheticCommitFault.None) == null, "mutable staging owner bypassed staging");
        Check(session.ExecuteStagedSave(plan, transaction, SyntheticCommitFault.None) == null,
            "public transaction reference cannot bypass private staging capability");
        Check(store.CommitAttempts == 0, "un-staged plan cannot reach compare-and-exchange");
        Check(transaction.Stage(SyntheticStageFault.None), "authorized fake staging remains usable");
    }

    internal static void RunPlanRestartProtectionRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticSavePlan plan = Plan(session, "2.00", "plan-restart-bypass");
        session.SelectScope("B");
        FieldInfo field = typeof(SyntheticSavePlan).GetField("Executed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && !field.IsPrivate) field.SetValue(plan, false);
        Check(session.ExecuteSave(plan, SyntheticCommitFault.None) == null, "mutable executed flag restarted canceled plan");
        Check(store.CommitAttempts == 0, "canceled plan cannot reach compare-and-exchange");
    }

    private static bool ConfirmedCurrent(MethodInfo method, SyntheticRateSession session, out decimal value)
    {
        object[] args = new object[] { 0m };
        bool confirmed = (bool)method.Invoke(session, args);
        value = (decimal)args[0];
        return confirmed;
    }

    internal static void RunConfirmedCurrentRed()
    {
        MethodInfo method = typeof(SyntheticRateSession).GetMethod("TryGetConfirmedCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Check(method != null, "confirmed-current guard is missing");
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        decimal value;
        Check(ConfirmedCurrent(method, session, out value) && value == 1m, "ready state returns confirmed value");
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "confirmed-restore-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "confirmed restore setup");
        Check(transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed, "confirmed restore commit");
        Check(transaction.Restore(store.Read("A").Version, "confirmed-restore").Kind == SyntheticCommitKind.Committed,
            "confirmed restore completes");
        Check(!ConfirmedCurrent(method, session, out value) && value == 0m, "restore pending re-read cannot return confirmed value");
        store.ReadUnavailable = true;
        Load(session);
        Check(!ConfirmedCurrent(method, session, out value) && value == 0m, "unavailable state cannot return confirmed value");
        store.ReadUnavailable = false; Load(session);
        SyntheticSavePlan unknown = Plan(session, "3.00", "confirmed-unknown");
        Check(session.AcceptSave(unknown, session.ExecuteSave(unknown, SyntheticCommitFault.UnknownBeforeWithoutReceipt)), "unknown setup");
        Check(!ConfirmedCurrent(method, session, out value) && value == 0m, "unknown state cannot return confirmed value");
        session.SelectScope("B");
        store.ExternalSet("B", Bytes("v1:1.00")); Load(session);
        SyntheticSavePlan conflict = Plan(session, "2.00", "confirmed-conflict");
        store.ExternalSet("B", Bytes("v1:9.00"));
        Check(session.AcceptSave(conflict, session.ExecuteSave(conflict, SyntheticCommitFault.None)), "conflict setup");
        Check(!ConfirmedCurrent(method, session, out value) && value == 0m, "conflict state cannot return confirmed value");
    }

    internal static void RunReceiptVersionAndReadFailureChecks()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "receipt-version-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "receipt conflict setup");
        Check(transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed, "receipt conflict initial commit");
        SyntheticVersion originalReceiptVersion = store.Read("A").Version;
        store.ExternalSet("A", Bytes("v1:9.00"));
        SyntheticCommitResult rejected = transaction.Restore(originalReceiptVersion, "receipt-version-restore");
        Check(rejected != null && rejected.Kind == SyntheticCommitKind.Conflict && session.State == SyntheticSessionState.Conflict,
            "original receipt version uses CAS and rejects later writer");
        EqualBytes(store.Read("A").Raw, Bytes("v1:9.00"), "receipt CAS conflict preserves later writer");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        transaction = FileTransaction(store, session,
            Plan(session, "2.00", "restore-unavailable-commit"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "restore unavailable setup");
        Check(transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed, "restore unavailable initial commit");
        SyntheticVersion version = store.Read("A").Version;
        store.ReadUnavailable = true;
        SyntheticCommitResult restored = transaction.Restore(version, "restore-unavailable");
        Check(restored != null && restored.Kind == SyntheticCommitKind.Committed && session.State == SyntheticSessionState.Unknown,
            "restore commit with unavailable verification stays unknown");
        Check(!session.ResolvePending(), "restore cannot resolve while current read unavailable");
        store.ReadUnavailable = false;
        Check(session.ResolvePending() && session.State.ToString() == "NeedsReload", "receipt resolution requires re-read");
        Load(session);
        Check(session.State == SyntheticSessionState.Ready && session.LastKnown == 1m, "successful re-read observes restored value");
    }

    internal static void RunForgedRestoreChecks()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticSavePlan plan = Plan(session, "2.00", "forged-original");
        SyntheticFileTransaction transaction = FileTransaction(store, session, plan, "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "forged restore setup");
        store.ExternalSet("A", Bytes("v1:2.00"));
        SyntheticVersion externallyWrittenVersion = store.Read("A").Version;
        SyntheticCommitResult forged = new SyntheticCommitResult(SyntheticCommitKind.Committed,
            plan.Lease, plan.Operation, externallyWrittenVersion);
        Check(session.AcceptSave(plan, forged) && session.State == SyntheticSessionState.Unknown,
            "forged success without original receipt cannot confirm the plan");
        Check(session.PrepareRestore(plan, externallyWrittenVersion, "forged-restore", Bytes("v1:1.00"), transaction) == null,
            "transaction reference cannot forge the private restoration capability");
        EqualBytes(store.Read("A").Raw, Bytes("v1:2.00"), "forged restore cannot change the target");

        store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        plan = Plan(session, "2.00", "genuine-original");
        transaction = FileTransaction(store, session, plan, "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false)
            && transaction.Commit(SyntheticCommitFault.None).Kind == SyntheticCommitKind.Committed,
            "genuine receipt exists for capability test");
        Check(session.PrepareRestore(plan, store.Read("A").Version, "wrong-capability", Bytes("v1:9.00"), transaction) == null,
            "genuine receipt alone cannot authorize a caller-supplied backup");
        EqualBytes(store.Read("A").Raw, Bytes("v1:2.00"), "untrusted backup cannot change the target");
    }

    internal static void RunBackupFailureScopeChecks()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        store.ExternalSet("B", Bytes("v1:3.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticFileTransaction transaction = FileTransaction(store, session,
            Plan(session, "2.00", "stale-backup-scope"), "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None), "old-scope backup setup");
        session.SelectScope("B"); Load(session);
        store.ReadUnavailable = true;
        Check(!transaction.MakeBackup(false), "old-scope unavailable backup is rejected");
        Check(session.State == SyntheticSessionState.Ready && session.LastKnown == 3m,
            "old-scope backup failure cannot invalidate the new scope");
    }

    internal static void RunUnstartedUnknownScopeRed()
    {
        SyntheticRateStorage store = new SyntheticRateStorage();
        store.ExternalSet("A", Bytes("v1:1.00"));
        store.ExternalSet("B", Bytes("v1:3.00"));
        SyntheticRateSession session = new SyntheticRateSession(store);
        session.SelectScope("A"); Load(session);
        SyntheticSavePlan unstarted = Plan(session, "2.00", "unstarted-forged-unknown");
        SyntheticFileTransaction transaction = FileTransaction(store, session, unstarted, "module-directory");
        Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "forged unknown staging setup");
        store.ExternalSet("A", Bytes("v1:2.00"));
        SyntheticCommitResult forged = new SyntheticCommitResult(SyntheticCommitKind.Committed,
            unstarted.Lease, unstarted.Operation, store.Read("A").Version);
        Check(session.AcceptSave(unstarted, forged) && session.State == SyntheticSessionState.Unknown,
            "untrusted success without receipt is unknown");
        session.SelectScope("B"); Load(session);
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unloaded,
            "unstarted forged result left scope permanently unknown");
        Check(session.ExecuteSave(unstarted, SyntheticCommitFault.None) == null,
            "scope switch irreversibly cancels the unstarted fake plan");
        int attemptsBefore = store.CommitAttempts;
        Check(transaction.Commit(SyntheticCommitFault.None) == null && store.CommitAttempts == attemptsBefore,
            "scope switch prevents the old staged transaction from entering the fake store");
        Load(session);
        decimal decimalValue;
        Check(session.TryGetConfirmedCurrent(out decimalValue) && decimalValue == 2m,
            "fresh read after cancellation observes the other fake writer");

        SyntheticSavePlan started = Plan(session, "4.00", "started-unknown-stays-unknown");
        SyntheticCommitResult unknown = session.ExecuteSave(started, SyntheticCommitFault.UnknownBeforeWithoutReceipt);
        Check(session.AcceptSave(started, unknown) && session.State == SyntheticSessionState.Unknown,
            "executed unknown result stays unresolved");
        session.SelectScope("B"); Load(session);
        session.SelectScope("A");
        Check(session.State == SyntheticSessionState.Unknown && !session.ResolvePending(),
            "started unknown operation remains blocked without its receipt");
        RunUnstartedTerminalScopeRed();
    }

    internal static void RunUnstartedTerminalScopeRed()
    {
        foreach (SyntheticCommitKind kind in new SyntheticCommitKind[] {
            SyntheticCommitKind.Conflict, SyntheticCommitKind.LeaseExpired })
        {
            SyntheticRateStorage store = new SyntheticRateStorage();
            store.ExternalSet("A", Bytes("v1:1.00"));
            store.ExternalSet("B", Bytes("v1:3.00"));
            SyntheticRateSession session = new SyntheticRateSession(store);
            session.SelectScope("A"); Load(session);
            SyntheticSavePlan plan = Plan(session, "2.00", "unstarted-terminal-" + kind);
            SyntheticFileTransaction transaction = FileTransaction(store, session, plan, "module-directory");
            Check(transaction.Stage(SyntheticStageFault.None) && transaction.MakeBackup(false), "unstarted terminal setup");
            SyntheticCommitResult forged = new SyntheticCommitResult(kind, plan.Lease, plan.Operation, null);
            bool accepted = session.AcceptSave(plan, forged);
            Check(!accepted || session.State == SyntheticSessionState.Conflict, "untrusted terminal result is not confirmed");
            session.SelectScope("B"); Load(session);
            int before = store.CommitAttempts;
            Check(transaction.Commit(SyntheticCommitFault.None) == null && store.CommitAttempts == before,
                "unstarted terminal result allowed a canceled staged plan into the store");
            decimal confirmed;
            Check(session.TryGetConfirmedCurrent(out confirmed) && confirmed == 3m, "old staged plan cannot affect new scope");
            EqualBytes(store.Read("A").Raw, Bytes("v1:1.00"), "forged terminal result preserves old fake target");
        }
        RunStartedForgedTerminalChecks();
    }

    private static void RunStartedForgedTerminalChecks()
    {
        foreach (SyntheticCommitKind kind in new SyntheticCommitKind[] {
            SyntheticCommitKind.Conflict, SyntheticCommitKind.LeaseExpired })
        {
            SyntheticRateStorage store = new SyntheticRateStorage();
            store.ExternalSet("A", Bytes("v1:1.00"));
            store.ExternalSet("B", Bytes("v1:3.00"));
            SyntheticRateSession session = new SyntheticRateSession(store);
            session.SelectScope("A"); Load(session);
            SyntheticSavePlan plan = Plan(session, "2.00", "started-terminal-" + kind);
            ManualResetEvent entered = new ManualResetEvent(false);
            ManualResetEvent resume = new ManualResetEvent(false);
            store.CommitEntered = entered; store.ContinueCommit = resume;
            SyntheticCommitResult actual = null;
            Thread worker = new Thread(delegate() { actual = session.ExecuteSave(plan, SyntheticCommitFault.None); });
            worker.IsBackground = true;
            worker.Start();
            try
            {
                Check(entered.WaitOne(5000), "started terminal operation reached fake barrier");
                SyntheticCommitResult forged = new SyntheticCommitResult(kind, plan.Lease, plan.Operation, null);
                Check(session.AcceptSave(plan, forged) && session.State == SyntheticSessionState.Unknown,
                    "unreceipted terminal result cannot clear a started operation");
                Check(session.PrepareSave("4.00", "blocked-" + kind) == null && !session.ResolvePending(),
                    "started operation stays blocked without its receipt");
                session.SelectScope("B"); Load(session);
                session.SelectScope("A");
                Check(session.State == SyntheticSessionState.Unknown && session.BeginRead() == null,
                    "scope changes preserve started terminal uncertainty");
            }
            finally
            {
                resume.Set();
                Check(worker.Join(5000), "started terminal operation returned");
                store.CommitEntered = null; store.ContinueCommit = null;
                entered.Close(); resume.Close();
            }
            Check(!store.CommitWaitTimedOut && actual != null && actual.Kind == SyntheticCommitKind.LeaseExpired,
                "started terminal operation sees the revoked lease");
            Check(session.ResolvePending() && session.State == SyntheticSessionState.Unloaded,
                "matching not-committed receipt clears started terminal uncertainty");
            Load(session);
            decimal confirmed;
            Check(session.TryGetConfirmedCurrent(out confirmed) && confirmed == 1m,
                "fresh read after receipt confirms the old scope");
        }
    }

    internal static void RunNotCommittedConcurrentWriterRed()
    {
        foreach (bool wasUnknown in new bool[] { true, false })
        {
            SyntheticRateStorage store = new SyntheticRateStorage();
            store.ExternalSet("A", Bytes("v1:1.00"));
            SyntheticRateSession session = new SyntheticRateSession(store);
            session.SelectScope("A"); Load(session);
            SyntheticSavePlan plan = Plan(session, "2.00", "not-committed-writer-" + wasUnknown);
            SyntheticCommitResult result = session.ExecuteSave(plan, wasUnknown
                ? SyntheticCommitFault.UnknownBeforeWithReceipt : SyntheticCommitFault.NotCommitted);
            if (wasUnknown)
                Check(session.AcceptSave(plan, result) && session.State == SyntheticSessionState.Unknown,
                    "receipt regression establishes unknown result");
            store.ExternalSet("A", Bytes("v1:9.00"));
            if (wasUnknown)
                Check(session.ResolvePending(), "not-committed receipt resolves historical outcome");
            else
                Check(session.AcceptSave(plan, result), "direct not-committed result is accepted");
            decimal confirmed;
            Check(!session.TryGetConfirmedCurrent(out confirmed),
                "not-committed receipt leaves old value confirmed after another writer");
            Check(session.LastKnown == 1m && session.PrepareSave("3.00", "before-reread-" + wasUnknown) == null,
                "historical value remains unconfirmed until a fresh read");
            store.ReadUnavailable = true;
            Load(session);
            Check(session.State == SyntheticSessionState.Unavailable && !session.TryGetConfirmedCurrent(out confirmed),
                "failed re-read does not restore the old confirmed value");
            store.ReadUnavailable = false;
            Load(session);
            Check(session.TryGetConfirmedCurrent(out confirmed) && confirmed == 9m,
                "successful re-read observes the other writer");
        }
    }

    internal static void Run()
    {
        Type storage = typeof(SyntheticRateStorageTests).Assembly.GetType("RealisticCityTime.Tests.SyntheticRateStorage");
        if (storage == null)
            throw new Exception("SyntheticRateStorage is missing; no versioned storage transaction fixture exists.");
        MissingAndNoImplicitWrites(); CopiesAndVersions(); CorruptionAndMigration(); FailedAndConflictingCommits();
        UnknownNeedsReceiptAndCurrentVersion(); ScopeSwitchAndLateResults(); SameScopeReadOrdering(); UnavailableReadPreservesCertaintyBoundary();
        StagingFailuresDoNotTouchTarget(); StagingMigrationBackupAndConflict(); StagingUnknownRetainsBackup();
        RunRestoreRed();
        RunScopeCancellationRed();
        RunRestoreStateRed();
        RunCrossDirectoryRed();
        RunBackupConflictStateRed(); RunBackupUnavailableStateRed(); RunRestoreVersionStateRed();
        RunPlanStagingProtectionRed(); RunPlanRestartProtectionRed(); RunConfirmedCurrentRed();
        RunReceiptVersionAndReadFailureChecks();
        RunForgedRestoreChecks(); RunBackupFailureScopeChecks();
        RunUnstartedUnknownScopeRed();
        RunNotCommittedConcurrentWriterRed();
        Console.WriteLine("PASS versioned fake rate storage, migration, receipt, conflict and scope isolation checks");
    }
}
