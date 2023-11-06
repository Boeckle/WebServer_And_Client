using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Client
{
    public partial class MainWindow
    {
        ClientWebSocket _client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartWebClient(string name, string port)
        {
            try
            {
                _client = new ClientWebSocket();
                var uri = new Uri($"ws://localhost:{port}/ws?name={name}");
                await _client.ConnectAsync(uri, CancellationToken.None).ContinueWith(task => UpdateUIConneted());
                await Task.WhenAny(Receive(_client));
            }
            catch (Exception ex)
            {
                AddMessageToOutput(ex.Message);
            }
        }

        private async void StopWebClient()
        {
            try
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ContinueWith(task => UpdateUIDisconneted());
            }
            catch (Exception ex)
            {
                AddMessageToOutput(ex.Message);
            }
        }

        private async Task Receive(ClientWebSocket client)
        {
            byte[] buffer = new byte[4096];
            while (client.State == WebSocketState.Open)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                AddMessageToOutput(string.Format("< {0}: {1}", DateTime.Now.ToString("T"), message));
            }
        }

        private void AddMessageToOutput(string message) => Dispatcher.Invoke(() => { lvOutputField.Items.Add(message); });

        private void UpdateUIConneted()
        {
            if (_client.State == WebSocketState.Open)
            {
                AddMessageToOutput("Connected successfully...");
                UpdateUi(true);
            }
        }

        private void UpdateUIDisconneted()
        {
            if (_client.State == WebSocketState.Closed)
            {
                AddMessageToOutput("Disonnected successfully...");
                UpdateUi(false);
            }
        }

        private void UpdateUi(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                btnConnect.IsEnabled = !connected;
                btnDisconnect.IsEnabled = btnSend.IsEnabled = connected;
                spConnection.Background = !connected ? Brushes.Red : Brushes.Green;
            });
        }

        private void btnConnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StartWebClient(txtName.Text, txtPort.Text);
        }

        private void btnDisconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            StopWebClient();
        }

        private async void btnSend_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_client != null && _client.State == WebSocketState.Open)
            {
                try
                {
                    ArraySegment<byte> bufferMessage = new ArraySegment<byte>(Encoding.ASCII.GetBytes(txtInput.Text));

                    await _client.SendAsync(bufferMessage, WebSocketMessageType.Text, true, CancellationToken.None);

                    Dispatcher.Invoke(() =>
                    {
                        AddMessageToOutput(string.Format("> {0}: {1}", DateTime.Now.ToString("T"), txtInput.Text));
                        txtInput.Clear();
                    });
                }
                catch (Exception ex)
                {
                    AddMessageToOutput(ex.Message);
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            StopWebClient();

            base.OnClosing(e);
        }
    }
}
