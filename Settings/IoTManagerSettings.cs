namespace IotManager.Settings
{
    /// <summary>
    /// アプリケーション設定のルートオブジェクト
    /// </summary>
    public class IoTManagerSettings
    {
        /// <summary>
        /// IoTHub関連の設定
        /// </summary>
        public IoTHubSettings IoTHub { get; set; } = new IoTHubSettings();

        /// <summary>
        /// EventHub関連の設定
        /// </summary>
        public EventHubSettings EventHub { get; set; } = new EventHubSettings();
    }
}
