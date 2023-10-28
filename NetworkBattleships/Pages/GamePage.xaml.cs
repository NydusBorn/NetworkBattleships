using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NetworkBattleships.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GamePage : Page
    {
        public GamePage()
        {
            this.InitializeComponent();
            Connector._GamePage = this;

            const int fieldSide = 11;
            const int cellSize = 50;
            for (int i = 0; i < fieldSide; i++)
            {
                RowDefinition rd = new RowDefinition
                {
                    Height = new GridLength(cellSize, GridUnitType.Pixel)
                };
                PlayerGrid.RowDefinitions.Add(rd);
                rd = new RowDefinition
                {
                    Height = new GridLength(cellSize, GridUnitType.Pixel)
                };
                OpponentGrid.RowDefinitions.Add(rd);
                ColumnDefinition cd = new ColumnDefinition
                {
                    Width = new GridLength(cellSize, GridUnitType.Pixel)
                };
                PlayerGrid.ColumnDefinitions.Add(cd);
                cd = new ColumnDefinition
                {
                    Width = new GridLength(cellSize, GridUnitType.Pixel)
                };
                OpponentGrid.ColumnDefinitions.Add(cd);
            }

            const int buttonSize = cellSize - 5;
            for (int i = 1; i < fieldSide; i++)
            {
                for (int j = 1; j < fieldSide; j++)
                {
                    Button bt = new Button
                    {
                        Width = buttonSize,
                        Height = buttonSize
                    };
                    Grid.SetRow(bt,i);
                    Grid.SetColumn(bt,j);
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
            for (int i = 1; i < fieldSide; i++)
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
            for (int i = 1; i < fieldSide; i++)
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
        }
    }
}
