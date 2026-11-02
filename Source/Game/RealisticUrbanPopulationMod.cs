// Signature: OpenAI-Codex_Realistic_Urban_Population
using ICities;
using RealisticUrbanPopulation.Runtime;

namespace RealisticUrbanPopulation.Game
{
    public sealed class RealisticUrbanPopulationMod : IUserMod
    {
        public string Name { get { return "Realistic Urban Population"; } }
        public string Description
        { get { return "Inactive foundation. Population and RCI control are not enabled."; } }
    }

    public sealed class PopulationLoadingExtension : LoadingExtensionBase
    {
        private readonly PopulationLifecycle lifecycle = new PopulationLifecycle();
        private readonly LifecycleBoundary boundary = new LifecycleBoundary();
        private long epoch;

        public override void OnLevelLoaded(LoadMode mode)
        {
            boundary.TryRun(delegate
            {
                epoch = lifecycle.BeginLevel(mode == LoadMode.NewGame || mode == LoadMode.LoadGame);
            });
        }

        public override void OnLevelUnloading() { CloseLevel(); }
        public override void OnReleased() { CloseLevel(); }

        private void CloseLevel()
        {
            boundary.TryRun(delegate
            {
                if (epoch != 0) lifecycle.EndLevel(epoch);
                epoch = 0;
            });
        }
    }
}
