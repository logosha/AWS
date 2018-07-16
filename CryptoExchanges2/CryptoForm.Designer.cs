using ACX;
using Allcoin;
using BitcoinIndonesia;
using BitFlyer;
using CexIO;
using Coinone;
using Gatecoin;
using GateIO;
using GDAX;
using LakeBTC;
using Livecoin;
using QuadrigaCX;
using Shared.Broker;
using Shared.Interfaces;
using System;
using System.ComponentModel;
using System.Windows.Forms;


namespace CryptoExchanges2
{
    partial class CryptoForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.timer_DateTime = new System.Windows.Forms.Timer(this.components);
            this.Exchange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Interface = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Coin = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Timestamp = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Elapsed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Bid = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BidSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Ask = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AskSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Last = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LastSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Exchange,
            this.Interface,
            this.Coin,
            this.Timestamp,
            this.Elapsed,
            this.Bid,
            this.BidSize,
            this.Ask,
            this.AskSize,
            this.Last,
            this.LastSize});
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView.DefaultCellStyle = dataGridViewCellStyle12;
            this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView.GridColor = System.Drawing.SystemColors.Control;
            this.dataGridView.Location = new System.Drawing.Point(3, 33);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(907, 268);
            this.dataGridView.TabIndex = 0;
            this.dataGridView.VirtualMode = true;
            this.dataGridView.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dataGridView_CellValueNeeded);
            this.dataGridView.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_ColumnHeaderMouseClick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.dataGridView, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 90F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(913, 304);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(907, 24);
            this.panel1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(251, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Local Time: ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Cursor = System.Windows.Forms.Cursors.No;
            this.label1.Location = new System.Drawing.Point(4, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Date: ";
            // 
            // timer_DateTime
            // 
            this.timer_DateTime.Enabled = true;
            this.timer_DateTime.Interval = 1000;
            this.timer_DateTime.Tick += new System.EventHandler(this._timer_DateTime);
            // 
            // Exchange
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            this.Exchange.DefaultCellStyle = dataGridViewCellStyle1;
            this.Exchange.HeaderText = "Exchange";
            this.Exchange.Name = "Exchange";
            this.Exchange.ReadOnly = true;
            // 
            // Interface
            // 
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Interface.DefaultCellStyle = dataGridViewCellStyle2;
            this.Interface.HeaderText = "Interface";
            this.Interface.Name = "Interface";
            this.Interface.ReadOnly = true;
            // 
            // Coin
            // 
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Coin.DefaultCellStyle = dataGridViewCellStyle3;
            this.Coin.HeaderText = "Coin Pair";
            this.Coin.Name = "Coin";
            this.Coin.ReadOnly = true;
            // 
            // Timestamp
            // 
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Timestamp.DefaultCellStyle = dataGridViewCellStyle4;
            this.Timestamp.HeaderText = "Timestamp";
            this.Timestamp.Name = "Timestamp";
            this.Timestamp.ReadOnly = true;
            // 
            // Elapsed
            // 
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Elapsed.DefaultCellStyle = dataGridViewCellStyle5;
            this.Elapsed.HeaderText = "Elapsed";
            this.Elapsed.Name = "Elapsed";
            this.Elapsed.ReadOnly = true;
            // 
            // Bid
            // 
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Bid.DefaultCellStyle = dataGridViewCellStyle6;
            this.Bid.HeaderText = "Bid";
            this.Bid.Name = "Bid";
            this.Bid.ReadOnly = true;
            // 
            // BidSize
            // 
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.BidSize.DefaultCellStyle = dataGridViewCellStyle7;
            this.BidSize.HeaderText = "Bid Size";
            this.BidSize.Name = "BidSize";
            this.BidSize.ReadOnly = true;
            // 
            // Ask
            // 
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Ask.DefaultCellStyle = dataGridViewCellStyle8;
            this.Ask.HeaderText = "Ask";
            this.Ask.Name = "Ask";
            this.Ask.ReadOnly = true;
            // 
            // AskSize
            // 
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.AskSize.DefaultCellStyle = dataGridViewCellStyle9;
            this.AskSize.HeaderText = "Ask Size";
            this.AskSize.Name = "AskSize";
            this.AskSize.ReadOnly = true;
            // 
            // Last
            // 
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.Last.DefaultCellStyle = dataGridViewCellStyle10;
            this.Last.HeaderText = "Last";
            this.Last.Name = "Last";
            this.Last.ReadOnly = true;
            // 
            // LastSize
            // 
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.LastSize.DefaultCellStyle = dataGridViewCellStyle11;
            this.LastSize.HeaderText = "Last Size";
            this.LastSize.Name = "LastSize";
            this.LastSize.ReadOnly = true;
            // 
            // CryptoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 304);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Margin = new System.Windows.Forms.Padding(1);
            this.Name = "CryptoForm";
            this.Text = "Crypto";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CryptoForm_Closed);
            this.Load += new System.EventHandler(this.CryptoForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private Label label2;
        private Label label1;
        private Timer timer_DateTime;
        private DataGridViewTextBoxColumn Exchange;
        private DataGridViewTextBoxColumn Interface;
        private DataGridViewTextBoxColumn Coin;
        private DataGridViewTextBoxColumn Timestamp;
        private DataGridViewTextBoxColumn Elapsed;
        private DataGridViewTextBoxColumn Bid;
        private DataGridViewTextBoxColumn BidSize;
        private DataGridViewTextBoxColumn Ask;
        private DataGridViewTextBoxColumn AskSize;
        private DataGridViewTextBoxColumn Last;
        private DataGridViewTextBoxColumn LastSize;
    }
}

