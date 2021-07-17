using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using static Carnation.ClassificationProvider;

namespace Carnation
{
    internal static class ThemeExporter
    {
        public static void Export(string fileName, IEnumerable<ClassificationGridItem> items)
        {
            var (fontFamily, fontSize) = FontsAndColorsHelper.GetEditorFontInfo(scaleFontSize: false);
            var (defaultForeground, defaultBackground) = FontsAndColorsHelper.GetPlainTextColors();

            var categories = items.GroupBy(item => item.Category);

            var vssettings = new StringBuilder();

            vssettings.AppendLine(
$@"<UserSettings>
  <ApplicationIdentity version=""16.0""/>
  <ToolsOptions>
    <ToolsOptionsCategory name=""Environment"" RegisteredName=""Environment""/>
  </ToolsOptions>
  <Category name=""Environment_Group"" RegisteredName=""Environment_Group"">
    <Category name=""Environment_FontsAndColors"" Category=""{{1EDA5DD4-927A-43a7-810E-7FD247D0DA1D}}"" Package=""{{DA9FB551-C724-11d0-AE1F-00A0C90FFFC3}}"" RegisteredName=""Environment_FontsAndColors"" PackageName=""Visual Studio Environment Package"">
      <PropertyValue name=""Version"">2</PropertyValue>
      <FontsAndColors Version=""2.0"">
        <Categories>");

            foreach (var categoryItems in categories)
            {
                vssettings.AppendLine(
$@"          <Category GUID=""{categoryItems.Key:B}"" FontName=""{fontFamily.Source}"" FontSize=""{fontSize}"" CharSet=""1"" FontIsDefault=""No"">
            <Items>");

                foreach (var item in categoryItems)
                {
                    vssettings.Append($@"              <Item Name=""{item.DefinitionName}""");

                    if (item.IsForegroundEditable)
                    {
                        var foreground = ToBGRString(item.Foreground);
                        vssettings.Append($@" Foreground=""{foreground}""");
                    }

                    if (item.IsBackgroundEditable)
                    {
                        var background = item.Background == defaultBackground && item.DefinitionName != "Plain Text"
                            ? "0x01000001"
                            : ToBGRString(item.Background);

                        vssettings.Append($@" Background=""{background}""");
                    }

                    if (item.IsBoldEditable)
                    {
                        vssettings.Append($@" BoldFont=""No""");
                    }

                    vssettings.AppendLine("/>");
                }

                vssettings.AppendLine(
$@"            </Items>
          </Category>");
            }

            vssettings.AppendLine(
$@"        </Categories>
      </FontsAndColors>
    </Category>
  </Category>
</UserSettings>");

            File.WriteAllText(fileName, vssettings.ToString());

            return;

            static string ToBGRString(Color color)
            {
                return $@"0x00{color.B:X2}{color.G:X2}{color.R:X2}";
            }
        }
    }
}
