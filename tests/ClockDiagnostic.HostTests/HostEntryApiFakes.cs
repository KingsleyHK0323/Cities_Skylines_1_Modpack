// Signature: OpenAI-Codex_native_validation_options
// Standalone fake game entry API. It is never compiled with the real Unity or game assemblies.
using System;
using System.Threading;

namespace UnityEngine
{
    public class Object
    {
        internal static Object[] Simulations = new Object[0];
        internal static Object[] Loadings = new Object[0];
        internal static int SimulationLookups;
        internal static int LoadingLookups;
        internal static bool ThrowOnLookup;
        internal static Action<Type> DuringLookup;

        public static Object[] FindObjectsOfType(Type type)
        {
            if (type == typeof(SimulationManager)) Interlocked.Increment(ref SimulationLookups);
            else if (type == typeof(LoadingManager)) Interlocked.Increment(ref LoadingLookups);
            else throw new InvalidOperationException("Unexpected synthetic object lookup.");
            Action<Type> action = DuringLookup;
            if (action != null) action(type);
            if (ThrowOnLookup) throw new InvalidOperationException("Synthetic object lookup failed.");
            return type == typeof(SimulationManager) ? Simulations : Loadings;
        }
    }
}

public sealed class SimulationManager : UnityEngine.Object
{
    private uint frame;
    private uint phase;
    private TimeSpan duration;
    private uint referenceFrame;
    private float referenceTimer;
    private float hour;
    private int reads;
    private int blockNext;
    public bool FailOnRead;
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
            if (!ReleaseRead.WaitOne(10000)) throw new InvalidOperationException("Synthetic game read timed out.");
        }
        if (FailOnRead) throw new InvalidOperationException("Synthetic game read was rejected.");
    }

    public uint m_currentFrameIndex { get { Touch(); return frame; } set { frame = value; } }
    public uint m_dayTimeFrame { get { Touch(); return phase; } set { phase = value; } }
    public TimeSpan m_timePerFrame { get { Touch(); return duration; } set { duration = value; } }
    public uint m_referenceFrameIndex { get { Touch(); return referenceFrame; } set { referenceFrame = value; } }
    public float m_referenceTimer { get { Touch(); return referenceTimer; } set { referenceTimer = value; } }
    public float m_currentDayTimeHour { get { Touch(); return hour; } set { hour = value; } }
}

public sealed class LoadingManager : UnityEngine.Object
{
    private bool currentlyLoading;
    private bool loadingComplete;
    private int reads;
    public bool FailOnRead;
    public int ReadCount { get { return reads; } }
    private void Touch()
    {
        Interlocked.Increment(ref reads);
        if (FailOnRead) throw new InvalidOperationException("Synthetic loading read was rejected.");
    }
    public bool m_currentlyLoading { get { Touch(); return currentlyLoading; } set { currentlyLoading = value; } }
    public bool m_loadingComplete { get { Touch(); return loadingComplete; } set { loadingComplete = value; } }
}

namespace ICities
{
    public interface IUserMod { string Name { get; } string Description { get; } }
    public enum LoadMode { NewGame, LoadGame, NewGameFromScenario, LoadScenario, Other }
    public class LoadingExtensionBase
    {
        public virtual void OnLevelLoaded(LoadMode mode) { }
        public virtual void OnLevelUnloading() { }
        public virtual void OnReleased() { }
    }
    public class ThreadingExtensionBase
    {
        public virtual void OnBeforeSimulationFrame() { }
        public virtual void OnAfterSimulationFrame() { }
        public virtual void OnUpdate(float realTimeDelta, float simulationTimeDelta) { }
        public virtual void OnReleased() { }
    }
}
