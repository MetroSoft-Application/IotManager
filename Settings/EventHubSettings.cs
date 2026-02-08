namespace IotManager.Settings
{
    /// <summary>
    /// Event Hub設定
    /// </summary>
    public class EventHubSettings
    {
        /// <summary>
        /// Event Hub接続文字列
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Storage接続文字列
        /// </summary>
        public string StorageConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Storageコンテナ名
        /// </summary>
        public string StorageContainerName { get; set; } = string.Empty;
    }
}
