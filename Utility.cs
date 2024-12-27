using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;

namespace IotManager
{
    /// <summary>
    /// ユーティリティクラス
    /// </summary>
    public static class Utility
    {
        public static readonly IConfiguration Configuration;

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
        /// <param name="connectionString">接続文字列</param>
        /// <returns>IoT Hubのホスト名</returns>
        public static string GetIoTHubHostNameFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
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
        /// <param name="connectionString">接続文字列</param>
        /// <returns>EntityPath</returns>
        public static string GetEntityPathFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
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
        /// <param name="device">デバイス情報</param>
        /// <param name="connectionstring">接続文字列</param>
        /// <returns>デバイス接続文字列</returns>
        public static string BuildDeviceConnectionString(string deviceId, Device device, string connectionstring)
        {
            var primaryKey = device.Authentication.SymmetricKey.PrimaryKey;
            var iotHubHostName = Utility.GetIoTHubHostNameFromConnectionString(connectionstring);
            var deviceConnectionString = $"HostName={iotHubHostName};DeviceId={deviceId};SharedAccessKey={primaryKey}";
            return deviceConnectionString;
        }
    }
}
