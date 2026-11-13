// Synthetic target only. It is not the game SimulationManager.
// Signature: OpenAI-Codex
using System;
using System.Runtime.CompilerServices;

public sealed class SimulationMetaData
{
    public DateTime m_currentDateTime;
}

public sealed class SimulationManager
{
    public SimulationMetaData m_metaData;
    public TimeSpan m_timePerFrame;
    public uint m_currentFrameIndex;
    public long m_timeOffsetTicks;
    public bool Paused;

    public SimulationManager(DateTime date, TimeSpan perFrame)
    {
        if (perFrame.Ticks <= 0) throw new ArgumentOutOfRangeException("perFrame");
        m_metaData = new SimulationMetaData();
        m_metaData.m_currentDateTime = date;
        m_timePerFrame = perFrame;
        m_timeOffsetTicks = date.Ticks;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void SimulationStep()
    {
        m_currentFrameIndex = checked(m_currentFrameIndex + 1U);
        m_metaData.m_currentDateTime += m_timePerFrame;
    }

    public bool Tick()
    {
        if (Paused) return false;
        SimulationStep();
        return true;
    }

    public DateTime FrameToTime(uint frame)
    {
        long ticks = checked(checked((long)frame * m_timePerFrame.Ticks) + m_timeOffsetTicks);
        return new DateTime(ticks, m_metaData.m_currentDateTime.Kind);
    }
}
