namespace IotManager.Settings
{
    /// <summary>
    /// IoTマネージャーの設定を管理するクラス
    /// </summary>
    public class IoTManagerSettings
    {
        /// <summary>
        /// IoT Hub関連の設定
        /// </summary>
        public IoTHubSettings IoTHub { get; set; } = new IoTHubSettings();

        /// <summary>
        /// Event Hub関連の設定
        /// </summary>
        public EventHubSettings EventHub { get; set; } = new EventHubSettings();
    }
}
