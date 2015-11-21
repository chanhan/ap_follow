namespace Follow
{
    partial class frmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.tspMain = new System.Windows.Forms.ToolStrip();
            this.tspbtnStop = new System.Windows.Forms.ToolStripButton();
            this.tspbtnStart = new System.Windows.Forms.ToolStripButton();
            this.tspcbbSport = new System.Windows.Forms.ToolStripComboBox();
            this.lsbInfo = new System.Windows.Forms.ListBox();
            this.tspMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // tspMain
            // 
            this.tspMain.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tspMain.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.tspMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tspbtnStop,
            this.tspbtnStart,
            this.tspcbbSport});
            this.tspMain.Location = new System.Drawing.Point(0, 0);
            this.tspMain.Name = "tspMain";
            this.tspMain.Padding = new System.Windows.Forms.Padding(5, 2, 5, 2);
            this.tspMain.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.tspMain.Size = new System.Drawing.Size(684, 35);
            this.tspMain.TabIndex = 0;
            this.tspMain.Text = "Tools";
            // 
            // tspbtnStop
            // 
            this.tspbtnStop.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tspbtnStop.Enabled = false;
            this.tspbtnStop.Image = ((System.Drawing.Image)(resources.GetObject("tspbtnStop.Image")));
            this.tspbtnStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tspbtnStop.Name = "tspbtnStop";
            this.tspbtnStop.Size = new System.Drawing.Size(60, 28);
            this.tspbtnStop.Text = "停止";
            this.tspbtnStop.Click += new System.EventHandler(this.tspbtnStop_Click);
            // 
            // tspbtnStart
            // 
            this.tspbtnStart.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tspbtnStart.Image = ((System.Drawing.Image)(resources.GetObject("tspbtnStart.Image")));
            this.tspbtnStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tspbtnStart.Name = "tspbtnStart";
            this.tspbtnStart.Size = new System.Drawing.Size(60, 28);
            this.tspbtnStart.Text = "開始";
            this.tspbtnStart.Click += new System.EventHandler(this.tspbtnStart_Click);
            // 
            // tspcbbSport
            // 
            this.tspcbbSport.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tspcbbSport.Name = "tspcbbSport";
            this.tspcbbSport.Size = new System.Drawing.Size(300, 31);
            this.tspcbbSport.SelectedIndexChanged += new System.EventHandler(this.tspcbbSport_SelectedIndexChanged);
            // 
            // lsbInfo
            // 
            this.lsbInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lsbInfo.FormattingEnabled = true;
            this.lsbInfo.ItemHeight = 12;
            this.lsbInfo.Location = new System.Drawing.Point(0, 35);
            this.lsbInfo.Name = "lsbInfo";
            this.lsbInfo.ScrollAlwaysVisible = true;
            this.lsbInfo.Size = new System.Drawing.Size(684, 407);
            this.lsbInfo.TabIndex = 1;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 442);
            this.Controls.Add(this.lsbInfo);
            this.Controls.Add(this.tspMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.Text = "速報跟盤";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMain_FormClosed);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.tspMain.ResumeLayout(false);
            this.tspMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip tspMain;
        private System.Windows.Forms.ListBox lsbInfo;
        private System.Windows.Forms.ToolStripButton tspbtnStop;
        private System.Windows.Forms.ToolStripButton tspbtnStart;
        private System.Windows.Forms.ToolStripComboBox tspcbbSport;
    }
}

