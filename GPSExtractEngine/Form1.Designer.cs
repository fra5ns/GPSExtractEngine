namespace GPSExtractEngine
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.txtMsg = new System.Windows.Forms.RichTextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripLastQuery = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripLastObjectID = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripTotalThread = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnStartProses = new System.Windows.Forms.Button();
            this.bwQueryMongo1 = new System.ComponentModel.BackgroundWorker();
            this.tmrQueryMongo = new System.Windows.Forms.Timer(this.components);
            this.lblTotalRaw = new System.Windows.Forms.Label();
            this.lblAmwellQueue = new System.Windows.Forms.Label();
            this.tmrSaveCache = new System.Windows.Forms.Timer(this.components);
            this.lblTeltoQueue = new System.Windows.Forms.Label();
            this.tmrRunningTime = new System.Windows.Forms.Timer(this.components);
            this.tmrExtract = new System.Windows.Forms.Timer(this.components);
            this.bwExtractAmwell = new System.ComponentModel.BackgroundWorker();
            this.bwExtractTelto = new System.ComponentModel.BackgroundWorker();
            this.lblLogFinal = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.bwSaveCache = new System.ComponentModel.BackgroundWorker();
            this.toolStripDel = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripRecPerMinute = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripExtractPerMin = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnExtract = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtMsg
            // 
            this.txtMsg.Location = new System.Drawing.Point(16, 15);
            this.txtMsg.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.txtMsg.Name = "txtMsg";
            this.txtMsg.Size = new System.Drawing.Size(1123, 1078);
            this.txtMsg.TabIndex = 89;
            this.txtMsg.Text = "";
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLastQuery,
            this.toolStripLastObjectID,
            this.toolStripTotalThread,
            this.toolStripDel,
            this.toolStripRecPerMinute,
            this.toolStripExtractPerMin});
            this.statusStrip1.Location = new System.Drawing.Point(0, 1117);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 13, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1424, 37);
            this.statusStrip1.TabIndex = 90;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripLastQuery
            // 
            this.toolStripLastQuery.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.toolStripLastQuery.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripLastQuery.Name = "toolStripLastQuery";
            this.toolStripLastQuery.Size = new System.Drawing.Size(140, 32);
            this.toolStripLastQuery.Text = "Last Query :";
            // 
            // toolStripLastObjectID
            // 
            this.toolStripLastObjectID.BackColor = System.Drawing.Color.Lime;
            this.toolStripLastObjectID.Name = "toolStripLastObjectID";
            this.toolStripLastObjectID.Size = new System.Drawing.Size(156, 32);
            this.toolStripLastObjectID.Text = "Last ObjectID";
            // 
            // toolStripTotalThread
            // 
            this.toolStripTotalThread.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.toolStripTotalThread.Name = "toolStripTotalThread";
            this.toolStripTotalThread.Size = new System.Drawing.Size(147, 32);
            this.toolStripTotalThread.Text = "Total Thread";
            // 
            // btnStartProses
            // 
            this.btnStartProses.BackColor = System.Drawing.Color.OldLace;
            this.btnStartProses.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStartProses.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnStartProses.Location = new System.Drawing.Point(1172, 55);
            this.btnStartProses.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.btnStartProses.Name = "btnStartProses";
            this.btnStartProses.Size = new System.Drawing.Size(182, 81);
            this.btnStartProses.TabIndex = 91;
            this.btnStartProses.Text = "Start Proses";
            this.btnStartProses.UseVisualStyleBackColor = false;
            this.btnStartProses.Click += new System.EventHandler(this.btnStartProses_Click);
            // 
            // bwQueryMongo1
            // 
            this.bwQueryMongo1.WorkerReportsProgress = true;
            this.bwQueryMongo1.WorkerSupportsCancellation = true;
            this.bwQueryMongo1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bwQueryMongo1_DoWork);
            // 
            // tmrQueryMongo
            // 
            this.tmrQueryMongo.Interval = 200;
            this.tmrQueryMongo.Tick += new System.EventHandler(this.tmrQueryMongo_Tick);
            // 
            // lblTotalRaw
            // 
            this.lblTotalRaw.AutoSize = true;
            this.lblTotalRaw.BackColor = System.Drawing.Color.Yellow;
            this.lblTotalRaw.Location = new System.Drawing.Point(1167, 302);
            this.lblTotalRaw.Name = "lblTotalRaw";
            this.lblTotalRaw.Size = new System.Drawing.Size(108, 25);
            this.lblTotalRaw.TabIndex = 92;
            this.lblTotalRaw.Text = "Total Raw";
            this.lblTotalRaw.Click += new System.EventHandler(this.lblTotalRaw_Click);
            // 
            // lblAmwellQueue
            // 
            this.lblAmwellQueue.AutoSize = true;
            this.lblAmwellQueue.BackColor = System.Drawing.Color.Yellow;
            this.lblAmwellQueue.Location = new System.Drawing.Point(1167, 369);
            this.lblAmwellQueue.Name = "lblAmwellQueue";
            this.lblAmwellQueue.Size = new System.Drawing.Size(150, 25);
            this.lblAmwellQueue.TabIndex = 93;
            this.lblAmwellQueue.Text = "Amwell Queue";
            // 
            // tmrSaveCache
            // 
            this.tmrSaveCache.Interval = 10000;
            this.tmrSaveCache.Tick += new System.EventHandler(this.tmrSaveCache_Tick);
            // 
            // lblTeltoQueue
            // 
            this.lblTeltoQueue.AutoSize = true;
            this.lblTeltoQueue.BackColor = System.Drawing.Color.Yellow;
            this.lblTeltoQueue.Location = new System.Drawing.Point(1167, 437);
            this.lblTeltoQueue.Name = "lblTeltoQueue";
            this.lblTeltoQueue.Size = new System.Drawing.Size(130, 25);
            this.lblTeltoQueue.TabIndex = 94;
            this.lblTeltoQueue.Text = "Telto Queue";
            // 
            // tmrRunningTime
            // 
            this.tmrRunningTime.Enabled = true;
            this.tmrRunningTime.Interval = 60000;
            this.tmrRunningTime.Tick += new System.EventHandler(this.tmrRunningTime_Tick);
            // 
            // tmrExtract
            // 
            this.tmrExtract.Enabled = true;
            this.tmrExtract.Interval = 200;
            this.tmrExtract.Tick += new System.EventHandler(this.tmrExtract_Tick);
            // 
            // bwExtractAmwell
            // 
            this.bwExtractAmwell.WorkerReportsProgress = true;
            this.bwExtractAmwell.WorkerSupportsCancellation = true;
            this.bwExtractAmwell.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bwExtractAmwell_DoWork);
            // 
            // bwExtractTelto
            // 
            this.bwExtractTelto.WorkerReportsProgress = true;
            this.bwExtractTelto.WorkerSupportsCancellation = true;
            this.bwExtractTelto.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bwExtractTelto_DoWork);
            // 
            // lblLogFinal
            // 
            this.lblLogFinal.AutoSize = true;
            this.lblLogFinal.BackColor = System.Drawing.Color.Yellow;
            this.lblLogFinal.Location = new System.Drawing.Point(1167, 513);
            this.lblLogFinal.Name = "lblLogFinal";
            this.lblLogFinal.Size = new System.Drawing.Size(101, 25);
            this.lblLogFinal.TabIndex = 95;
            this.lblLogFinal.Text = "Log Final";
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.OldLace;
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.button1.Location = new System.Drawing.Point(1172, 872);
            this.button1.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(182, 81);
            this.button1.TabIndex = 96;
            this.button1.Text = "Check Var";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // bwSaveCache
            // 
            this.bwSaveCache.WorkerReportsProgress = true;
            this.bwSaveCache.WorkerSupportsCancellation = true;
            this.bwSaveCache.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bwSaveCache_DoWork);
            // 
            // toolStripDel
            // 
            this.toolStripDel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.toolStripDel.Name = "toolStripDel";
            this.toolStripDel.Size = new System.Drawing.Size(51, 32);
            this.toolStripDel.Text = "Del";
            // 
            // toolStripRecPerMinute
            // 
            this.toolStripRecPerMinute.Name = "toolStripRecPerMinute";
            this.toolStripRecPerMinute.Size = new System.Drawing.Size(132, 32);
            this.toolStripRecPerMinute.Text = "Per Minute";
            // 
            // toolStripExtractPerMin
            // 
            this.toolStripExtractPerMin.Name = "toolStripExtractPerMin";
            this.toolStripExtractPerMin.Size = new System.Drawing.Size(85, 32);
            this.toolStripExtractPerMin.Text = "Extract";
            // 
            // btnExtract
            // 
            this.btnExtract.BackColor = System.Drawing.Color.OldLace;
            this.btnExtract.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExtract.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnExtract.Location = new System.Drawing.Point(1172, 161);
            this.btnExtract.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.btnExtract.Name = "btnExtract";
            this.btnExtract.Size = new System.Drawing.Size(182, 81);
            this.btnExtract.TabIndex = 97;
            this.btnExtract.Text = "Stop Extract";
            this.btnExtract.UseVisualStyleBackColor = false;
            this.btnExtract.Click += new System.EventHandler(this.button2_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1424, 1154);
            this.Controls.Add(this.btnExtract);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lblLogFinal);
            this.Controls.Add(this.lblTeltoQueue);
            this.Controls.Add(this.lblAmwellQueue);
            this.Controls.Add(this.lblTotalRaw);
            this.Controls.Add(this.btnStartProses);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.txtMsg);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Extract GPS Raw Data";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.RichTextBox txtMsg;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripLastQuery;
        internal System.Windows.Forms.Button btnStartProses;
        private System.ComponentModel.BackgroundWorker bwQueryMongo1;
        private System.Windows.Forms.Timer tmrQueryMongo;
        private System.Windows.Forms.ToolStripStatusLabel toolStripLastObjectID;
        private System.Windows.Forms.ToolStripStatusLabel toolStripTotalThread;
        private System.Windows.Forms.Label lblTotalRaw;
        private System.Windows.Forms.Label lblAmwellQueue;
        private System.Windows.Forms.Timer tmrSaveCache;
        private System.Windows.Forms.Label lblTeltoQueue;
        private System.Windows.Forms.Timer tmrRunningTime;
        private System.Windows.Forms.Timer tmrExtract;
        private System.ComponentModel.BackgroundWorker bwExtractAmwell;
        private System.ComponentModel.BackgroundWorker bwExtractTelto;
        private System.Windows.Forms.Label lblLogFinal;
        internal System.Windows.Forms.Button button1;
        private System.ComponentModel.BackgroundWorker bwSaveCache;
        private System.Windows.Forms.ToolStripStatusLabel toolStripDel;
        private System.Windows.Forms.ToolStripStatusLabel toolStripRecPerMinute;
        private System.Windows.Forms.ToolStripStatusLabel toolStripExtractPerMin;
        internal System.Windows.Forms.Button btnExtract;
    }
}

