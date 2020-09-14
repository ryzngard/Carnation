using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Carnation
{
    internal static class ClassificationHelpers
    {
        public static ImmutableArray<(string, string)> GetClassificationNames()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return VSServiceHelpers.GetMefExports<EditorFormatDefinition>()
                .Select(definition => definition.GetType())
                .Where(type => type.GetCustomAttribute<UserVisibleAttribute>()?.UserVisible == true)
                .Select(type => (type.GetCustomAttribute<ClassificationTypeAttribute>()?.ClassificationTypeNames, type.GetCustomAttribute<NameAttribute>()?.Name))
                .Where(names => !string.IsNullOrEmpty(names.ClassificationTypeNames) && !string.IsNullOrEmpty(names.Name))
                .ToImmutableArray();
        }

        public static ImmutableArray<string> GetClassificationsForSpan(IWpfTextView view, Span span)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Limit the search area to 1 character around the cursor.
            var snapshotSpan = new SnapshotSpan(view.TextSnapshot, span.Start, 1);

            var classificationService = VSServiceHelpers.GetMefExport<IClassifierAggregatorService>();
            var classifier = (IAccurateClassifier)classificationService.GetClassifier(view.TextBuffer);

            var classifiedSpans = classifier.GetAllClassificationSpans(snapshotSpan, CancellationToken.None);

            return classifiedSpans.Select(classifiedSpan => classifiedSpan.ClassificationType.Classification)
                .Distinct()
                .ToImmutableArray();
        }
    }
}
