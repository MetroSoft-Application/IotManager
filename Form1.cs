using System.Text;
using System.Text.Json;
using System.Windows.Forms;
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
    /// <summary>
    /// IoTデバイスとIoT Hubの管理を行うフォーム
    /// </summary>
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
        private string storageConnectionString;
        private EventProcessorClient processorClient;
        private const int MAX_LINE = 30;
        private string currentDirectMethodName = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            iotHubConnectionString = Utility.Configuration["IoTHub:ConnectionString"];
            txtIotHubConnectionString.Text = iotHubConnectionString;

            eventHubConnectionString = Utility.Configuration["EventHub:ConnectionString"];
            txtEventHubConnectionString.Text = eventHubConnectionString;

            storageConnectionString = Utility.Configuration["EventHub:StorageConnectionString"];
            txtStorageConnectionString.Text = storageConnectionString;

            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2), (exception, timeSpan, retryCount, context) =>
                {
                    MessageBox.Show($"操作が失敗しました {exception.Message}. リトライ回数: {retryCount}");
                });

            registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
        }

        /// <summary>
        /// イベントハンドラでメッセージを処理
        /// </summary>
        /// <param name="eventArgs">イベント引数</param>
        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            try
            {
                var message = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                Invoke(new Action(() =>
                {
                    rtxtHubReceive.AppendText($"[Message][{timestamp}] {message}{Environment.NewLine}");
                    EnsureMaxLines(rtxtHubReceive, MAX_LINE);
                }));

                await eventArgs.UpdateCheckpointAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing event: {ex.Message}");
            }
        }

        /// <summary>
        /// エラーハンドラでエラーを処理
        /// </summary>
        /// <param name="eventArgs">イベント引数</param>
        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            MessageBox.Show($"Error: {eventArgs.Exception.Message}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// デバイスIDをロード
        /// </summary>
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

        /// <summary>
        /// IoT Hubを開くボタンのクリックイベント
        /// </summary>
        private async void btnHubOpen_Click(object sender, EventArgs e)
        {
            if (isIotHubOpen)
            {
                await processorClient.StopProcessingAsync();
                btnHubOpen.Text = "Open";
                btnHubSend.Enabled = false;
                btnDirectMethod.Enabled = false;
                isIotHubOpen = false;
                return;
            }

            try
            {
                if (processorClient is null)
                {
                    eventHubConnectionString = txtEventHubConnectionString.Text;
                    eventHubName = Utility.GetEntityPathFromConnectionString(eventHubConnectionString);

                    storageConnectionString = txtStorageConnectionString.Text;
                    var blobContainerName = Utility.Configuration["EventHub:StorageContainerName"] ?? "eventhub-checkpoints";
                    var blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
                    blobContainerClient.CreateIfNotExists();

                    processorClient = new EventProcessorClient(blobContainerClient, EventHubConsumerClient.DefaultConsumerGroupName, eventHubConnectionString, eventHubName);

                    processorClient.ProcessEventAsync += ProcessEventHandler;
                    processorClient.ProcessErrorAsync += ProcessErrorHandler;
                }

                await processorClient.StartProcessingAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Event Processor: {ex.Message}");
            }

            isIotHubOpen = true;
            btnHubOpen.Text = "Close";
            btnHubSend.Enabled = true;
            btnDirectMethod.Enabled = true;
        }

        /// <summary>
        /// IoT Hubにメッセージを送信するボタンのクリックイベント
        /// </summary>
        private async void btnHubSend_Click(object sender, EventArgs e)
        {
            var deviceId = cmbDeviceId.SelectedItem.ToString();
            var message = rtxtHubSend.Text;
            await SendCloudToDeviceMessageAsync(deviceId, message);
        }

        /// <summary>
        /// クラウドからデバイスにメッセージを送信
        /// </summary>
        private async Task SendCloudToDeviceMessageAsync(string deviceId, string message)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(deviceId, commandMessage);
            MessageBox.Show($"メッセージがデバイス {deviceId} に送信されました: {message}");
        }

        /// <summary>
        /// デバイスを開くボタンのクリックイベント
        /// </summary>
        private async void btnDevicerOpen_Click(object sender, EventArgs e)
        {
            await DeviceOpen();
        }

        /// <summary>
        /// デバイスを開く
        /// </summary>
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
        }

        /// <summary>
        /// デバイスにメッセージを送信するボタンのクリックイベント
        /// </summary>
        private async void btnDeviceSend_Click(object sender, EventArgs e)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await SendMessageAsync(rtxtDeviceSend.Text);
            });
        }

        /// <summary>
        /// メッセージを受信した際の処理
        /// </summary>
        private async Task OnMessageReceived(Microsoft.Azure.Devices.Client.Message receivedMessage, object userContext)
        {
            var messageText = Encoding.UTF8.GetString(receivedMessage.GetBytes());
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    rtxtDeviceReceive.AppendText($"[Message][{timestamp}] {messageText}{Environment.NewLine}");
                    EnsureMaxLines(rtxtDeviceReceive, MAX_LINE);
                }));
            }
            else
            {
                rtxtDeviceReceive.AppendText($"[{timestamp}] {messageText}{Environment.NewLine}");
            }

            await deviceClient.CompleteAsync(receivedMessage);
        }

        /// <summary>
        /// メッセージを送信
        /// </summary>
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

        /// <summary>
        /// フォームロード時の処理
        /// </summary>
        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadDeviceIds();
        }

        /// <summary>
        /// RichTextBoxの行数を制限し、古い行を削除
        /// </summary>
        /// <param name="richTextBox">対象のRichTextBox</param>
        /// <param name="MaxLines">最大行数</param>
        private void EnsureMaxLines(RichTextBox richTextBox, int MaxLines)
        {
            if (richTextBox.Lines.Length > MaxLines)
            {
                var lines = richTextBox.Lines.Skip(richTextBox.Lines.Length - MaxLines).ToArray();
                richTextBox.Lines = lines;
                richTextBox.SelectionStart = richTextBox.Text.Length;
            }
            richTextBox.ScrollToCaret();
        }

        private async void btnDirectMethod_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDirectMethod.Text))
            {
                return;
            }

            // 既存のハンドラを削除
            if (!string.IsNullOrEmpty(currentDirectMethodName))
            {
                await deviceClient.SetMethodHandlerAsync(currentDirectMethodName, null, null);
            }

            // 新しいハンドラを設定
            await deviceClient.SetMethodHandlerAsync(txtDirectMethod.Text, DeviceMethodCallback, null);
            currentDirectMethodName = txtDirectMethod.Text;

            // ダイレクトメソッドを呼び出す
            if (cmbDeviceId.SelectedItem != null)
            {
                var deviceId = cmbDeviceId.SelectedItem.ToString();
                var payload = rtxtHubSend.Text;

                // JSON 形式かどうかをチェック
                if (!IsValidJson(payload))
                {
                    MessageBox.Show("ペイロードは有効なJSON形式ではありません。");
                    return;
                }

                var methodInvocation = new CloudToDeviceMethod(currentDirectMethodName) { ResponseTimeout = TimeSpan.FromSeconds(30) };
                methodInvocation.SetPayloadJson(payload);

                try
                {
                    var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
                    var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
                    MessageBox.Show($"メソッド呼び出し成功: {response.Status}, ペイロード: {response.GetPayloadAsJson()}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"メソッド呼び出し失敗: {ex.Message}");
                }
            }
        }

        private bool IsValidJson(string strInput)
        {
            try
            {
                JsonDocument.Parse(strInput);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<MethodResponse> DeviceMethodCallback(MethodRequest methodRequest, object userContext)
        {
            await Task.Delay(0);
            var payload = methodRequest.DataAsJson;

            // コマンドを実行し、レスポンスを生成
            var responsePayload = InvokeCommand(payload);
            var responseBytes = Encoding.UTF8.GetBytes(responsePayload);

            return new MethodResponse(responseBytes, 200);
        }

        private string InvokeCommand(string payload)
        {
            var message = payload;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    rtxtDeviceReceive.AppendText($"[DirectMethod][{timestamp}] {message}{Environment.NewLine}");
                    EnsureMaxLines(rtxtDeviceReceive, MAX_LINE);
                }));
            }
            else
            {
                rtxtDeviceReceive.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            }
            return payload;
        }
    }
}
