// Signature: OpenAI-Codex_Realistic_Import_Export_Control
namespace CargoPreview;

// Boundary labels must come from caller evidence, not a guessed building ID range.
public enum Boundary { Unknown, Local, External }
public enum ObservedFlow { Unknown, Domestic, Import, Export, Transit }

// Id identifies one source record within this capture, not merely a building or vehicle.
// RawCounter is opaque: this adapter does not interpret it as mass, loads or inventory.
public sealed record RawCargoRecord(string Id, string RawReason, long? RawCounter, Boundary From, Boundary To);
public sealed record RawCargoCapture(string Source, string Generation, long Sequence, bool Complete,
    bool Consistent, IReadOnlyList<RawCargoRecord> Records);
public sealed record CargoObservationRow(string Id, string RawReason, long? RawCounter, ObservedFlow Direction)
{
    public string? QuantityUnit => null;
}
public sealed record CargoObservation(string Status, string Source, IReadOnlyList<CargoObservationRow> Rows,
    int DuplicatesIgnored, IReadOnlyList<string> Messages)
{
    public long? CargoTotal => null;
    public bool CanDispatch => false;
    public bool CanCreatePlannerSnapshot => false;
}

// This is a transport-neutral reader of caller-supplied records, not a native game capture.
// No reason whitelist, unit conversion, in-transit subtraction or allocation is inferred.
public static class CargoObservationAdapter
{
    public static CargoObservation Unavailable() => Block("Game data source unavailable", "", "native-capture-not-implemented");

    public static CargoObservation Read(RawCargoCapture capture, string currentGeneration, long currentSequence)
    {
        if (string.IsNullOrWhiteSpace(capture.Source)) return Block("Observation unavailable", "", "source-unknown");
        if (string.IsNullOrWhiteSpace(capture.Generation) || capture.Generation != currentGeneration || capture.Sequence != currentSequence)
            return Block("Observation unavailable", capture.Source, "stale-capture");
        if (!capture.Complete || !capture.Consistent)
            return Block("Observation unavailable", capture.Source, "incomplete-or-non-simultaneous-capture");
        var records = new Dictionary<string, RawCargoRecord>(StringComparer.Ordinal);
        int duplicates = 0;
        foreach (var record in capture.Records)
        {
            if (string.IsNullOrWhiteSpace(record.Id)) return Block("Observation unavailable", capture.Source, "missing-record-id");
            if (records.TryGetValue(record.Id, out var previous))
            {
                if (previous != record) return Block("Observation unavailable", capture.Source, "conflicting-source-record");
                duplicates++;
            }
            else records.Add(record.Id, record);
        }
        var rows = records.Values.OrderBy(r => r.Id, StringComparer.Ordinal)
            .Select(r => new CargoObservationRow(r.Id, r.RawReason, r.RawCounter, Classify(r.From, r.To)))
            .ToArray();
        return new("Caller-supplied records; quantity semantics unverified", capture.Source,
            Array.AsReadOnly(rows), duplicates, new[] { "raw-counters-not-quantities", "in-transit-semantics-unknown", "native-reason-mapping-unknown" });
    }

    static CargoObservation Block(string status, string source, string reason)
        => new(status, source, Array.Empty<CargoObservationRow>(), 0, new[] { reason });

    static ObservedFlow Classify(Boundary from, Boundary to) => (from, to) switch
    {
        (Boundary.Local, Boundary.Local) => ObservedFlow.Domestic,
        (Boundary.External, Boundary.Local) => ObservedFlow.Import,
        (Boundary.Local, Boundary.External) => ObservedFlow.Export,
        (Boundary.External, Boundary.External) => ObservedFlow.Transit,
        _ => ObservedFlow.Unknown
    };
}
