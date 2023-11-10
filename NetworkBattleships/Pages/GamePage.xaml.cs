using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Popups;
using BattleshipsModel;
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
        private List<GameModel.Types> _ships = new List<GameModel.Types>();

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
                    bt.Click += EnemyTileClick;
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
            _ships.Add(GameModel.Types.Destroyer);
            _ships.Add(GameModel.Types.Submarine);
            _ships.Add(GameModel.Types.Cruiser);
            _ships.Add(GameModel.Types.Battleship);
            _ships.Add(GameModel.Types.Carrier);
            ShipsPanel.ItemsSource = ships;
            DispatcherQueue.TryEnqueue(SetColors);
            Connector._GameModel.OnReceive += ReceivedAttack;
            Connector._GameModel.OnAttack += MadeAttack;
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

            imageIndex = selected;
            var img = images[selected];
            _dragCancellationTokenSource = new CancellationTokenSource();
            int width = (int)img.Width;
            int height = (int)img.Height;
            Task.Run(() => ShipDragProcess(height, width, _dragCancellationTokenSource.Token));
        }

        private Windows.Foundation.Point _currentCursorPosition;
        private void PointerMove(object sender, PointerRoutedEventArgs e)
        {
            _currentCursorPosition = e.GetCurrentPoint(PlayerGrid).Position;
            TextBlockStatus.Text = $"{_currentCursorPosition.X}\n{_currentCursorPosition.Y}";
        }

        private GameModel.Orientation _rotation = GameModel.Orientation.Up;
        
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
                    case GameModel.Orientation.Up:
                        row = Math.Clamp(row - (imgHeight / CellSize) + 1, 1, FieldSide - (imgHeight / CellSize));
                        break;
                    case GameModel.Orientation.Down:
                        row = Math.Clamp(row, 1, FieldSide - (imgHeight / CellSize));
                        break;
                    case GameModel.Orientation.Left:
                        col = Math.Clamp(col - (imgHeight / CellSize) + 1, 1, FieldSide - (imgHeight / CellSize));
                        break;
                    case GameModel.Orientation.Right:
                        col = Math.Clamp(col, 1, FieldSide - (imgHeight / CellSize));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (_rotation is GameModel.Orientation.Down or GameModel.Orientation.Up)
                {
                    for (int i = row; i < row + (imgHeight / CellSize); i++)
                    {
                        newPositions.Add(new Point() { X = i, Y = col });
                    }
                }
                else
                {
                    for (int i = col; i < col + (imgHeight / CellSize); i++)
                    {
                        newPositions.Add(new Point() { X = row, Y = i });
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

        private int imageIndex;
        
        private void ShipDrop(object o, DragEventArgs dragEventArgs)
        {
            _dragCancellationTokenSource.Cancel();
            var currentCursorPosition = _currentCursorPosition;
            var image = (ShipsPanel.ItemsSource as System.Collections.ObjectModel.ObservableCollection<Image>)[imageIndex];
            int row = Math.Clamp((int)currentCursorPosition.Y / CellSize, 1, FieldSide - 1);
            int col = Math.Clamp((int)currentCursorPosition.X / CellSize, 1, FieldSide - 1);
            switch (_rotation)
            {
                case GameModel.Orientation.Up:
                    row = Math.Clamp(row - ((int)image.Height / CellSize) + 1, 1, FieldSide - ((int)image.Height / CellSize));
                    break;
                case GameModel.Orientation.Down:
                    row = Math.Clamp(row, 1, FieldSide - ((int)image.Height / CellSize));
                    break;
                case GameModel.Orientation.Left:
                    col = Math.Clamp(col - ((int)image.Height / CellSize) + 1, 1, FieldSide - ((int)image.Height / CellSize));
                    break;
                case GameModel.Orientation.Right:
                    col = Math.Clamp(col, 1, FieldSide - ((int)image.Height / CellSize));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            (ShipsPanel.ItemsSource as System.Collections.ObjectModel.ObservableCollection<Image>).RemoveAt(imageIndex);
            
            PlayerGrid.Children.Add(image);
            image.PointerPressed += PlayerShipClick;
            Grid.SetRow(image, 0);
            Grid.SetColumn(image, 0);
            Grid.SetRowSpan(image, (int)image.Height / CellSize);
            switch (_rotation)
            {
                case GameModel.Orientation.Up:
                    image.RenderTransform = new CompositeTransform() {CenterX = image.Width / 2, CenterY = image.Height / 2, TranslateX = CellSize * col, TranslateY = CellSize * row};
                    break;
                case GameModel.Orientation.Down:
                    image.RenderTransform = new CompositeTransform(){ Rotation = 180, CenterX = image.Width / 2, CenterY = image.Height / 2, TranslateX = CellSize * col, TranslateY = CellSize * row};
                    break;
                case GameModel.Orientation.Left:
                    image.RenderTransform = new CompositeTransform(){Rotation = 270, TranslateX = CellSize * col, TranslateY = CellSize * (1 + row)};
                    break;
                case GameModel.Orientation.Right:
                    image.RenderTransform = new CompositeTransform(){Rotation = 90, TranslateX = CellSize * (col + ((int)image.Height / CellSize)), TranslateY = CellSize * (row)};
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            int shipSize = (int)image.Height / CellSize;
            
            Connector._GameModel.AddShip(new Point() { X = col, Y = row }, _rotation, _ships[imageIndex]);
            _ships.RemoveAt(imageIndex);
        }

        private void ShipDropComplete(UIElement sender, DropCompletedEventArgs args)
        {
            _dragCancellationTokenSource.Cancel();
        }

        private void ChangeRotation(object sender, RoutedEventArgs e)
        {
            ChangeRotationState();
        }

        private void ChangeRotationState()
        {
            switch (_rotation)
            {
                case GameModel.Orientation.Up:
                    _rotation = GameModel.Orientation.Right;
                    ChangeRotationButton.Content = "Right";
                    break;
                case GameModel.Orientation.Right:
                    _rotation = GameModel.Orientation.Down;
                    ChangeRotationButton.Content = "Down";
                    break;
                case GameModel.Orientation.Down:
                    _rotation = GameModel.Orientation.Left;
                    ChangeRotationButton.Content = "Left";
                    break;
                case GameModel.Orientation.Left:
                    _rotation = GameModel.Orientation.Up;
                    ChangeRotationButton.Content = "Up";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void KeyPress(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.R)
            {
                ChangeRotationState();
            }
        }
        
        private void PlayerShipClick(object sender, PointerRoutedEventArgs eventArgs)
        {
            if (Connector._GameModel.State == GameModel.GameState.Preparation)
            {
                Image image = sender as Image;
                var point = eventArgs.GetCurrentPoint(PlayerGrid).Position;
                var shipType = Connector._GameModel.RemoveShip(((int)point.X / 50) - 1, ((int)point.Y / 50) - 1);
                _ships.Add(shipType);
                PlayerGrid.Children.Remove(image);
                image.RenderTransform = null;
                image.PointerPressed -= PlayerShipClick;
                (ShipsPanel.ItemsSource as System.Collections.ObjectModel.ObservableCollection<Image>).Add(image);
            }
        }

        private void EnemyTileClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (Connector._GameModel.State != GameModel.GameState.Playing) return;
            Button tile = sender as Button;
            Connector._GameModel.AttemptAttack(Grid.GetColumn(tile), Grid.GetRow(tile));
        }

        private void ReceivedAttack(int xCoord, int yCoord)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (Connector._GameModel.PlayerGrid[xCoord][yCoord])
                {
                    case GameModel.CellStatus.Empty:
                        break;
                    case GameModel.CellStatus.EmptyMiss:
                        (PlayerGrid.Children[((yCoord - 1) * (FieldSide - 1)) + xCoord - 1] as Button).Background = new SolidColorBrush(CurrentMissButtonColor.Value);
                        break;
                    case GameModel.CellStatus.Alive:
                        break;
                    case GameModel.CellStatus.Sunk:
                        (PlayerGrid.Children[((yCoord - 1) * (FieldSide - 1)) + xCoord - 1] as Button).Background = new SolidColorBrush(CurrentSunkButtonColor.Value);
                        break;
                    case GameModel.CellStatus.Unknown:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
        }

        private void MadeAttack(int xCoord, int yCoord)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (Connector._GameModel.OpponentGrid[xCoord][yCoord])
                {
                    case GameModel.CellStatus.Empty:
                        break;
                    case GameModel.CellStatus.EmptyMiss:
                        (OpponentGrid.Children[((yCoord - 1) * (FieldSide - 1)) + xCoord - 1] as Button).Background = new SolidColorBrush(CurrentMissButtonColor.Value);
                        break;
                    case GameModel.CellStatus.Alive:
                        break;
                    case GameModel.CellStatus.Sunk:
                        (OpponentGrid.Children[((yCoord - 1) * (FieldSide - 1)) + xCoord - 1] as Button).Background = new SolidColorBrush(CurrentSunkButtonColor.Value);
                        break;
                    case GameModel.CellStatus.Unknown:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
            
        }

        private void StartGame(object sender, RoutedEventArgs e)
        {
            if (Connector._GameModel.State == GameModel.GameState.Preparation)
            {
                Connector._GameModel.StartGame();
            }
        }
    }
}