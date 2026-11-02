// OpenAI-Codex_Intercity_Resource_Sharing
using System;

namespace IntercityResourceSharing
{
    // Instance-owned state, with no city references or resource effects.
    internal sealed class Lifecycle
    {
        internal bool Active { get; private set; }
        internal bool Failed { get; private set; }

        internal void Begin(bool playable, Action initialize)
        {
            End();
            if (!playable) return;
            try
            {
                if (initialize != null) initialize();
                Active = true;
            }
            catch (Exception)
            {
                Active = false;
                Failed = true;
            }
        }

        internal void End()
        {
            Active = false;
            Failed = false;
        }
    }
}
