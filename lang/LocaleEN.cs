// lang/LocaleEN.cs
// English (en-US) strings for Options UI.

namespace RiderControl
{
    using System.Collections.Generic;
    using Colossal;

    public sealed class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Settings page title
                { m_Setting.GetSettingsLocaleID(), "Rider Control" },
 
                // Tab + group
                { m_Setting.GetOptionTabLocaleID(Setting.kMainTab), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBehaviorGroup),Behavior" },
 
                // Options
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BlockTaxiUsage)), "Block taxi usage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BlockTaxiUsage)),
                    "Prevents cims from choosing taxis.\n" +
                    "Also clears any cims currently waiting for a taxi so they re-route using other modes." },
            };
        }

        public void Unload()
        {
        }
    }
}
