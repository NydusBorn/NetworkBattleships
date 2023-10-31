using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT;
using Color = Windows.UI.Color;

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
            Task.Run(() => { DispatcherQueue.TryEnqueue(SetColors); });
        }

        private async void SetColors()
        {
            var bt = PlayerGrid.Children[0] as Button;
            while (bt.Background is null)
            {
                await Task.Delay(50);
            }

            // Default dark theme is #0FFFFFFF
            CurrentDefaultButtonColor = (bt.Background as SolidColorBrush).Color;
            CurrentHoverButtonColor = new Color()
            {
                R = (byte)(CurrentDefaultButtonColor.Value.R - 120),
                G = (byte)(CurrentDefaultButtonColor.Value.G - 120),
                B = (byte)(CurrentDefaultButtonColor.Value.B - 120), A = CurrentDefaultButtonColor.Value.A
            };
            CurrentSunkButtonColor = new Color()
            {
                R = (byte)(CurrentDefaultButtonColor.Value.R), G = (byte)(CurrentDefaultButtonColor.Value.G - 200),
                B = (byte)(CurrentDefaultButtonColor.Value.B - 200), A = CurrentDefaultButtonColor.Value.A
            };
            CurrentMissButtonColor = new Color()
            {
                R = (byte)(Math.Clamp((int)CurrentDefaultButtonColor.Value.R, 100, 140)),
                G = (byte)(Math.Clamp((int)CurrentDefaultButtonColor.Value.G, 100, 140)),
                B = (byte)(Math.Clamp((int)CurrentDefaultButtonColor.Value.B, 100, 140)), A = 120
            };
        }

        private void ShipDragOver(object sender, DragEventArgs e)
        {
            var cursorPosition = e.GetPosition(PlayerGrid);
            if (cursorPosition is { X: >= CellSize, Y: >= CellSize }) e.AcceptedOperation = DataPackageOperation.Move;
            _currentCursorPosition = cursorPosition;
        }

        private CancellationTokenSource _dragCancellationTokenSource = new CancellationTokenSource();

        private void ShipDragStart(object sender, DragStartingEventArgs e)
        {
            var images = ShipsPanel.ItemsSource as System.Collections.ObjectModel.ObservableCollection<Image>;
            var cursor = e.GetPosition(ShipsPanel);
            int selected = 0;
            int currentHeight = 0;
            foreach (var image in images)
            {
                currentHeight += (int)image.Height;
                if (cursor.Y > currentHeight)
                {
                    selected++;
                }
                else
                {
                    break;
                }
            }
            var img = images[selected];
            _dragCancellationTokenSource = new CancellationTokenSource();
            int width = (int)img.Width;
            int height = (int)img.Height;
            _rotation = Rotation.Down;
            Task.Run(() => ShipDragProcess(height, width, _dragCancellationTokenSource.Token));
        }

        private Windows.Foundation.Point _currentCursorPosition;
        private void PointerMove(object sender, PointerRoutedEventArgs e)
        {
            _currentCursorPosition = e.GetCurrentPoint(PlayerGrid).Position;
            TextBlockStatus.Text = $"{_currentCursorPosition.X}\n{_currentCursorPosition.Y}";
        }

        enum Rotation
        {
            Up, Down, Left, Right
        }

        private Rotation _rotation;
        
        private async void ShipDragProcess(int imgHeight, int imgWidth, CancellationToken ct)
        {
            HashSet<Point> lastPositions = new HashSet<Point>();
            while (true)
            {
                var currentCursorPosition = _currentCursorPosition;
                HashSet<Point> newPositions = new HashSet<Point>();
                int row = Math.Clamp((int)currentCursorPosition.Y / CellSize, 1, FieldSide - 1);
                int col = Math.Clamp((int)currentCursorPosition.X / CellSize, 1, FieldSide - 1);
                switch (_rotation)
                {
                    case Rotation.Up:
                        row = Math.Clamp(row, 1 + (imgHeight / CellSize), FieldSide - 1);
                        break;
                    case Rotation.Down:
                        row = Math.Clamp(row, 1, FieldSide - 1 - (imgHeight / CellSize));
                        break;
                    case Rotation.Left:
                        col = Math.Clamp(col, 1 + (imgWidth / CellSize), FieldSide - 1);
                        break;
                    case Rotation.Right:
                        col = Math.Clamp(col, 1, FieldSide - 1 - (imgWidth / CellSize));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (_rotation is Rotation.Down or Rotation.Up)
                {
                    for (int i = row; i < row + (imgHeight / CellSize); i++)
                    {
                        
                    }
                }

                foreach (var position in lastPositions.Where(x => !newPositions.Contains(x)))
                {
                    DispatcherQueue.TryEnqueue(() =>
                        (PlayerGrid.Children[((position.X - 1) * (FieldSide - 1)) + position.Y - 1] as Button)
                        .Background =
                        new SolidColorBrush(CurrentDefaultButtonColor.Value));
                }

                foreach (var position in newPositions.Where(x=>!lastPositions.Contains(x)))
                {
                    DispatcherQueue.TryEnqueue(() =>
                        (PlayerGrid.Children[((position.X - 1) * (FieldSide - 1)) + position.Y - 1] as Button)
                        .Background =
                        new SolidColorBrush(CurrentHoverButtonColor.Value));
                }

                lastPositions = newPositions;
                await Task.Delay(10);
                if (ct.IsCancellationRequested)
                {
                    break;
                }
            }
            foreach (var position in lastPositions)
            {
                DispatcherQueue.TryEnqueue(() =>
                    (PlayerGrid.Children[((position.X - 1) * (FieldSide - 1)) + position.Y - 1] as Button)
                    .Background =
                    new SolidColorBrush(CurrentDefaultButtonColor.Value));
            }
        }

        private void ShipDrop(object o, DragEventArgs dragEventArgs)
        {
            _dragCancellationTokenSource.Cancel();
            
        }

        private void ShipDropComplete(UIElement sender, DropCompletedEventArgs args)
        {
            _dragCancellationTokenSource.Cancel();
        }
    }
}