using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Polly;
using Polly.Retry;

namespace IotManager.Device
{
    /// <summary>
    /// デバイス操作を管理するクラス
    /// </summary>
    public class DeviceManager
    {
        private readonly Dictionary<string, DeviceClient> deviceClients = new Dictionary<string, DeviceClient>();
        private readonly Dictionary<string, string> currentDirectMethodNames = new Dictionary<string, string>();
        private readonly string iotHubConnectionString;
        private readonly AsyncRetryPolicy retryPolicy;
        private readonly RegistryManager registryManager;

        public event Func<string, Task> OnMessageReceived;
        public event Func<string, Task> OnDirectMethodReceived;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="iotHubConnectionString">IoT Hub接続文字列</param>
        public DeviceManager(string iotHubConnectionString)
        {
            this.iotHubConnectionString = iotHubConnectionString;

            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(2));

            registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
        }

        /// <summary>
        /// デバイスIDリストを取得
        /// </summary>
        public async Task<List<string>> GetDeviceIdsAsync()
        {
            var deviceIds = new List<string>();
            var query = registryManager.CreateQuery("SELECT * FROM devices");
            
            while (query.HasMoreResults)
            {
                var devices = await query.GetNextAsTwinAsync();
                deviceIds.AddRange(devices.Select(x => x.DeviceId));
            }
            
            return deviceIds.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// デバイスを開く
        /// </summary>
        /// <param name="deviceId">開くデバイスのID</param>
        public async Task OpenDeviceAsync(string deviceId)
        {
            var device = await registryManager.GetDeviceAsync(deviceId);
            var deviceConnectionString = Utility.BuildDeviceConnectionString(deviceId, device.Authentication.SymmetricKey.PrimaryKey, iotHubConnectionString);

            await retryPolicy.ExecuteAsync(async () =>
            {
                var client = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
                await client.OpenAsync();
                deviceClients[deviceId] = client;
                await this.SendDeviceMessageAsync(deviceId, "Device connected successfully.");
            });

            await retryPolicy.ExecuteAsync(async () =>
            {
                await deviceClients[deviceId].SetReceiveMessageHandlerAsync(async (msg, ctx) => await OnDeviceMessageReceived(deviceId, msg, ctx), null);
            });
        }

        /// <summary>
        /// デバイスを閉じる
        /// </summary>
        /// <param name="deviceId">閉じるデバイスのID</param>
        public async Task CloseDeviceAsync(string deviceId)
        {
            if (deviceClients.TryGetValue(deviceId, out var client))
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    await client.CloseAsync();
                });
                deviceClients.Remove(deviceId);
                currentDirectMethodNames.Remove(deviceId);
            }
        }

        /// <summary>
        /// デバイスにメッセージを送信
        /// </summary>
        /// <param name="deviceId">対象デバイスのID</param>
        /// <param name="message">送信するメッセージ本文</param>
        public async Task SendDeviceMessageAsync(string deviceId, string message)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await SendMessageAsync(deviceId, message);
            });
        }

        /// <summary>
        /// ダイレクトメソッドを設定して呼び出し
        /// </summary>
        /// <param name="deviceId">対象デバイスのID</param>
        /// <param name="methodName">呼び出すメソッド名</param>
        /// <param name="payload">メソッドに渡すJSON形式のペイロード</param>
        public async Task<CloudToDeviceMethodResult> InvokeDirectMethodAsync(string deviceId, string methodName, string payload)
        {
            if (!deviceClients.TryGetValue(deviceId, out var client))
            {
                throw new InvalidOperationException($"Device {deviceId} is not connected.");
            }

            // 既存のハンドラを削除
            if (currentDirectMethodNames.TryGetValue(deviceId, out var currentMethodName) && !string.IsNullOrEmpty(currentMethodName))
            {
                await client.SetMethodHandlerAsync(currentMethodName, null, null);
            }

            // 新しいハンドラを設定
            await client.SetMethodHandlerAsync(methodName, async (req, ctx) => await DeviceMethodCallback(deviceId, req, ctx), null);
            currentDirectMethodNames[deviceId] = methodName;

            // ダイレクトメソッドを呼び出す
            var methodInvocation = new CloudToDeviceMethod(methodName) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            methodInvocation.SetPayloadJson(payload);

            var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            return await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
        }

        /// <summary>
        /// デバイスからのメッセージを受信したときの処理
        /// </summary>
        /// <param name="deviceId">デバイスID</param>
        /// <param name="receivedMessage">受信したメッセージ</param>
        /// <param name="userContext">ユーザーコンテキスト</param>
        private async Task OnDeviceMessageReceived(string deviceId, Microsoft.Azure.Devices.Client.Message receivedMessage, object userContext)
        {
            var messageText = Encoding.UTF8.GetString(receivedMessage.GetBytes());
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (OnMessageReceived != null)
            {
                await OnMessageReceived($"[{deviceId}][Message][{timestamp}] {messageText}");
            }

            if (deviceClients.TryGetValue(deviceId, out var client))
            {
                await client.CompleteAsync(receivedMessage);
            }
        }

        /// <summary>
        /// ダイレクトメソッドのコールバック処理
        /// </summary>
        /// <param name="deviceId">デバイスID</param>
        /// <param name="methodRequest">メソッドリクエスト</param>
        /// <param name="userContext">ユーザーコンテキスト</param>
        private async Task<MethodResponse> DeviceMethodCallback(string deviceId, MethodRequest methodRequest, object userContext)
        {
            var payload = methodRequest.DataAsJson;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (OnDirectMethodReceived != null)
            {
                await OnDirectMethodReceived($"[{deviceId}][DirectMethod][{timestamp}] {payload}");
            }

            var responseBytes = Encoding.UTF8.GetBytes(payload);
            return new MethodResponse(responseBytes, 200);
        }

        /// <summary>
        /// メッセージを送信する
        /// </summary>
        /// <param name="deviceId">デバイスID</param>
        /// <param name="message">送信するメッセージ</param>
        private async Task SendMessageAsync(string deviceId, string message)
        {
            if (!deviceClients.TryGetValue(deviceId, out var client))
            {
                throw new InvalidOperationException($"Device {deviceId} is not connected.");
            }

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

            await client.SendEventAsync(messageToSend);
        }
    }
}
