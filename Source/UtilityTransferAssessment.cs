// Signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage
using System;

namespace WirelessUtilitiesAndGlobalCoverage
{
    public enum UtilityResource
    {
        Electricity = 1,
        Water = 2,
        Sewage = 3,
        Heating = 4
    }

    public enum UtilityQuantityState
    {
        Unknown = 0,
        Unsupported = 1,
        Known = 2
    }

    // An observation supplied by the caller; this class does not read game state.
    public sealed class UtilityQuantity
    {
        private readonly UtilityResource resource;
        private readonly Guid sessionId;
        private readonly long epoch;
        private readonly string unit;
        private readonly long? amount;
        private readonly UtilityQuantityState state;

        private UtilityQuantity(UtilityResource resource, Guid sessionId, long epoch,
            string unit, long? amount, UtilityQuantityState state)
        {
            this.resource = resource;
            this.sessionId = sessionId;
            this.epoch = epoch;
            this.unit = unit;
            this.amount = amount;
            this.state = state;
        }

        public UtilityResource Resource { get { return resource; } }
        public Guid SessionId { get { return sessionId; } }
        public long Epoch { get { return epoch; } }
        public string Unit { get { return unit; } }
        public long? Amount { get { return amount; } }
        public UtilityQuantityState State { get { return state; } }

        public static UtilityQuantity Known(UtilityResource resource, Guid sessionId,
            long epoch, string unit, long amount)
        {
            return new UtilityQuantity(resource, sessionId, epoch, unit, amount,
                UtilityQuantityState.Known);
        }

        public static UtilityQuantity Unknown(UtilityResource resource, Guid sessionId, long epoch)
        {
            return new UtilityQuantity(resource, sessionId, epoch, null, null,
                UtilityQuantityState.Unknown);
        }

        public static UtilityQuantity Unsupported(UtilityResource resource, Guid sessionId, long epoch)
        {
            return new UtilityQuantity(resource, sessionId, epoch, null, null,
                UtilityQuantityState.Unsupported);
        }
    }

    public enum UtilityAssessmentStatus
    {
        Unobserved = 0,
        Consistent = 1,
        Unsupported = 2,
        InvalidContext = 3,
        ContextMismatch = 4,
        InvalidUnit = 5,
        UnitMismatch = 6,
        NegativeQuantity = 7,
        ExceedsOffer = 8,
        ExceedsDemand = 9
    }

    public sealed class UtilityAssessment
    {
        private readonly UtilityAssessmentStatus status;
        private readonly long? offerRemaining;
        private readonly long? unmetDemand;

        internal UtilityAssessment(UtilityAssessmentStatus status, long? offerRemaining, long? unmetDemand)
        {
            this.status = status;
            this.offerRemaining = offerRemaining;
            this.unmetDemand = unmetDemand;
        }

        public UtilityAssessmentStatus Status { get { return status; } }
        public long? OfferRemaining { get { return offerRemaining; } }
        public long? UnmetDemand { get { return unmetDemand; } }
    }

    // Evaluates one supplied observation only. It is not a ledger, allocator or game adapter.
    public static class UtilityTransferAssessment
    {
        public static UtilityAssessment Assess(UtilityQuantity offered, UtilityQuantity requested,
            UtilityQuantity accepted)
        {
            if (offered == null || requested == null || accepted == null)
                return WithoutAmounts(UtilityAssessmentStatus.Unobserved);

            if (!ValidContext(offered) || !ValidContext(requested) || !ValidContext(accepted))
                return WithoutAmounts(UtilityAssessmentStatus.InvalidContext);

            if (!SameContext(offered, requested) || !SameContext(offered, accepted))
                return WithoutAmounts(UtilityAssessmentStatus.ContextMismatch);

            if (offered.State == UtilityQuantityState.Unsupported
                || requested.State == UtilityQuantityState.Unsupported
                || accepted.State == UtilityQuantityState.Unsupported)
                return WithoutAmounts(UtilityAssessmentStatus.Unsupported);

            if (offered.State != UtilityQuantityState.Known
                || requested.State != UtilityQuantityState.Known
                || accepted.State != UtilityQuantityState.Known
                || !offered.Amount.HasValue || !requested.Amount.HasValue || !accepted.Amount.HasValue)
                return WithoutAmounts(UtilityAssessmentStatus.Unobserved);

            if (!ValidUnit(offered.Unit) || !ValidUnit(requested.Unit) || !ValidUnit(accepted.Unit))
                return WithoutAmounts(UtilityAssessmentStatus.InvalidUnit);

            if (!String.Equals(offered.Unit, requested.Unit, StringComparison.Ordinal)
                || !String.Equals(offered.Unit, accepted.Unit, StringComparison.Ordinal))
                return WithoutAmounts(UtilityAssessmentStatus.UnitMismatch);

            long offer = offered.Amount.Value;
            long demand = requested.Amount.Value;
            long delivery = accepted.Amount.Value;
            if (offer < 0 || demand < 0 || delivery < 0)
                return WithoutAmounts(UtilityAssessmentStatus.NegativeQuantity);
            if (delivery > offer)
                return WithoutAmounts(UtilityAssessmentStatus.ExceedsOffer);
            if (delivery > demand)
                return WithoutAmounts(UtilityAssessmentStatus.ExceedsDemand);

            // With nonnegative inputs and delivery bounded above, both subtractions are safe.
            return new UtilityAssessment(UtilityAssessmentStatus.Consistent,
                offer - delivery, demand - delivery);
        }

        private static bool ValidContext(UtilityQuantity quantity)
        {
            return quantity.Resource >= UtilityResource.Electricity
                && quantity.Resource <= UtilityResource.Heating
                && quantity.SessionId != Guid.Empty && quantity.Epoch >= 0;
        }

        private static bool SameContext(UtilityQuantity left, UtilityQuantity right)
        {
            return left.Resource == right.Resource && left.SessionId == right.SessionId
                && left.Epoch == right.Epoch;
        }

        private static bool ValidUnit(string value)
        {
            return !String.IsNullOrEmpty(value) && value.Length == value.Trim().Length;
        }

        private static UtilityAssessment WithoutAmounts(UtilityAssessmentStatus status)
        {
            return new UtilityAssessment(status, null, null);
        }
    }
}
