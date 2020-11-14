using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Carnation.Helpers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;

namespace Carnation
{
    internal static class SavedColorsManager
    {
        private static readonly string CollectionName = typeof(SavedColorsManager).FullName;
        public static readonly int NumberOfSavedColors = 18;


        public static async Task<ImmutableArray<Color>> GetColorsAsync()
        {
            var settingsStore = await OptionsHelper.GetReadonlySettingsStoreAsync();

            if (!settingsStore.CollectionExists(CollectionName))
            {
                return ImmutableArray<Color>.Empty;
            }

            var index = 0;
            var colors = new Color[NumberOfSavedColors];

            foreach (var name in GetColorNames())
            {
                var colorInt = 0;
                try
                {
                    // In case we ever change the values we're storing we might 
                    // fail to retrieve a color. That's okay, just leave it to the default color
                    // value 
                    colorInt = settingsStore.GetInt32(CollectionName, name);
                }
                catch { }

                var color = ColorHelpers.ToColor((uint)colorInt);
                colors[index++] = color;
            }

            return colors.ToImmutableArray();
        }

        public static async System.Threading.Tasks.Task SaveColorAsync(Color color, int index)
        {
            var settingsStore = await OptionsHelper.GetWritableSettingsStoreAsync();

            if (!settingsStore.CollectionExists(CollectionName))
            {
                settingsStore.CreateCollection(CollectionName);
            }

            settingsStore.SetInt32(CollectionName, $"Color{index}", ColorHelpers.ToInt(color));
        }

        private static IEnumerable<string> GetColorNames()
        {
            for (var i = 0; i < NumberOfSavedColors; i++)
            {
                yield return $"Color{i}";
            }
        }
    }
}
