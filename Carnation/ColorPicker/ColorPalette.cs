using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SWM = System.Windows.Media;

namespace Carnation
{
    public class ColorPalleteSelectedArgs : EventArgs
    {
        public Color Color { get; }
        public int Index { get; }

        public ColorPalleteSelectedArgs(Color color, int index)
        {
            Color = color;
            Index = index;
        }
    }

    public class ColorPalette : UserControl
    {

        private static readonly Color[] s_colorSwatch = new[]
        {
            SWM.Colors.Black,
            SWM.Colors.Gray,
            SWM.Colors.White,
            SWM.Colors.Red,
            SWM.Colors.Orange,
            SWM.Colors.Yellow,
            SWM.Colors.YellowGreen,
            SWM.Colors.LightGreen,
            SWM.Colors.Green,
            Color.FromRgb(0, 255, 33), // green/cyan
            SWM.Colors.Cyan,
            SWM.Colors.DarkCyan,
            SWM.Colors.Blue,
            SWM.Colors.Purple,
            Color.FromRgb(178, 0, 255), // purple/magenta
            SWM.Colors.Magenta,
            Color.FromRgb(255, 0, 110), // magenta/red,
            SWM.Colors.MediumVioletRed
        };

        public static readonly uint Variable = 0;

        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
            nameof(Rows),
            typeof(uint),
            typeof(ColorPalette),
            new PropertyMetadata(Variable, RefreshGrid));

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            nameof(Columns),
            typeof(uint),
            typeof(ColorPalette),
            new PropertyMetadata(Variable, RefreshGrid));

        public static readonly DependencyProperty ColorsProperty = DependencyProperty.Register(
            nameof(Colors),
            typeof(ImmutableArray<Color>),
            typeof(ColorPalette),
            new PropertyMetadata(s_colorSwatch.ToImmutableArray(), RefreshGrid));

        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            nameof(Size),
            typeof(GridLength),
            typeof(ColorPalette),
            new PropertyMetadata(new GridLength(32, GridUnitType.Pixel), RefreshGrid));

        public static readonly DependencyProperty ColorPaddingProperty = DependencyProperty.Register(
            nameof(ColorPadding),
            typeof(Thickness),
            typeof(ColorPalette),
            new PropertyMetadata(new Thickness(0), RefreshGrid));

        public event EventHandler<ColorPalleteSelectedArgs> ColorSelected;

        private readonly Grid _grid = new Grid();

        public ColorPalette()
        {
            AddChild(_grid);
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _grid.Children.Clear();
            _grid.RowDefinitions.Clear();
            _grid.ColumnDefinitions.Clear();

            var (rows, columns) = GetActualRowsAndColumns();

            for (var row = 0; row < rows; row++)
            {
                var rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                _grid.RowDefinitions.Add(rowDefinition);

                for (var column = 0; column < columns; column++)
                {
                    var columnDefinition = new ColumnDefinition();
                    columnDefinition.Width = GridLength.Auto;
                    _grid.ColumnDefinitions.Add(columnDefinition);

                    var index = (row * (int)columns) + column;
                    var color = index >= Colors.Length
                        ? SWM.Colors.White
                        : Colors[index];

                    var item = GenerateItem(row, column, color, index);
                    _grid.Children.Add(item);
                }
            }
        }
        private (int rows, int columns) GetActualRowsAndColumns()
        {
            var actualRows = (int)Rows;
            var actualColumns = (int)Columns;

            if (actualRows == Variable && actualColumns == Variable)
            {
                // Try to make a square from the colors
                actualRows = (int)Math.Ceiling(Math.Sqrt(Colors.Length));
                actualColumns = (int)Math.Ceiling(Colors.Length / (double)actualRows);
            }
            else if (actualRows == Variable)
            {
                actualRows = (int)Math.Ceiling(Colors.Length / (double)actualColumns);
            }
            else if (actualColumns == Variable)
            {
                actualColumns = (int)Math.Ceiling(Colors.Length / (double)actualRows);
            }

            return (actualRows, actualColumns);
        }

        private UIElement GenerateItem(int row, int column, Color color, int index)
        {
            var rectangle = new Rectangle();
            rectangle.Fill = new SolidColorBrush(color);

            if (Size.GridUnitType == GridUnitType.Pixel)
            {
                rectangle.Height = Size.Value;
                rectangle.Width = Size.Value;
            }
            else
            {
                rectangle.Height = 16;
                rectangle.Width = 16;
            }

            var border = new Border();
            border.Margin = ColorPadding;
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = new SolidColorBrush(SWM.Colors.Black);
            border.Width = rectangle.Width;
            border.Height = rectangle.Height;

            border.Child = rectangle;

            var focusBorder = new FocusableBorder();
            focusBorder.Child = border;
            focusBorder.Focusable = true;
            focusBorder.Padding = new Thickness(1);

            var dashBorderBrush = new VisualBrush();
            var dashVisual = new Rectangle();
            dashVisual.StrokeDashArray = new DoubleCollection(new double[] { 4, 2 });
            dashVisual.Stroke = new SolidColorBrush(SWM.Colors.Gray);
            dashVisual.StrokeThickness = 1;

            dashBorderBrush.Visual = dashVisual;

            focusBorder.GotFocus += (s, a) => focusBorder.BorderBrush = dashBorderBrush;
            focusBorder.LostFocus += (s, a) => focusBorder.BorderBrush = new SolidColorBrush(SWM.Colors.Transparent);

            focusBorder.BorderBrush = new SolidColorBrush(SWM.Colors.Transparent);
            
            AutomationProperties.SetHelpText(focusBorder, GetColorHelpText(color));

            focusBorder.MouseDown += (s, a) => ColorSelected?.Invoke(this, new ColorPalleteSelectedArgs(color, index));
            focusBorder.KeyDown += (s, a) =>
            {
                if (a.Key == System.Windows.Input.Key.Enter)
                {
                    ColorSelected?.Invoke(this, new ColorPalleteSelectedArgs(color, index));
                }
            };

            Grid.SetColumn(focusBorder, column);
            Grid.SetRow(focusBorder, row);

            return focusBorder;
        }

        private static string GetColorHelpText(Color color)
        {
            if (ColorHelpers.TryGetWindowsMediaName(color, out var name))
            {
                return name;
            }

            if (ColorHelpers.TryGetSystemDrawingName(color, out name))
            {
                return name;
            }

            return $"Color Red={color.R}, Green={color.G}, Blue={color.B}";
        }

        private static void RefreshGrid(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var palette = (ColorPalette)d;
            palette.RefreshGrid();
        }

        public uint Rows
        {
            get => (uint)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public uint Columns
        {
            get => (uint)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public GridLength Size
        {
            get => (GridLength)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public ImmutableArray<Color> Colors
        {
            get => (ImmutableArray<Color>)GetValue(ColorsProperty);
            set
            {
                var (rows, columns) = GetActualRowsAndColumns();
                var maxLength = rows * columns;
                
                if (value.Length <= maxLength)
                {
                    var newArray = new Color[maxLength];
                    value.CopyTo(newArray);

                    SetValue(ColorsProperty, newArray.ToImmutableArray());
                }
                else
                {
                    SetValue(ColorsProperty, value.Take(maxLength).ToImmutableArray());
                }
            }
        }

        public Thickness ColorPadding
        {
            get => (Thickness)GetValue(ColorPaddingProperty);
            set => SetValue(ColorPaddingProperty, value);
        }

        private class FocusableBorder : Border
        {
            protected override AutomationPeer OnCreateAutomationPeer()
            {
                return new MyAutomationPeer(this);
            }

            private class MyAutomationPeer : FrameworkElementAutomationPeer
            {
                public MyAutomationPeer(FrameworkElement owner) : base(owner)
                {
                }

                protected override AutomationControlType GetAutomationControlTypeCore()
                    => AutomationControlType.Button;
            }
        }
    }
}
