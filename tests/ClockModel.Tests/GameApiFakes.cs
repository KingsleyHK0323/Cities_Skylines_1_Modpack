// Signature: OpenAI-Codex
using System;
using System.Threading;

// Test fixtures only. These types are never compiled into the game assembly.
namespace ICities
{
    public interface IUserMod { string Name { get; } string Description { get; } }
    public interface ILoading { }
    public delegate void OnTextChanged(string value);
    public delegate void OnTextSubmitted(string value);
    public delegate void OnButtonClicked();
    public abstract class UIHelperBase
    {
        public abstract object AddTextfield(string text, string defaultContent,
            OnTextChanged changed, OnTextSubmitted submitted);
        public abstract object AddButton(string text, OnButtonClicked clicked);
    }
    public enum LoadMode { NewGame, LoadGame, NewGameFromScenario, LoadScenario, Other }
    public class LoadingExtensionBase
    {
        public static ManualResetEvent EnteredCreated;
        public static ManualResetEvent ContinueCreated;
        public virtual void OnCreated(ILoading loading)
        {
            if (EnteredCreated != null)
            {
                EnteredCreated.Set();
                if (!ContinueCreated.WaitOne(5000)) throw new InvalidOperationException("Synthetic creation timed out.");
            }
        }
        public virtual void OnLevelLoaded(LoadMode mode) { }
        public virtual void OnLevelUnloading() { }
        public virtual void OnReleased() { }
    }
    public class ThreadingExtensionBase { public virtual void OnAfterSimulationTick() { } }
}

namespace ColossalFramework.UI
{
    public class UITextField
    {
        private string value = String.Empty;
        private int maximum = 1024;
        public bool readOnly { get; set; }
        public bool submitOnFocusLost { get; set; }
        public bool numericalOnly { get; set; }
        public Func<string, string> Filter;
        public int maxLength
        {
            get { return maximum; }
            set
            {
                maximum = Math.Max(0, value);
                if (this.value.Length > maximum) text = this.value.Substring(0, maximum);
            }
        }
        public ICities.OnTextChanged Changed;
        public string text
        {
            get { return value; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                string visible = value.Length > maximum ? value.Substring(0, maximum) : value;
                if (Filter != null) visible = Filter(visible) ?? String.Empty;
                if (this.value == visible) return;
                this.value = visible;
                if (Changed != null) Changed(visible);
            }
        }
        public void SimulateCancelWithoutChangeEvent(string restored) { value = restored; }
    }
}

namespace ColossalFramework
{
    public static class Singleton<T> where T : class
    {
        private static bool available;
        public static ManualResetEvent EnteredExists;
        public static ManualResetEvent ContinueExists;
        public static volatile bool ExistsWaitTimedOut;
        public static int ExistsReadCount;
        public static int ExistsBarrierAtRead;
        public static bool exists
        {
            get
            {
                int readNumber = Interlocked.Increment(ref ExistsReadCount);
                if (EnteredExists != null && readNumber == ExistsBarrierAtRead)
                {
                    EnteredExists.Set();
                    if (!ContinueExists.WaitOne(5000))
                    {
                        ExistsWaitTimedOut = true;
                        throw new InvalidOperationException("Synthetic singleton lookup timed out.");
                    }
                }
                return available;
            }
            set { available = value; }
        }
        public static T instance;
    }
}

public sealed class SimulationManager
{
    public DateTime m_currentGameTime;
    public uint m_currentFrameIndex;
    public bool ForcedSimulationPaused;
    public bool Paused;
    public ManualResetEvent EnteredPauseRead;
    public ManualResetEvent ContinuePauseRead;
    public volatile bool PauseWaitTimedOut;

    public bool SimulationPaused
    {
        get
        {
            if (EnteredPauseRead != null)
            {
                EnteredPauseRead.Set();
                if (!ContinuePauseRead.WaitOne(5000))
                {
                    PauseWaitTimedOut = true;
                    throw new InvalidOperationException("Synthetic read timed out.");
                }
            }
            return Paused;
        }
    }
}

public sealed class LoadingManager
{
    public volatile bool m_currentlyLoading;
    public volatile bool m_loadingComplete;
}
