using System.Text.Json;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;

namespace IotManager
{
    /// <summary>
    /// ユーティリティクラス
    /// </summary>
    public static class Utility
    {
        public static IConfiguration Configuration { get; }

        /// <summary>
        /// 静的コンストラクタ
        /// </summary>
        static Utility()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        /// <summary>
        /// 接続文字列からIoT Hubのホスト名を取得
        /// </summary>
        /// <param name="iotHubConnectionString">IotHubの接続文字列</param>
        /// <returns>IoT Hubのホスト名</returns>
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
        /// 接続文字列からEntityPathを取得
        /// </summary>
        /// <param name="eventHubConnectionString">Eventhubの接続文字列</param>
        /// <returns>EntityPath</returns>
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
        /// デバイス接続文字列を構築
        /// </summary>
        /// <param name="deviceId">デバイスID</param>
        /// <param name="primaryKey">デバイスのプライマリキー</param>
        /// <param name="iotHubconnectionstring">IotHubの接続文字列</param>
        /// <returns>デバイス接続文字列</returns>
        public static string BuildDeviceConnectionString(string deviceId, string primaryKey, string iotHubconnectionstring)
        {
            var iotHubHostName = Utility.GetIoTHubHostNameFromConnectionString(iotHubconnectionstring);
            var deviceConnectionString = $"HostName={iotHubHostName};DeviceId={deviceId};SharedAccessKey={primaryKey}";
            return deviceConnectionString;
        }

        /// <summary>
        /// JSON形式の文字列かどうかを検証
        /// </summary>
        /// <param name="strInput">検証する文字列</param>
        /// <returns>JSON形式の場合true</returns>
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
