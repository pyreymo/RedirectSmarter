using RedirectSmarter.Targeting;

namespace RedirectSmarter.Configuration
{
    internal sealed class RedirectionEditor(PluginConfiguration configuration)
    {
        private const int MaxRedirects = 12;

        public Redirection GetRedirection(uint actionId)
        {
            return configuration.Redirections.TryGetValue(actionId, out var redirection) ? redirection : new Redirection { ID = actionId };
        }

        public static bool CanAdd(Redirection redirection)
        {
            return redirection.Count < MaxRedirects;
        }

        public bool AddDefaultTarget(Redirection redirection)
        {
            if (!CanAdd(redirection))
            {
                return false;
            }

            redirection.Add(configuration.DefaultRedirection);
            return true;
        }

        public static bool SetPreventDefault(Redirection redirection, bool value)
        {
            if (redirection.PreventDefault == value)
            {
                return false;
            }

            redirection.PreventDefault = value;
            return true;
        }

        public static bool SetTarget(Redirection redirection, int index, string target)
        {
            if (redirection[index] == target)
            {
                return false;
            }

            redirection.SetTarget(index, target);
            return true;
        }

        public static bool SetTargetParameter(Redirection redirection, int index, TargetParameterDefinition definition, string value)
        {
            if (!IsValidIndex(redirection, index))
            {
                return false;
            }

            if (!definition.TryNormalize(value, out var normalizedValue))
            {
                return false;
            }

            var parameters = redirection.GetTargetOptions(index).Parameters;
            if (parameters.TryGetValue(definition.Name, out var currentValue) && currentValue == normalizedValue)
            {
                return false;
            }

            parameters[definition.Name] = normalizedValue;
            return true;
        }

        public static bool RemoveAt(Redirection redirection, int index)
        {
            if (index < 0 || index >= redirection.Count)
            {
                return false;
            }

            redirection.RemoveAt(index);
            return true;
        }

        public void Apply(uint actionId, Redirection redirection)
        {
            if (redirection.Count > 0 || redirection.PreventDefault)
            {
                configuration.Redirections[actionId] = redirection;
                return;
            }

            configuration.Redirections.Remove(actionId);
        }

        private static bool IsValidIndex(Redirection redirection, int index)
        {
            return index >= 0 && index < redirection.Count;
        }
    }
}
