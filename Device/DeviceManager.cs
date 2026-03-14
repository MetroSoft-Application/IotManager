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
    /// デバイス接続 メッセージ送信 ダイレクトメソッド処理を管理する
    /// </summary>
    public class DeviceManager
    {
        /// <summary>
        /// デバイスIDごとの接続済みクライアント
        /// </summary>
        private readonly Dictionary<string, DeviceClient> deviceClients = new Dictionary<string, DeviceClient>();
        /// <summary>
        /// IoTHub関連の設定
        /// </summary>
        private readonly IoTHubSettings settings;
        /// <summary>
        /// 再試行ポリシー
        /// </summary>
        private readonly AsyncRetryPolicy retryPolicy;
        /// <summary>
        /// デバイス情報取得に使用するレジストリマネージャー
        /// </summary>
        private readonly RegistryManager registryManager;
        /// <summary>
        /// サービス側操作に使用するクライアント
        /// </summary>
        private readonly ServiceClient serviceClient;

        /// <summary>
        /// デバイスメッセージ受信時に通知するイベント
        /// </summary>
        public event Func<string, Task> OnMessageReceived;

        /// <summary>
        /// ダイレクトメソッド受信時に通知するイベント
        /// </summary>
        public event Func<string, Task> OnDirectMethodReceived;

        /// <summary>
        /// <see cref="DeviceManager" /> クラスの新しいインスタンスを初期化する
        /// </summary>
        /// <param name="settings">IoTHub接続およびデバイス操作に使用する設定</param>
        public DeviceManager(IoTHubSettings settings)
        {
            this.settings = settings;

            retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(settings.RetryCount, retryAttempt => TimeSpan.FromSeconds(settings.RetryDelaySeconds));

            registryManager = RegistryManager.CreateFromConnectionString(settings.ConnectionString);
            serviceClient = ServiceClient.CreateFromConnectionString(settings.ConnectionString);
        }

        /// <summary>
        /// IoTHubに登録されているデバイスID一覧を取得する
        /// </summary>
        /// <returns>昇順に並べたデバイスID一覧</returns>
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
        /// 指定デバイスとの接続を確立し受信ハンドラーを登録する
        /// </summary>
        /// <param name="deviceId">接続対象のデバイスID</param>
        /// <returns>接続完了を表すタスク</returns>
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
        /// 指定デバイスとの接続を閉じる
        /// </summary>
        /// <param name="deviceId">切断対象のデバイスID</param>
        /// <returns>切断完了を表すタスク</returns>
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
        /// 指定デバイスからメッセージを送信する
        /// </summary>
        /// <param name="deviceId">送信元となるデバイスID</param>
        /// <param name="message">送信するメッセージ本文</param>
        /// <returns>送信完了を表すタスク</returns>
        public async Task SendDeviceMessageAsync(string deviceId, string message)
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                await SendMessageAsync(deviceId, message);
            });
        }

        /// <summary>
        /// デバイスの接続状態を取得する
        /// </summary>
        /// <param name="deviceId">状態確認対象のデバイスID</param>
        /// <returns>接続中の場合は <see langword="true" /> それ以外は <see langword="false" /></returns>
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
        /// 指定デバイスに対してダイレクトメソッドを呼び出す
        /// </summary>
        /// <param name="deviceId">呼び出し対象のデバイスID</param>
        /// <param name="methodName">呼び出すメソッド名</param>
        /// <param name="payload">メソッドに渡す JSON 形式のペイロード</param>
        /// <returns>ダイレクトメソッド呼び出し結果</returns>
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

            return await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
        }

        /// <summary>
        /// デバイスからのメッセージ受信時にイベント通知と完了応答を行う
        /// </summary>
        /// <param name="deviceId">受信元デバイスID</param>
        /// <param name="receivedMessage">受信したメッセージ</param>
        /// <param name="userContext">登録時に渡されたユーザーコンテキスト</param>
        /// <returns>受信処理完了を表すタスク</returns>
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
        /// ダイレクトメソッド受信時の既定処理を実行する
        /// </summary>
        /// <param name="deviceId">呼び出し対象デバイスID</param>
        /// <param name="methodRequest">受信したメソッド要求</param>
        /// <param name="userContext">登録時に渡されたユーザーコンテキスト</param>
        /// <returns>デバイスから返却するメソッド応答</returns>
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
        /// コマンドプロンプトを利用して指定コマンドを実行する
        /// </summary>
        /// <param name="command">実行するコマンド文字列</param>
        /// <returns>標準出力またはエラーを含む実行結果文字列</returns>
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
#if NET6_0_OR_GREATER
            await process.WaitForExitAsync();
#else
            await Task.Run(() => process.WaitForExit());
#endif

            var output = outputTask.Result;
            var error = errorTask.Result;

            if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
            {
                return $"Error (ExitCode: {process.ExitCode}): {error}";
            }

            return string.IsNullOrWhiteSpace(output) ? "Command executed successfully (no output)" : output;
        }

        /// <summary>
        /// 実際のデバイス送信処理を実行する
        /// </summary>
        /// <param name="deviceId">送信対象のデバイスID</param>
        /// <param name="message">送信するメッセージ本文</param>
        /// <returns>送信完了を表すタスク</returns>
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
        /// 設定文字列からトランスポート種別を解決する
        /// </summary>
        /// <param name="transportType">設定ファイルから読み込んだトランスポート種別文字列</param>
        /// <returns>対応する <see cref="Microsoft.Azure.Devices.Client.TransportType" /></returns>
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
