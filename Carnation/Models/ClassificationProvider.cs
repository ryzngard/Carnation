using System.Collections.Immutable;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;

namespace Carnation
{
    internal partial class ClassificationProvider
    {
        public static Color PlainTextForeground { get; private set; }
        public static Color PlainTextBackground { get; private set; }
        public static ImmutableDictionary<string, string> ClassificationNameMap { get; private set; }
        public static bool IsUpdating { get; private set; }

        public ImmutableArray<ClassificationGridItem> GridItems { get; }

        public ClassificationProvider()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ClassificationNameMap = ClassificationHelpers.GetClassificationNameMap();
            (PlainTextForeground, PlainTextBackground) = FontsAndColorsHelper.GetPlainTextColors();
            var infos = FontsAndColorsHelper.GetTextEditorInfos();

            GridItems = infos.Keys
                .SelectMany(category => infos[category].Select(info => FontsAndColorsHelper.TryGetClassificationItemForInfo(category, info)))
                .OfType<ClassificationGridItem>()
                .ToImmutableArray();
        }

        public void Refresh(ILookup<string, string> definitionNames)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                IsUpdating = true;

                (PlainTextForeground, PlainTextBackground) = FontsAndColorsHelper.GetPlainTextColors();
                var infos = FontsAndColorsHelper.GetTextEditorInfos()
                    .ToImmutableDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                            .Where(info => definitionNames.Contains(info.bstrName))
                            .ToImmutableDictionary(info => info.bstrName));

                foreach (var item in GridItems.Where(item => definitionNames.Contains(item.DefinitionName)))
                {
                    FontsAndColorsHelper.RefreshClassificationItem(item, infos[item.Category][item.DefinitionName]);
                }
            }
            finally
            {
                IsUpdating = false;
            }
        }
    }
}
