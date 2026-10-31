// Signature: OpenAI-Codex_Responsible: Realistic_Pedestrian_Traffic_Control
namespace PedestrianTrafficControl {
    internal enum CrossingRule { Unknown, Inherit, Allow, Deny }
    internal enum CrossingGeometry { Unknown, Absent, Supported }
    internal enum CrossingSignal { Unknown, NotSignalized, Red, Green, Transition }
    internal enum MarkingState { Unknown, Visible, Hidden }
    internal enum ObservationSource { Unknown, Synthetic }
    internal enum PreviewStatus { Unavailable, Stale, Unsupported, Observed }
    internal sealed class CrossingObservation {
        internal readonly string City;
        internal readonly int Segment, Node, Topology, Revision;
        internal readonly bool Start;
        internal readonly CrossingRule Rule;
        internal readonly CrossingGeometry Geometry;
        internal readonly CrossingSignal Signal;
        internal readonly MarkingState Marking;
        internal readonly ObservationSource Source;
        internal readonly int? WaitingCount;
        internal CrossingObservation(string city, int segment, int node, bool start, int topology, int revision,
            CrossingRule rule, CrossingGeometry geometry, CrossingSignal signal, MarkingState marking,
            ObservationSource source, int? waitingCount) {
            City = city; Segment = segment; Node = node; Start = start; Topology = topology; Revision = revision;
            Rule = rule; Geometry = geometry; Signal = signal; Marking = marking; Source = source; WaitingCount = waitingCount;
        }
    }
    internal sealed class CrossingPreview {
        internal readonly PreviewStatus Status;
        internal readonly ObservationSource Source;
        internal readonly string Reason;
        internal readonly bool? WouldHideMarkingAfterConfirmedDeny;
        internal CrossingPreview(PreviewStatus status, string reason, bool? wouldHide, ObservationSource source) {
            Status = status; Reason = reason; WouldHideMarkingAfterConfirmedDeny = wouldHide; Source = source;
        }
        internal static CrossingPreview Evaluate(CrossingObservation observation, string city, int segment,
            int node, bool start, int topology, int revision) {
            if (observation == null)
                return new CrossingPreview(PreviewStatus.Unavailable, "No game observation source is connected.", null, ObservationSource.Unknown);
            ObservationSource source = observation.Source == ObservationSource.Synthetic
                ? ObservationSource.Synthetic : ObservationSource.Unknown;
            if (source == ObservationSource.Unknown)
                return new CrossingPreview(PreviewStatus.Unavailable, "Observation source is unknown or invalid.", null, source);
            if (string.IsNullOrEmpty(city) || city.Trim().Length == 0 || segment <= 0 || node <= 0 || topology < 0 || revision < 0
                || string.IsNullOrEmpty(observation.City) || observation.City.Trim().Length == 0
                || observation.Segment <= 0 || observation.Node <= 0 || observation.Topology < 0 || observation.Revision < 0)
                return new CrossingPreview(PreviewStatus.Unavailable, "Invalid crossing identity.", null, source);
            if (!System.Enum.IsDefined(typeof(CrossingRule), observation.Rule)
                || !System.Enum.IsDefined(typeof(CrossingGeometry), observation.Geometry)
                || !System.Enum.IsDefined(typeof(CrossingSignal), observation.Signal)
                || !System.Enum.IsDefined(typeof(MarkingState), observation.Marking)
                || (observation.WaitingCount.HasValue && observation.WaitingCount.Value < 0))
                return new CrossingPreview(PreviewStatus.Unavailable, "Invalid state or waiting count.", null, source);
            if (!string.Equals(city, observation.City, System.StringComparison.Ordinal)
                || segment != observation.Segment || node != observation.Node || start != observation.Start
                || topology != observation.Topology || revision != observation.Revision)
                return new CrossingPreview(PreviewStatus.Stale, "Crossing identity or revision no longer matches.", null, source);
            if (observation.Geometry == CrossingGeometry.Unknown)
                return new CrossingPreview(PreviewStatus.Unavailable, "Crossing geometry is unknown.", null, source);
            if (observation.Geometry == CrossingGeometry.Absent)
                return new CrossingPreview(PreviewStatus.Unsupported, "No supported crossing exists at this endpoint.", null, source);
            if (observation.Rule == CrossingRule.Unknown)
                return new CrossingPreview(PreviewStatus.Unavailable, "Effective crossing rule is unknown.", null, source);
            bool? wouldHide = observation.Rule == CrossingRule.Inherit ? (bool?)null : observation.Rule == CrossingRule.Deny;
            return new CrossingPreview(PreviewStatus.Observed,
                "Synthetic data only. Visibility is hypothetical; no control or game observation is available.", wouldHide, source);
        }
    }
}
