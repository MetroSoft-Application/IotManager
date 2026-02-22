using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FastColoredTextBoxNS;
using System.Text.RegularExpressions;
using System.Data;

namespace IotManager
{
    public partial class FormDeviceTwin : System.Windows.Forms.Form
    {
        private readonly string iotHubConnectionString;
        private RegistryManager registryManager;
        private DataTable twinDataTable;

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
#if !NET6_0_OR_GREATER
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
#endif
            InitializeComponent();
            iotHubConnectionString = connectionString;
            registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            txtSQL.Language = Language.SQL;

            // DataTableの初期化
            twinDataTable = new DataTable();
            dgvDeviceTwin.DataSource = twinDataTable;

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
                twinDataTable.Clear();
                twinDataTable.Columns.Clear();

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
                var allResults = new List<JObject>();

                while (query.HasMoreResults)
                {
                    var page = await query.GetNextAsJsonAsync();
                    foreach (var twin in page)
                    {
                        var jObject = JObject.Parse(twin);
                        allResults.Add(jObject);

                        // JSONを整形して表示
                        var formattedJson = JsonConvert.SerializeObject(jObject, Formatting.Indented);
                        txtTwinStatus.AppendText(formattedJson);
                        txtTwinStatus.AppendText("\r\n");
                    }
                }

                // DataGridViewにマッピング
                if (allResults.Count > 0)
                {
                    var hasOrderBy = Regex.IsMatch(sqlQuery, @"\bORDER\s+BY\b", RegexOptions.IgnoreCase);
                    var displayResults = hasOrderBy
                        ? allResults
                        : allResults.OrderBy(j => j["deviceId"]?.ToString() ?? string.Empty).ToList();
                    MapJsonToDataTable(displayResults);
                }
            }
            catch (Exception ex)
            {
                txtTwinStatus.Clear();
                txtTwinStatus.AppendText($"エラー:\r\n{ex.Message}");
            }
        }

        /// <summary>
        /// JSON結果をDataTableに自動マッピング
        /// </summary>
        private void MapJsonToDataTable(List<JObject> jsonObjects)
        {
            if (jsonObjects == null || jsonObjects.Count == 0)
                return;

            // 優先順位付きカラム定義
            var priorityColumns = new List<string>
            {
                "deviceId",
                "etag",
                "deviceEtag",
                "status",
                "statusUpdateTime",
                "connectionState",
                "lastActivityTime",
                "cloudToDeviceMessageCount",
                "authenticationType",
                "x509Thumbprint",
                "modelId",
                "version",
                "properties",
                "capabilities"
            };

            // すべてのJSONオブジェクトからユニークなプロパティを収集
            var allProperties = new HashSet<string>();
            foreach (var jObj in jsonObjects)
            {
                FlattenJson(jObj, "", allProperties);
            }

            // カラムを優先順位に従って作成
            var orderedProperties = new List<string>();

            // 優先カラムを先に追加
            foreach (var priorityCol in priorityColumns)
            {
                var matchingProps = allProperties.Where(p => p.Equals(priorityCol, StringComparison.OrdinalIgnoreCase) || p.StartsWith(priorityCol + ".", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p).ToList();
                foreach (var prop in matchingProps)
                {
                    if (!orderedProperties.Contains(prop))
                    {
                        orderedProperties.Add(prop);
                    }
                }
            }

            // 残りのカラムを最後尾に追加
            foreach (var prop in allProperties.OrderBy(p => p))
            {
                if (!orderedProperties.Contains(prop))
                {
                    orderedProperties.Add(prop);
                }
            }

            // DataTableにカラムを追加
            foreach (var property in orderedProperties)
            {
                twinDataTable.Columns.Add(property, typeof(string));
            }

            // データ行を追加
            foreach (var jObj in jsonObjects)
            {
                var row = twinDataTable.NewRow();
                var flatData = new Dictionary<string, string>();
                FlattenJsonToDict(jObj, "", flatData);

                foreach (var kvp in flatData)
                {
                    if (twinDataTable.Columns.Contains(kvp.Key))
                    {
                        row[kvp.Key] = kvp.Value;
                    }
                }

                twinDataTable.Rows.Add(row);
            }
        }

        /// <summary>
        /// JSONをフラット化してプロパティ名を収集
        /// </summary>
        private void FlattenJson(JToken token, string prefix, HashSet<string> properties)
        {
            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    var propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                    if (property.Value is JObject || property.Value is JArray)
                    {
                        // ネストされたオブジェクトや配列は文字列として扱う
                        properties.Add(propertyName);
                    }
                    else
                    {
                        properties.Add(propertyName);
                    }
                }
            }
        }

        /// <summary>
        /// JSONをフラット化して辞書に格納
        /// </summary>
        private void FlattenJsonToDict(JToken token, string prefix, Dictionary<string, string> result)
        {
            if (token is JObject jObject)
            {
                foreach (var property in jObject.Properties())
                {
                    var propertyName = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                    if (property.Value is JObject || property.Value is JArray)
                    {
                        // ネストされたオブジェクトや配列は文字列として表示
                        result[propertyName] = property.Value.ToString(Formatting.None);
                    }
                    else if (property.Value.Type == JTokenType.Null)
                    {
                        result[propertyName] = "";
                    }
                    else
                    {
                        result[propertyName] = property.Value.ToString();
                    }
                }
            }
        }

        private void FormDeviceTwin_FormClosing(object sender, FormClosingEventArgs e)
        {
            registryManager?.Dispose();
        }
    }
}
