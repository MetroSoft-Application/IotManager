using System.Text.Json;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;

namespace IotManager
{
    /// <summary>
    /// 設定取得や接続文字列解析などの共通処理を提供する
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// アプリケーション全体で共有する構成情報
        /// </summary>
        public static IConfiguration Configuration { get; }

        /// <summary>
        /// <see cref="Utility" /> クラスを初期化し設定ファイルを読み込む
        /// </summary>
        static Utility()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();
        }

        /// <summary>
        /// IoTHub接続文字列からホスト名を抽出する
        /// </summary>
        /// <param name="iotHubConnectionString">解析対象のIoTHub接続文字列</param>
        /// <returns>接続文字列に含まれるIoTHubのホスト名</returns>
        public static string GetIoTHubHostNameFromConnectionString(string iotHubConnectionString)
        {
            var parts = iotHubConnectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("HostName=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring("HostName=".Length);
                }
            }

            throw new ArgumentException("Invalid connection string, no HostName found.");
        }

        /// <summary>
        /// EventHub接続文字列から`EntityPath`を抽出する
        /// </summary>
        /// <param name="eventHubConnectionString">解析対象のEventHub接続文字列</param>
        /// <returns>接続文字列に含まれる `EntityPath` の値</returns>
        public static string GetEntityPathFromConnectionString(string eventHubConnectionString)
        {
            var parts = eventHubConnectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("EntityPath=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring("EntityPath=".Length);
                }
            }

            throw new ArgumentException("Invalid connection string, no EntityPath found.");
        }

        /// <summary>
        /// デバイス接続用の接続文字列を構築する
        /// </summary>
        /// <param name="deviceId">対象デバイスの ID</param>
        /// <param name="primaryKey">対象デバイスのプライマリ キー</param>
        /// <param name="iotHubconnectionstring">IoTHub接続文字列</param>
        /// <returns>デバイス接続に使用する接続文字列</returns>
        public static string BuildDeviceConnectionString(string deviceId, string primaryKey, string iotHubconnectionstring)
        {
            var iotHubHostName = Utility.GetIoTHubHostNameFromConnectionString(iotHubconnectionstring);
            var deviceConnectionString = $"HostName={iotHubHostName};DeviceId={deviceId};SharedAccessKey={primaryKey}";
            return deviceConnectionString;
        }

        /// <summary>
        /// 指定文字列が JSON として解釈可能か判定する
        /// </summary>
        /// <param name="strInput">検証対象の文字列</param>
        /// <returns>有効な JSON の場合は <see langword="true" /> それ以外は <see langword="false" /></returns>
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
