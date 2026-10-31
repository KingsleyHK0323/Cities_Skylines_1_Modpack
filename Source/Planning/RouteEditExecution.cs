using System;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    internal interface IRouteEditSurface
    {
        bool IsCurrent { get; }
        bool HasExpectedIdentity { get; }
        int StopCount { get; }
        bool Complete { get; }
        Vector3 ReadPosition(int index);
        bool MoveStop(int index, Vector3 position, bool fixedPlatform);
        void UpdateLine();
    }

    internal enum RouteEditExecutionOutcome
    {
        Refused,
        Applied,
        PositionsRestored,
        RecoveryIncomplete,
        Invalidated
    }

    internal sealed class RouteEditExecutionResult
    {
        public readonly RouteEditExecutionOutcome Outcome;
        public readonly string Detail;

        public RouteEditExecutionResult(RouteEditExecutionOutcome outcome, string detail)
        {
            Outcome = outcome;
            Detail = detail ?? string.Empty;
        }
    }

    internal static class RouteEditExecutor
    {
        public static RouteEditExecutionResult Execute(RouteEditPlan plan, IRouteEditSurface surface)
        {
            if (plan == null || surface == null) { return Result(RouteEditExecutionOutcome.Refused, "An edit plan and a live route are required."); }
            int count = plan.Original.Stops.Count;
            try
            {
                if (!surface.IsCurrent) { return Invalidated(); }
                if (!CanAccess(surface, count) || !surface.Complete || !MatchesOriginal(plan, surface))
                { return Result(RouteEditExecutionOutcome.Refused, "The original route no longer matches the edit."); }
            }
            catch (Exception error) { return Result(RouteEditExecutionOutcome.Refused, error.Message); }

            string failure;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    if (!surface.IsCurrent) { return Invalidated(); }
                    if (!CanAccess(surface, count)) { return Incomplete("The line identity or stop count changed."); }
                    if (Near(surface.ReadPosition(i), plan.Stops[i].Position, 0.0001f)) { continue; }
                    if (!surface.IsCurrent) { return Invalidated(); }
                    if (!CanAccess(surface, count)) { return Incomplete("The line changed before a stop move."); }
                    // A false return or exception can still follow a native-side mutation.
                    if (!surface.MoveStop(i, plan.Stops[i].Position, plan.Stops[i].FixedPlatform))
                    { throw new InvalidOperationException("MoveStop refused index " + i + "."); }
                }
                if (!surface.IsCurrent) { return Invalidated(); }
                if (!CanAccess(surface, count)) { return Incomplete("The line identity or stop count changed."); }
                for (int i = 0; i < count; i++)
                {
                    if (!Near(surface.ReadPosition(i), plan.Stops[i].Position, 0.25f))
                    { throw new InvalidOperationException("A requested stop position was not retained."); }
                }
                if (!surface.Complete) { throw new InvalidOperationException("The edited line is incomplete."); }
                if (!surface.IsCurrent) { return Invalidated(); }
                if (!CanAccess(surface, count)) { return Incomplete("The line identity or stop count changed."); }
                surface.UpdateLine();
                // UpdateLine may also expose native-side changes; read back before reporting success.
                if (!surface.IsCurrent) { return Invalidated(); }
                if (!CanAccess(surface, count) || !surface.Complete) { throw new InvalidOperationException("The updated line is incomplete or changed."); }
                for (int i = 0; i < count; i++)
                {
                    if (!Near(surface.ReadPosition(i), plan.Stops[i].Position, 0.25f))
                    { throw new InvalidOperationException("The updated line changed a requested stop position."); }
                }
                return Result(RouteEditExecutionOutcome.Applied, "Requested positions observed within 0.5 metres; adjacent-path checks remain pending.");
            }
            catch (Exception error) { failure = error.Message; }

            // Recovery is position-only and must never cross a request/line identity boundary.
            for (int i = 0; i < count; i++)
            {
                try
                {
                    if (!surface.IsCurrent) { return Invalidated(); }
                    if (!CanAccess(surface, count)) { return Incomplete(failure + " Recovery stopped because the line changed."); }
                    Vector3 original = plan.Original.Stops[i].Position;
                    if (Near(surface.ReadPosition(i), original, 0.0001f)) { continue; }
                    if (!surface.IsCurrent) { return Invalidated(); }
                    if (!CanAccess(surface, count)) { return Incomplete(failure + " The line changed before recovery."); }
                    // Do not classify recovery by this return value: verify observed positions below.
                    surface.MoveStop(i, original, plan.Original.Stops[i].FixedPlatform);
                }
                catch { /* Continue to the next recoverable position, rechecking ownership first. */ }
            }
            try
            {
                if (!surface.IsCurrent) { return Invalidated(); }
                if (!CanAccess(surface, count)) { return Incomplete(failure + " The line cannot be safely updated."); }
                surface.UpdateLine();
                if (!surface.IsCurrent) { return Invalidated(); }
                if (!CanAccess(surface, count) || !surface.Complete || !MatchesOriginal(plan, surface))
                { return Incomplete(failure + " Original positions or route completeness were not restored."); }
                return Result(RouteEditExecutionOutcome.PositionsRestored, failure + " Original stop positions observed within 0.5 metres; passenger, vehicle and path effects are not restored.");
            }
            catch (Exception error) { return Incomplete(failure + " Recovery verification failed: " + error.Message); }
        }

        private static bool CanAccess(IRouteEditSurface surface, int count)
        {
            return surface.IsCurrent && surface.HasExpectedIdentity && surface.StopCount == count;
        }

        private static bool MatchesOriginal(RouteEditPlan plan, IRouteEditSurface surface)
        {
            for (int i = 0; i < plan.Original.Stops.Count; i++)
            {
                if (!Near(surface.ReadPosition(i), plan.Original.Stops[i].Position, 0.25f)) { return false; }
            }
            return true;
        }

        private static bool Near(Vector3 actual, Vector3 expected, float squaredTolerance)
        {
            return Finite(actual) && Finite(expected) && (actual - expected).sqrMagnitude <= squaredTolerance;
        }

        private static bool Finite(Vector3 position)
        {
            return !float.IsNaN(position.x) && !float.IsInfinity(position.x) &&
                   !float.IsNaN(position.y) && !float.IsInfinity(position.y) &&
                   !float.IsNaN(position.z) && !float.IsInfinity(position.z);
        }

        private static RouteEditExecutionResult Result(RouteEditExecutionOutcome outcome, string detail)
        { return new RouteEditExecutionResult(outcome, detail); }
        private static RouteEditExecutionResult Incomplete(string detail)
        { return Result(RouteEditExecutionOutcome.RecoveryIncomplete, detail + " Stop and restore your save backup."); }
        private static RouteEditExecutionResult Invalidated()
        { return Result(RouteEditExecutionOutcome.Invalidated, "The request or city changed; no further apply or recovery writes are allowed."); }
    }
}
