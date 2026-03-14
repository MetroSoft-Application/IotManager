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
    /// EventHub受信処理とクラウド発デバイスメッセージ送信を管理する
    /// </summary>
    public class HubManager : IDisposable
    {
        /// <summary>
        /// EventHub関連の設定
        /// </summary>
        private readonly EventHubSettings eventHubSettings;
        /// <summary>
        /// IoTHub関連の設定
        /// </summary>
        private readonly IoTHubSettings ioTHubSettings;
        /// <summary>
        /// EventHub名
        /// </summary>
        private readonly string eventHubName;
        /// <summary>
        /// EventHub受信プロセッサー
        /// </summary>
        private EventProcessorClient processorClient;
        /// <summary>
        /// クラウド発デバイスメッセージ送信用クライアント
        /// </summary>
        private ServiceClient serviceClient;

        /// <summary>
        /// Hubメッセージ受信時に通知するイベント
        /// </summary>
        public event Func<string, Task> OnHubMessageReceived;

        /// <summary>
        /// <see cref="HubManager" /> クラスの新しいインスタンスを初期化する
        /// </summary>
        /// <param name="eventHubSettings">EventHub受信に使用する設定</param>
        /// <param name="ioTHubSettings">IoTHub送信に使用する設定</param>
        public HubManager(EventHubSettings eventHubSettings, IoTHubSettings ioTHubSettings)
        {
            this.eventHubSettings = eventHubSettings;
            this.ioTHubSettings = ioTHubSettings;
            eventHubName = Utility.GetEntityPathFromConnectionString(eventHubSettings.ConnectionString);
        }

        /// <summary>
        /// EventHubの受信処理を開始する
        /// </summary>
        /// <returns>開始処理完了を表すタスク</returns>
        public async Task StartEventHubProcessingAsync()
        {
            if (processorClient is null && !string.IsNullOrWhiteSpace(eventHubSettings.ConnectionString) && !string.IsNullOrWhiteSpace(eventHubSettings.StorageConnectionString))
            {
                var blobContainerClient = new BlobContainerClient(eventHubSettings.StorageConnectionString, eventHubSettings.StorageContainerName);
                blobContainerClient.CreateIfNotExists();

                processorClient = new EventProcessorClient(blobContainerClient, EventHubConsumerClient.DefaultConsumerGroupName, eventHubSettings.ConnectionString, eventHubName);

                processorClient.ProcessEventAsync += ProcessEventHandler;
                processorClient.ProcessErrorAsync += ProcessErrorHandler;

                await processorClient.StartProcessingAsync();
            }
        }

        /// <summary>
        /// EventHubの受信処理を停止する
        /// </summary>
        /// <returns>停止処理完了を表すタスク</returns>
        public async Task StopEventHubProcessingAsync()
        {
            if (processorClient != null)
            {
                await processorClient.StopProcessingAsync();
            }
        }

        /// <summary>
        /// クラウドから指定デバイスへメッセージを送信する
        /// </summary>
        /// <param name="deviceId">メッセージ送信先のデバイスID</param>
        /// <param name="message">送信するメッセージ本文</param>
        /// <returns>送信完了を表すタスク</returns>
        public async Task SendCloudToDeviceMessageAsync(string deviceId, string message)
        {
            serviceClient = ServiceClient.CreateFromConnectionString(ioTHubSettings.ConnectionString);
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        /// <summary>
        /// EventHubからイベントを受信した際の処理を実行する
        /// </summary>
        /// <param name="eventArgs">受信イベントに関する情報</param>
        /// <returns>イベント処理完了を表すタスク</returns>
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
        /// EventHub処理中のエラー通知を受け取る
        /// </summary>
        /// <param name="eventArgs">エラー発生時のイベント情報</param>
        /// <returns>完了済みタスク</returns>
        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 管理中の通信リソースを解放する
        /// </summary>
        public void Dispose()
        {
            serviceClient?.Dispose();
            processorClient?.StopProcessingAsync().GetAwaiter().GetResult();
        }
    }
}