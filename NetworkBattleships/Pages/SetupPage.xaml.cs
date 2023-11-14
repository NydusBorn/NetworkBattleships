using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Windows.UI.Popups;
using BattleshipsModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NetworkBattleships.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SetupPage : Page
    {
        /// <summary>
        /// Looks for local ip addresses
        /// Makes a request to https://api.ipify.org to get public ip 
        /// </summary>
        private void GetIPs()
        {
            var foundIps = new List<IPAddress>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    foundIps.Add(ip);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Current IP addresses:");
            sb.AppendLine("local:");
            foreach (var ip in foundIps)
            {
                sb.AppendLine($"\t{ip.ToString()}");
            }

            string url = "https://api.ipify.org";
        
            HttpClient httpClient = new HttpClient();
            string mainIp = httpClient.GetStringAsync(url).Result;
        
            sb.AppendLine("public:");
            sb.AppendLine($"\t{mainIp}");
            IpTextBlock.Text = sb.ToString();
        }
    
        public Socket CurrentSocket;
        public Socket ClientSocket;

        /// <summary>
        /// Attempts to open as a server, if successful goes to gamepage 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eArgs"></param>
        private void OpenServer(object sender, RoutedEventArgs eArgs)
        {
            try
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                CurrentSocket = server;
                CurrentSocket.Bind(new IPEndPoint(IPAddress.Any, int.Parse(ServerPort.Text)));
                CurrentSocket.Listen(10);
                ClientSocket = CurrentSocket.Accept();
                Connector._GameModel = new GameModel(ClientSocket);
            }
            catch (Exception e)
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.Title = "Error on connection";
                dialog.Content = $"Error: {e.Message}";
                dialog.CloseButtonText = "Close";
                dialog.DefaultButton = ContentDialogButton.Close;

                var result = dialog.ShowAsync().GetResults();
                
                return;
            }
            NextPage();
        }

        /// <summary>
        /// Attempts to open as a client, if successful goes to gamepage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OpenClient(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                CurrentSocket = client;
                CurrentSocket.Connect(IPAddress.Parse(ClientIp.Text), int.Parse(ClientPort.Text));
                Connector._GameModel = new GameModel(CurrentSocket);
            }
            catch (Exception e)
            {
                ContentDialog dialog = new ContentDialog();

                // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
                dialog.XamlRoot = this.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.Title = "Error on connection";
                dialog.Content = $"Error: {e.Message}";
                dialog.CloseButtonText = "Close";
                dialog.DefaultButton = ContentDialogButton.Close;

                var result = dialog.ShowAsync().GetResults();
                
                return;
            }
            
            NextPage();
        }

        public SetupPage()
        {
            this.InitializeComponent();
            Connector._SetupPage = this;
            GetIPs();
        }

        private void NextPage()
        {
            Connector._MainWindow.ContentFrame.Navigate(typeof(GamePage));
        }
    }
}
