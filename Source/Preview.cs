// OpenAI-Codex_Intercity_Resource_Sharing
namespace Intercity;
public enum Resource { Water, Electricity, Heat }
// Synthetic integer quantities per manually advanced window, not game units.
public sealed record Input(string Scenario, string Contract, int Version, Resource Resource,
    string Source, string Receiver, long Window, long Supply, long OwnDemand,
    long Demand, long LocalSupply, long Limit, bool Available);
public sealed record Output(long Transfer, long? Surplus, long? Shortfall, string Reason);
// Serialized, in-memory use only. No game state, persistent ledger, or cancellation.
public sealed class Preview
{
    private string? scenario;
    private readonly Dictionary<Resource, (Input Input, Output Output)> latest = new();
    public Output Calculate(Input input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (new[] {input.Scenario,input.Contract,input.Source,input.Receiver}.Any(string.IsNullOrWhiteSpace)
            || input.Source == input.Receiver || input.Window < 0 || input.Version < 1
            || !Enum.IsDefined(input.Resource)) throw new ArgumentException("Invalid identity or window");
        foreach (long value in new[] {input.Supply,input.OwnDemand,input.Demand,input.LocalSupply,input.Limit})
            if (value < 0 || value > 1000000000) throw new ArgumentException("Invalid model quantity");
        if (scenario != null && scenario != input.Scenario) throw new ArgumentException("Scenario changed");
        if (latest.TryGetValue(input.Resource, out var previous))
        {
            if (input == previous.Input) return previous.Output;
            var old = previous.Input;
            if (old.Contract != input.Contract || old.Source != input.Source || old.Receiver != input.Receiver
                || input.Window < old.Window || (input.Window == old.Window && old.Available)
                || input.Version < old.Version
                || (input.Limit != old.Limit && input.Version == old.Version))
                throw new ArgumentException("Conflicting contract, retry, or old window");
        }
        Output result;
        if (!input.Available)
        {
            // An unavailable window is pending, with no trustworthy remaining quantities.
            result = new Output(0, null, null, "Unavailable");
        }
        else
        {
            long surplus = Math.Max(0,input.Supply-input.OwnDemand);
            long shortage = Math.Max(0,input.Demand-input.LocalSupply);
            long transfer = Math.Min(input.Limit,Math.Min(surplus,shortage));
            result = new Output(transfer,surplus-transfer,shortage-transfer,"SyntheticPreview");
        }
        scenario = input.Scenario;
        latest[input.Resource] = (input,result);
        return result;
    }
}
