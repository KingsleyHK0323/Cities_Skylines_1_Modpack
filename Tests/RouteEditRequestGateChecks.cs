using System;
using AutomatedPublicTransportPlanning.Planning;

// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
internal static class RouteEditRequestGateChecks
{
    public static void Run()
    {
        // Signature: OpenAI-Codex_Automated_Public_Transport_Planning
        DelayedLifecycleCallbacks();
        WorkerChangesSessionBeforeThrowing();
        RouteEditRequestGate gate = new RouteEditRequestGate();
        RouteEditRequestTicket first;
        Require(gate.TryBegin(out first) && gate.IsBusy && gate.IsOwner(first), "first request owns the gate");
        RouteEditRequestTicket rejected;
        Require(!gate.TryBegin(out rejected) && rejected == null, "busy request cannot replace the owner");
        int published = 0;
        Require(gate.TryPublish(first, delegate { published++; }), "current result can publish");
        Require(published == 1, "publication runs once");
        gate.Reset(delegate { published = 0; });
        Require(!gate.IsBusy && !gate.IsOwner(first) && published == 0, "reset invalidates and clears the old request");
        RouteEditRequestTicket next;
        Require(gate.TryBegin(out next), "new city can begin another request");
        Require(!gate.TryPublish(first, delegate { published = 99; }), "old preview cannot publish in the new city");
        Require(!gate.Complete(first) && gate.IsBusy && gate.IsOwner(next), "old finally cannot unlock the new owner");
        Require(published == 0, "old callback did not execute");
        Require(!gate.IsOwner(null) && !gate.Complete(null), "null is never an owner");
        Require(gate.Complete(next) && !gate.IsBusy, "the actual owner can complete");
        Require(!gate.TryPublish(next, delegate { published++; }), "completed callbacks cannot publish later");
    }

    // Drain deferred callbacks after reset, without game state or timing dependencies.
    private static void DelayedLifecycleCallbacks()
    {
        RouteEditRequestGate gate = new RouteEditRequestGate();
        RouteEditRequestTicket old;
        Require(gate.TryBegin(out old), "old session starts");
        string status = "old";
        bool busy = true;
        ushort pendingLine = 17;
        Action oldSuccess = delegate { gate.TryPublish(old, delegate { status = "old success"; }); };
        Action oldError = delegate { gate.TryPublish(old, delegate { status = "old error"; }); };
        Action oldFinally = delegate
        {
            gate.TryPublish(old, delegate { busy = false; pendingLine = 0; });
            gate.Complete(old);
        };
        gate.Reset(delegate { status = "reset"; busy = false; pendingLine = 0; });
        oldError(); oldFinally();
        Require(status == "reset" && !busy, "late failure cannot replace reset state");
        RouteEditRequestTicket current;
        Require(gate.TryBegin(out current), "new session starts");
        gate.TryPublish(current, delegate { status = "new checking"; busy = true; pendingLine = 17; });
        oldSuccess(); oldError(); oldFinally();
        Require(status == "new checking" && busy && pendingLine == 17 && gate.IsOwner(current),
            "old callbacks cannot alter a new request with a reused numeric line ID");
        Require(gate.TryPublish(current, delegate { status = "current error"; }), "current failure publishes");
        Require(gate.TryPublish(current, delegate { busy = false; pendingLine = 0; }), "current cleanup runs");
        Require(gate.Complete(current) && !gate.IsBusy && !busy && pendingLine == 0 && status == "current error",
            "current finally releases its own request");
        RouteEditRequestTicket later;
        Require(gate.TryBegin(out later), "later request starts without reset");
        Require(!gate.TryPublish(current, delegate { status = "late duplicate"; }) && !gate.Complete(current),
            "completed callback cannot affect a later request");
        Require(gate.IsOwner(later) && status == "current error", "later owner remains active");
    }

    private static void WorkerChangesSessionBeforeThrowing()
    {
        RouteEditRequestGate gate = new RouteEditRequestGate();
        RouteEditRequestTicket old;
        Require(gate.TryBegin(out old), "worker begins");
        RouteEditRequestTicket current = null;
        int errors = 0;
        int cleanups = 0;
        gate.RunOwned(old, delegate
        {
            gate.Reset(delegate { });
            Require(gate.TryBegin(out current), "new session starts while old calculation runs");
            throw new InvalidOperationException("delayed old calculation failure");
        }, delegate(Exception error) { errors++; }, delegate { cleanups++; gate.Complete(old); });
        Require(errors == 0 && cleanups == 0 && gate.IsOwner(current),
            "production callback runner suppresses stale exception and finally publications");
        bool staleWorkRan = false;
        gate.RunOwned(old, delegate { staleWorkRan = true; },
            delegate(Exception error) { errors++; }, delegate { cleanups++; });
        Require(!staleWorkRan && cleanups == 0, "queued obsolete work never starts");
        gate.RunOwned(current, delegate { throw new InvalidOperationException("current failure"); },
            delegate(Exception error) { errors++; }, delegate { cleanups++; gate.Complete(current); });
        Require(errors == 1 && cleanups == 1 && !gate.IsBusy,
            "current exception publishes once and finally completes once");
        RouteEditRequestTicket pending;
        Require(gate.TryBegin(out pending), "successful creation begins");
        bool awaitingValidation = false;
        gate.RunOwned(pending, delegate { awaitingValidation = true; },
            delegate(Exception error) { errors++; }, delegate
            {
                if (!awaitingValidation) { gate.Complete(pending); }
            });
        Require(awaitingValidation && gate.IsOwner(pending) && errors == 1,
            "successful creation retains ownership while post-check is pending");
        Require(gate.Complete(pending), "post-check can complete its retained ticket");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }
}
