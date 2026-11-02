// Signature: OpenAI-Codex_Realistic_Railway_Traffic_Control
using System;

namespace RealisticRailwayTrafficControl.Runtime
{
    internal delegate void LifecycleOperation();

    // Contains errors in this module's bookkeeping only; it neither hooks nor controls game AI.
    internal sealed class RailwayModLifecycle
    {
        private readonly object sync = new object();
        private readonly LifecycleOperation beginCity;
        private readonly LifecycleOperation endCity;
        private bool levelActive;
        private string failureStage;
        private string failureType;
        private bool cleanupFailed;

        internal RailwayModLifecycle(LifecycleOperation beginCity, LifecycleOperation endCity)
        {
            if (beginCity == null) throw new ArgumentNullException("beginCity");
            if (endCity == null) throw new ArgumentNullException("endCity");
            this.beginCity = beginCity;
            this.endCity = endCity;
        }

        public bool IsLevelActive { get { lock (sync) { return levelActive; } } }
        public bool Faulted { get { lock (sync) { return failureStage != null; } } }
        public bool CleanupFailed { get { lock (sync) { return cleanupFailed; } } }
        public string LastFailureStage { get { lock (sync) { return failureStage; } } }
        public string LastFailureType { get { lock (sync) { return failureType; } } }

        public bool TryLoad(bool gameplay)
        {
            lock (sync)
            {
                failureStage = null;
                failureType = null;
                cleanupFailed = false;
                if (!Stop("BeforeLoad")) return false;
                if (!gameplay) return true;
                try
                {
                    beginCity();
                    levelActive = true;
                    return true;
                }
                catch (Exception error)
                {
                    Remember("Load", error);
                    Stop("LoadCleanup");
                    return false;
                }
            }
        }

        public bool TryUnload()
        {
            lock (sync) { return Stop("Unload"); }
        }

        public void RecordFailure(string stage, Exception error)
        {
            lock (sync)
            {
                Remember(stage, error);
                Stop("FailureCleanup");
            }
        }

        private bool Stop(string stage)
        {
            levelActive = false;
            try { endCity(); return true; }
            catch (Exception error)
            {
                cleanupFailed = true;
                Remember(stage, error);
                return false;
            }
        }

        private void Remember(string stage, Exception error)
        {
            if (failureStage != null) return;
            failureStage = String.IsNullOrEmpty(stage) ? "Unknown" : stage;
            // Do not retain arbitrary exception messages, paths, credentials, or game objects.
            failureType = error == null ? "Unknown" : error.GetType().FullName;
        }
    }
}
