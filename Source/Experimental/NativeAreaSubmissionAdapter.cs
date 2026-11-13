// OpenAI-Codex_build_review
#if FULLMAP_NATIVE_AREA_SUBMISSION_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using ICities;

namespace FullMapNativeResearch
{
    public enum NativeAreaOutcome
    {
        Unavailable,
        Pending,
        Unknown
    }

    // Research only. Private access and reflection are not a same-process security boundary.
    // This class has no authenticated city, lifecycle, or game-thread guarantee.
    public sealed class NativeAreaSubmissionAdapter
    {
        private readonly object stateLock = new object();
        private NativeAreaOutcome state = NativeAreaOutcome.Unavailable;
        private bool callbackEntered;

        public NativeAreaOutcome Status
        {
            get { lock (stateLock) { return state; } }
        }

        // All ordinarily reachable submission remains unavailable without inspecting its inputs.
        public NativeAreaOutcome Submit(IAreas areas, IThreading threading, int x, int z)
        {
            return NativeAreaOutcome.Unavailable;
        }

        // This method has no ordinary caller; isolated tests reach it with fake providers only.
        private NativeAreaOutcome Core(IAreas areas, IThreading threading, int x, int z)
        {
            lock (stateLock)
            {
                if (state != NativeAreaOutcome.Unavailable)
                    return state;
                if (areas == null || threading == null || x < 0 || x > 4 || z < 0 || z > 4)
                    return NativeAreaOutcome.Unavailable;
                state = NativeAreaOutcome.Pending;
            }

            try
            {
                threading.QueueSimulationThread(delegate
                {
                    lock (stateLock)
                    {
                        if (state != NativeAreaOutcome.Pending || callbackEntered)
                            return;
                        callbackEntered = true;
                    }

                    // A callback that passed the guard cannot be revoked by this class.
                    try
                    {
                        areas.UnlockArea(x, z, false);
                    }
                    catch (Exception)
                    {
                        // A writer exception does not establish whether any effect occurred.
                    }
                    finally
                    {
                        lock (stateLock) { state = NativeAreaOutcome.Unknown; }
                    }
                });
            }
            catch (Exception)
            {
                // Queue failure can occur before or after the callback is accepted or run.
                lock (stateLock) { state = NativeAreaOutcome.Unknown; }
            }

            lock (stateLock) { return state; }
        }
    }
}
#endif