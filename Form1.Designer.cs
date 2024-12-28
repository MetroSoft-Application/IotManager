namespace IotManager
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            groupBox1 = new GroupBox();
            splitContainer2 = new SplitContainer();
            btnDeviceSend = new Button();
            btnDevicerOpen = new Button();
            cmbDeviceId = new ComboBox();
            label1 = new Label();
            splitContainer3 = new SplitContainer();
            groupBox3 = new GroupBox();
            rtxtDeviceSend = new RichTextBox();
            groupBox4 = new GroupBox();
            rtxtDeviceReceive = new RichTextBox();
            groupBox2 = new GroupBox();
            splitContainer4 = new SplitContainer();
            txtStorageConnectionString = new TextBox();
            label4 = new Label();
            txtEventHubConnectionString = new TextBox();
            label3 = new Label();
            btnHubOpen = new Button();
            btnHubSend = new Button();
            txtIotHubConnectionString = new TextBox();
            label2 = new Label();
            splitContainer5 = new SplitContainer();
            groupBox5 = new GroupBox();
            rtxtHubSend = new RichTextBox();
            groupBox6 = new GroupBox();
            rtxtHubReceive = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer4).BeginInit();
            splitContainer4.Panel1.SuspendLayout();
            splitContainer4.Panel2.SuspendLayout();
            splitContainer4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer5).BeginInit();
            splitContainer5.Panel1.SuspendLayout();
            splitContainer5.Panel2.SuspendLayout();
            splitContainer5.SuspendLayout();
            groupBox5.SuspendLayout();
            groupBox6.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(groupBox2);
            splitContainer1.Size = new Size(1113, 589);
            splitContainer1.SplitterDistance = 538;
            splitContainer1.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(splitContainer2);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(0, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(538, 589);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Device";
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(3, 19);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(btnDeviceSend);
            splitContainer2.Panel1.Controls.Add(btnDevicerOpen);
            splitContainer2.Panel1.Controls.Add(cmbDeviceId);
            splitContainer2.Panel1.Controls.Add(label1);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(splitContainer3);
            splitContainer2.Size = new Size(532, 567);
            splitContainer2.SplitterDistance = 77;
            splitContainer2.TabIndex = 0;
            // 
            // btnDeviceSend
            // 
            btnDeviceSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDeviceSend.Enabled = false;
            btnDeviceSend.Location = new Point(454, 29);
            btnDeviceSend.Name = "btnDeviceSend";
            btnDeviceSend.Size = new Size(75, 23);
            btnDeviceSend.TabIndex = 4;
            btnDeviceSend.Text = "Send";
            btnDeviceSend.UseVisualStyleBackColor = true;
            btnDeviceSend.Click += btnDeviceSend_Click;
            // 
            // btnDevicerOpen
            // 
            btnDevicerOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDevicerOpen.Location = new Point(454, 4);
            btnDevicerOpen.Name = "btnDevicerOpen";
            btnDevicerOpen.Size = new Size(75, 23);
            btnDevicerOpen.TabIndex = 2;
            btnDevicerOpen.Text = "Open";
            btnDevicerOpen.UseVisualStyleBackColor = true;
            btnDevicerOpen.Click += btnDevicerOpen_Click;
            // 
            // cmbDeviceId
            // 
            cmbDeviceId.FormattingEnabled = true;
            cmbDeviceId.Location = new Point(61, 2);
            cmbDeviceId.Name = "cmbDeviceId";
            cmbDeviceId.Size = new Size(155, 23);
            cmbDeviceId.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(5, 2);
            label1.Name = "label1";
            label1.Size = new Size(52, 15);
            label1.TabIndex = 0;
            label1.Text = "DeviceId";
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(groupBox3);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(groupBox4);
            splitContainer3.Size = new Size(532, 486);
            splitContainer3.SplitterDistance = 199;
            splitContainer3.TabIndex = 1;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(rtxtDeviceSend);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Location = new Point(0, 0);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(532, 199);
            groupBox3.TabIndex = 0;
            groupBox3.TabStop = false;
            groupBox3.Text = "DeviceSendMessage";
            // 
            // rtxtDeviceSend
            // 
            rtxtDeviceSend.Dock = DockStyle.Fill;
            rtxtDeviceSend.Location = new Point(3, 19);
            rtxtDeviceSend.Name = "rtxtDeviceSend";
            rtxtDeviceSend.Size = new Size(526, 177);
            rtxtDeviceSend.TabIndex = 0;
            rtxtDeviceSend.Text = "";
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(rtxtDeviceReceive);
            groupBox4.Dock = DockStyle.Fill;
            groupBox4.Location = new Point(0, 0);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(532, 283);
            groupBox4.TabIndex = 1;
            groupBox4.TabStop = false;
            groupBox4.Text = "DeviceReceiveMessage";
            // 
            // rtxtDeviceReceive
            // 
            rtxtDeviceReceive.Dock = DockStyle.Fill;
            rtxtDeviceReceive.Location = new Point(3, 19);
            rtxtDeviceReceive.Name = "rtxtDeviceReceive";
            rtxtDeviceReceive.ReadOnly = true;
            rtxtDeviceReceive.Size = new Size(526, 261);
            rtxtDeviceReceive.TabIndex = 1;
            rtxtDeviceReceive.Text = "";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(splitContainer4);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(0, 0);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(571, 589);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "IotHub";
            // 
            // splitContainer4
            // 
            splitContainer4.Dock = DockStyle.Fill;
            splitContainer4.Location = new Point(3, 19);
            splitContainer4.Name = "splitContainer4";
            splitContainer4.Orientation = Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            splitContainer4.Panel1.Controls.Add(txtStorageConnectionString);
            splitContainer4.Panel1.Controls.Add(label4);
            splitContainer4.Panel1.Controls.Add(txtEventHubConnectionString);
            splitContainer4.Panel1.Controls.Add(label3);
            splitContainer4.Panel1.Controls.Add(btnHubOpen);
            splitContainer4.Panel1.Controls.Add(btnHubSend);
            splitContainer4.Panel1.Controls.Add(txtIotHubConnectionString);
            splitContainer4.Panel1.Controls.Add(label2);
            // 
            // splitContainer4.Panel2
            // 
            splitContainer4.Panel2.Controls.Add(splitContainer5);
            splitContainer4.Size = new Size(565, 567);
            splitContainer4.SplitterDistance = 77;
            splitContainer4.TabIndex = 0;
            // 
            // txtStorageConnectionString
            // 
            txtStorageConnectionString.Location = new Point(164, 54);
            txtStorageConnectionString.Name = "txtStorageConnectionString";
            txtStorageConnectionString.Size = new Size(221, 23);
            txtStorageConnectionString.TabIndex = 8;
            txtStorageConnectionString.UseSystemPasswordChar = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(5, 55);
            label4.Name = "label4";
            label4.Size = new Size(139, 15);
            label4.TabIndex = 7;
            label4.Text = "StorageConnectionString";
            // 
            // txtEventHubConnectionString
            // 
            txtEventHubConnectionString.Location = new Point(164, 28);
            txtEventHubConnectionString.Name = "txtEventHubConnectionString";
            txtEventHubConnectionString.Size = new Size(221, 23);
            txtEventHubConnectionString.TabIndex = 6;
            txtEventHubConnectionString.UseSystemPasswordChar = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(5, 29);
            label3.Name = "label3";
            label3.Size = new Size(151, 15);
            label3.TabIndex = 5;
            label3.Text = "EventHubConnectionString";
            // 
            // btnHubOpen
            // 
            btnHubOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnHubOpen.Location = new Point(487, 4);
            btnHubOpen.Name = "btnHubOpen";
            btnHubOpen.Size = new Size(75, 23);
            btnHubOpen.TabIndex = 4;
            btnHubOpen.Text = "Open";
            btnHubOpen.UseVisualStyleBackColor = true;
            btnHubOpen.Click += btnHubOpen_Click;
            // 
            // btnHubSend
            // 
            btnHubSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnHubSend.Enabled = false;
            btnHubSend.Location = new Point(487, 29);
            btnHubSend.Name = "btnHubSend";
            btnHubSend.Size = new Size(75, 23);
            btnHubSend.TabIndex = 3;
            btnHubSend.Text = "Send";
            btnHubSend.UseVisualStyleBackColor = true;
            btnHubSend.Click += btnHubSend_Click;
            // 
            // txtIotHubConnectionString
            // 
            txtIotHubConnectionString.Location = new Point(164, 2);
            txtIotHubConnectionString.Name = "txtIotHubConnectionString";
            txtIotHubConnectionString.Size = new Size(221, 23);
            txtIotHubConnectionString.TabIndex = 1;
            txtIotHubConnectionString.UseSystemPasswordChar = true;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(5, 2);
            label2.Name = "label2";
            label2.Size = new Size(136, 15);
            label2.TabIndex = 0;
            label2.Text = "IotHubConnectionString";
            // 
            // splitContainer5
            // 
            splitContainer5.Dock = DockStyle.Fill;
            splitContainer5.Location = new Point(0, 0);
            splitContainer5.Name = "splitContainer5";
            splitContainer5.Orientation = Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            splitContainer5.Panel1.Controls.Add(groupBox5);
            // 
            // splitContainer5.Panel2
            // 
            splitContainer5.Panel2.Controls.Add(groupBox6);
            splitContainer5.Size = new Size(565, 486);
            splitContainer5.SplitterDistance = 201;
            splitContainer5.TabIndex = 1;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(rtxtHubSend);
            groupBox5.Dock = DockStyle.Fill;
            groupBox5.Location = new Point(0, 0);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(565, 201);
            groupBox5.TabIndex = 1;
            groupBox5.TabStop = false;
            groupBox5.Text = "IotHubSendMessage";
            // 
            // rtxtHubSend
            // 
            rtxtHubSend.Dock = DockStyle.Fill;
            rtxtHubSend.Location = new Point(3, 19);
            rtxtHubSend.Name = "rtxtHubSend";
            rtxtHubSend.Size = new Size(559, 179);
            rtxtHubSend.TabIndex = 0;
            rtxtHubSend.Text = "";
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(rtxtHubReceive);
            groupBox6.Dock = DockStyle.Fill;
            groupBox6.Location = new Point(0, 0);
            groupBox6.Name = "groupBox6";
            groupBox6.Size = new Size(565, 281);
            groupBox6.TabIndex = 1;
            groupBox6.TabStop = false;
            groupBox6.Text = "IotHubReceiveMessage";
            // 
            // rtxtHubReceive
            // 
            rtxtHubReceive.Dock = DockStyle.Fill;
            rtxtHubReceive.Location = new Point(3, 19);
            rtxtHubReceive.Name = "rtxtHubReceive";
            rtxtHubReceive.ReadOnly = true;
            rtxtHubReceive.Size = new Size(559, 259);
            rtxtHubReceive.TabIndex = 0;
            rtxtHubReceive.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1113, 589);
            Controls.Add(splitContainer1);
            Name = "Form1";
            Text = "IotManager";
            Load += Form1_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel1.PerformLayout();
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            splitContainer4.Panel1.ResumeLayout(false);
            splitContainer4.Panel1.PerformLayout();
            splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer4).EndInit();
            splitContainer4.ResumeLayout(false);
            splitContainer5.Panel1.ResumeLayout(false);
            splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer5).EndInit();
            splitContainer5.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            groupBox6.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private SplitContainer splitContainer2;
        private Button btnDevicerOpen;
        private TextBox txtIotHubConnectionString;
        private Label label1;
        private SplitContainer splitContainer3;
        private GroupBox groupBox3;
        private RichTextBox rtxtDeviceSend;
        private GroupBox groupBox4;
        private RichTextBox rtxtDeviceReceive;
        private SplitContainer splitContainer4;
        private ComboBox cmbDeviceId;
        private Label label2;
        private SplitContainer splitContainer5;
        private GroupBox groupBox5;
        private RichTextBox rtxtHubSend;
        private GroupBox groupBox6;
        private RichTextBox rtxtHubReceive;
        private Button btnHubSend;
        private Button btnHubOpen;
        private Button btnDeviceSend;
        private TextBox txtEventHubConnectionString;
        private Label label3;
        private TextBox txtStorageConnectionString;
        private Label label4;
    }
}
