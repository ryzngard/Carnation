using System.Collections.Generic;
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
        private static readonly HashSet<string> s_ignoredClassifications = new HashSet<string> { "(TRANSIENT)", "formal language" };

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

            // The span to classify must have a length.
            var snapshotSpan = span.Length == 0
                ? new SnapshotSpan(view.TextSnapshot, span.Start, 1)
                : new SnapshotSpan(view.TextSnapshot, span);

            var classificationService = VSServiceHelpers.GetMefExport<IClassifierAggregatorService>();
            var classifier = (IAccurateClassifier)classificationService.GetClassifier(view.TextBuffer);

            var classifiedSpans = classifier.GetAllClassificationSpans(snapshotSpan, CancellationToken.None);

            var classifications = ImmutableArray.CreateBuilder<string>();
            foreach (var classifiedSpan in classifiedSpans)
            {
                CollectClassifications(classifiedSpan.ClassificationType, classifications);
            }

            return classifications.ToImmutable();

            static void CollectClassifications(IClassificationType classificationType, ImmutableArray<string>.Builder classifications)
            {
                var baseClassificationTypes = classificationType.BaseTypes.ToArray();
                if (baseClassificationTypes.Length <= 1 &&
                    !s_ignoredClassifications.Contains(classificationType.Classification) &&
                    !classifications.Contains(classificationType.Classification))
                {
                    classifications.Add(classificationType.Classification);
                }

                foreach (var baseClassificationType in baseClassificationTypes)
                {
                    CollectClassifications(baseClassificationType, classifications);
                }
            }
        }
    }
}
