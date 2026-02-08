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

        /// <summary>
        /// デバイス接続タイムアウト（秒）
        /// </summary>
        public int DeviceConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 操作タイムアウト（秒）
        /// </summary>
        public int OperationTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 再試行回数
        /// </summary>
        public int RetryCount { get; set; } = 5;

        /// <summary>
        /// 再試行間隔（秒）
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 2;

        /// <summary>
        /// トランスポートタイプ (Mqtt_WebSocket_Only, Amqp_WebSocket_Only, Mqtt, Amqp, Http1)
        /// </summary>
        public string TransportType { get; set; } = "Mqtt_WebSocket_Only";
    }
}
