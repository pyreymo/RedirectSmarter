using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RedirectSmarter.Configuration
{
    [Serializable]
    public class Redirection
    {
        public uint ID { get; init; }
        public List<string> Priority { get; set; } = [];
        public List<RedirectionTargetOptions> TargetOptions { get; set; } = [];
        public bool PreventDefault { get; set; } = false;

        [JsonIgnore]
        public int Count => Priority?.Count ?? 0;

        [JsonIgnore]
        public string this[int i]
        {
            get { return Priority[i]; }
            set { SetTarget(i, value); }
        }

        public void RemoveAt(int i)
        {
            NormalizeTargetOptions();
            Priority.RemoveAt(i);

            if (i < TargetOptions.Count)
            {
                TargetOptions.RemoveAt(i);
            }

            NormalizeTargetOptions();
        }

        public void Move(int fromIndex, int toIndex)
        {
            NormalizeTargetOptions();

            if (fromIndex < 0 || fromIndex >= Priority.Count || toIndex < 0 || toIndex >= Priority.Count || fromIndex == toIndex)
            {
                return;
            }

            var target = Priority[fromIndex];
            var options = TargetOptions[fromIndex];

            Priority.RemoveAt(fromIndex);
            TargetOptions.RemoveAt(fromIndex);

            Priority.Insert(toIndex, target);
            TargetOptions.Insert(toIndex, options);
        }

        public void Add(string value)
        {
            NormalizeTargetOptions();
            Priority.Add(value);
            TargetOptions.Add(new RedirectionTargetOptions());
        }

        public void SetTarget(int i, string value)
        {
            NormalizeTargetOptions();
            Priority[i] = value;
            TargetOptions[i].Reset();
        }

        public RedirectionTargetOptions GetTargetOptions(int i)
        {
            NormalizeTargetOptions();
            return TargetOptions[i];
        }

        public Redirection Clone(uint id)
        {
            NormalizeTargetOptions();

            return new Redirection
            {
                ID = id,
                Priority = [.. Priority],
                TargetOptions = [.. TargetOptions.Select(options => options.Clone())],
                PreventDefault = PreventDefault,
            };
        }

        public void NormalizeTargetOptions()
        {
            Priority ??= [];
            TargetOptions ??= [];

            while (TargetOptions.Count < Priority.Count)
            {
                TargetOptions.Add(new RedirectionTargetOptions());
            }

            while (TargetOptions.Count > Priority.Count)
            {
                TargetOptions.RemoveAt(TargetOptions.Count - 1);
            }

            for (var i = 0; i < TargetOptions.Count; i++)
            {
                TargetOptions[i] ??= new RedirectionTargetOptions();
                TargetOptions[i].Normalize();
            }
        }
    }
}
