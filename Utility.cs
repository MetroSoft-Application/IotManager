using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration;

namespace IotManager
{
    public static class Utility
    {
        public static readonly IConfiguration Configuration;

        static Utility() 
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string GetIoTHubHostNameFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("HostName=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring("HostName=".Length);
                }
            }
            throw new ArgumentException("Invalid connection string, no HostName found.");
        }

        public static string BuildDeviceConnectionString(string deviceId, Device device ,string connectionstring)
        {
            var primaryKey = device.Authentication.SymmetricKey.PrimaryKey;
            var iotHubHostName = Utility.GetIoTHubHostNameFromConnectionString(connectionstring);
            var deviceConnectionString = $"HostName={iotHubHostName};DeviceId={deviceId};SharedAccessKey={primaryKey}";
            return deviceConnectionString;
        }
    }
}
