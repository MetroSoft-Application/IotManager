using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Polly;
using Polly.Retry;
using IotManager.Settings;

namespace IotManager.Device
{
    /// <summary>
    /// デバイス操作を管理するクラス
    /// </summary>
    public class DeviceManager
    {
        private readonly Dictionary<string, DeviceClient> deviceClients = new Dictionary<string, DeviceClient>();
        private readonly IoTHubSettings settings;
        private readonly AsyncRetryPolicy retryPolicy;
        private readonly RegistryManager registryManager;

        public event Func<string, Task> OnMessageReceived;
        public event Func<string, Task> OnDirectMethodReceived;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="settings">IoT Hub設定</param>
        public DeviceManager(IoTHubSettings settings)
        {
            this.settings = settings;

            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(settings.RetryCount, retryAttempt => TimeSpan.FromSeconds(settings.RetryDelaySeconds));

            registryManager = RegistryManager.CreateFromConnectionString(settings.ConnectionString);
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
            var deviceConnectionString = Utility.BuildDeviceConnectionString(deviceId, device.Authentication.SymmetricKey.PrimaryKey, settings.ConnectionString);

            await retryPolicy.ExecuteAsync(async () =>
            {
                var transportType = ParseTransportType(settings.TransportType);
                var client = DeviceClient.CreateFromConnectionString(deviceConnectionString, transportType);
                client.OperationTimeoutInMilliseconds = (uint)(settings.OperationTimeoutSeconds * 1000);
                await client.OpenAsync();
                deviceClients[deviceId] = client;
                await this.SendDeviceMessageAsync(deviceId, "Device connected successfully.");
            });

            await retryPolicy.ExecuteAsync(async () =>
            {
                await deviceClients[deviceId].SetReceiveMessageHandlerAsync(async (msg, ctx) => await OnDeviceMessageReceived(deviceId, msg, ctx), null);
                await deviceClients[deviceId].SetMethodDefaultHandlerAsync(async (req, ctx) => await DeviceMethodCallback(deviceId, req, ctx), null);
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
        /// デバイスの接続状態を確認
        /// </summary>
        /// <param name="deviceId">対象デバイスのID</param>
        /// <returns>接続されている場合true</returns>
        public async Task<bool> IsDeviceConnectedAsync(string deviceId)
        {
            try
            {
                var twin = await registryManager.GetTwinAsync(deviceId);
                return twin.ConnectionState == DeviceConnectionState.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ダイレクトメソッドを呼び出し
        /// </summary>
        /// <param name="deviceId">対象デバイスのID</param>
        /// <param name="methodName">呼び出すメソッド名</param>
        /// <param name="payload">メソッドに渡すJSON形式のペイロード</param>
        public async Task<CloudToDeviceMethodResult> InvokeDirectMethodAsync(string deviceId, string methodName, string payload)
        {
            // デバイスの接続状態を事前確認（注意: 完全にリアルタイムではない）
            var isConnected = await IsDeviceConnectedAsync(deviceId);
            if (!isConnected)
            {
                throw new InvalidOperationException($"Device {deviceId} is not connected to IoT Hub.");
            }

            // ダイレクトメソッドを呼び出す
            var methodInvocation = new CloudToDeviceMethod(methodName) { ResponseTimeout = TimeSpan.FromSeconds(settings.DirectMethodTimeoutSeconds) };
            methodInvocation.SetPayloadJson(payload);

            var serviceClient = ServiceClient.CreateFromConnectionString(settings.ConnectionString);
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

            // "command" メソッドの場合、ペイロードのmessageをコマンドプロンプトで実行
            if (methodRequest.Name.Equals("command", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var payloadObject = JsonSerializer.Deserialize<JsonElement>(payload);
                    if (payloadObject.TryGetProperty("message", out var messageElement))
                    {
                        var command = messageElement.GetString();
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            var result = await ExecuteCommandAsync(command);

                            // 実行結果をUIに通知
                            if (OnDirectMethodReceived != null)
                            {
                                var resultTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                await OnDirectMethodReceived($"[{deviceId}][{methodRequest.Name}][{resultTimestamp}]{result}");
                            }

                            var responseObject = new { status = "success", output = result };
                            var responseJson = JsonSerializer.Serialize(responseObject);
                            var commandResponseBytes = Encoding.UTF8.GetBytes(responseJson);
                            return new MethodResponse(commandResponseBytes, 200);
                        }
                    }

                    var errorResponse = new { status = "error", message = "Invalid payload: 'message' field not found or empty" };
                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    return new MethodResponse(Encoding.UTF8.GetBytes(errorJson), 400);
                }
                catch (Exception ex)
                {
                    // エラーもUIに通知
                    if (OnDirectMethodReceived != null)
                    {
                        var errorTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        await OnDirectMethodReceived($"[{deviceId}][CommandError][{errorTimestamp}] {ex.Message}");
                    }

                    var errorResponse = new { status = "error", message = ex.Message };
                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    return new MethodResponse(Encoding.UTF8.GetBytes(errorJson), 500);
                }
            }

            var responseBytes = Encoding.UTF8.GetBytes(payload);
            return new MethodResponse(responseBytes, 200);
        }

        /// <summary>
        /// コマンドプロンプトでコマンドを実行
        /// </summary>
        /// <param name="command">実行するコマンド</param>
        /// <returns>コマンドの実行結果</returns>
        private async Task<string> ExecuteCommandAsync(string command)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            // 標準出力とエラー出力を並列に読み取る（デッドロック防止）
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            var output = outputTask.Result;
            var error = errorTask.Result;

            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
            {
                return $"Error (ExitCode: {process.ExitCode}): {error}";
            }

            return string.IsNullOrWhiteSpace(output) ? "Command executed successfully (no output)" : output;
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

        /// <summary>
        /// 文字列からTransportTypeを解析
        /// </summary>
        /// <param name="transportType">トランスポートタイプ文字列</param>
        /// <returns>TransportType列挙型</returns>
        private Microsoft.Azure.Devices.Client.TransportType ParseTransportType(string transportType)
        {
            return transportType switch
            {
                "Mqtt" => Microsoft.Azure.Devices.Client.TransportType.Mqtt,
                "Amqp" => Microsoft.Azure.Devices.Client.TransportType.Amqp,
                "Http1" => Microsoft.Azure.Devices.Client.TransportType.Http1,
                "Amqp_WebSocket_Only" => Microsoft.Azure.Devices.Client.TransportType.Amqp_WebSocket_Only,
                "Mqtt_WebSocket_Only" => Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only,
                _ => Microsoft.Azure.Devices.Client.TransportType.Mqtt_WebSocket_Only
            };
        }
    }
}
