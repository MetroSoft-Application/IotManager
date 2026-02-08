using System.Windows.Forms;
using IotManager.Device;
using IotManager.Hub;
using IotManager.Form;
using Microsoft.Extensions.Configuration;

namespace IotManager
{
    /// <summary>
    /// IoTデバイスとIoT Hubの管理を行うフォーム
    /// </summary>
    public partial class Form1 : System.Windows.Forms.Form
    {
        private DeviceManager deviceManager;
        private HubManager hubManager;
        private bool isDeviceOpen = false;
        private bool isIotHubOpen = false;
        private const int MAX_LINE = 50000;
        private const int MAX_LINE_COMMAND_RESULT = 50000; // コマンド実行結果用の拡張行数

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;

            var iotHubConnectionString = Utility.Configuration["IoTHub:ConnectionString"];
            txtIotHubConnectionString.Text = iotHubConnectionString;

            var eventHubConnectionString = Utility.Configuration["EventHub:ConnectionString"];
            txtEventHubConnectionString.Text = eventHubConnectionString;

            var storageConnectionString = Utility.Configuration["EventHub:StorageConnectionString"];
            txtStorageConnectionString.Text = storageConnectionString;

            // DeviceManagerとHubManagerを初期化
            deviceManager = new DeviceManager(iotHubConnectionString);
            hubManager = new HubManager(iotHubConnectionString, eventHubConnectionString, storageConnectionString);

            // イベントハンドラを登録
            deviceManager.OnMessageReceived += OnDeviceMessageReceived;
            deviceManager.OnDirectMethodReceived += OnDirectMethodReceived;
            hubManager.OnHubMessageReceived += OnHubMessageReceived;
        }

        /// <summary>
        /// デバイスメッセージ受信時の処理
        /// </summary>
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
        /// Hub メッセージ受信時の処理
        /// </summary>
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
        /// ダイレクトメソッド受信時の処理
        /// </summary>
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
        /// デバイスIDをロード
        /// </summary>
        private async Task LoadDeviceIds()
        {
            try
            {
                var deviceIds = await deviceManager.GetDeviceIdsAsync();
                if (deviceIds.Any())
                {
                    cmbDeviceId.Items.Clear();
                    foreach (var deviceId in deviceIds)
                    {
                        cmbDeviceId.Items.Add(deviceId);
                    }

                    cmbDeviceId.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// IoT Hubを開くボタンのクリックイベント
        /// </summary>
        /// <param name="sender">イベント送信元のオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private async void btnHubOpen_Click(object sender, EventArgs e)
        {
            try
            {
                if (isIotHubOpen)
                {
                    await hubManager.StopEventHubProcessingAsync();
                    btnHubOpen.Text = "Open";
                    btnHubSend.Enabled = false;
                    btnDirectMethod.Enabled = false;
                    btnDeviceTwin.Enabled = false;
                    isIotHubOpen = false;
                    return;
                }

                await hubManager.StartEventHubProcessingAsync();

                isIotHubOpen = true;
                btnHubOpen.Text = "Close";
                btnHubSend.Enabled = true;
                btnDirectMethod.Enabled = true;
                btnDeviceTwin.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Event Processor: {ex.Message}");
            }
        }

        /// <summary>
        /// IoT Hubにメッセージを送信するボタンのクリックイベント
        /// </summary>
        /// <param name="sender">イベント送信元のオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private async void btnHubSend_Click(object sender, EventArgs e)
        {
            try
            {
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
        /// デバイスを開くボタンのクリックイベント
        /// </summary>
        /// <param name="sender">イベント送信元のオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private async void btnDevicerOpen_Click(object sender, EventArgs e)
        {
            await DeviceOpen();
        }

        /// <summary>
        /// デバイスを開く
        /// </summary>
        private async Task DeviceOpen()
        {
            try
            {
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
        /// デバイスにメッセージを送信するボタンのクリックイベント
        /// </summary>
        /// <param name="sender">イベント送信元のオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private async void btnDeviceSend_Click(object sender, EventArgs e)
        {
            try
            {
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
        /// フォームロード時の処理
        /// </summary>
        /// <param name="sender">イベント送信元のオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadDeviceIds();
            LoadDirectMethods();
        }

        /// <summary>
        /// ダイレクトメソッド一覧を読み込む
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
        /// 選択されたダイレクトメソッドのサンプルペイロードを取得
        /// </summary>
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
        /// ダイレクトメソッド選択変更時の処理
        /// </summary>
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
        /// RichTextBoxの行数を制限し、古い行を削除
        /// </summary>
        /// <param name="richTextBox">対象のRichTextBox</param>
        /// <param name="MaxLines">最大行数</param>
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

        /// <param name="sender">イベント送信元のオブジェクト</param>
        /// <param name="e">イベント引数</param>
        private async void btnDirectMethod_Click(object sender, EventArgs e)
        {
            try
            {
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

        private void button1_Click(object sender, EventArgs e)
        {
            // FormDeviceTwinを表示
            var formDeviceTwin = new FormDeviceTwin(txtIotHubConnectionString.Text);
            formDeviceTwin.Show();
        }

        private void btnDeviceRegister_Click(object sender, EventArgs e)
        {
            // FormRegisterを表示
            var formRegister = new FormRegister(txtIotHubConnectionString.Text);
            formRegister.Show();
        }
    }
}
