using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace IotManager
{
    public partial class Form1 : Form
    {
        private static DeviceClient deviceClient;
        private string iotHubConnectionString;
        private AsyncRetryPolicy retryPolicy;
        private RegistryManager registryManager;
        private bool isDeviceOpen = false;
        private bool isIotHubOpen = false;
        private string eventHubConnectionString;
        private string eventHubName;
        private EventProcessorClient processorClient;

        public Form1()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

             iotHubConnectionString = Utility.Configuration["IoTHub:ConnectionString"];
            txtConnectionString.Text = iotHubConnectionString;

            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2), (exception, timeSpan, retryCount, context) =>
                {
                    MessageBox.Show($"操作が失敗しました {exception.Message}. リトライ回数: {retryCount}");
                });

            registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
        }

        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            try
            {
                // メッセージの内容を取得
                var message = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                Invoke(new Action(() => rtxtHubReceive.AppendText($"[{timestamp}] {message}{Environment.NewLine}")));

                await eventArgs.UpdateCheckpointAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing event: {ex.Message}");
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            MessageBox.Show($"Error: {eventArgs.Exception.Message}");
            return Task.CompletedTask;
        }

        private async Task LoadDeviceIds()
        {
            try
            {
                var query = registryManager.CreateQuery("SELECT * FROM devices");
                var devices = await query.GetNextAsTwinAsync();
                if (devices.Any())
                {
                    cmbDeviceId.Items.Clear();
                    foreach (var twin in devices)
                    {
                        cmbDeviceId.Items.Add(twin.DeviceId);
                    }

                    cmbDeviceId.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void btnHubOpen_Click(object sender, EventArgs e)
        {
            if (isIotHubOpen)
            {
                await processorClient.StopProcessingAsync();
                btnHubOpen.Text = "Open";
                btnHubSend.Enabled = false;
                isIotHubOpen = false;
                return;
            }

            try
            {
                if (processorClient is null)
                {
                    eventHubConnectionString = Utility.Configuration["EventHub:ConnectionString"];
                    eventHubName = Utility.Configuration["EventHub:EvebtHubName"];

                    // Blobコンテナのクライアントを作成
                    var blobStorageConnectionString = Utility.Configuration["EventHub:StorageConnectionString"];
                    var blobContainerName = Utility.Configuration["EventHub:StorageContainerName"];
                    var blobContainerClient = new BlobContainerClient(blobStorageConnectionString, blobContainerName);
                    // Blobコンテナが存在しない場合は作成
                    blobContainerClient.CreateIfNotExists();

                    // EventProcessorClientを初期化
                    processorClient = new EventProcessorClient(blobContainerClient, EventHubConsumerClient.DefaultConsumerGroupName, eventHubConnectionString, eventHubName);

                    // イベントハンドラを設定
                    processorClient.ProcessEventAsync += ProcessEventHandler;
                    processorClient.ProcessErrorAsync += ProcessErrorHandler;
                }

                // メッセージ受信の開始
                await processorClient.StartProcessingAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Event Processor: {ex.Message}");
            }

            isIotHubOpen = true;
            btnHubOpen.Text = "Close";
            btnHubSend.Enabled = true;
        }

        private async void btnHubSend_Click(object sender, EventArgs e)
        {
            var deviceId = cmbDeviceId.SelectedItem.ToString();
            var message = rtxtHubSend.Text;
            await SendCloudToDeviceMessageAsync(deviceId, message);
        }

        private async Task SendCloudToDeviceMessageAsync(string deviceId, string message)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(deviceId, commandMessage);
            MessageBox.Show($"メッセージがデバイス {deviceId} に送信されました: {message}");
        }

        private async void btnDevicerOpen_Click(object sender, EventArgs e)
        {
            await DeviceOpen();
        }

        private async Task DeviceOpen()
        {
            var deviceId = cmbDeviceId.SelectedItem.ToString();
            var device = await registryManager.GetDeviceAsync(deviceId);
            var deviceConnectionString = Utility.BuildDeviceConnectionString(deviceId, device, iotHubConnectionString);

            if (isDeviceOpen)
            {
                btnDevicerOpen.Text = "Open";
                await retryPolicy.ExecuteAsync(async () =>
                {
                    deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
                    await deviceClient.CloseAsync();
                });

                btnDeviceSend.Enabled = false;
                cmbDeviceId.Enabled = true;
                isDeviceOpen = false;
                return;
            }

            await retryPolicy.ExecuteAsync(async () =>
            {
                deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
                await deviceClient.OpenAsync();
            });

            await retryPolicy.ExecuteAsync(async () =>
            {
                await deviceClient.SetReceiveMessageHandlerAsync(OnMessageReceived, null);
            });

            isDeviceOpen = true;
            btnDevicerOpen.Text = "Close";
            btnDeviceSend.Enabled = true;
            cmbDeviceId.Enabled = false;

            //await retryPolicy.ExecuteAsync(async () =>
            //{
            //    var method = args.Length > 1 ? args[1] : configuration["IoTHub:DirectMethod"];
            //    await deviceClient.SetMethodHandlerAsync(method, DirectMethodCallback, null);
            //    Console.WriteLine($"ダイレクトメソッド待機:{method}");
            //});
        }

        private async void btnDeviceSend_Click(object sender, EventArgs e)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await SendMessageAsync(rtxtDeviceSend.Text);
            });
        }

        private async Task OnMessageReceived(Microsoft.Azure.Devices.Client.Message receivedMessage, object userContext)
        {
            var messageBytes = receivedMessage.GetBytes();
            var messageText = Encoding.UTF8.GetString(messageBytes);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // UIスレッドでの操作
            if (InvokeRequired)
            {
                Invoke(new Action(() => rtxtDeviceReceive.AppendText($"[{timestamp}] {messageText}{Environment.NewLine}")));
            }
            else
            {
                rtxtDeviceReceive.AppendText($"[{timestamp}] {messageText}{Environment.NewLine}");
            }

            // メッセージを完了としてマーク
            await deviceClient.CompleteAsync(receivedMessage);
        }

        private static async Task SendMessageAsync(string message)
        {
            var jstTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Tokyo Standard Time");
            var messageObject = new { message = $"{message}" };
            var jsonMessage = JsonSerializer.Serialize(messageObject);
            var messageToSend = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(jsonMessage))
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };
            // ユーザー定義プロパティを追加
            messageToSend.Properties.Add("insertJstTime", jstTime.ToString("o"));
            messageToSend.MessageId = Guid.NewGuid().ToString();

            try
            {
                var sendStartTime = DateTime.Now;
                await deviceClient.SendEventAsync(messageToSend);
                var sendEndTime = DateTime.Now;
                MessageBox.Show($"メッセージ送信完了: {sendEndTime:o} - {jsonMessage}");
            }
            catch (OperationCanceledException ex)
            {
                MessageBox.Show($"メッセージ送信がキャンセルされました {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"メッセージ送信が失敗しました {ex.Message}");
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadDeviceIds();
        }
    }
}
