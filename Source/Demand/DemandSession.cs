// Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population
using System;

namespace RealisticUrbanPopulation.Demand
{
    public enum ControlPermission { Unknown, Allowed, Conflict }

    public sealed class DemandSessionState
    {
        private readonly long epoch;
        private readonly long revision;
        private readonly bool loaded;
        private readonly bool armed;
        private readonly ControlPermission permission;
        private readonly DemandSettings settings;

        internal DemandSessionState(long epoch, long revision, bool loaded, bool armed,
            ControlPermission permission, DemandSettings settings)
        {
            this.epoch = epoch;
            this.revision = revision;
            this.loaded = loaded;
            this.armed = armed;
            this.permission = permission;
            this.settings = settings;
        }
        public long Epoch { get { return epoch; } }
        public long Revision { get { return revision; } }
        public bool Loaded { get { return loaded; } }
        public bool Armed { get { return armed; } }
        public ControlPermission Permission { get { return permission; } }
        public DemandSettings Settings { get { return settings; } }
    }

    public sealed class DemandSession
    {
        // All epoch checks and state changes share this lock; no check-then-publish window.
        private readonly object sync = new object();
        private long currentEpoch;
        private long revision;
        private bool active;
        private bool loaded;
        private bool armed;
        private ControlPermission permission = ControlPermission.Unknown;
        private DemandSettings settings = DemandSettings.Native();

        private bool Matches(long epoch)
        { return active && epoch == currentEpoch; }

        public long BeginCity()
        {
            lock (sync)
            {
                currentEpoch = checked(currentEpoch + 1);
                revision++;
                active = true;
                loaded = false;
                armed = false;
                permission = ControlPermission.Unknown;
                settings = DemandSettings.Native();
                return currentEpoch;
            }
        }

        public bool CompleteCityLoad(long epoch)
        {
            lock (sync)
            {
                if (!Matches(epoch)) return false;
                if (!loaded) { loaded = true; revision++; }
                return true;
            }
        }

        public bool SetControlPermission(long epoch, ControlPermission permission)
        {
            lock (sync)
            {
                if (!Matches(epoch)) return false;
                if (permission != ControlPermission.Allowed && permission != ControlPermission.Conflict)
                    permission = ControlPermission.Unknown;
                this.permission = permission;
                if (permission != ControlPermission.Allowed) armed = false;
                revision++;
                return true;
            }
        }

        public bool TryApply(long epoch, DemandSettings settings)
        {
            lock (sync)
            {
                if (!Matches(epoch) || !loaded || permission != ControlPermission.Allowed || settings == null)
                    return false;
                this.settings = settings;
                armed = true;
                revision++;
                return true;
            }
        }

        public int Evaluate(long epoch, DemandChannel channel, int input)
        {
            lock (sync)
            {
                if (!Matches(epoch) || !loaded || !armed || permission != ControlPermission.Allowed)
                    return input;
                switch (channel)
                {
                    case DemandChannel.Residential: return DemandPolicy.Evaluate(input, settings.Residential);
                    case DemandChannel.Commercial: return DemandPolicy.Evaluate(input, settings.Commercial);
                    case DemandChannel.Workplace: return DemandPolicy.Evaluate(input, settings.Workplace);
                    default: return input;
                }
            }
        }

        public bool RestoreNative(long epoch)
        {
            lock (sync)
            {
                if (!Matches(epoch)) return false;
                armed = false;
                settings = DemandSettings.Native();
                revision++;
                return true;
            }
        }

        public bool EndCity(long epoch)
        {
            lock (sync)
            {
                if (!Matches(epoch)) return false;
                active = false;
                loaded = false;
                armed = false;
                permission = ControlPermission.Unknown;
                settings = DemandSettings.Native();
                revision++;
                return true;
            }
        }

        public DemandSessionState ReadState()
        {
            lock (sync)
                return new DemandSessionState(currentEpoch, revision, loaded, armed, permission, settings);
        }
    }
}
