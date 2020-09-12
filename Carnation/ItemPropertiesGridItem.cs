using System.Windows.Media;

namespace Carnation
{
    internal class ItemPropertiesGridItem : ColorItemBase
    {
        public string Text => "Sample Text";

        public ItemPropertiesGridItem(
            Color foreground,
            Color background)
            : base(foreground, background)
        {
        }
    }
}
