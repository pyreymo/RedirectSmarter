using System;
using System.Collections.Generic;

namespace RedirectSmarter.Configuration
{
    [Serializable]
    public class RedirectionTargetOptions
    {
        public Dictionary<string, string> Parameters { get; set; } = [];

        internal void Normalize()
        {
            Parameters ??= [];
        }

        internal void Reset()
        {
            Parameters.Clear();
        }
    }
}
