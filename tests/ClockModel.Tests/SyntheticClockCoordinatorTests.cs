// Signature: OpenAI-Codex
using System;

internal static class SyntheticClockCoordinatorTests
{
    internal static void Run()
    {
        Type coordinator = typeof(SyntheticClockCoordinatorTests).Assembly.GetType(
            "RealisticCityTime.Game.SyntheticClockCoordinator");
        if (coordinator == null)
            throw new Exception("Missing synthetic coordinator for the game-compatible clock boundary.");
    }
}
