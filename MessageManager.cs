using System.Text;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace IotManager
{
    public class MessageManager
    {
        //private static async Task OnMessageReceived(Microsoft.Azure.Devices.Client.Message receivedMessage, object userContext)
        //{
        //    var messageBytes = receivedMessage.GetBytes();
        //    var messageText = Encoding.UTF8.GetString(messageBytes);
        //    Console.WriteLine($"Received Message: {messageText}");

        //    // メッセージを完了としてマーク
        //    await deviceClient.CompleteAsync(receivedMessage);
        //}
    }
}
