using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Azure.Devices;
using IotManager.Settings;

namespace IotManager.Hub
{
    /// <summary>
    /// Hub操作を管理するクラス
    /// </summary>
    public class HubManager
    {
        private readonly EventHubSettings settings;
        private readonly string eventHubName;
        private EventProcessorClient processorClient;

        public event Func<string, Task> OnHubMessageReceived;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="settings">Event Hub設定</param>
        public HubManager(EventHubSettings settings)
        {
            this.settings = settings;
            eventHubName = Utility.GetEntityPathFromConnectionString(settings.ConnectionString);
        }

        /// <summary>
        /// Event Hub処理を開始
        /// </summary>
        public async Task StartEventHubProcessingAsync()
        {
            if (processorClient is null && !string.IsNullOrWhiteSpace(settings.ConnectionString) && !string.IsNullOrWhiteSpace(settings.StorageConnectionString))
            {
                var blobContainerClient = new BlobContainerClient(settings.StorageConnectionString, settings.StorageContainerName);
                blobContainerClient.CreateIfNotExists();

                processorClient = new EventProcessorClient(blobContainerClient, EventHubConsumerClient.DefaultConsumerGroupName, settings.ConnectionString, eventHubName);

                processorClient.ProcessEventAsync += ProcessEventHandler;
                processorClient.ProcessErrorAsync += ProcessErrorHandler;

                await processorClient.StartProcessingAsync();
            }
        }

        /// <summary>
        /// Event Hub処理を停止
        /// </summary>
        public async Task StopEventHubProcessingAsync()
        {
            if (processorClient != null)
            {
                await processorClient.StopProcessingAsync();
            }
        }

        /// <summary>
        /// クラウドからデバイスにメッセージを送信
        /// </summary>
        /// <param name="deviceId">メッセージ送信先のデバイス ID</param>
        /// <param name="message">送信するメッセージ本文</param>
        public async Task SendCloudToDeviceMessageAsync(string deviceId, string message)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        /// <summary>
        /// Event を受信した時のハンドラ
        /// </summary>
        /// <param name="eventArgs">処理中のイベント情報</param>
        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            var message = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            if (OnHubMessageReceived != null)
            {
                await OnHubMessageReceived($"[Message][{timestamp}] {message}");
            }

            await eventArgs.UpdateCheckpointAsync();
        }

        /// <summary>
        /// エラー発生時のハンドラ
        /// </summary>
        /// <param name="eventArgs">エラー発生時のイベント情報</param>
        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }
    }
}