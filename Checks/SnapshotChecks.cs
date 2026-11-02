// Signature: OpenAI-Codex_Realistic_Urban_Population
using System;
using RealisticUrbanPopulation.Demand;

internal static class SnapshotChecks
{
    private static int assertions;
    public static int AssertionCount { get { return assertions; } }
    private sealed class Source : IDemandSnapshotSource
    {
        public bool Success = true;
        public bool Throws;
        public int Reads;
        public long RequestedEpoch;
        public Action DuringRead;
        public DemandReadValues Values = new DemandReadValues(-5, 37, 150);
        public bool TryRead(long expectedEpoch, out DemandReadValues values)
        {
            Reads++;
            RequestedEpoch = expectedEpoch;
            DemandReadValues captured = Values;
            if (DuringRead != null) DuringRead();
            if (Throws) throw new InvalidOperationException("Synthetic read failure");
            values = captured;
            return Success;
        }
    }

    private static void Assert(bool value, string message)
    { assertions++; if (!value) throw new Exception(message); }

    public static void ReadOnlyBoundary()
    {
        DemandSession session = new DemandSession();
        DemandSnapshotReader reader = new DemandSnapshotReader(session);
        Source source = new Source();
        Assert(!reader.Bind(0, source, "fixture"), "No city cannot bind");
        long a = session.BeginCity();
        Assert(!reader.Bind(a, source, "fixture"), "Loading cannot bind");
        Assert(reader.Capture().Values == null && source.Reads == 0, "No read before readiness");
        session.CompleteCityLoad(a);
        Assert(reader.Bind(a, source, "fixture-A"), "Ready binding");
        long revision = session.ReadState().Revision;
        DemandReadSnapshot first = reader.Capture();
        Assert(first.Status == DemandReadStatus.Captured && first.SourceId == "fixture-A" && first.Epoch == a,
            "Captured provenance");
        Assert(source.RequestedEpoch == a, "Read source receives expected city epoch");
        Assert(first.Values.Residential == -5 && first.Values.Commercial == 37 && first.Values.Workplace == 150,
            "Preserve raw values without policy clamping");
        Assert(!first.GameSourceVerified && !first.OfficeCouplingKnown, "Never invent game evidence");
        Assert(session.ReadState().Revision == revision && !session.ReadState().Armed, "Reader does not modify session");
        Assert(reader.Capture().Sequence > first.Sequence, "Observation sequence increases");
        source.Success = false;
        Assert(reader.Capture().Status == DemandReadStatus.SourceFailed && reader.ReadLatest().Values == null,
            "Failure cannot reuse old values");
        source.Success = true;
        source.Throws = true;
        Assert(reader.Capture().Status == DemandReadStatus.SourceFailed, "Read exception is unavailable");
        source.Throws = false;
        source.Values = null;
        Assert(reader.Capture().Status == DemandReadStatus.SourceFailed, "Missing sample rejected");
        source.Values = new DemandReadValues(1, 2, 3);
        reader.Capture();
        session.SetControlPermission(a, ControlPermission.Allowed);
        Assert(reader.ReadLatest().Values == null, "Session revision invalidates cached observation");
        Assert(reader.Capture().Status == DemandReadStatus.Captured, "New observation after revision change");
        source.DuringRead = delegate { session.RestoreNative(a); };
        Assert(reader.Capture().Status == DemandReadStatus.Invalidated, "Revision changed during read");
        source.DuringRead = delegate { reader.Invalidate(a); };
        Assert(reader.Capture().Status == DemandReadStatus.Invalidated && reader.ReadLatest().Values == null,
            "Invalidation during read cannot resurrect data");
        source.DuringRead = null;
        Assert(reader.Bind(a, source, "fixture-A"), "Rebind after invalidation");
        source.Values = new DemandReadValues(1, 2, 3);
        source.DuringRead = delegate
        {
            source.DuringRead = null;
            source.Values = new DemandReadValues(4, 5, 6);
            reader.Capture();
        };
        Assert(reader.Capture().Status == DemandReadStatus.Invalidated, "Older same-source completion discarded");
        Assert(reader.ReadLatest().Values.Residential == 4, "Older completion cannot replace newer observation");

        long b = session.BeginCity();
        Assert(reader.ReadLatest().Values == null, "New city invalidates A");
        session.CompleteCityLoad(b);
        Source second = new Source();
        Assert(reader.Bind(b, second, "fixture-B"), "Bind B");
        DemandReadSnapshot snapshotB = reader.Capture();
        Assert(!reader.Invalidate(a) && !reader.Bind(a, source, "stale-A"), "A cannot invalidate or rebind B");
        Assert(reader.ReadLatest().Sequence == snapshotB.Sequence, "Stale events preserve B");
        second.DuringRead = delegate
        {
            second.DuringRead = null;
            reader.Bind(b, source, "replacement-B");
            reader.Capture();
        };
        Assert(reader.Capture().Status == DemandReadStatus.Invalidated, "Replaced source completion discarded");
        Assert(reader.ReadLatest().SourceId == "replacement-B" && reader.ReadLatest().Values != null,
            "Discarded completion preserves newer observation");
        session.EndCity(b);
        int reads = source.Reads;
        Assert(reader.ReadLatest().Values == null && reader.Capture().Values == null && source.Reads == reads,
            "Capture initiated after closure does not read or retain values");
        Console.WriteLine("Snapshot boundary fixtures only; no verified game source, simulation-thread proof or office separation.");
    }
}
