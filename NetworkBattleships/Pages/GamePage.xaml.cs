using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NetworkBattleships.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GamePage : Page
    {
        const int FieldSide = 11;
        const int CellSize = 50;
        private Color? CurrentDefaultButtonColor = null;
        private Color? CurrentHoverButtonColor = null;
        private Color? CurrentSunkButtonColor = null;
        private Color? CurrentMissButtonColor = null;

        public GamePage()
        {
            this.InitializeComponent();
            Connector._GamePage = this;

            for (int i = 0; i < FieldSide; i++)
            {
                RowDefinition rd = new RowDefinition
                {
                    Height = new GridLength(CellSize, GridUnitType.Pixel)
                };
                PlayerGrid.RowDefinitions.Add(rd);
                rd = new RowDefinition
                {
                    Height = new GridLength(CellSize, GridUnitType.Pixel)
                };
                OpponentGrid.RowDefinitions.Add(rd);
                ColumnDefinition cd = new ColumnDefinition
                {
                    Width = new GridLength(CellSize, GridUnitType.Pixel)
                };
                PlayerGrid.ColumnDefinitions.Add(cd);
                cd = new ColumnDefinition
                {
                    Width = new GridLength(CellSize, GridUnitType.Pixel)
                };
                OpponentGrid.ColumnDefinitions.Add(cd);
            }

            const int buttonSize = CellSize - 5;
            for (int i = 1; i < FieldSide; i++)
            {
                for (int j = 1; j < FieldSide; j++)
                {
                    Button bt = new Button
                    {
                        Width = buttonSize,
                        Height = buttonSize
                    };
                    Grid.SetRow(bt, i);
                    Grid.SetColumn(bt, j);
                    PlayerGrid.Children.Add(bt);
                    bt = new Button
                    {
                        Width = buttonSize,
                        Height = buttonSize
                    };
                    Grid.SetRow(bt, i);
                    Grid.SetColumn(bt, j);
                    OpponentGrid.Children.Add(bt);
                }
            }

            int fontSize = 40;
            for (int i = 1; i < FieldSide; i++)
            {
                TextBlock tb = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = fontSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tb, i);
                Grid.SetRow(tb, 0);
                PlayerGrid.Children.Add(tb);
                tb = new TextBlock
                {
                    Text = i.ToString(),
                    FontSize = fontSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tb, i);
                Grid.SetRow(tb, 0);
                OpponentGrid.Children.Add(tb);
            }

            for (int i = 1; i < FieldSide; i++)
            {
                TextBlock tb = new TextBlock
                {
                    Text = ((char)(i + 'A' - 1)).ToString(),
                    FontSize = fontSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tb, 0);
                Grid.SetRow(tb, i);
                PlayerGrid.Children.Add(tb);
                tb = new TextBlock
                {
                    Text = ((char)(i + 'A' - 1)).ToString(),
                    FontSize = fontSize,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(tb, 0);
                Grid.SetRow(tb, i);
                OpponentGrid.Children.Add(tb);
            }

            var ships = new System.Collections.ObjectModel.ObservableCollection<Image>
            {
                new Image()
                {
                    Source = new SvgImageSource(new Uri($"{Environment.CurrentDirectory}/Assets/Destroyer.svg")),
                    Stretch = Stretch.Fill, RasterizationScale = 8, Width = CellSize, Height = CellSize * 2
                },
                new Image()
                {
                    Source = new SvgImageSource(new Uri($"{Environment.CurrentDirectory}/Assets/Submarine.svg")),
                    Stretch = Stretch.Fill, RasterizationScale = 8, Width = CellSize, Height = CellSize * 3
                },
                new Image()
                {
                    Source = new SvgImageSource(new Uri($"{Environment.CurrentDirectory}/Assets/Cruiser.svg")),
                    Stretch = Stretch.Fill, RasterizationScale = 8, Width = CellSize, Height = CellSize * 3
                },
                new Image()
                {
                    Source = new SvgImageSource(new Uri($"{Environment.CurrentDirectory}/Assets/Battleship.svg")),
                    Stretch = Stretch.Fill, RasterizationScale = 8, Width = CellSize, Height = CellSize * 4
                },
                new Image()
                {
                    Source = new SvgImageSource(new Uri($"{Environment.CurrentDirectory}/Assets/Carrier.svg")),
                    Stretch = Stretch.Fill, RasterizationScale = 8, Width = CellSize, Height = CellSize * 5
                }
            };
            ShipsPanel.ItemsSource = ships;
            Task.Run(() =>
            {
                DispatcherQueue.TryEnqueue(SetColors);
            });
        }

        private async void SetColors()
        {
            await Task.Delay(500);
            var bt = PlayerGrid.Children[0] as Button;
            // Default dark theme is #0FFFFFFF
            CurrentDefaultButtonColor = (bt.Background as SolidColorBrush).Color;
            CurrentHoverButtonColor = new Color()
            {
                R = (byte)(CurrentDefaultButtonColor.Value.R - 120), G = (byte)(CurrentDefaultButtonColor.Value.G - 120),
                B = (byte)(CurrentDefaultButtonColor.Value.B - 120), A = CurrentDefaultButtonColor.Value.A
            };
            CurrentSunkButtonColor = new Color()
            {
                R = (byte)(CurrentDefaultButtonColor.Value.R), G = (byte)(CurrentDefaultButtonColor.Value.G - 200),
                B = (byte)(CurrentDefaultButtonColor.Value.B - 200), A = CurrentDefaultButtonColor.Value.A
            };
            CurrentMissButtonColor = new Color()
            {
                R = (byte)(Math.Clamp((int)CurrentDefaultButtonColor.Value.R, 100, 140)), G = (byte)(Math.Clamp((int)CurrentDefaultButtonColor.Value.G, 100, 140)),
                B = (byte)(Math.Clamp((int)CurrentDefaultButtonColor.Value.B, 100, 140)), A = 120
            };
        }

        private void ShipDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            // var blob = e.DataView.GetBitmapAsync().GetResults().OpenReadAsync().GetResults();
            // var img = blob.As<BitmapImage>();
            var img = new BitmapImage(new Uri("ms-appx:///Assets/Destroyer.svg"));
            var cursorPosition = e.GetPosition(PlayerGrid);
            int row = (int)cursorPosition.Y / CellSize;
            int col = (int)cursorPosition.X / CellSize;
            for (int i = Math.Clamp(row, 1, FieldSide - 1);
                 i <= Math.Clamp(row + (img.PixelHeight / CellSize), 1, FieldSide - 1);
                 i++)
            {
                for (int j = Math.Clamp(col, 1, FieldSide - 1);
                     j <= Math.Clamp(col + (img.PixelWidth / CellSize), 1, FieldSide - 1);
                     j++)
                {
                    var bt = PlayerGrid.Children[((i - 1) * (FieldSide - 1)) + j - 1] as Button;
                    bt.Background = new SolidColorBrush(CurrentHoverButtonColor.Value);
                }
            }
        }
    }
}