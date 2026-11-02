// Signature: OpenAI-Codex
using ICities;

namespace RealisticBorderControl.Game
{
    public sealed class BorderControlMod : IUserMod
    {
        public string Name { get { return "Realistic Border Control"; } }
        public string Description { get { return "Initial inactive shell. Passenger observation and border controls are unavailable."; } }
        public void OnEnabled() { BorderControlLifecycle.Enable(); }
        public void OnDisabled() { BorderControlLifecycle.Disable(); }
    }
}
