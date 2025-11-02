using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Polly;
using Polly.Retry;

namespace IotManager.Device
{
    /// <summary>
    /// デバイス操作を管理するクラス
    /// </summary>
    public class DeviceManager
    {
        private static DeviceClient deviceClient;
        private readonly string iotHubConnectionString;
        private readonly AsyncRetryPolicy retryPolicy;
        private readonly RegistryManager registryManager;
        private string currentDirectMethodName = string.Empty;

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
            var devices = await query.GetNextAsTwinAsync();
            deviceIds.AddRange(devices.Select(x => x.DeviceId));
            return deviceIds;
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
                deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only);
                await deviceClient.OpenAsync();
                await deviceClient.SendEventAsync(new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes($"{deviceId}:Connected!")));
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
        /// デバイスにメッセージを送信
        /// </summary>
        /// <param name="message">送信するメッセージ本文</param>
        public async Task SendDeviceMessageAsync(string message)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await SendMessageAsync(message);
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

        /// <summary>
        /// デバイスからのメッセージを受信したときの処理
        /// </summary>
        /// <param name="receivedMessage">受信したメッセージ</param>
        /// <param name="userContext">ユーザーコンテキスト</param>
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

        /// <summary>
        /// ダイレクトメソッドのコールバック処理
        /// </summary>
        /// <param name="methodRequest">メソッドリクエスト</param>
        /// <param name="userContext">ユーザーコンテキスト</param>
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

        /// <summary>
        /// メッセージを送信する
        /// </summary>
        /// <param name="message">送信するメッセージ</param>
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
    }
}
