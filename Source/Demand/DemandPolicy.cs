// Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population
using System;

namespace RealisticUrbanPopulation.Demand
{
    public enum DemandMode { Native, FixedTarget }
    public enum DemandChannel { Residential, Commercial, Workplace }

    public sealed class CategoryPolicy
    {
        public DemandMode Mode { get { throw new NotImplementedException(); } }
        public int? Target { get { throw new NotImplementedException(); } }
    }

    public sealed class DemandSettings
    {
        public DemandSettings(CategoryPolicy residential, CategoryPolicy commercial, CategoryPolicy workplace)
        { throw new NotImplementedException(); }
        public static DemandSettings Native() { throw new NotImplementedException(); }
        public CategoryPolicy Residential { get { throw new NotImplementedException(); } }
        public CategoryPolicy Commercial { get { throw new NotImplementedException(); } }
        public CategoryPolicy Workplace { get { throw new NotImplementedException(); } }
    }

    public static class DemandPolicy
    {
        public static bool TryCreate(int mode, int? target, out CategoryPolicy policy)
        { policy = null; throw new NotImplementedException(); }
        public static int Evaluate(int input, CategoryPolicy policy) { throw new NotImplementedException(); }
    }
}
