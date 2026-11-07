// OpenAI-Codex_Intercity_Resource_Sharing
using Intercity;
static void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
static void Reject(Action action) { try { action(); } catch (ArgumentException) { return; } throw new Exception("Expected rejection"); }
var input = new Input("synthetic","contract",1,Resource.Water,"A","B",0,100,70,60,40,25,true);
var model = new Preview();
var result = model.Calculate(input);
Assert(result.Transfer == 20 && result.Surplus == 10 && result.Shortfall == 0);
Assert(model.Calculate(input) == result);
Reject(() => model.Calculate(input with { Supply = 101 }));
Reject(() => model.Calculate(input with { Window = 1, Supply = -1 }));
Reject(() => model.Calculate(input with { Window = 1, Resource = (Resource)99 }));
Reject(() => model.Calculate(input with { Window = 1, Source = "B" }));
Reject(() => model.Calculate(input with { Window = 1, Contract = "other" }));
Assert(model.Calculate(input with { Window = 1, Available = false }).Transfer == 0);
Reject(() => model.Calculate(input));
foreach (var resource in Enum.GetValues<Resource>())
{
    Assert(new Preview().Calculate(input with { Resource = resource, Supply = 0 }).Transfer == 0);
    Assert(new Preview().Calculate(input with { Resource = resource, Limit = 0 }).Transfer == 0);
    Assert(new Preview().Calculate(input with { Resource = resource, LocalSupply = 100 }).Transfer == 0);
    Assert(new Preview().Calculate(input with { Resource = resource, Supply = 1000000000, OwnDemand = 0, Demand = 1000000000, LocalSupply = 0, Limit = 1000000000 }).Transfer == 1000000000);
}

// An unavailable synthetic window may recover without reopening a completed window.
var recovery = new Preview();
recovery.Calculate(input);
var unavailable = input with { Window = 1, Available = false };
var pending = recovery.Calculate(unavailable);
Assert(pending.Transfer == 0 && pending.Reason == "Unavailable");
Assert(recovery.Calculate(unavailable) == pending);
var corrected = unavailable with { Available = true, Supply = 99 };
var completed = recovery.Calculate(corrected);
Assert(object.Equals(pending.Surplus, null) && object.Equals(pending.Shortfall, null));
Assert(completed.Transfer == 20 && completed.Surplus == 9 && completed.Shortfall == 0);
Assert(recovery.Calculate(corrected) == completed);
Reject(() => recovery.Calculate(corrected with { Supply = 100 }));
Reject(() => recovery.Calculate(corrected with { Available = false }));
Reject(() => recovery.Calculate(input));

// A later unavailable window cannot permit backfilling an older window.
var waiting = corrected with { Window = 3, Available = false };
recovery.Calculate(waiting);
Reject(() => recovery.Calculate(corrected with { Window = 2 }));
var independentlyAvailable = input with { Resource = Resource.Electricity, Window = 2 };
Assert(recovery.Calculate(independentlyAvailable).Transfer == 20);
Assert(recovery.Calculate(waiting with { Available = true }).Transfer == 20);

// Invalid retries must not alter the pending window or its fixed contract.
var isolated = new Preview();
isolated.Calculate(unavailable);
Reject(() => isolated.Calculate(unavailable with { Source = "other" }));
Reject(() => isolated.Calculate(unavailable with { Supply = -1 }));
Reject(() => isolated.Calculate(unavailable with { Limit = 1 }));
Assert(isolated.Calculate(corrected).Transfer == 20);
Console.WriteLine("Synthetic checks passed; game behavior is not tested.");
