using System.Windows.Forms;
using IotManager.Device;
using IotManager.Hub;
using IotManager.Form;
using IotManager.Settings;
using Microsoft.Extensions.Configuration;

namespace IotManager
{
    /// <summary>
    /// IoTHubとデバイス操作を統合して管理するメイン画面
    /// </summary>
    public partial class Form1 : System.Windows.Forms.Form
    {
        /// <summary>
        /// アプリケーション設定
        /// </summary>
        private readonly IoTManagerSettings settings;
        /// <summary>
        /// IoTHub接続文字列変更後の再取得待機タイマー
        /// </summary>
        private readonly System.Windows.Forms.Timer iotHubConnectionChangedTimer;
        /// <summary>
        /// デバイス操作管理オブジェクト
        /// </summary>
        private DeviceManager deviceManager;
        /// <summary>
        /// Hub操作管理オブジェクト
        /// </summary>
        private HubManager hubManager;
        /// <summary>
        /// デバイス接続中かどうかを示すフラグ
        /// </summary>
        private bool isDeviceOpen = false;
        /// <summary>
        /// Hub受信処理が開始済みかどうかを示すフラグ
        /// </summary>
        private bool isIotHubOpen = false;
        /// <summary>
        /// 受信ログの最大保持行数
        /// </summary>
        private const int MAX_LINE = 50000;
        /// <summary>
        /// デバイス操作側で使用中のIoTHub接続文字列
        /// </summary>
        private string currentDeviceIoTHubConnectionString = string.Empty;
        /// <summary>
        /// Hub操作側で使用中のIoTHub接続文字列
        /// </summary>
        private string currentHubIoTHubConnectionString = string.Empty;
        /// <summary>
        /// 使用中のEventHub接続文字列
        /// </summary>
        private string currentEventHubConnectionString = string.Empty;
        /// <summary>
        /// 使用中のStorage接続文字列
        /// </summary>
        private string currentStorageConnectionString = string.Empty;

        /// <summary>
        /// <see cref="Form1" /> クラスの新しいインスタンスを初期化する
        /// </summary>
        public Form1()
        {
#if !NET6_0_OR_GREATER
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
#endif
            InitializeComponent();

            // 設定をバインド
            settings = new IoTManagerSettings();
            Utility.Configuration.Bind(settings);

            txtIotHubConnectionString.Text = settings.IoTHub.ConnectionString;
            txtEventHubConnectionString.Text = settings.EventHub.ConnectionString;
            txtStorageConnectionString.Text = settings.EventHub.StorageConnectionString;

            iotHubConnectionChangedTimer = new System.Windows.Forms.Timer { Interval = 500 };
            iotHubConnectionChangedTimer.Tick += IotHubConnectionChangedTimer_Tick;

            txtIotHubConnectionString.TextChanged += txtIotHubConnectionString_TextChanged;
            txtEventHubConnectionString.TextChanged += txtEventHubRelatedConnectionString_TextChanged;
            txtStorageConnectionString.TextChanged += txtEventHubRelatedConnectionString_TextChanged;
        }

        /// <summary>
        /// デバイス接続に関する内部状態と画面表示を初期状態へ戻す
        /// </summary>
        private void ResetDeviceState()
        {
            deviceManager = null;
            currentDeviceIoTHubConnectionString = string.Empty;
            isDeviceOpen = false;
            btnDevicerOpen.Text = "Open";
            btnDeviceSend.Enabled = false;
            cmbDeviceId.Enabled = true;
            cmbDeviceId.Items.Clear();
        }

        /// <summary>
        /// Hub 接続に関する内部状態と関連ボタンの状態を初期化する
        /// </summary>
        private void ResetHubState()
        {
            hubManager?.Dispose();
            hubManager = null;
            currentHubIoTHubConnectionString = string.Empty;
            currentEventHubConnectionString = string.Empty;
            currentStorageConnectionString = string.Empty;
            isIotHubOpen = false;
            btnHubOpen.Text = "Open";
            btnHubSend.Enabled = false;
            btnDirectMethod.Enabled = false;
            btnDeviceTwin.Enabled = false;
            txtIotHubConnectionString.ReadOnly = false;
            txtEventHubConnectionString.ReadOnly = false;
            txtStorageConnectionString.ReadOnly = false;
        }

        /// <summary>
        /// IoTHub接続文字列の変更を検知し関連状態をリセットしてデバイス一覧の再取得を予約する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private void txtIotHubConnectionString_TextChanged(object sender, EventArgs e)
        {
            ResetDeviceState();
            ResetHubState();

            iotHubConnectionChangedTimer.Stop();
            iotHubConnectionChangedTimer.Start();
        }

        /// <summary>
        /// EventHubまたはStorage接続文字列の変更を検知しHub関連状態のみをリセットする
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private void txtEventHubRelatedConnectionString_TextChanged(object sender, EventArgs e)
        {
            ResetHubState();
        }

        /// <summary>
        /// 入力停止後にIoTHubへ再接続しデバイス一覧を再読み込みする
        /// </summary>
        /// <param name="sender">イベント送信元のタイマー</param>
        /// <param name="e">イベントデータ</param>
        private async void IotHubConnectionChangedTimer_Tick(object sender, EventArgs e)
        {
            iotHubConnectionChangedTimer.Stop();
            await EnsureDeviceManagerAsync(loadDeviceIds: true, showError: false);
        }

        /// <summary>
        /// 画面入力値を現在の設定オブジェクトへ反映する
        /// </summary>
        private void SyncSettingsFromInputs()
        {
            settings.IoTHub.ConnectionString = txtIotHubConnectionString.Text.Trim();
            settings.EventHub.ConnectionString = txtEventHubConnectionString.Text.Trim();
            settings.EventHub.StorageConnectionString = txtStorageConnectionString.Text.Trim();
        }

        /// <summary>
        /// 必要に応じて <see cref="DeviceManager" /> を初期化し要求に応じてデバイス一覧を読み込む
        /// </summary>
        /// <param name="loadDeviceIds">初期化後にデバイス一覧を再取得する場合は <see langword="true" /></param>
        /// <param name="showError">初期化失敗時にエラーダイアログを表示する場合は <see langword="true" /></param>
        /// <returns>初期化に成功した場合は <see langword="true" /> 失敗した場合は <see langword="false" /></returns>
        private async Task<bool> EnsureDeviceManagerAsync(bool loadDeviceIds = false, bool showError = true)
        {
            SyncSettingsFromInputs();

            if (string.IsNullOrWhiteSpace(settings.IoTHub.ConnectionString))
            {
                if (showError)
                {
                    MessageBox.Show("IoT Hub 接続文字列を入力してください。");
                }

                return false;
            }

            if (deviceManager == null || !string.Equals(currentDeviceIoTHubConnectionString, settings.IoTHub.ConnectionString, StringComparison.Ordinal))
            {
                try
                {
                    deviceManager = new DeviceManager(settings.IoTHub);
                    deviceManager.OnMessageReceived += OnDeviceMessageReceived;
                    deviceManager.OnDirectMethodReceived += OnDirectMethodReceived;
                    currentDeviceIoTHubConnectionString = settings.IoTHub.ConnectionString;
                    isDeviceOpen = false;
                    btnDevicerOpen.Text = "Open";
                    btnDeviceSend.Enabled = false;
                    cmbDeviceId.Enabled = true;
                }
                catch (Exception ex)
                {
                    deviceManager = null;
                    cmbDeviceId.Items.Clear();

                    if (showError)
                    {
                        MessageBox.Show($"IoT Hub 接続の初期化に失敗しました: {ex.Message}");
                    }

                    return false;
                }
            }

            if (loadDeviceIds)
            {
                await LoadDeviceIds(showError);
            }

            return true;
        }

        /// <summary>
        /// 必要に応じて <see cref="HubManager" /> を初期化する
        /// </summary>
        /// <param name="showError">初期化失敗時にエラーダイアログを表示する場合は <see langword="true" /></param>
        /// <returns>初期化に成功した場合は <see langword="true" /> 失敗した場合は <see langword="false" /></returns>
        private bool EnsureHubManager(bool showError = true)
        {
            SyncSettingsFromInputs();

            if (string.IsNullOrWhiteSpace(settings.IoTHub.ConnectionString) ||
                string.IsNullOrWhiteSpace(settings.EventHub.ConnectionString) ||
                string.IsNullOrWhiteSpace(settings.EventHub.StorageConnectionString))
            {
                if (showError)
                {
                    MessageBox.Show("IoT Hub / Event Hub / Storage の接続文字列を入力してください。");
                }

                return false;
            }

            if (hubManager == null ||
                !string.Equals(currentHubIoTHubConnectionString, settings.IoTHub.ConnectionString, StringComparison.Ordinal) ||
                !string.Equals(currentEventHubConnectionString, settings.EventHub.ConnectionString, StringComparison.Ordinal) ||
                !string.Equals(currentStorageConnectionString, settings.EventHub.StorageConnectionString, StringComparison.Ordinal))
            {
                try
                {
                    hubManager?.Dispose();
                    hubManager = new HubManager(settings.EventHub, settings.IoTHub);
                    hubManager.OnHubMessageReceived += OnHubMessageReceived;
                    currentHubIoTHubConnectionString = settings.IoTHub.ConnectionString;
                    currentEventHubConnectionString = settings.EventHub.ConnectionString;
                    currentStorageConnectionString = settings.EventHub.StorageConnectionString;
                    isIotHubOpen = false;
                    btnHubOpen.Text = "Open";
                    btnHubSend.Enabled = false;
                    btnDirectMethod.Enabled = false;
                    btnDeviceTwin.Enabled = false;
                }
                catch (Exception ex)
                {
                    hubManager = null;

                    if (showError)
                    {
                        MessageBox.Show($"Hub 接続の初期化に失敗しました: {ex.Message}");
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// デバイスからの受信メッセージを受信ログへ追記する
        /// </summary>
        /// <param name="message">表示対象の受信メッセージ</param>
        /// <returns>非同期 UI 更新処理を表すタスク</returns>
        private async Task OnDeviceMessageReceived(string message)
        {
            await Task.Run(() =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        rtxtDeviceReceive.AppendText($"{message}{Environment.NewLine}");
                        EnsureMaxLines(rtxtDeviceReceive, MAX_LINE);
                    }));
                }
                else
                {
                    rtxtDeviceReceive.AppendText($"{message}{Environment.NewLine}");
                    EnsureMaxLines(rtxtDeviceReceive, MAX_LINE);
                }
            });
        }

        /// <summary>
        /// Hubから受信したメッセージを受信ログへ追記する
        /// </summary>
        /// <param name="message">表示対象のHubメッセージ</param>
        /// <returns>非同期 UI 更新処理を表すタスク</returns>
        private async Task OnHubMessageReceived(string message)
        {
            await Task.Run(() =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        rtxtHubReceive.AppendText($"{message}{Environment.NewLine}");
                        EnsureMaxLines(rtxtHubReceive, MAX_LINE);
                    }));
                }
                else
                {
                    rtxtHubReceive.AppendText($"{message}{Environment.NewLine}");
                    EnsureMaxLines(rtxtHubReceive, MAX_LINE);
                }
            });
        }

        /// <summary>
        /// ダイレクトメソッド関連の受信内容をデバイスログへ追記する
        /// </summary>
        /// <param name="message">表示対象のメソッド受信メッセージ</param>
        /// <returns>非同期 UI 更新処理を表すタスク</returns>
        private async Task OnDirectMethodReceived(string message)
        {
            await Task.Run(() =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        rtxtDeviceReceive.AppendText($"{message}{Environment.NewLine}");
                        EnsureMaxLines(rtxtDeviceReceive, MAX_LINE);
                    }));
                }
                else
                {
                    rtxtDeviceReceive.AppendText($"{message}{Environment.NewLine}");
                    EnsureMaxLines(rtxtDeviceReceive, MAX_LINE);
                }
            });
        }

        /// <summary>
        /// IoTHubからデバイスID一覧を取得してコンボボックスへ反映する
        /// </summary>
        /// <param name="showError">取得失敗時にエラーダイアログを表示する場合は <see langword="true" /></param>
        /// <returns>非同期読み込み処理を表すタスク</returns>
        private async Task LoadDeviceIds(bool showError = true)
        {
            if (deviceManager == null)
            {
                cmbDeviceId.Items.Clear();
                return;
            }

            try
            {
                var deviceIds = await deviceManager.GetDeviceIdsAsync();
                cmbDeviceId.Items.Clear();

                if (deviceIds.Any())
                {
                    foreach (var deviceId in deviceIds)
                    {
                        cmbDeviceId.Items.Add(deviceId);
                    }

                    cmbDeviceId.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                cmbDeviceId.Items.Clear();

                if (showError)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        /// <summary>
        /// Hub接続の開始または停止を切り替える
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private async void btnHubOpen_Click(object sender, EventArgs e)
        {
            try
            {
                if (!EnsureHubManager())
                {
                    return;
                }

                if (isIotHubOpen)
                {
                    await hubManager.StopEventHubProcessingAsync();
                    btnHubOpen.Text = "Open";
                    btnHubSend.Enabled = false;
                    btnDirectMethod.Enabled = false;
                    btnDeviceTwin.Enabled = false;
                    isIotHubOpen = false;
                    txtIotHubConnectionString.ReadOnly = false;
                    txtEventHubConnectionString.ReadOnly = false;
                    txtStorageConnectionString.ReadOnly = false;
                    return;
                }

                await hubManager.StartEventHubProcessingAsync();

                isIotHubOpen = true;
                btnHubOpen.Text = "Close";
                btnHubSend.Enabled = true;
                btnDirectMethod.Enabled = true;
                btnDeviceTwin.Enabled = true;
                txtIotHubConnectionString.ReadOnly = true;
                txtEventHubConnectionString.ReadOnly = true;
                txtStorageConnectionString.ReadOnly = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Event Processor: {ex.Message}");
            }
        }

        /// <summary>
        /// 選択中のデバイスへクラウド発メッセージを送信する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private async void btnHubSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (!EnsureHubManager())
                {
                    return;
                }

                if (cmbDeviceId.SelectedItem == null)
                {
                    MessageBox.Show("DeviceId を選択してください。");
                    return;
                }

                var deviceId = cmbDeviceId.SelectedItem.ToString();
                var message = rtxtHubSend.Text;
                await hubManager.SendCloudToDeviceMessageAsync(deviceId, message);
                MessageBox.Show($"メッセージがデバイス {deviceId} に送信されました: {message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"メッセージ送信に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 選択中デバイスの接続開始または切断を要求する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private async void btnDevicerOpen_Click(object sender, EventArgs e)
        {
            await DeviceOpen();
        }

        /// <summary>
        /// 選択中デバイスとの接続状態を切り替える
        /// </summary>
        /// <returns>接続または切断処理を表すタスク</returns>
        private async Task DeviceOpen()
        {
            try
            {
                if (!await EnsureDeviceManagerAsync(loadDeviceIds: true))
                {
                    return;
                }

                if (cmbDeviceId.SelectedItem == null)
                {
                    MessageBox.Show("有効な DeviceId を取得できませんでした。接続文字列を確認してください。");
                    return;
                }

                var deviceId = cmbDeviceId.SelectedItem.ToString();

                if (isDeviceOpen)
                {
                    await deviceManager.CloseDeviceAsync(deviceId);
                    btnDevicerOpen.Text = "Open";
                    btnDeviceSend.Enabled = false;
                    cmbDeviceId.Enabled = true;
                    isDeviceOpen = false;
                    return;
                }

                await deviceManager.OpenDeviceAsync(deviceId);

                isDeviceOpen = true;
                btnDevicerOpen.Text = "Close";
                btnDeviceSend.Enabled = true;
                cmbDeviceId.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"デバイス操作でエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 接続済みデバイスからメッセージを送信する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private async void btnDeviceSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (!await EnsureDeviceManagerAsync())
                {
                    return;
                }

                if (cmbDeviceId.SelectedItem == null)
                {
                    MessageBox.Show("DeviceId を選択してください。");
                    return;
                }

                var deviceId = cmbDeviceId.SelectedItem.ToString();
                await deviceManager.SendDeviceMessageAsync(deviceId, rtxtDeviceSend.Text);
                MessageBox.Show("メッセージ送信完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"メッセージ送信が失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// フォーム表示時に初期データを読み込む
        /// </summary>
        /// <param name="sender">イベント送信元のフォーム</param>
        /// <param name="e">イベントデータ</param>
        private async void Form1_Load(object sender, EventArgs e)
        {
            LoadDirectMethods();
            await EnsureDeviceManagerAsync(loadDeviceIds: true, showError: false);
        }

        /// <summary>
        /// 設定ファイルからダイレクトメソッド一覧を読み込み選択候補へ反映する
        /// </summary>
        private void LoadDirectMethods()
        {
            var directMethodsSection = Utility.Configuration.GetSection("DirectMethods");
            if (directMethodsSection.Exists())
            {
                cmbDirectMethod.Items.Clear();
                foreach (var method in directMethodsSection.GetChildren())
                {
                    cmbDirectMethod.Items.Add(method.Key);
                }
                if (cmbDirectMethod.Items.Count > 0)
                {
                    cmbDirectMethod.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// 選択されたダイレクトメソッドに対応するサンプルペイロードを取得する
        /// </summary>
        /// <param name="methodName">対象のダイレクトメソッド名</param>
        /// <returns>設定済みのサンプルペイロード 未設定の場合は空JSON文字列</returns>
        private string GetSamplePayload(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return "{}";
            }

            var samplePayload = Utility.Configuration.GetSection($"DirectMethods:{methodName}:SamplePayload").Value;
            return samplePayload ?? "{}";
        }

        /// <summary>
        /// ダイレクトメソッド選択変更時にサンプルペイロードを送信欄へ反映する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private void cmbDirectMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDirectMethod.SelectedItem != null)
            {
                var methodName = cmbDirectMethod.SelectedItem.ToString();
                var samplePayload = GetSamplePayload(methodName);
                rtxtHubSend.Text = samplePayload;
            }
        }

        /// <summary>
        /// 指定された <see cref="RichTextBox" /> の行数を上限以内に保ち末尾へスクロールする
        /// </summary>
        /// <param name="richTextBox">行数制限の対象となるコントロール</param>
        /// <param name="MaxLines">保持する最大行数</param>
        private void EnsureMaxLines(RichTextBox richTextBox, int MaxLines)
        {
            if (richTextBox.Lines.Length > MaxLines)
            {
                var lines = richTextBox.Lines.Skip(richTextBox.Lines.Length - MaxLines).ToArray();
                richTextBox.Lines = lines;
                richTextBox.SelectionStart = richTextBox.Text.Length;
            }
            richTextBox.ScrollToCaret();
        }

        /// <summary>
        /// 選択中デバイスに対してダイレクトメソッドを呼び出す
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private async void btnDirectMethod_Click(object sender, EventArgs e)
        {
            try
            {
                if (!await EnsureDeviceManagerAsync())
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(cmbDirectMethod.Text))
                {
                    return;
                }

                if (cmbDeviceId.SelectedItem != null)
                {
                    var deviceId = cmbDeviceId.SelectedItem.ToString();
                    var payload = rtxtHubSend.Text;

                    // JSON 形式かどうかをチェック
                    if (!Utility.IsValidJson(payload))
                    {
                        MessageBox.Show("ペイロードは有効なJSON形式ではありません。");
                        return;
                    }

                    var response = await deviceManager.InvokeDirectMethodAsync(deviceId, cmbDirectMethod.Text, payload);
                    MessageBox.Show($"メソッド呼び出し成功: {response.Status}, ペイロード: {response.GetPayloadAsJson()}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"メソッド呼び出し失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// DeviceTwin操作用の画面を表示する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // FormDeviceTwinを表示
                var selectedDeviceId = cmbDeviceId.SelectedItem?.ToString();
                var formDeviceTwin = new FormDeviceTwin(txtIotHubConnectionString.Text, selectedDeviceId);
                formDeviceTwin.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Device Twin 画面を開けませんでした: {ex.Message}");
            }
        }

        /// <summary>
        /// デバイス一括登録画面を表示する
        /// </summary>
        /// <param name="sender">イベント送信元のコントロール</param>
        /// <param name="e">イベントデータ</param>
        private void btnDeviceRegister_Click(object sender, EventArgs e)
        {
            try
            {
                // FormRegisterを表示
                var formRegister = new FormRegister(txtIotHubConnectionString.Text);
                formRegister.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"デバイス登録画面を開けませんでした: {ex.Message}");
            }
        }
    }
}
