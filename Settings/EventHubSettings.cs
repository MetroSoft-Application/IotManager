namespace IotManager.Settings
{
    /// <summary>
    /// EventHub受信処理に関する設定
    /// </summary>
    public class EventHubSettings
    {
        /// <summary>
        /// EventHub接続文字列
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// チェックポイント保存先のStorage接続文字列
        /// </summary>
        public string StorageConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// チェックポイント保存に使用するStorageコンテナー名
        /// </summary>
        public string StorageContainerName { get; set; } = string.Empty;
    }
}
