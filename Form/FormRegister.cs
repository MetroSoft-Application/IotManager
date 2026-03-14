using System.Data;
using Microsoft.Azure.Devices;

namespace IotManager.Form
{
    /// <summary>
    /// CSVまたはTSVからデバイスを一括登録する画面
    /// </summary>
    public partial class FormRegister : System.Windows.Forms.Form
    {
        /// <summary>
        /// IoTHub接続文字列
        /// </summary>
        private readonly string iotHubConnectionString;
        /// <summary>
        /// デバイス登録に使用するレジストリマネージャー
        /// </summary>
        private RegistryManager registryManager;
        /// <summary>
        /// 画面表示用のデバイス一覧テーブル
        /// </summary>
        private DataTable deviceTable;

        /// <summary>
        /// <see cref="FormRegister" /> クラスの新しいインスタンスを初期化する
        /// </summary>
        /// <param name="iotHubConnectionString">デバイス登録に使用するIoTHub接続文字列</param>
        public FormRegister(string iotHubConnectionString)
        {
#if !NET6_0_OR_GREATER
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
#endif
            InitializeComponent();
            this.iotHubConnectionString = iotHubConnectionString;

            InitializeDataTable();
            InitializeEventHandlers();
        }

        /// <summary>
        /// 必要に応じて <see cref="RegistryManager" /> を初期化する
        /// </summary>
        private void EnsureRegistryManager()
        {
            if (registryManager == null)
            {
                registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            }
        }

        /// <summary>
        /// 一覧表示に使用する <see cref="DataTable" /> を初期化する
        /// </summary>
        private void InitializeDataTable()
        {
            deviceTable = new DataTable();
            // カラムはCSV/TSV読み込み時に動的に生成
            dataGridView1.DataSource = deviceTable;
        }

        /// <summary>
        /// 画面上のイベント ハンドラーを関連付ける
        /// </summary>
        private void InitializeEventHandlers()
        {
            btnLoad.Click += BtnLoad_Click;
            btnExec.Click += BtnExec_Click;
        }

        /// <summary>
        /// デバイス一覧ファイルを選択して読み込む
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private async void BtnLoad_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV Files (*.csv)|*.csv|TSV Files (*.tsv)|*.tsv|All Files (*.*)|*.*";
                openFileDialog.Title = "Select Device List File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        await LoadDeviceFileAsync(openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 指定ファイルからデバイス一覧を読み込み表形式へ反映する
        /// </summary>
        /// <param name="filePath">読み込むCSVまたはTSVファイルのパス</param>
        /// <returns>ファイル読み込み処理を表すタスク</returns>
        private async Task LoadDeviceFileAsync(string filePath)
        {
            deviceTable.Clear();
            deviceTable.Columns.Clear();

            var extension = Path.GetExtension(filePath).ToLower();
            var delimiter = extension == ".tsv" ? '\t' : ',';

#if NET6_0_OR_GREATER
            var lines = await File.ReadAllLinesAsync(filePath);
#else
            var lines = await Task.Run(() => File.ReadAllLines(filePath));
#endif
            if (lines.Length < 2)
            {
                MessageBox.Show("File must contain at least a header row and one data row.", "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Parse header
            var headers = lines[0].Split(delimiter).Select(h => h.Trim()).ToArray();
            var deviceIdIndex = Array.FindIndex(headers, h => h.Equals("DeviceId", StringComparison.OrdinalIgnoreCase));
            var statusIndex = Array.FindIndex(headers, h => h.Equals("Status", StringComparison.OrdinalIgnoreCase));

            if (deviceIdIndex == -1 || statusIndex == -1)
            {
                MessageBox.Show("CSV/TSV must contain 'DeviceId' and 'Status' columns.", "Invalid Format", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 動的にカラムを生成
            deviceTable.Columns.Add("Selected", typeof(bool));
            foreach (var header in headers)
            {
                deviceTable.Columns.Add(header, typeof(string));
            }
            deviceTable.Columns.Add("RegistrationStatus", typeof(string));

            // カラム幅設定
            dataGridView1.Columns["Selected"].Width = 50;
            if (dataGridView1.Columns.Contains("RegistrationStatus"))
            {
                dataGridView1.Columns["RegistrationStatus"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(delimiter).Select(v => v.Trim()).ToArray();

                if (values.Length < headers.Length)
                    continue;

                var deviceId = values[deviceIdIndex];
                var status = values[statusIndex];

                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                // Validate status
                if (!status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) &&
                    !status.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Row {i + 1}: Status must be 'Enabled' or 'Disabled'. Found: '{status}'", "Invalid Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                // 行データ作成
                var row = deviceTable.NewRow();
                row["Selected"] = true;
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    row[headers[j]] = values[j];
                }
                row["RegistrationStatus"] = "待機中";
                deviceTable.Rows.Add(row);
            }

            MessageBox.Show($"Loaded {deviceTable.Rows.Count} devices.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 選択されたデバイスをIoTHubへ一括登録する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private async void BtnExec_Click(object sender, EventArgs e)
        {
            try
            {
                EnsureRegistryManager();
            }
            catch (Exception ex)
            {
                return;
            }

            var selectedRows = deviceTable.AsEnumerable().Where(row => row.Field<bool>("Selected")).ToList();

            if (selectedRows.Count == 0)
            {
                MessageBox.Show("No devices selected.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnExec.Enabled = false;
            btnLoad.Enabled = false;

            try
            {
                int successCount = 0;
                int failureCount = 0;

                foreach (var row in selectedRows)
                {
                    var deviceId = row.Field<string>("DeviceId");
                    var status = row.Field<string>("Status");

                    try
                    {
                        var device = new Microsoft.Azure.Devices.Device(deviceId)
                        {
                            Status = status.Equals("Enabled", StringComparison.OrdinalIgnoreCase)
                                ? DeviceStatus.Enabled
                                : DeviceStatus.Disabled
                        };

                        await registryManager.AddDeviceAsync(device);
                        row.SetField("RegistrationStatus", "✓ 成功");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        row.SetField("RegistrationStatus", $"✗ 失敗: {ex.Message}");
                        failureCount++;
                    }

                    dataGridView1.Refresh();
                    await Task.Delay(100); // Rate limiting
                }

                MessageBox.Show($"Registration completed.\nSuccess: {successCount}\nFailed: {failureCount}",
                    "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                btnExec.Enabled = true;
                btnLoad.Enabled = true;
            }
        }

        /// <summary>
        /// 管理中のコンポーネントおよびIoTHub接続リソースを解放する
        /// </summary>
        /// <param name="disposing">マネージドリソースも解放する場合は <see langword="true" /></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                registryManager?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
