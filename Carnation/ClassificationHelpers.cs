using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Text.Classification;

namespace Carnation
{
    internal static class ClassificationHelpers
    {
        public static ImmutableArray<string> GetClassificationNames()
        {
            return VSServiceHelpers.GetMefExports<EditorFormatDefinition>()
                .Select(definition => definition.GetType())
                .Where(type => type.GetCustomAttribute<UserVisibleAttribute>()?.UserVisible == true)
                .Select(type => type.GetCustomAttribute<ClassificationTypeAttribute>()?.ClassificationTypeNames)
                .OfType<string>()
                .Distinct()
                .ToImmutableArray();
        }
    }
}
