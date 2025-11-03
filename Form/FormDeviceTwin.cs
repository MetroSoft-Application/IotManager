using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;

namespace IotManager
{
    public partial class FormDeviceTwin : System.Windows.Forms.Form
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
        private TextStyle sqlCommentStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);

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
--WHERE
    --devices.deviceId = ''
";

            // JSONシンタックスハイライトのイベントハンドラを設定
            txtTwinStatus.TextChanged += TxtTwinStatus_TextChanged;

            // SQLシンタックスハイライトのイベントハンドラを設定
            txtSQL.TextChanged += TxtSQL_TextChanged;
        }

        private void TxtSQL_TextChanged(object sender, TextChangedEventArgs e)
        {
            // SQLシンタックスハイライトを適用
            e.ChangedRange.ClearStyle(sqlStringStyle, sqlFunctionStyle, sqlCommentStyle);

            // コメント (--以降)
            e.ChangedRange.SetStyle(sqlCommentStyle, @"--.*$", RegexOptions.Multiline);

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

        /// <summary>
        /// SQLクエリから--以降のコメントを除去
        /// </summary>
        /// <param name="sqlQuery">元のSQLクエリ</param>
        /// <returns>コメントを除去したSQLクエリ</returns>
        private string RemoveComments(string sqlQuery)
        {
            var lines = sqlQuery.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var processedLines = new List<string>();

            foreach (var line in lines)
            {
                // --の位置を探す
                var commentIndex = line.IndexOf("--");
                if (commentIndex >= 0)
                {
                    // --より前の部分のみを取得
                    var lineWithoutComment = line.Substring(0, commentIndex).TrimEnd();
                    if (!string.IsNullOrWhiteSpace(lineWithoutComment))
                    {
                        processedLines.Add(lineWithoutComment);
                    }
                }
                else
                {
                    // コメントがない行はそのまま追加
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        processedLines.Add(line);
                    }
                }
            }

            return string.Join(" ", processedLines);
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

                // コメント行を除去(--以降をコメントとして処理)
                sqlQuery = RemoveComments(sqlQuery);

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
