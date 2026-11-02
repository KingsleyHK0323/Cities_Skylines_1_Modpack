// Signature: OpenAI-Codex_Realistic_Import_Export_Control
using CargoPreview;

static class ObservationChecks
{
    public static void Run(Action<bool, string> check)
    {
        var row = new RawCargoRecord("source-record-1", "raw-reason", 12, Boundary.Local, Boundary.External);
        var capture = new RawCargoCapture("fixture only", "city-1", 2, true, true, new[] { row });
        var result = CargoObservationAdapter.Read(capture, "city-1", 2);
        check(result.Rows.Single().RawCounter == 12 && result.Rows.Single().QuantityUnit == null, "raw counter is not cargo quantity");
        check(result.Rows.Single().Direction == ObservedFlow.Export && !result.CanDispatch, "explicit endpoint evidence yields export without dispatch");
        check(result.CargoTotal == null && !result.CanCreatePlannerSnapshot, "unverified units never produce total or planner snapshot");
        var duplicate = CargoObservationAdapter.Read(capture with { Records = new[] { row, row } }, "city-1", 2);
        check(duplicate.Rows.Count == 1 && duplicate.DuplicatesIgnored == 1, "identical source record observed once");
        var conflict = CargoObservationAdapter.Read(capture with { Records = new[] { row, row with { RawCounter = 13 } } }, "city-1", 2);
        check(conflict.Rows.Count == 0 && conflict.Messages.Contains("conflicting-source-record"), "conflicting duplicate invalidates observation");
        var unknown = CargoObservationAdapter.Read(capture with { Records = new[] { row with { To = Boundary.Unknown } } }, "city-1", 2);
        check(unknown.Rows.Single().Direction == ObservedFlow.Unknown, "unknown boundary cannot imply export");
        var transit = CargoObservationAdapter.Read(capture with { Records = new[] { row with { From = Boundary.External } } }, "city-1", 2);
        check(transit.Rows.Single().Direction == ObservedFlow.Transit && transit.CargoTotal == null, "external transit not counted as city trade");
        check(CargoObservationAdapter.Read(capture, "other-city", 2).Rows.Count == 0, "stale observation generation blocked");
        check(CargoObservationAdapter.Read(capture, "city-1", 3).Rows.Count == 0, "stale observation sequence blocked");
        check(CargoObservationAdapter.Read(capture with { Complete = false }, "city-1", 2).Rows.Count == 0, "partial observation blocked");
        check(CargoObservationAdapter.Read(capture with { Consistent = false }, "city-1", 2).Rows.Count == 0, "cross-time observation blocked");
        check(CargoObservationAdapter.Read(capture with { Source = "" }, "city-1", 2).Rows.Count == 0, "unknown observation source blocked");
        check(CargoObservationAdapter.Read(capture with { Records = new[] { row with { Id = "" } } }, "city-1", 2).Rows.Count == 0, "unidentified record cannot be deduplicated");
        check(CargoObservationAdapter.Unavailable().Rows.Count == 0 && CargoObservationAdapter.Unavailable().Status == "Game data source unavailable", "missing adapter never supplies synthetic game rows");
    }
}
