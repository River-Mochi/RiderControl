// File: Mod.cs
// Entry point for "Smart Traveler".

namespace RiderControl
{
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using Game.Simulation;
    using System.IO;
    using System.Reflection;

    public sealed class Mod : IMod
    {
        public const string ModName = "Smart Traveler";
        public const string ModId = "SmartTraveler";
        public const string ModTag = "[ST]";
        public const string ShortName = "Smart Traveler";

        private static bool s_BannerLogged;

        public static readonly string ModVersion =
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        public static readonly ILog s_Log =
            LogManager.GetLogger(ModId).SetShowsErrorsInUI(false);

        public static Setting? Setting
        {
            get; private set;
        }

        public void OnLoad(UpdateSystem updateSystem)
        {
            if (!s_BannerLogged)
            {
                s_BannerLogged = true;
                s_Log.Info($"{ModId} {ModTag} v{ModVersion} OnLoad");
            }

            if (s_Log is UnityLogger unityLogger)   // Stabilize file logging: keep stream open (still Colossal.Logging, not UnityEngine.Debug).
            {
                unityLogger.keepStreamOpen = true;

                try
                {
                    string? dir = Path.GetDirectoryName(unityLogger.logPath);
                    if (!string.IsNullOrEmpty(dir))     // Ensure log directory exists 
                        Directory.CreateDirectory(dir);
                }
                catch
                {  // Do not crash OnLoad for logging setup.
                }
            }

            Setting setting = new Setting(this);
            Setting = setting;

            LocalizationManager? lm = GameManager.instance?.localizationManager;
            if (lm != null)
            {
                lm.AddSource("en-US", new LocaleEN(setting));
            }
            else
            {
                s_Log.Warn($"{ModTag} LocalizationManager is null; skipping locale registration.");
            }

            Setting defaults = new Setting(this);
            AssetDatabase.global.LoadSettings(ModId, setting, defaults, userSetting: true);

            setting.RegisterInOptionsUI();

            // Register systems.
            updateSystem.UpdateBefore<MovingAwayFixSystem, ResidentAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAfter<RiderControlSystem, ResidentAISystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<RiderControlSystem, TaxiDispatchSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateBefore<RiderControlSystem, RideNeederSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            s_Log.Info(nameof(OnDispose));

            Setting?.UnregisterInOptionsUI();
            Setting = null;
        }
    }
}
