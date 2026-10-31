// Signature: OpenAI-Codex_Responsible: Realistic_Import_Export_Control
using CargoPreview;

static class Checks
{
    static int count;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
    static Offer O(string id, long amount, bool supply = true, string kind = "goods", string endpoint = "", int? priority = 1, long? wait = 10)
        => new(id, endpoint == "" ? id : endpoint, kind, "test-units", amount, supply, false, true, priority, wait, TaskKind.Cargo);
    static Snapshot S(params Offer[] offers) => new("city-1", 1, true, true, "synthetic fixture; not game data", offers);
    static Preview P(Snapshot s) => Planner.Build(s, "city-1", 1);
    public static void Main()
    {
        var basic = P(S(O("s", 10), O("d", 6, false)));
        Check(basic.Matches.Single().Amount == 6 && basic.Total == 6, "10 supply and 6 demand conserve quantity");
        var competing = P(S(O("s", 10), O("d1", 8, false), O("d2", 8, false)));
        Check(competing.Total == 10 && competing.Matches.Sum(m => m.Amount) == 10, "competing demand shares remaining supply");
        var differentGoods = P(S(O("s", 10), O("d", 6, false, "ore")));
        Check(differentGoods.Matches.Count == 0 && differentGoods.Total == null, "different goods never match or share a total");
        Check(P(S(O("s", 10, endpoint: "same"), O("d", 6, false, endpoint: "same"))).Total == 0, "same endpoint never matches");
        Check(P(S(O("s", -1), O("d", 6, false))).Matches.Count == 0, "negative amount rejected");
        foreach (var category in new[] { TaskKind.Rescue, TaskKind.Garbage, TaskKind.Deathcare, TaskKind.Unknown })
            Check(P(S(O("s", 10) with { Task = category }, O("d", 6, false))).Matches.Count == 0, "exclude " + category);
        Check(P(S(O("s", 10) with { Unit = "" }, O("d", 6, false))).Matches.Count == 0, "unknown unit rejected");
        Check(P(S(O("s", 10) with { InTransitKnown = false }, O("d", 6, false))).Matches.Count == 0, "unknown in-transit data blocks allocation");
        Check(P(S(O("s", 10), O("s", 10), O("d", 20, false))).Total == 10, "duplicate source not counted twice");
        Check(P(S(O("s", 10), O("s", 11), O("d", 20, false))).Matches.Count == 0, "conflicting duplicate source invalidates preview");
        Check(P(S(O("s", 10), O("d", 6, false)) with { Complete = false }).Matches.Count == 0, "partial snapshot cannot allocate");
        Check(P(S(O("s", 10), O("d", 6, false)) with { Consistent = false }).Matches.Count == 0, "cross-batch inconsistency cannot allocate");
        Check(Planner.Build(S(O("s", 10), O("d", 6, false)), "other-city", 1).Matches.Count == 0, "stale city rejected");
        Check(Planner.Build(S(O("s", 10), O("d", 6, false)), "city-1", 2).Matches.Count == 0, "stale capture rejected");
        var alternatives = P(S(O("s", 10, priority: null), O("d1", 8, false), O("d2", 8, false)));
        Check(alternatives.ExclusiveAlternatives && alternatives.Total == null && alternatives.Matches.Count == 2, "unranked alternatives cannot be summed");
        Check(basic.Matches.All(m => !m.ReachabilityKnown) && !basic.CanDispatch, "unknown reachability is never dispatchable");
        var reversed = P(S(O("d2", 8, false), O("d1", 8, false), O("s", 10)));
        Check(competing.Matches.SequenceEqual(reversed.Matches), "ranked results stable under reordered input");
        var huge = P(S(O("s1", long.MaxValue), O("s2", long.MaxValue), O("d1", long.MaxValue, false), O("d2", long.MaxValue, false)));
        Check(huge.Total == null && huge.Matches.Count == 0 && huge.Messages.Contains("quantity-overflow"), "overflow fails closed");
        Check(!Observation.Disconnected().HasGameData && Observation.Disconnected().Status == "Game data source unavailable", "disconnected observation is explicit");
        Check(P(S(O("s", 10) with { External = true }, O("d", 6, false))).Matches.Single().Direction == Flow.Import, "external supply classified as import");
        Check(P(S(O("s", 10), O("d", 6, false) with { External = true })).Matches.Single().Direction == Flow.Export, "external demand classified as export");
        Check(P(S(O("s", 10) with { External = true }, O("d", 6, false) with { External = true })).Matches.Count == 0, "outside transit excluded from city allocation");
        Check(P(S(O("s", 10), O("d", 6, false) with { Unit = "other-unit" })).Matches.Count == 0, "units never silently converted");
        Check(P(S(O("s", 10), O("d", 6, false)) with { Source = "" }).Matches.Count == 0, "unknown source rejected");
        var mixed = P(S(O("s1", 10), O("d1", 6, false), O("s2", 10, kind: "ore"), O("d2", 6, false, "ore")));
        Check(mixed.Total == null && mixed.Matches.Count == 2 && mixed.Matches.All(m => m.Unit == "test-units"), "mixed commodities retain units and have no combined total");
        Console.WriteLine($"{count} checks passed; synthetic fixtures only.");
    }
}
