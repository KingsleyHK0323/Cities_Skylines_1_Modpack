// Signature: OpenAI-Codex_clock_surface
using System;
using System.Reflection;
using ICities;
using RealisticCityTime.Game;

internal static class ChannelIdentityTests
{
    internal static void Run(string[] args)
    {
        if (args == null || args.Length != 2 ||
            !String.Equals(args[0], "--expect-channel", StringComparison.Ordinal) ||
            (!String.Equals(args[1], "Unspecified", StringComparison.Ordinal) &&
             !String.Equals(args[1], "Development", StringComparison.Ordinal) &&
             !String.Equals(args[1], "Release", StringComparison.Ordinal)))
            throw new ArgumentException("Expected exactly --expect-channel followed by Unspecified, Development, or Release.");
        string expectedChannel = args[1];
        if (!Object.ReferenceEquals(typeof(IUserMod).Assembly, typeof(ChannelIdentityTests).Assembly))
            throw new Exception("Channel identity: IUserMod is not in the fake test assembly.");
        IUserMod mod = new RealisticCityTimeMod();
        Equal("Realistic City Time", mod.Name, "Name");
        string actualChannel = ReadBuildChannel();
        Equal(expectedChannel, actualChannel, "GameBuildChannel.Value");
        string expectedDescription = String.Equals(expectedChannel, "Release", StringComparison.Ordinal)
            ? "Observation-only foundation. Does not change game time or speed."
            : "Development observation-only foundation. Does not change game time or speed.";
        Equal(expectedDescription, mod.Description, "Description");
    }

    private static string ReadBuildChannel()
    {
        Assembly assembly = typeof(RealisticCityTimeMod).Assembly;
        if (!Object.ReferenceEquals(assembly, typeof(ChannelIdentityTests).Assembly))
            throw new Exception("Channel identity: mod is not in the fake test assembly.");
        Type marker = assembly.GetType("RealisticCityTime.Game.GameBuildChannel", false);
        if (marker == null)
            throw new Exception("Channel identity: GameBuildChannel type is missing.");
        if (!marker.IsNotPublic)
            throw new Exception("Channel identity: GameBuildChannel is not internal.");
        FieldInfo value = marker.GetField("Value", BindingFlags.Static | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (value == null)
            throw new Exception("Channel identity: GameBuildChannel.Value field is missing.");
        if (!value.IsAssembly)
            throw new Exception("Channel identity: GameBuildChannel.Value is not internal.");
        if (!value.IsLiteral || value.FieldType != typeof(string))
            throw new Exception("Channel identity: GameBuildChannel.Value is not a literal string.");
        object raw = value.GetRawConstantValue();
        if (!(raw is string))
            throw new Exception("Channel identity: GameBuildChannel.Value is null or invalid.");
        return (string)raw;
    }

    private static void Equal(string expected, string actual, string property)
    {
        if (!String.Equals(expected, actual, StringComparison.Ordinal))
            throw new Exception("Channel identity: unexpected " + property + ".");
    }
}
