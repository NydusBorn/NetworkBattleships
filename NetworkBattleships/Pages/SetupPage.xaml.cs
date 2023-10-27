using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
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
        
        private void getIPs()
        {
            var found_ips = new List<IPAddress>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    found_ips.Add(ip);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Current IP addresses:");
            sb.AppendLine("local:");
            foreach (var ip in found_ips)
            {
                sb.AppendLine($"\t{ip.ToString()}");
            }

            string url = "https://api.ipify.org";
        
            HttpClient httpClient = new HttpClient();
            string mainIP = httpClient.GetStringAsync(url).Result;
        
            sb.AppendLine("public:");
            sb.AppendLine($"\t{mainIP}");
            IpTextBlock.Text = sb.ToString();
        }
    
        private async void transfer(Socket Connection, string message)
        {
            Connection.SendAsync(Encoding.Default.GetBytes(message), SocketFlags.None);
            byte[] buffer = new byte[1024];
            int received_bytes = Connection.Receive(buffer);
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
            transfer(ClientSocket, "Welcome from Server!");
        }

        public void OpenClient(object sender, RoutedEventArgs routedEventArgs)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            CurrentSocket = client;
            CurrentSocket.Connect(IPAddress.Parse(ClientIp.Text), int.Parse(ClientPort.Text));
            transfer(CurrentSocket, "Hello from Client!");
        }

        public SetupPage()
        {
            this.InitializeComponent();
            Connector._SetupPage = this;
            getIPs();
        }

        private void NextPage(object sender, RoutedEventArgs e)
        {
            Connector._MainWindow.ContentFrame.Navigate(typeof(GamePage));
        }
    }
}
