// Signature: OpenAI-Codex_Realistic_Border_Control
using System;
using System.Collections.Generic;
using RealisticBorderControl;

static class ObservationChecks
{
    public static void Run(Action<bool> check, Action<Action> reject)
    {
        var context = new ObservationStamp("city-load-1", 4, 2, 100);
        var rows = new List<SuppliedConnectionRow> { new SuppliedConnectionRow("road-1", 7, 12, null) };
        var snapshot = SuppliedObservationAdapter.CopyForPreview(context, rows);
        rows.Clear();
        IReadOnlyList<SuppliedConnectionRow> preview;
        check(snapshot.TryGetPreviewRows(new ObservationStamp("city-load-1", 4, 2, 105), 5, out preview) == SnapshotFreshness.Current);
        check(preview.Count == 1 && preview[0].InboundPassengers == 12 && preview[0].OutboundPassengers == null);
        check(preview[0].ConnectionGeneration == 7);
        check(!snapshot.IsGameConnected && snapshot.DataSource.Contains("Unverified"));
        check(snapshot.TryGetPreviewRows(new ObservationStamp("city-load-2", 4, 2, 100), 5, out preview) == SnapshotFreshness.CityChanged && preview.Count == 0);
        check(snapshot.TryGetPreviewRows(new ObservationStamp("city-load-1", 5, 2, 100), 5, out preview) == SnapshotFreshness.Superseded && preview.Count == 0);
        check(snapshot.TryGetPreviewRows(new ObservationStamp("city-load-1", 3, 2, 100), 5, out preview) == SnapshotFreshness.Superseded);
        check(snapshot.TryGetPreviewRows(new ObservationStamp("city-load-1", 4, 3, 100), 5, out preview) == SnapshotFreshness.PolicyChanged && preview.Count == 0);
        check(snapshot.TryGetPreviewRows(new ObservationStamp("city-load-1", 4, 2, 99), 5, out preview) == SnapshotFreshness.TimeReversed && preview.Count == 0);
        check(snapshot.TryGetPreviewRows(new ObservationStamp("city-load-1", 4, 2, 106), 5, out preview) == SnapshotFreshness.Expired && preview.Count == 0);
        check(snapshot.TryGetPreviewRows(context, 0, out preview) == SnapshotFreshness.Current);
        var empty = SuppliedObservationAdapter.CopyForPreview(context, new SuppliedConnectionRow[0]);
        check(empty.TryGetPreviewRows(context, 0, out preview) == SnapshotFreshness.Current && preview.Count == 0 && !empty.IsGameConnected);
        var unknown = SuppliedObservationAdapter.CopyForPreview(context, new[] { new SuppliedConnectionRow("unknown", 0, null, null) });
        check(unknown.TryGetPreviewRows(context, 0, out preview) == SnapshotFreshness.Current && preview[0].InboundPassengers == null);
        var zero = SuppliedObservationAdapter.CopyForPreview(context, new[] { new SuppliedConnectionRow("zero", 0, 0, 0) });
        check(zero.TryGetPreviewRows(context, 0, out preview) == SnapshotFreshness.Current && preview[0].InboundPassengers == 0);
        reject(() => new ObservationStamp("", 0, 0, 0));
        reject(() => new ObservationStamp("x", -1, 0, 0));
        reject(() => new ObservationStamp("x", 0, -1, 0));
        reject(() => new ObservationStamp("x", 0, 0, -1));
        reject(() => new SuppliedConnectionRow("", 0, null, null));
        reject(() => new SuppliedConnectionRow("x", -1, null, null));
        reject(() => new SuppliedConnectionRow("x", 0, -1, null));
        reject(() => new SuppliedConnectionRow("x", 0, null, -1));
        reject(() => SuppliedObservationAdapter.CopyForPreview(null, rows));
        reject(() => SuppliedObservationAdapter.CopyForPreview(context, null));
        reject(() => SuppliedObservationAdapter.CopyForPreview(context, new SuppliedConnectionRow[] { null }));
        reject(() => SuppliedObservationAdapter.CopyForPreview(context, new[] { new SuppliedConnectionRow("same", 1, 1, 1), new SuppliedConnectionRow("same", 2, 2, 2) }));
        reject(() => snapshot.TryGetPreviewRows(context, -1, out preview));
        reject(() => snapshot.TryGetPreviewRows(null, 1, out preview));
        var maximum = SuppliedObservationAdapter.CopyForPreview(new ObservationStamp("x", 0, 0, 0), new SuppliedConnectionRow[0]);
        check(maximum.TryGetPreviewRows(new ObservationStamp("x", 0, 0, long.MaxValue), long.MaxValue, out preview) == SnapshotFreshness.Current);
        check(maximum.TryGetPreviewRows(new ObservationStamp("x", 0, 0, long.MaxValue), long.MaxValue - 1, out preview) == SnapshotFreshness.Expired);
    }
}
