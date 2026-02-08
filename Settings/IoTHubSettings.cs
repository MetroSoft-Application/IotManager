namespace IotManager.Settings
{
    /// <summary>
    /// IoT Hub設定
    /// </summary>
    public class IoTHubSettings
    {
        /// <summary>
        /// IoT Hub接続文字列
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// ダイレクトメソッドのタイムアウト（秒）
        /// </summary>
        public int DirectMethodTimeoutSeconds { get; set; } = 300;
    }
}
