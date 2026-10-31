// Signature: OpenAI-Codex_Responsible: Realistic_Import_Export_Control
namespace CargoPreview;

public enum TaskKind { Unknown, Cargo, Rescue, Garbage, Deathcare }
public enum Flow { Domestic, Import, Export }

// AvailableAmount must already exclude known in-transit commitments in the caller's unit.
// IDs belong to one snapshot generation; no game TransferReason mapping is supplied here.
public sealed record Offer(string Id, string Endpoint, string Commodity, string Unit,
    long AvailableAmount, bool IsSupply, bool External, bool InTransitKnown,
    int? Priority, long? WaitingTicks, TaskKind Task);

public sealed record Snapshot(string Generation, long Sequence, bool Complete,
    bool Consistent, string Source, IReadOnlyList<Offer> Offers);

public sealed record Match(string SupplyId, string DemandId, long Amount, Flow Direction, string Commodity, string Unit)
{
    public bool ReachabilityKnown => false;
}

public sealed record Preview(IReadOnlyList<Match> Matches, long? Total,
    bool ExclusiveAlternatives, IReadOnlyList<string> Messages)
{
    public bool CanDispatch => false;
}

public sealed record Observation(bool HasGameData, string Status)
{
    public static Observation Disconnected() => new(false, "Game data source unavailable");
}

public static class Planner
{
    public static Preview Build(Snapshot snapshot, string currentGeneration, long currentSequence)
    {
        static Preview Block(string reason) => new(Array.Empty<Match>(), null, false, new[] { reason });
        if (string.IsNullOrWhiteSpace(snapshot.Source)) return Block("source-unknown");
        if (string.IsNullOrWhiteSpace(snapshot.Generation) || snapshot.Generation != currentGeneration || snapshot.Sequence != currentSequence)
            return Block("stale-snapshot");
        if (!snapshot.Complete) return Block("incomplete-snapshot");
        if (!snapshot.Consistent) return Block("non-simultaneous-snapshot");
        var messages = new List<string>();
        var offers = new List<Offer>();
        var seen = new Dictionary<string, Offer>(StringComparer.Ordinal);
        foreach (var offer in snapshot.Offers)
        {
            if (string.IsNullOrWhiteSpace(offer.Id)) { messages.Add("missing-id"); continue; }
            if (seen.TryGetValue(offer.Id, out var previous))
            {
                if (previous != offer) return Block("conflicting-duplicate");
                continue;
            }
            seen.Add(offer.Id, offer);
            if (offer.Task != TaskKind.Cargo) { messages.Add("excluded-task:" + offer.Id); continue; }
            if (string.IsNullOrWhiteSpace(offer.Endpoint) || string.IsNullOrWhiteSpace(offer.Commodity) ||
                string.IsNullOrWhiteSpace(offer.Unit) || offer.AvailableAmount <= 0 || !offer.InTransitKnown || offer.WaitingTicks < 0)
            { messages.Add("unusable-offer:" + offer.Id); continue; }
            offers.Add(offer);
        }
        bool alternatives = offers.Any(o => o.Priority == null || o.WaitingTicks == null);
        var supplies = offers.Where(o => o.IsSupply).OrderByDescending(o => o.Priority)
            .ThenByDescending(o => o.WaitingTicks).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();
        var demands = offers.Where(o => !o.IsSupply).OrderByDescending(o => o.Priority)
            .ThenByDescending(o => o.WaitingTicks).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();
        var remaining = offers.ToDictionary(o => o.Id, o => o.AvailableAmount, StringComparer.Ordinal);
        var matches = new List<Match>();
        bool singleQuantityGroup = offers.Select(o => (o.Commodity, o.Unit)).Distinct().Count() <= 1;
        long total = 0;
        try
        {
            foreach (var supply in supplies)
                foreach (var demand in demands)
                {
                    if (supply.Endpoint == demand.Endpoint || supply.Commodity != demand.Commodity || supply.Unit != demand.Unit ||
                        (supply.External && demand.External)) continue;
                    long amount = alternatives ? Math.Min(supply.AvailableAmount, demand.AvailableAmount)
                        : Math.Min(remaining[supply.Id], remaining[demand.Id]);
                    if (amount == 0) continue;
                    var direction = supply.External ? Flow.Import : demand.External ? Flow.Export : Flow.Domestic;
                    matches.Add(new(supply.Id, demand.Id, amount, direction, supply.Commodity, supply.Unit));
                    if (!alternatives)
                    {
                        if (singleQuantityGroup) total = checked(total + amount);
                        remaining[supply.Id] -= amount;
                        remaining[demand.Id] -= amount;
                    }
                }
        }
        catch (OverflowException) { return Block("quantity-overflow"); }
        if (alternatives) messages.Add("exclusive-alternatives-not-additive");
        if (!singleQuantityGroup) messages.Add("mixed-quantities-no-total");
        messages.Add("reachability-unknown-no-dispatch");
        return new(matches.AsReadOnly(), alternatives || !singleQuantityGroup ? null : total, alternatives, messages.AsReadOnly());
    }
}
