// Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population
using System;

namespace RealisticUrbanPopulation.Demand
{
    public enum DemandMode { Native, FixedTarget }
    public enum DemandChannel { Residential, Commercial, Workplace }

    public sealed class CategoryPolicy
    {
        private readonly DemandMode mode;
        private readonly int? target;
        internal CategoryPolicy(DemandMode mode, int? target)
        { this.mode = mode; this.target = target; }
        public DemandMode Mode { get { return mode; } }
        public int? Target { get { return target; } }
    }

    public sealed class DemandSettings
    {
        private readonly CategoryPolicy residential;
        private readonly CategoryPolicy commercial;
        private readonly CategoryPolicy workplace;
        public DemandSettings(CategoryPolicy residential, CategoryPolicy commercial, CategoryPolicy workplace)
        {
            if (residential == null) throw new ArgumentNullException("residential");
            if (commercial == null) throw new ArgumentNullException("commercial");
            if (workplace == null) throw new ArgumentNullException("workplace");
            this.residential = residential;
            this.commercial = commercial;
            this.workplace = workplace;
        }
        public static DemandSettings Native()
        { return new DemandSettings(DemandPolicy.NativePolicy, DemandPolicy.NativePolicy, DemandPolicy.NativePolicy); }
        public CategoryPolicy Residential { get { return residential; } }
        public CategoryPolicy Commercial { get { return commercial; } }
        public CategoryPolicy Workplace { get { return workplace; } }
    }

    public static class DemandPolicy
    {
        internal static readonly CategoryPolicy NativePolicy = new CategoryPolicy(DemandMode.Native, null);

        public static bool TryCreate(int mode, int? target, out CategoryPolicy policy)
        {
            policy = null;
            if (mode == (int)DemandMode.Native && !target.HasValue)
            { policy = NativePolicy; return true; }
            if (mode != (int)DemandMode.FixedTarget || !target.HasValue || target.Value < 0 || target.Value > 100)
                return false;
            policy = new CategoryPolicy(DemandMode.FixedTarget, target);
            return true;
        }

        public static int Evaluate(int input, CategoryPolicy policy)
        { return policy != null && policy.Mode == DemandMode.FixedTarget ? policy.Target.Value : input; }
    }
}
