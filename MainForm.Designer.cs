namespace WhatsAppBulkSender
{
    partial class MainForm
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
            this.btnLoad = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnSend = new System.Windows.Forms.Button();
            this.lstStatus = new System.Windows.Forms.ListBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.numDelayMs = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.lblLoaded = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkHeadless = new System.Windows.Forms.CheckBox();
            this.btnAttach = new System.Windows.Forms.Button();
            this.btnClearAttachments = new System.Windows.Forms.Button();
            this.lblAttachments = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numDelayMs)).BeginInit();
            this.SuspendLayout();
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(12, 12);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(127, 34);
            this.btnLoad.TabIndex = 0;
            this.btnLoad.Text = "Load Excel...";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // txtMessage
            // 
            this.txtMessage.AcceptsReturn = true;
            this.txtMessage.AcceptsTab = true;
            this.txtMessage.Location = new System.Drawing.Point(12, 86);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.PlaceholderText = "Type or paste your message once...";
            this.txtMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessage.Size = new System.Drawing.Size(618, 120);
            this.txtMessage.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 63);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(169, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Message to broadcast:";
            // 
            // btnSend
            // 
            this.btnSend.Enabled = false;
            this.btnSend.Location = new System.Drawing.Point(12, 250);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(94, 34);
            this.btnSend.TabIndex = 3;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // lstStatus
            // 
            this.lstStatus.FormattingEnabled = true;
            this.lstStatus.HorizontalScrollbar = true;
            this.lstStatus.ItemHeight = 20;
            this.lstStatus.Location = new System.Drawing.Point(12, 311);
            this.lstStatus.Name = "lstStatus";
            this.lstStatus.Size = new System.Drawing.Size(618, 244);
            this.lstStatus.TabIndex = 4;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 566);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(618, 16);
            this.progressBar.TabIndex = 5;
            // 
            // numDelayMs
            // 
            this.numDelayMs.Increment = new decimal(new int[] {
            250,
            0,
            0,
            0});
            this.numDelayMs.Location = new System.Drawing.Point(170, 287);
            this.numDelayMs.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.numDelayMs.Minimum = new decimal(new int[] {
            250,
            0,
            0,
            0});
            this.numDelayMs.Name = "numDelayMs";
            this.numDelayMs.Size = new System.Drawing.Size(120, 27);
            this.numDelayMs.TabIndex = 6;
            this.numDelayMs.Value = new decimal(new int[] {
            1200,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 289);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(152, 20);
            this.label2.TabIndex = 7;
            this.label2.Text = "Delay between sends";
            // 
            // lblLoaded
            // 
            this.lblLoaded.AutoSize = true;
            this.lblLoaded.Location = new System.Drawing.Point(145, 19);
            this.lblLoaded.Name = "lblLoaded";
            this.lblLoaded.Size = new System.Drawing.Size(155, 20);
            this.lblLoaded.TabIndex = 8;
            this.lblLoaded.Text = "No file loaded. 0 nums";
            // 
            // btnCancel
            // 
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(112, 250);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(94, 34);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkHeadless
            // 
            this.chkHeadless.AutoSize = true;
            this.chkHeadless.Location = new System.Drawing.Point(457, 255);
            this.chkHeadless.Name = "chkHeadless";
            this.chkHeadless.Size = new System.Drawing.Size(173, 24);
            this.chkHeadless.TabIndex = 10;
            this.chkHeadless.Text = "Run Chrome headless";
            this.chkHeadless.UseVisualStyleBackColor = true;
            // 
            // btnAttach
            // 
            this.btnAttach.Location = new System.Drawing.Point(12, 212);
            this.btnAttach.Name = "btnAttach";
            this.btnAttach.Size = new System.Drawing.Size(120, 30);
            this.btnAttach.TabIndex = 11;
            this.btnAttach.Text = "Attach Files";
            this.btnAttach.UseVisualStyleBackColor = true;
            this.btnAttach.Click += new System.EventHandler(this.btnAttach_Click);
            // 
            // btnClearAttachments
            // 
            this.btnClearAttachments.Location = new System.Drawing.Point(138, 212);
            this.btnClearAttachments.Name = "btnClearAttachments";
            this.btnClearAttachments.Size = new System.Drawing.Size(150, 30);
            this.btnClearAttachments.TabIndex = 12;
            this.btnClearAttachments.Text = "Clear Attachments";
            this.btnClearAttachments.UseVisualStyleBackColor = true;
            this.btnClearAttachments.Click += new System.EventHandler(this.btnClearAttachments_Click);
            // 
            // lblAttachments
            // 
            this.lblAttachments.AutoSize = true;
            this.lblAttachments.Location = new System.Drawing.Point(294, 218);
            this.lblAttachments.Name = "lblAttachments";
            this.lblAttachments.Size = new System.Drawing.Size(126, 20);
            this.lblAttachments.TabIndex = 13;
            this.lblAttachments.Text = "No files selected";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(642, 594);
            this.Controls.Add(this.lblAttachments);
            this.Controls.Add(this.btnClearAttachments);
            this.Controls.Add(this.btnAttach);
            this.Controls.Add(this.chkHeadless);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblLoaded);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numDelayMs);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lstStatus);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.btnLoad);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WhatsApp Bulk Sender (Demo)";
            ((System.ComponentModel.ISupportInitialize)(this.numDelayMs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.ListBox lstStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.NumericUpDown numDelayMs;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblLoaded;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkHeadless;

        // NEW controls
        private System.Windows.Forms.Button btnAttach;
        private System.Windows.Forms.Button btnClearAttachments;
        private System.Windows.Forms.Label lblAttachments;
    }
}
