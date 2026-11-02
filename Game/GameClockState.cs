// Signature: OpenAI-Codex_Realistic_City_Time
using System;

namespace RealisticCityTime.Game
{
    // This is a cached observation, not a guarantee that the game is still advancing.
    internal sealed class GameClockSnapshot
    {
        internal readonly long Generation;
        internal readonly long Sequence;
        internal readonly DateTime? GameTime;
        internal readonly bool? Paused;
        internal readonly uint? Frame;
        internal readonly long? MonotonicTicks;

        internal GameClockSnapshot(long generation, long sequence, DateTime? gameTime,
            bool? paused, uint? frame, long? monotonicTicks)
        {
            Generation = generation; Sequence = sequence; GameTime = gameTime;
            Paused = paused; Frame = frame; MonotonicTicks = monotonicTicks;
        }
    }

    internal sealed class GameClockState
    {
        private readonly object gate = new object();
        private long generation;
        private long sequence;
        private bool active;
        private GameClockSnapshot latest;

        internal long Begin()
        {
            lock (gate)
            {
                long next = checked(generation + 1);
                generation = next; sequence = 0; latest = null; active = true;
                return next;
            }
        }

        internal void End()
        {
            lock (gate) { active = false; latest = null; }
        }

        internal long CurrentGeneration
        {
            get { lock (gate) { return active ? generation : 0; } }
        }

        internal bool Publish(long token, DateTime? gameTime, bool? paused, uint? frame, long? monotonicTicks)
        {
            lock (gate)
            {
                if (!active || token != generation || sequence == long.MaxValue) return false;
                latest = new GameClockSnapshot(generation, sequence++, gameTime, paused, frame, monotonicTicks);
                return true;
            }
        }

        internal GameClockSnapshot Latest
        {
            get { lock (gate) { return latest; } }
        }
    }
}
