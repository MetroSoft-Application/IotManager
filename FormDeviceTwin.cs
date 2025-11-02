using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;

namespace IotManager
{
    public partial class FormDeviceTwin : Form
    {
        private readonly string iotHubConnectionString;
        private RegistryManager registryManager;

        // JSONシンタックスハイライト用のスタイル
        private TextStyle keywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private TextStyle stringStyle = new TextStyle(Brushes.Brown, null, FontStyle.Regular);
        private TextStyle numberStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);
        private TextStyle booleanStyle = new TextStyle(Brushes.DarkCyan, null, FontStyle.Bold);

        // SQLシンタックスハイライト用のスタイル
        private TextStyle sqlStringStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);
        private TextStyle sqlFunctionStyle = new TextStyle(Brushes.Magenta, null, FontStyle.Regular);

        public FormDeviceTwin(string connectionString)
        {
            InitializeComponent();
            iotHubConnectionString = connectionString;
            registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            txtSQL.Language = Language.SQL;

            // デフォルトのSQLクエリを設定
            txtSQL.Text =
@"SELECT
    deviceId
    , status
    , statusUpdateTime
    , connectionState
    , lastActivityTime
    , cloudToDeviceMessageCount
    , authenticationType
    , version
    , properties.desired
    , properties.reported
FROM
    devices
";

            // JSONシンタックスハイライトのイベントハンドラを設定
            txtTwinStatus.TextChanged += TxtTwinStatus_TextChanged;

            // SQLシンタックスハイライトのイベントハンドラを設定
            txtSQL.TextChanged += TxtSQL_TextChanged;
        }

        private void TxtSQL_TextChanged(object sender, TextChangedEventArgs e)
        {
            // SQLシンタックスハイライトを適用
            e.ChangedRange.ClearStyle(sqlStringStyle, sqlFunctionStyle);

            // 文字列
            e.ChangedRange.SetStyle(sqlStringStyle, @"'[^']*'");

            // デバイス関連キーワード
            e.ChangedRange.SetStyle(sqlFunctionStyle, @"\b(devices|deviceId|properties|tags|reported|desired|connectionState|status|lastActivityTime)\b", RegexOptions.IgnoreCase);
        }

        private void TxtTwinStatus_TextChanged(object sender, TextChangedEventArgs e)
        {
            // JSONシンタックスハイライトを適用
            e.ChangedRange.ClearStyle(keywordStyle, stringStyle, numberStyle, booleanStyle);

            // 文字列 (キーと値)
            e.ChangedRange.SetStyle(stringStyle, "\".*?\"", RegexOptions.Singleline);

            // 数値
            e.ChangedRange.SetStyle(numberStyle, @"\b\d+\.?\d*\b");

            // Boolean値とnull
            e.ChangedRange.SetStyle(booleanStyle, @"\b(true|false|null)\b");

            // キー (括弧とコロン)
            e.ChangedRange.SetStyle(keywordStyle, @"[{}[\]:]");
        }

        private async void Exec_Click(object sender, EventArgs e)
        {
            try
            {
                txtTwinStatus.Clear();

                var sqlQuery = txtSQL.Text.Trim();
                if (string.IsNullOrEmpty(sqlQuery))
                {
                    MessageBox.Show("SQLクエリを入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Device Twin クエリを実行
                var query = registryManager.CreateQuery(sqlQuery);

                while (query.HasMoreResults)
                {
                    var page = await query.GetNextAsJsonAsync();
                    foreach (var twin in page)
                    {
                        // JSONを整形して表示
                        var formattedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(twin), Formatting.Indented);
                        txtTwinStatus.AppendText(formattedJson);
                        txtTwinStatus.AppendText("\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtTwinStatus.Clear();
                txtTwinStatus.AppendText($"エラー:\r\n{ex.Message}");
            }
        }

        private void FormDeviceTwin_FormClosing(object sender, FormClosingEventArgs e)
        {
            registryManager?.Dispose();
        }
    }
}
