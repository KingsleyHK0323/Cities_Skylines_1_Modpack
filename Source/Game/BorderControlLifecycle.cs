// Signature: OpenAI-Codex
namespace RealisticBorderControl.Game
{
    // Internal lifecycle flags only: no game managers, hooks, IO, or scheduled work.
    public static class BorderControlLifecycle
    {
        public static bool Enabled { get; private set; }
        public static bool CityLoaded { get; private set; }
        public static void Enable() { Enabled = true; }
        public static void Disable() { Enabled = false; CityLoaded = false; }
        public static void Load(bool playableCity) { CityLoaded = Enabled && playableCity; }
        public static void Unload() { CityLoaded = false; }
    }
}
