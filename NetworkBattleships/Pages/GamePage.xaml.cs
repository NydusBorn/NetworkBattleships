using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

            int FieldSide = 10;
            int CellSize = 50;
            for (int i = 0; i < FieldSide; i++)
            {
                RowDefinition rd = new RowDefinition();
                rd.Height = new GridLength(CellSize, GridUnitType.Pixel);
                PlayerGrid.RowDefinitions.Add(rd);
                rd = new RowDefinition();
                rd.Height = new GridLength(CellSize, GridUnitType.Pixel);
                OpponentGrid.RowDefinitions.Add(rd);
                ColumnDefinition cd = new ColumnDefinition();
                cd.Width = new GridLength(CellSize, GridUnitType.Pixel);
                PlayerGrid.ColumnDefinitions.Add(cd);
                cd = new ColumnDefinition();
                cd.Width = new GridLength(CellSize, GridUnitType.Pixel);
                OpponentGrid.ColumnDefinitions.Add(cd);
            }

            int ButtonSize = CellSize - 5;
            for (int i = 0; i < FieldSide; i++)
            {
                for (int j = 0; j < FieldSide; j++)
                {
                    Button bt = new Button
                    {
                        Width = ButtonSize,
                        Height = ButtonSize
                    };
                    Grid.SetRow(bt,i);
                    Grid.SetColumn(bt,j);
                    PlayerGrid.Children.Add(bt);
                    bt = new Button
                    {
                        Width = ButtonSize,
                        Height = ButtonSize
                    };
                    Grid.SetRow(bt, i);
                    Grid.SetColumn(bt, j);
                    OpponentGrid.Children.Add(bt);
                }
            }
        }
    }
}
