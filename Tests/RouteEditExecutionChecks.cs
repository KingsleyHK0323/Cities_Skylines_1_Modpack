using System;
using System.Collections.Generic;
using AutomatedPublicTransportPlanning.Planning;
using UnityEngine;

// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
internal static class RouteEditExecutionChecks
{
    public static void Run()
    {
        Check("ordinary placement flag survives failure recovery", PlacementFlagRecovery);
        Check("normal edit", NormalEdit);
        Check("unchanged edit performs no moves", UnchangedEdit);
        Check("false result after mutation recovers observed positions", FalseAfterMutation);
        Check("exception after mutation recovers observed positions", ExceptionAfterMutation);
        Check("true recovery result cannot hide incorrect positions", FalseRecoverySuccess);
        Check("false recovery result with restored positions uses observations", FalseRecoveryReturn);
        Check("changed stop count stops all subsequent writes", ChangedStopCount);
        Check("incomplete original is refused without writes", IncompleteOriginal);
        Check("incomplete recovered route is reported incomplete", IncompleteRecovery);
        Check("NaN observed position cannot pass applied validation", NonfiniteObservedPosition);
        Check("invalidation during apply prevents later writes", InvalidateDuringApply);
        Check("invalidation during recovery prevents later writes", InvalidateDuringRecovery);
        Check("wrong original identity is refused without writes", WrongIdentity);
        Check("identity lost during apply prevents recovery writes", LostIdentity);
        Check("recovery skips unchanged original positions", SkipUnchangedRecovery);
        Check("first UpdateLine exception recovers original positions", FirstUpdateException);
        Check("UpdateLine position corruption cannot be reported applied", UpdatePositionCorruption);
        Check("persistent UpdateLine exception reports incomplete recovery", PersistentUpdateException);
        Check("UpdateLine invalidation prevents every subsequent write", InvalidateDuringUpdate);
    }

    private static void PlacementFlagRecovery()
    {
        RouteEditPlan baseline = Plan(false);
        ExistingRouteStop[] original = new ExistingRouteStop[3];
        for (int i = 0; i < 3; i++) { original[i] = new ExistingRouteStop(baseline.Original.Stops[i].NodeId, baseline.Original.Stops[i].Position, true); }
        RouteEditPlan plan; string reason;
        Require(RouteEditPlan.TryCreate(new ExistingRouteSnapshot(17, 23, "Bus", original), RouteEditMode.Manual, baseline.Stops, out plan, out reason), reason);
        Require(!plan.Original.Matches(17, 23, "Bus", baseline.Original.Stops), "placement argument changes invalidate snapshot");
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position) { surface.Positions[index] = position; return position.x < 100; };
        Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(plan, surface));
        Require(surface.PlacementFlags.Count == 2 && !surface.PlacementFlags[0] && surface.PlacementFlags[1], "recovery must forward original true argument");
    }
    private static void NormalEdit()
    {
        FakeSurface surface = new FakeSurface();
        Result(RouteEditExecutionOutcome.Applied, RouteEditExecutor.Execute(Plan(false), surface));
        Positions(surface, 100, 120, 140);
        Require(surface.Moves.Count == 3, "normal edit must move each changed stop once");
    }

    private static void UnchangedEdit()
    {
        FakeSurface surface = new FakeSurface();
        Result(RouteEditExecutionOutcome.Applied, RouteEditExecutor.Execute(Plan(true), surface));
        Require(surface.Moves.Count == 0, "an unchanged edit must not call MoveStop");
    }

    private static void FalseAfterMutation()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            return position.x < 100 || index != 1;
        };
        Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(Plan(false), surface));
        Positions(surface, 0, 20, 40);
    }

    private static void ExceptionAfterMutation()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            if (index == 1 && position.x >= 100) { throw new InvalidOperationException("injected move failure"); }
            return true;
        };
        Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(Plan(false), surface));
        Positions(surface, 0, 20, 40);
    }

    private static void FalseRecoverySuccess()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            if (position.x < 100 && index == 0) { return true; }
            surface.Positions[index] = position;
            return position.x < 100 || index != 1;
        };
        Result(RouteEditExecutionOutcome.RecoveryIncomplete, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Positions[0].x == 100, "injection must leave a wrongly restored station");
    }

    private static void FalseRecoveryReturn()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            if (position.x < 100) { return false; }
            return index != 1;
        };
        Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(Plan(false), surface));
        Positions(surface, 0, 20, 40);
    }

    private static void ChangedStopCount()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            surface.Count = 2;
            return true;
        };
        Result(RouteEditExecutionOutcome.RecoveryIncomplete, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Moves.Count == 1 && surface.UpdateCalls == 0,
            "stop-count changes must prevent later moves and line updates");
    }

    private static void IncompleteOriginal()
    {
        FakeSurface surface = new FakeSurface();
        surface.IsComplete = false;
        Result(RouteEditExecutionOutcome.Refused, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Moves.Count == 0 && surface.UpdateCalls == 0, "incomplete original must not be written");
    }

    private static void IncompleteRecovery()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            if (index == 1 && position.x >= 100) { surface.IsComplete = false; return false; }
            return true;
        };
        Result(RouteEditExecutionOutcome.RecoveryIncomplete, RouteEditExecutor.Execute(Plan(false), surface));
    }

    private static void NonfiniteObservedPosition()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = index == 0 && position.x >= 100
                ? new Vector3(float.NaN, 0, 0) : position;
            return true;
        };
        Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(Plan(false), surface));
        Positions(surface, 0, 20, 40);
    }

    private static void InvalidateDuringApply()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            surface.Current = false;
            return true;
        };
        Result(RouteEditExecutionOutcome.Invalidated, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Moves.Count == 1 && surface.WritesAfterInvalidation == 0,
            "an invalidated request cannot apply or recover further positions");
    }

    private static void InvalidateDuringRecovery()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            if (position.x < 100) { surface.Current = false; }
            return position.x < 100 || index != 1;
        };
        Result(RouteEditExecutionOutcome.Invalidated, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Moves.Count == 3 && surface.WritesAfterInvalidation == 0,
            "recovery must stop immediately after its first invalidating move");
    }

    private static void WrongIdentity()
    {
        FakeSurface surface = new FakeSurface();
        surface.Identity = false;
        Result(RouteEditExecutionOutcome.Refused, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Moves.Count == 0 && surface.UpdateCalls == 0, "wrong identity must not be written");
    }

    private static void LostIdentity()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            surface.Identity = false;
            return true;
        };
        Result(RouteEditExecutionOutcome.RecoveryIncomplete, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Moves.Count == 1 && surface.UpdateCalls == 0, "a replaced line must not receive recovery writes");
    }

    private static void SkipUnchangedRecovery()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnMove = delegate(int index, Vector3 position)
        {
            surface.Positions[index] = position;
            return position.x < 100;
        };
        RouteEditPlan plan = Plan(false, new[] { 0f, 120f, 40f });
        Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(plan, surface));
        Require(surface.Moves.Count == 2 && surface.Moves[0] == 1 && surface.Moves[1] == 1,
            "unchanged first and third positions must never be moved during apply or recovery");
    }

    private static RouteEditPlan Plan(bool unchanged)
    {
        return Plan(unchanged, unchanged ? new[] { 0f, 20f, 40f } : new[] { 100f, 120f, 140f });
    }

    private static void FirstUpdateException()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnUpdate = delegate
        {
            if (surface.UpdateCalls == 1) { throw new InvalidOperationException("injected first update failure"); }
        };
        Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(Plan(false), surface));
        Positions(surface, 0, 20, 40);
        Require(surface.UpdateCalls == 2, "recovery must complete a later successful line update");
    }

    private static void UpdatePositionCorruption()
    {
        foreach (float invalidPosition in new[] { 999f, float.NaN })
        {
            FakeSurface surface = new FakeSurface();
            surface.OnUpdate = delegate
            {
                if (surface.UpdateCalls == 1) { surface.Positions[0] = new Vector3(invalidPosition, 0, 0); }
            };
            Result(RouteEditExecutionOutcome.PositionsRestored, RouteEditExecutor.Execute(Plan(false), surface));
            Positions(surface, 0, 20, 40);
            Require(surface.UpdateCalls == 2, "post-update corruption must trigger recovery and its own update");
        }
    }

    private static void PersistentUpdateException()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnUpdate = delegate { throw new InvalidOperationException("injected persistent update failure"); };
        Result(RouteEditExecutionOutcome.RecoveryIncomplete, RouteEditExecutor.Execute(Plan(false), surface));
        Positions(surface, 0, 20, 40);
        Require(surface.UpdateCalls == 2, "both original and recovery updates must have been attempted");
    }

    private static void InvalidateDuringUpdate()
    {
        FakeSurface surface = new FakeSurface();
        surface.OnUpdate = delegate { surface.Current = false; };
        Result(RouteEditExecutionOutcome.Invalidated, RouteEditExecutor.Execute(Plan(false), surface));
        Require(surface.Moves.Count == 3 && surface.UpdateCalls == 1 && surface.WritesAfterInvalidation == 0,
            "an invalidating line update cannot be followed by apply, recovery or another update");
    }

    private static RouteEditPlan Plan(bool unchanged, float[] positions)
    {
        ExistingRouteStop[] old = {
            new ExistingRouteStop(1, new Vector3(0, 0, 0)),
            new ExistingRouteStop(2, new Vector3(20, 0, 0)),
            new ExistingRouteStop(3, new Vector3(40, 0, 0)) };
        RouteStopSite[] targets = new RouteStopSite[3];
        for (int i = 0; i < 3; i++) { targets[i] = new RouteStopSite((ushort)(10 + i), 1, true, 0, new Vector3(positions[i], 0, 0), false); }
        RouteEditPlan plan;
        string reason;
        Require(RouteEditPlan.TryCreate(new ExistingRouteSnapshot(17, 23, "Bus", old), RouteEditMode.Manual,
            targets, out plan, out reason), reason);
        return plan;
    }

    private static void Result(RouteEditExecutionOutcome expected, RouteEditExecutionResult actual)
    {
        Require(actual != null && actual.Outcome == expected,
            "expected " + expected + ", received " + (actual == null ? "null" : actual.Outcome + ": " + actual.Detail));
    }

    private static void Positions(FakeSurface surface, float first, float second, float third)
    {
        Require(surface.Positions[0].x == first && surface.Positions[1].x == second && surface.Positions[2].x == third,
            "expected exact observed final positions");
    }

    private static void Check(string name, Action action)
    {
        try { action(); Console.WriteLine("PASS route execution: " + name); }
        catch (Exception error) { throw new InvalidOperationException("Route execution check failed: " + name, error); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }

    private sealed class FakeSurface : IRouteEditSurface
    {
        internal readonly Vector3[] Positions = { new Vector3(0, 0, 0), new Vector3(20, 0, 0), new Vector3(40, 0, 0) };
        internal readonly List<int> Moves = new List<int>();
        internal readonly List<bool> PlacementFlags = new List<bool>();
        internal bool Current = true, Identity = true, IsComplete = true;
        internal int Count = 3, UpdateCalls, WritesAfterInvalidation;
        internal Func<int, Vector3, bool> OnMove;
        internal Action OnUpdate;
        public bool IsCurrent { get { return Current; } }
        public bool HasExpectedIdentity { get { return Identity; } }
        public int StopCount { get { return Count; } }
        public bool Complete { get { return IsComplete; } }
        public Vector3 ReadPosition(int index) { return Positions[index]; }
        public bool MoveStop(int index, Vector3 position, bool fixedPlatform)
        {
            if (!Current) { WritesAfterInvalidation++; }
            Moves.Add(index);
            PlacementFlags.Add(fixedPlatform);
            if (OnMove != null) { return OnMove(index, position); }
            Positions[index] = position;
            return true;
        }
        public void UpdateLine()
        {
            if (!Current) { WritesAfterInvalidation++; }
            UpdateCalls++;
            if (OnUpdate != null) { OnUpdate(); }
        }
    }
}
