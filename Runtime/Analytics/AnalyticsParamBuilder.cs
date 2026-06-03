using System.Collections.Generic;
using System.Globalization;

namespace MGKit.Analytics
{
    public sealed class AnalyticsParamBuilder
    {
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>();

        public static AnalyticsParamBuilder Create() => new AnalyticsParamBuilder();

        public AnalyticsParamBuilder Put(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || value == null) return this;
            _data[key] = value;
            return this;
        }

        public AnalyticsParamBuilder Put(string key, int value) => Put(key, value.ToString(CultureInfo.InvariantCulture));

        public AnalyticsParamBuilder Put(string key, float value) =>
            Put(key, value.ToString("0.###", CultureInfo.InvariantCulture));

        public AnalyticsParamBuilder Put(string key, bool value) => Put(key, value ? "1" : "0");

        public Dictionary<string, string> Build() => _data;
    }
}
