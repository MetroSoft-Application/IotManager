namespace IotManager
{
    partial class FormDeviceTwin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            splitContainer1 = new SplitContainer();
            btnExec = new Button();
            txtSQL = new FastColoredTextBoxNS.FastColoredTextBox();
            txtTwinStatus = new FastColoredTextBoxNS.FastColoredTextBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)txtSQL).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtTwinStatus).BeginInit();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(btnExec);
            splitContainer1.Panel1.Controls.Add(txtSQL);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(txtTwinStatus);
            splitContainer1.Size = new Size(727, 572);
            splitContainer1.SplitterDistance = 191;
            splitContainer1.TabIndex = 1;
            // 
            // btnExec
            // 
            btnExec.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExec.Location = new Point(660, 6);
            btnExec.Name = "btnExec";
            btnExec.Size = new Size(61, 23);
            btnExec.TabIndex = 2;
            btnExec.Text = "Exec";
            btnExec.UseVisualStyleBackColor = true;
            btnExec.Click += Exec_Click;
            // 
            // txtSQL
            // 
            txtSQL.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtSQL.AutoCompleteBracketsList = new char[]
    {
    '(',
    ')',
    '{',
    '}',
    '[',
    ']',
    '"',
    '"',
    '\'',
    '\''
    };
            txtSQL.AutoScrollMinSize = new Size(27, 14);
            txtSQL.BackBrush = null;
            txtSQL.CharHeight = 14;
            txtSQL.CharWidth = 8;
            txtSQL.Cursor = Cursors.IBeam;
            txtSQL.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            txtSQL.Font = new Font("Courier New", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            txtSQL.IsReplaceMode = false;
            txtSQL.LeftBracket = '(';
            txtSQL.LeftBracket2 = '{';
            txtSQL.Location = new Point(3, 3);
            txtSQL.Name = "txtSQL";
            txtSQL.Paddings = new Padding(0);
            txtSQL.RightBracket = ')';
            txtSQL.RightBracket2 = '}';
            txtSQL.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            txtSQL.ServiceColors = null;
            txtSQL.Size = new Size(649, 185);
            txtSQL.TabIndex = 1;
            txtSQL.Zoom = 100;
            // 
            // txtTwinStatus
            // 
            txtTwinStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtTwinStatus.AutoCompleteBracketsList = new char[]
    {
    '(',
    ')',
    '{',
    '}',
    '[',
    ']',
    '"',
    '"',
    '\'',
    '\''
    };
            txtTwinStatus.AutoScrollMinSize = new Size(27, 14);
            txtTwinStatus.BackBrush = null;
            txtTwinStatus.CharHeight = 14;
            txtTwinStatus.CharWidth = 8;
            txtTwinStatus.Cursor = Cursors.IBeam;
            txtTwinStatus.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            txtTwinStatus.Font = new Font("Courier New", 9.75F, FontStyle.Regular, GraphicsUnit.Point);
            txtTwinStatus.IsReplaceMode = false;
            txtTwinStatus.LeftBracket = '(';
            txtTwinStatus.LeftBracket2 = '{';
            txtTwinStatus.Location = new Point(0, 0);
            txtTwinStatus.Name = "txtTwinStatus";
            txtTwinStatus.Paddings = new Padding(0);
            txtTwinStatus.ReadOnly = true;
            txtTwinStatus.RightBracket = ')';
            txtTwinStatus.RightBracket2 = '}';
            txtTwinStatus.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            txtTwinStatus.ServiceColors = null;
            txtTwinStatus.Size = new Size(721, 365);
            txtTwinStatus.TabIndex = 2;
            txtTwinStatus.Zoom = 100;
            // 
            // FormDeviceTwin
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(727, 572);
            Controls.Add(splitContainer1);
            Name = "FormDeviceTwin";
            Text = "FormDeviceTwin";
            FormClosing += FormDeviceTwin_FormClosing;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)txtSQL).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtTwinStatus).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private FastColoredTextBoxNS.FastColoredTextBox txtSQL;
        private FastColoredTextBoxNS.FastColoredTextBox txtTwinStatus;
        private Button btnExec;
    }
}