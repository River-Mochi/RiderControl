// Mod.cs
// Entry point for "Rider Control".

namespace RiderControl
{
    using System.Reflection;
    using Colossal.IO.AssetDatabase;
    using Colossal.Localization;
    using Colossal.Logging;
    using Game;
    using Game.Modding;
    using Game.SceneFlow;
    using Game.Simulation;

    public sealed class Mod : IMod
    {
        public const string ModName = "Rider Control";
        public const string ModId = "RiderControl";
        public const string ModTag = "[RC]";
        public const string ShortName = "Rider Control";

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

            Setting setting = new Setting(this);
            Setting = setting;

            // Locales: EN only for now.
            // Add more locales later as you create files.
            LocalizationManager? lm = GameManager.instance?.localizationManager;
            if (lm != null)
            {
                lm.AddSource("en-US", new LocaleEN(setting));

                // lm.AddSource("de-DE", new LocaleDE(setting));
                // lm.AddSource("es-ES", new LocaleES(setting));
                // lm.AddSource("fr-FR", new LocaleFR(setting));
                // lm.AddSource("it-IT", new LocaleIT(setting));
                // lm.AddSource("ja-JP", new LocaleJA(setting));
                // lm.AddSource("ko-KR", new LocaleKO(setting));
                // lm.AddSource("pl-PL", new LocalePL(setting));
                // lm.AddSource("pt-BR", new LocalePT_BR(setting));
                // lm.AddSource("pt-PT", new LocalePT_PT(setting));
                // lm.AddSource("zh-HANS", new LocaleZH_CN(setting));
                // lm.AddSource("zh-HANT", new LocaleZH_HANT(setting));
            }
            else
            {
                s_Log.Warn($"{ModTag} LocalizationManager is null; skipping locale registration.");
            }

            // Load saved settings (if any). Defaults are defined in Setting.SetDefaults().
            Setting defaults = new Setting(this);
            AssetDatabase.global.LoadSettings(ModId, setting, defaults, userSetting: true);

            // Register in Options UI last.
            setting.RegisterInOptionsUI();

            // Run before ResidentAISystem so we can unwind taxi-wait state cleanly.
            updateSystem.UpdateBefore<RiderControlSystem, ResidentAISystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            s_Log.Info(nameof(OnDispose));
            if (Setting != null)
            {
                Setting.UnregisterInOptionsUI();
                Setting = null;
            }

   
        }
    }
}
