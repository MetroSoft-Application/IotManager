using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Polly;
using Polly.Retry;

namespace IotManager
{
    /// <summary>
    /// IoT操作を管理するクラス
    /// </summary>
    public class IotManagerLogic
    {
        private static DeviceClient deviceClient;
        private readonly string iotHubConnectionString;
        private readonly AsyncRetryPolicy retryPolicy;
        private readonly RegistryManager registryManager;
        private readonly string eventHubConnectionString;
        private readonly string eventHubName;
        private readonly string storageConnectionString;
        private EventProcessorClient processorClient;
        private string currentDirectMethodName = string.Empty;

        public event Func<string, Task> OnMessageReceived;
        public event Func<string, Task> OnHubMessageReceived;
        public event Func<string, Task> OnDirectMethodReceived;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iotHubConnectionString">IoT Hub接続文字列</param>
        /// <param name="eventHubConnectionString">Event Hub接続文字列</param>
        /// <param name="storageConnectionString">Storage接続文字列</param>
        public IotManagerLogic(string iotHubConnectionString, string eventHubConnectionString, string storageConnectionString)
        {
            this.iotHubConnectionString = iotHubConnectionString;
            this.eventHubConnectionString = eventHubConnectionString;
            this.storageConnectionString = storageConnectionString;

            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2));

            registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            eventHubName = Utility.GetEntityPathFromConnectionString(eventHubConnectionString);
        }

        /// <summary>
        /// デバイスIDリストを取得
        /// </summary>
        public async Task<List<string>> GetDeviceIdsAsync()
        {
            var deviceIds = new List<string>();
            var query = registryManager.CreateQuery("SELECT * FROM devices");
            var devices = await query.GetNextAsTwinAsync();

            foreach (var twin in devices)
            {
                deviceIds.Add(twin.DeviceId);
            }

            return deviceIds;
        }

        /// <summary>
        /// Event Hub処理を開始
        /// </summary>
        public async Task StartEventHubProcessingAsync()
        {
            if (processorClient is null)
            {
                var blobContainerName = Utility.Configuration["EventHub:StorageContainerName"] ?? "eventhub-checkpoints";
                var blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
                blobContainerClient.CreateIfNotExists();

                processorClient = new EventProcessorClient(blobContainerClient, EventHubConsumerClient.DefaultConsumerGroupName, eventHubConnectionString, eventHubName);

                processorClient.ProcessEventAsync += ProcessEventHandler;
                processorClient.ProcessErrorAsync += ProcessErrorHandler;
            }

            await processorClient.StartProcessingAsync();
        }

        /// <summary>
        /// Event Hub処理を停止
        /// </summary>
        public async Task StopEventHubProcessingAsync()
        {
            if (processorClient != null)
            {
                await processorClient.StopProcessingAsync();
            }
        }

        /// <summary>
        /// デバイスを開く
        /// </summary>
        public async Task OpenDeviceAsync(string deviceId)
        {
            var device = await registryManager.GetDeviceAsync(deviceId);
            var deviceConnectionString = Utility.BuildDeviceConnectionString(deviceId, device, iotHubConnectionString);

            await retryPolicy.ExecuteAsync(async () =>
            {
                deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
                await deviceClient.OpenAsync();
            });

            await retryPolicy.ExecuteAsync(async () =>
            {
                await deviceClient.SetReceiveMessageHandlerAsync(OnDeviceMessageReceived, null);
            });
        }

        /// <summary>
        /// デバイスを閉じる
        /// </summary>
        public async Task CloseDeviceAsync()
        {
            if (deviceClient != null)
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    await deviceClient.CloseAsync();
                });
            }
        }

        /// <summary>
        /// デバイスからメッセージを送信
        /// </summary>
        public async Task SendDeviceMessageAsync(string message)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await SendMessageAsync(message);
            });
        }

        /// <summary>
        /// クラウドからデバイスにメッセージを送信
        /// </summary>
        public async Task SendCloudToDeviceMessageAsync(string deviceId, string message)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        /// <summary>
        /// ダイレクトメソッドを設定して呼び出し
        /// </summary>
        public async Task<CloudToDeviceMethodResult> InvokeDirectMethodAsync(string deviceId, string methodName, string payload)
        {
            // 既存のハンドラを削除
            if (!string.IsNullOrEmpty(currentDirectMethodName))
            {
                await deviceClient.SetMethodHandlerAsync(currentDirectMethodName, null, null);
            }

            // 新しいハンドラを設定
            await deviceClient.SetMethodHandlerAsync(methodName, DeviceMethodCallback, null);
            currentDirectMethodName = methodName;

            // ダイレクトメソッドを呼び出す
            var methodInvocation = new CloudToDeviceMethod(methodName) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            methodInvocation.SetPayloadJson(payload);

            var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            return await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
        }

        // プライベートメソッド

        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            try
            {
                var message = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                if (OnHubMessageReceived != null)
                {
                    await OnHubMessageReceived($"[Message][{timestamp}] {message}");
                }

                await eventArgs.UpdateCheckpointAsync();
            }
            catch (Exception)
            {
                // エラーハンドリングはUI側で処理
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            // エラーハンドリングはUI側で処理
            return Task.CompletedTask;
        }

        private async Task OnDeviceMessageReceived(Microsoft.Azure.Devices.Client.Message receivedMessage, object userContext)
        {
            var messageText = Encoding.UTF8.GetString(receivedMessage.GetBytes());
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (OnMessageReceived != null)
            {
                await OnMessageReceived($"[Message][{timestamp}] {messageText}");
            }

            await deviceClient.CompleteAsync(receivedMessage);
        }

        private async Task<MethodResponse> DeviceMethodCallback(MethodRequest methodRequest, object userContext)
        {
            await Task.Delay(0);
            var payload = methodRequest.DataAsJson;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (OnDirectMethodReceived != null)
            {
                await OnDirectMethodReceived($"[DirectMethod][{timestamp}] {payload}");
            }

            var responseBytes = Encoding.UTF8.GetBytes(payload);
            return new MethodResponse(responseBytes, 200);
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
            messageToSend.Properties.Add("insertJstTime", jstTime.ToString("o"));
            messageToSend.MessageId = Guid.NewGuid().ToString();

            await deviceClient.SendEventAsync(messageToSend);
        }

        public static bool IsValidJson(string strInput)
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
    }
}