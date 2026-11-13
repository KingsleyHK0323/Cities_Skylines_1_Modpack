// Synthetic arithmetic checks only; no game or Harmony assembly is loaded.
// Signature: OpenAI-Codex
using System;
using RealisticCityTime.ClockSafetyFixture;

public static class SyntheticClockChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    public static void Run()
    {
        checks = 0;
        const long ticksPerSecond = 10000000L;
        const long perFrame = 1476562500L;
        Snapshot initial = new Snapshot(1000U, perFrame, 0L, 1000L * perFrame);
        Check(SyntheticClock.IsCoherent(initial), "synthetic baseline coherent");

        Snapshot next;
        ProposalStatus status = SyntheticClock.TryPropose(initial, ticksPerSecond, true, true, false, true, false, out next);
        Check(status == ProposalStatus.Available && next.Frame == 1001U && next.DateTicks == initial.DateTicks + ticksPerSecond, "eligible frame proposes one simulated second");
        Check(next.DateTicks >= initial.DateTicks && SyntheticClock.IsCoherent(next), "proposed boundary monotonic and coherent");
        Check(SyntheticClock.FrameToTime(next, next.Frame - 1) < SyntheticClock.FrameToTime(next, next.Frame) &&
              SyntheticClock.FrameToTime(next, next.Frame) < SyntheticClock.FrameToTime(next, next.Frame + 1), "synthetic neighboring event dates remain ordered");
        Check(SyntheticClock.TimeToFrame(next, SyntheticClock.FrameToTime(next, next.Frame + 1)) == next.Frame + 1, "synthetic frame conversion round-trip");

        long metadataOnly = initial.DateTicks + ticksPerSecond;
        long convertedAfterFrame = SyntheticClock.FrameToTime(initial, initial.Frame + 1);
        Check(convertedAfterFrame - metadataOnly == 1466562500L, "metadata-only counterexample exposes 146.65625 second gap");
        long beforeFrameCompensation = initial.DateTicks + ticksPerSecond - perFrame;
        Check(beforeFrameCompensation < initial.DateTicks && initial.DateTicks - beforeFrameCompensation == 1466562500L, "before-frame compensation exposes backward metadata");
        Snapshot halfRate = new Snapshot(initial.Frame, perFrame / 2, (long)initial.Frame * (perFrame - perFrame / 2), initial.DateTicks);
        long beforeDisplay = SyntheticClock.RawDisplay(initial, initial.Frame, 0.5f);
        long afterDisplay = SyntheticClock.RawDisplay(halfRate, initial.Frame, 0.5f);
        Check(beforeDisplay - afterDisplay == 369140640L, "raw display counterexample exposes backward interpolation");

        long candidateDisplay = SyntheticClock.RawDisplay(next, initial.Frame, 0.5f);
        Check(beforeDisplay - candidateDisplay == 1466562500L, "coherent synthetic proposal can still move raw display backward");
        Snapshot zeroDelta;
        Check(SyntheticClock.TryPropose(initial, 0, true, true, false, true, false, out zeroDelta) == ProposalStatus.Available &&
              zeroDelta.DateTicks == initial.DateTicks && zeroDelta.Frame == initial.Frame + 1 && SyntheticClock.IsCoherent(zeroDelta), "zero elapsed keeps metadata unchanged at synthetic boundary");
        Snapshot skipped;
        Check(SyntheticClock.TryPropose(initial, ticksPerSecond, true, true, true, true, false, out skipped) == ProposalStatus.Skipped && Object.ReferenceEquals(initial, skipped), "paused synthetic interval makes no proposal");
        Check(SyntheticClock.TryPropose(initial, ticksPerSecond, true, false, false, true, false, out skipped) == ProposalStatus.Skipped && Object.ReferenceEquals(initial, skipped), "zero-frame synthetic interval makes no proposal");
        Check(SyntheticClock.TryPropose(initial, ticksPerSecond, false, true, false, true, false, out skipped) == ProposalStatus.Skipped && Object.ReferenceEquals(initial, skipped), "disabled model makes no proposal");
        Snapshot native = SyntheticClock.FakeNativeStep(initial);
        Check(native.DateTicks == initial.DateTicks + perFrame && native.Frame == initial.Frame + 1, "disabled proposal does not mean fake native clock stopped");
        Check(SyntheticClock.TryPropose(initial, ticksPerSecond, true, true, false, false, false, out skipped) == ProposalStatus.Unavailable && Object.ReferenceEquals(initial, skipped), "unknown continuity refuses proposal");
        Check(SyntheticClock.TryPropose(initial, ticksPerSecond, true, true, false, true, true, out skipped) == ProposalStatus.Unavailable && Object.ReferenceEquals(initial, skipped), "uncertain prior mutation refuses proposal");
        Check(SyntheticClock.TryPropose(initial, -1, true, true, false, true, false, out skipped) == ProposalStatus.Unavailable && Object.ReferenceEquals(initial, skipped), "negative elapsed refuses proposal");
        Snapshot zeroRate = new Snapshot(0U, 0L, 0L, 0L);
        Check(SyntheticClock.TryPropose(zeroRate, 1, true, true, false, true, false, out skipped) == ProposalStatus.Unavailable && Object.ReferenceEquals(zeroRate, skipped), "zero native frame duration refuses proposal");
        Snapshot dateLimit = new Snapshot(0U, 1L, DateTime.MaxValue.Ticks, DateTime.MaxValue.Ticks);
        Check(SyntheticClock.TryPropose(dateLimit, 1, true, true, false, true, false, out skipped) == ProposalStatus.Unavailable && Object.ReferenceEquals(dateLimit, skipped), "date overflow refuses proposal");
        Snapshot frameLimit = new Snapshot(UInt32.MaxValue, 1L, 0L, UInt32.MaxValue);
        Check(SyntheticClock.TryPropose(frameLimit, 1, true, true, false, true, false, out skipped) == ProposalStatus.Unavailable && Object.ReferenceEquals(frameLimit, skipped), "frame wrap refuses proposal");

        Snapshot cumulative = initial;
        long[] deltas = { 10 * ticksPerSecond, 20 * ticksPerSecond, 30 * ticksPerSecond };
        foreach (long delta in deltas)
        {
            Snapshot proposed;
            if (SyntheticClock.TryPropose(cumulative, delta, true, true, false, true, false, out proposed) != ProposalStatus.Available)
                throw new InvalidOperationException("Synthetic multi-frame proposal failed.");
            cumulative = proposed;
        }
        Check(cumulative.DateTicks - initial.DateTicks == 60 * ticksPerSecond && SyntheticClock.IsCoherent(cumulative), "assumed eligible 60 seconds allocated across three fake frames");
        long reconstructedOffset = initial.DateTicks - (long)next.Frame * next.PerFrameTicks;
        Check((long)next.Frame * next.PerFrameTicks + reconstructedOffset == initial.DateTicks &&
              next.Frame != initial.Frame && initial.DateTicks != next.DateTicks, "mixed save can satisfy reconstructed relation while losing elapsed time");
        Check(initial.Frame == 1000U && initial.OffsetTicks == 0L && initial.DateTicks == 1000L * perFrame, "pure proposal leaves original snapshot unchanged");
        Console.WriteLine("TOTAL: " + checks);
    }
}
