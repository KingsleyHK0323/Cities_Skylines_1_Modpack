// Signature: OpenAI-Codex_Responsible: Realistic_Pedestrian_Traffic_Control
using System;
using PedestrianTrafficControl;
internal static class Program {
    private static int checks;
    private static void Assert(bool condition, string name) {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++; Console.WriteLine("PASS: " + name);
    }
    private static CrossingObservation Data(CrossingRule rule, CrossingSignal signal, MarkingState marking,
        ObservationSource source, CrossingGeometry geometry, int? waiting) {
        return new CrossingObservation("city-a", 10, 20, true, 3, 4, rule, geometry, signal, marking, source, waiting);
    }
    private static CrossingPreview Read(CrossingObservation o) {
        return CrossingPreview.Evaluate(o, "city-a", 10, 20, true, 3, 4);
    }
    private static int Main() {
        try {
            CrossingObservation o = Data(CrossingRule.Deny, CrossingSignal.Red, MarkingState.Visible,
                ObservationSource.Synthetic, CrossingGeometry.Supported, null);
            CrossingPreview p = Read(o);
            Assert(p.Status == PreviewStatus.Observed, "valid synthetic data is inspectable as synthetic");
            Assert(p.WouldHideMarkingAfterConfirmedDeny == true, "deny visual preview is separate from currently visible marking");
            Assert(o.Marking == MarkingState.Visible && o.WaitingCount == null, "preview neither mutates input nor invents waiting counts");
            Assert(Read(null).Status == PreviewStatus.Unavailable, "missing source is unavailable");
            Assert(Read(Data(CrossingRule.Allow, CrossingSignal.Green, MarkingState.Visible,
                ObservationSource.Unknown, CrossingGeometry.Supported, null)).Status == PreviewStatus.Unavailable,
                "plausible values without source cannot become observations");
            Assert(CrossingPreview.Evaluate(o,"other-city",10,20,true,3,4).Status == PreviewStatus.Stale, "city changes invalidate IDs");
            Assert(CrossingPreview.Evaluate(o,"city-a",10,20,true,9,4).Status == PreviewStatus.Stale, "reused IDs with new topology are stale");
            Assert(CrossingPreview.Evaluate(o,"city-a",10,20,false,3,4).Status == PreviewStatus.Stale, "segment endpoints are distinct");
            Assert(CrossingPreview.Evaluate(o,"city-a",10,20,true,3,5).Status == PreviewStatus.Stale, "snapshot revision must match");
            Assert(CrossingPreview.Evaluate(o,"city-a",11,20,true,3,4).Status == PreviewStatus.Stale, "segment identity must match");
            Assert(CrossingPreview.Evaluate(o,"city-a",10,21,true,3,4).Status == PreviewStatus.Stale, "node identity must match");
            Assert(Read(Data(CrossingRule.Deny,CrossingSignal.Unknown,MarkingState.Unknown,
                ObservationSource.Synthetic,CrossingGeometry.Unknown,null)).Status == PreviewStatus.Unavailable, "unknown geometry cannot be treated as present");
            Assert(Read(Data(CrossingRule.Deny,CrossingSignal.Unknown,MarkingState.Unknown,
                ObservationSource.Synthetic,CrossingGeometry.Absent,null)).Status == PreviewStatus.Unsupported, "absent crossing is unsupported");
            foreach (CrossingSignal signal in Enum.GetValues(typeof(CrossingSignal))) {
                CrossingObservation allowed = Data(CrossingRule.Allow,signal,MarkingState.Hidden,
                    ObservationSource.Synthetic,CrossingGeometry.Supported,0);
                Assert(Read(allowed).WouldHideMarkingAfterConfirmedDeny == false,
                    "hidden marking or signal does not change allowed rule: " + signal);
            }
            Assert(Read(Data(CrossingRule.Inherit,CrossingSignal.NotSignalized,MarkingState.Visible,
                ObservationSource.Synthetic,CrossingGeometry.Supported,null)).WouldHideMarkingAfterConfirmedDeny == null,
                "inherited rule cannot invent effective permission or visibility");
            Assert(Read(Data(CrossingRule.Unknown,CrossingSignal.Green,MarkingState.Visible,
                ObservationSource.Synthetic,CrossingGeometry.Supported,null)).Status == PreviewStatus.Unavailable, "unknown rule cannot become permission");
            Assert(Read(Data(CrossingRule.Allow,CrossingSignal.Red,MarkingState.Hidden,
                ObservationSource.Synthetic,CrossingGeometry.Supported,-1)).Status == PreviewStatus.Unavailable, "negative waiting count is invalid");
            Console.WriteLine("Passed " + checks + " model assertions. Synthetic input only; no game integration.");
            return 0;
        } catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
    }
}
