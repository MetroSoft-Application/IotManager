namespace IotManager.Settings
{
    /// <summary>
    /// IoTHub接続とデバイス操作に関する設定
    /// </summary>
    public class IoTHubSettings
    {
        /// <summary>
        /// IoTHubのサービス側接続文字列
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// ダイレクトメソッドの応答待機時間秒
        /// </summary>
        public int DirectMethodTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// デバイス接続開始時のタイムアウト秒
        /// </summary>
        public int DeviceConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 各種操作のタイムアウト秒
        /// </summary>
        public int OperationTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 通信失敗時の再試行回数
        /// </summary>
        public int RetryCount { get; set; } = 5;

        /// <summary>
        /// 再試行間の待機時間秒
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 2;

        /// <summary>
        /// トランスポート種別
        /// `Mqtt_WebSocket_Only` `Amqp_WebSocket_Only` `Mqtt` `Amqp` `Http1` を指定可能
        /// </summary>
        public string TransportType { get; set; } = "Mqtt_WebSocket_Only";
    }
}
