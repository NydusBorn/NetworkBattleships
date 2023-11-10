using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
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

        private void OpenServer(object sender, RoutedEventArgs e)
        {
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            CurrentSocket = server;
            CurrentSocket.Bind(new IPEndPoint(IPAddress.Any, int.Parse(ServerPort.Text)));
            CurrentSocket.Listen(10);
            ClientSocket = CurrentSocket.Accept();
            Connector._GameModel = new GameModel(GameModel.Roles.Server, ClientSocket);
            NextPage();
        }

        private void OpenClient(object sender, RoutedEventArgs routedEventArgs)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            CurrentSocket = client;
            CurrentSocket.Connect(IPAddress.Parse(ClientIp.Text), int.Parse(ClientPort.Text));
            Connector._GameModel = new GameModel(GameModel.Roles.Client, CurrentSocket);
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
