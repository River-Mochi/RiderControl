// Settings/Setting.cs
// Options UI settings for RiderControl.
// All user-facing strings are in lang/LocaleEN.cs.

namespace RiderControl
{
    using Colossal.IO.AssetDatabase;
    using Game.Modding;
    using Game.Settings;

    [FileLocation(nameof(RiderControl))]
    [SettingsUITabOrder(kMainTab)]
    [SettingsUIGroupOrder(kBehaviorGroup)]
    [SettingsUIShowGroupName(kBehaviorGroup)]
    public sealed class Setting : ModSetting
    {
        public const string kMainTab = "Main";
        public const string kBehaviorGroup = "Behavior";

        public Setting(IMod mod) : base(mod)
        {
        }

        [SettingsUISection(kMainTab, kBehaviorGroup)]
        public bool BlockTaxiUsage
        {
            get; set;
        }

        public override void SetDefaults()
        {
            BlockTaxiUsage = false;
        }
    }
}
