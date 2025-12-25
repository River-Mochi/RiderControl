// Mod.cs
// Entry point for RiderControl mod.

namespace RiderControl
{
    using Colossal.IO.AssetDatabase;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using Game.Simulation;

    public sealed class Mod : IMod
    {
        internal static readonly ILog s_Log =
            LogManager.GetLogger($"{nameof(RiderControl)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        internal static Setting Settings { get; private set; } = null!;

        public void OnLoad(UpdateSystem updateSystem)
        {
            s_Log.Info(nameof(OnLoad));

            Settings = new Setting(this);
            AssetDatabase.global.LoadSettings(nameof(RiderControl), Settings, new Setting(this), userSetting: true);

            Settings.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));

            // Must run before ResidentAISystem so we can unwind taxi-wait state cleanly.
            updateSystem.UpdateBefore<RiderControlSystem, ResidentAISystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            s_Log.Info(nameof(OnDispose));

            // Not setting Settings = null (avoids nullable warnings + not needed).
            Settings.UnregisterInOptionsUI();
        }
    }
}
