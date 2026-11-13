// Signature: OpenAI-Codex_diagnostic_tests
// Test-only stand-ins: no real game assembly, singleton, player identity or save data.
using System;
using System.Threading;

public sealed class SimulationManager
{
    private uint frame;
    private uint dayPhase;
    private TimeSpan frameDuration;
    private uint referenceFrame;
    private float referenceTimer;
    private float visualHour;
    private int reads;
    private int blockNext;

    public bool FailOnRead;
    public Action OnNextRead;
    public readonly ManualResetEvent ReadEntered = new ManualResetEvent(false);
    public readonly ManualResetEvent ReleaseRead = new ManualResetEvent(false);

    public int ReadCount { get { return reads; } }

    public void BlockOneRead() { Interlocked.Exchange(ref blockNext, 1); }

    private void Touch()
    {
        Interlocked.Increment(ref reads);
        if (Interlocked.Exchange(ref blockNext, 0) == 1)
        {
            ReadEntered.Set();
            if (!ReleaseRead.WaitOne(10000)) throw new InvalidOperationException("Timed out waiting for fake read release.");
        }
        Action action = OnNextRead;
        OnNextRead = null;
        if (action != null) action();
        if (FailOnRead) throw new InvalidOperationException("Fake simulation read was rejected.");
    }

    public uint m_currentFrameIndex { get { Touch(); return frame; } set { frame = value; } }
    public uint m_dayTimeFrame { get { Touch(); return dayPhase; } set { dayPhase = value; } }
    public TimeSpan m_timePerFrame { get { Touch(); return frameDuration; } set { frameDuration = value; } }
    public uint m_referenceFrameIndex { get { Touch(); return referenceFrame; } set { referenceFrame = value; } }
    public float m_referenceTimer { get { Touch(); return referenceTimer; } set { referenceTimer = value; } }
    public float m_currentDayTimeHour { get { Touch(); return visualHour; } set { visualHour = value; } }
}

public sealed class LoadingManager
{
    private bool currentlyLoading;
    private bool loadingComplete;
    private int reads;
    public bool FailOnRead;

    public int ReadCount { get { return reads; } }

    private void Touch()
    {
        Interlocked.Increment(ref reads);
        if (FailOnRead) throw new InvalidOperationException("Fake loading read was rejected.");
    }

    public bool m_currentlyLoading { get { Touch(); return currentlyLoading; } set { currentlyLoading = value; } }
    public bool m_loadingComplete { get { Touch(); return loadingComplete; } set { loadingComplete = value; } }
}
