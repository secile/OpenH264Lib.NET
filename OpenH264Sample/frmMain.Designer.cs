namespace OpenH264Sample
{
    partial class frmMain
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnEncode = new System.Windows.Forms.Button();
            this.nudFps = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.pbxScreen = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.nudFps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbxScreen)).BeginInit();
            this.SuspendLayout();
            // 
            // btnEncode
            // 
            this.btnEncode.Location = new System.Drawing.Point(12, 12);
            this.btnEncode.Name = "btnEncode";
            this.btnEncode.Size = new System.Drawing.Size(75, 23);
            this.btnEncode.TabIndex = 0;
            this.btnEncode.Text = "Encode";
            this.btnEncode.UseVisualStyleBackColor = true;
            this.btnEncode.Click += new System.EventHandler(this.btnEncode_Click);
            // 
            // nudFps
            // 
            this.nudFps.Location = new System.Drawing.Point(104, 13);
            this.nudFps.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nudFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudFps.Name = "nudFps";
            this.nudFps.Size = new System.Drawing.Size(45, 22);
            this.nudFps.TabIndex = 1;
            this.nudFps.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudFps.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(153, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "fps";
            // 
            // pbxScreen
            // 
            this.pbxScreen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbxScreen.Location = new System.Drawing.Point(12, 41);
            this.pbxScreen.Name = "pbxScreen";
            this.pbxScreen.Size = new System.Drawing.Size(258, 200);
            this.pbxScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbxScreen.TabIndex = 3;
            this.pbxScreen.TabStop = false;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Controls.Add(this.pbxScreen);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nudFps);
            this.Controls.Add(this.btnEncode);
            this.Name = "frmMain";
            this.Text = "OpenH264Sample";
            ((System.ComponentModel.ISupportInitialize)(this.nudFps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbxScreen)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnEncode;
        private System.Windows.Forms.NumericUpDown nudFps;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pbxScreen;
    }
}

