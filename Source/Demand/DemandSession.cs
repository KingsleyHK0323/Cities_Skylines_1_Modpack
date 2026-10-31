// Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population
using System;

namespace RealisticUrbanPopulation.Demand
{
    public enum ControlPermission { Unknown, Allowed, Conflict }

    public sealed class DemandSessionState
    {
        public long Epoch { get { throw new NotImplementedException(); } }
        public long Revision { get { throw new NotImplementedException(); } }
        public bool Loaded { get { throw new NotImplementedException(); } }
        public bool Armed { get { throw new NotImplementedException(); } }
        public ControlPermission Permission { get { throw new NotImplementedException(); } }
        public DemandSettings Settings { get { throw new NotImplementedException(); } }
    }

    public sealed class DemandSession
    {
        public long BeginCity() { throw new NotImplementedException(); }
        public bool CompleteCityLoad(long epoch) { throw new NotImplementedException(); }
        public bool SetControlPermission(long epoch, ControlPermission permission) { throw new NotImplementedException(); }
        public bool TryApply(long epoch, DemandSettings settings) { throw new NotImplementedException(); }
        public int Evaluate(long epoch, DemandChannel channel, int input) { throw new NotImplementedException(); }
        public bool RestoreNative(long epoch) { throw new NotImplementedException(); }
        public bool EndCity(long epoch) { throw new NotImplementedException(); }
        public DemandSessionState ReadState() { throw new NotImplementedException(); }
    }
}
