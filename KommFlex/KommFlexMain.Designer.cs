namespace KommFlex
{
    partial class KommFlexMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(KommFlexMain));
            this.panelMainBrowser = new System.Windows.Forms.Panel();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtMsgBox = new System.Windows.Forms.TextBox();
            this.btnStartClient = new System.Windows.Forms.Button();
            this.btnExitCall = new System.Windows.Forms.Button();
            this.btnSendMessage = new System.Windows.Forms.Button();
            this.btnStartCall = new System.Windows.Forms.Button();
            this.btn2ndCamera = new System.Windows.Forms.Button();
            this.pictureBackground = new System.Windows.Forms.PictureBox();
            this.picClientCamera = new System.Windows.Forms.PictureBox();
            this.lblImagePage = new System.Windows.Forms.Label();
            this.btnPrevImage = new System.Windows.Forms.Button();
            this.btnNextImage = new System.Windows.Forms.Button();
            this.panelMainBrowser.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBackground)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picClientCamera)).BeginInit();
            this.SuspendLayout();
            // 
            // panelMainBrowser
            // 
            this.panelMainBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelMainBrowser.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(230)))), ((int)(((byte)(231)))));
            this.panelMainBrowser.Controls.Add(this.lblDescription);
            this.panelMainBrowser.Location = new System.Drawing.Point(425, 0);
            this.panelMainBrowser.Name = "panelMainBrowser";
            this.panelMainBrowser.Size = new System.Drawing.Size(497, 574);
            this.panelMainBrowser.TabIndex = 1;
            this.panelMainBrowser.Enter += new System.EventHandler(this.OnVideoPanelFocus);
            // 
            // lblDescription
            // 
            this.lblDescription.BackColor = System.Drawing.Color.LightSkyBlue;
            this.lblDescription.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDescription.Font = new System.Drawing.Font("Microsoft Sans Serif", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDescription.ForeColor = System.Drawing.Color.MidnightBlue;
            this.lblDescription.Location = new System.Drawing.Point(0, 0);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(497, 44);
            this.lblDescription.TabIndex = 12;
            this.lblDescription.Text = "KommFlexRoom";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtMsgBox
            // 
            this.txtMsgBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMsgBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.txtMsgBox.Location = new System.Drawing.Point(175, 580);
            this.txtMsgBox.Name = "txtMsgBox";
            this.txtMsgBox.Size = new System.Drawing.Size(677, 47);
            this.txtMsgBox.TabIndex = 8;
            this.txtMsgBox.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnMessageText_KeyUp);
            // 
            // btnStartClient
            // 
            this.btnStartClient.Location = new System.Drawing.Point(380, 591);
            this.btnStartClient.Name = "btnStartClient";
            this.btnStartClient.Size = new System.Drawing.Size(161, 27);
            this.btnStartClient.TabIndex = 0;
            this.btnStartClient.Text = "btnStartClient";
            this.btnStartClient.UseVisualStyleBackColor = true;
            this.btnStartClient.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OnCallButtonKeyUp);
            // 
            // btnExitCall
            // 
            this.btnExitCall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExitCall.BackgroundImage = global::KommFlex.Properties.Resources.icon_exit;
            this.btnExitCall.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnExitCall.Location = new System.Drawing.Point(63, 579);
            this.btnExitCall.Name = "btnExitCall";
            this.btnExitCall.Size = new System.Drawing.Size(50, 50);
            this.btnExitCall.TabIndex = 10;
            this.btnExitCall.UseVisualStyleBackColor = true;
            this.btnExitCall.Click += new System.EventHandler(this.btnExitCall_Click);
            // 
            // btnSendMessage
            // 
            this.btnSendMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSendMessage.BackgroundImage = global::KommFlex.Properties.Resources.icon_sendmsg;
            this.btnSendMessage.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnSendMessage.Location = new System.Drawing.Point(858, 579);
            this.btnSendMessage.Name = "btnSendMessage";
            this.btnSendMessage.Size = new System.Drawing.Size(50, 50);
            this.btnSendMessage.TabIndex = 9;
            this.btnSendMessage.UseVisualStyleBackColor = true;
            this.btnSendMessage.Click += new System.EventHandler(this.btnSendMessage_Click);
            // 
            // btnStartCall
            // 
            this.btnStartCall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnStartCall.BackColor = System.Drawing.Color.Transparent;
            this.btnStartCall.BackgroundImage = global::KommFlex.Properties.Resources.icon_camera;
            this.btnStartCall.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btnStartCall.Location = new System.Drawing.Point(7, 579);
            this.btnStartCall.Name = "btnStartCall";
            this.btnStartCall.Size = new System.Drawing.Size(50, 50);
            this.btnStartCall.TabIndex = 7;
            this.btnStartCall.UseVisualStyleBackColor = false;
            this.btnStartCall.Click += new System.EventHandler(this.btnCall_Click);
            // 
            // btn2ndCamera
            // 
            this.btn2ndCamera.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn2ndCamera.BackgroundImage = global::KommFlex.Properties.Resources.icon_2ndcam;
            this.btn2ndCamera.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn2ndCamera.Location = new System.Drawing.Point(119, 579);
            this.btn2ndCamera.Name = "btn2ndCamera";
            this.btn2ndCamera.Size = new System.Drawing.Size(50, 50);
            this.btn2ndCamera.TabIndex = 3;
            this.btn2ndCamera.UseVisualStyleBackColor = true;
            this.btn2ndCamera.Click += new System.EventHandler(this.btn2ndCamera_Click);
            // 
            // pictureBackground
            // 
            this.pictureBackground.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBackground.BackColor = System.Drawing.Color.Black;
            this.pictureBackground.Location = new System.Drawing.Point(583, 591);
            this.pictureBackground.Name = "pictureBackground";
            this.pictureBackground.Size = new System.Drawing.Size(73, 27);
            this.pictureBackground.TabIndex = 13;
            this.pictureBackground.TabStop = false;
            // 
            // picClientCamera
            // 
            this.picClientCamera.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.picClientCamera.InitialImage = null;
            this.picClientCamera.Location = new System.Drawing.Point(-2, 0);
            this.picClientCamera.Name = "picClientCamera";
            this.picClientCamera.Size = new System.Drawing.Size(421, 574);
            this.picClientCamera.TabIndex = 14;
            this.picClientCamera.TabStop = false;
            // 
            // lblImagePage
            // 
            this.lblImagePage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblImagePage.AutoSize = true;
            this.lblImagePage.BackColor = System.Drawing.Color.WhiteSmoke;
            this.lblImagePage.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblImagePage.Location = new System.Drawing.Point(153, 535);
            this.lblImagePage.Name = "lblImagePage";
            this.lblImagePage.Size = new System.Drawing.Size(50, 24);
            this.lblImagePage.TabIndex = 15;
            this.lblImagePage.Text = "1 / 3";
            this.lblImagePage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnPrevImage
            // 
            this.btnPrevImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPrevImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPrevImage.Image = global::KommFlex.Properties.Resources.arrow_left;
            this.btnPrevImage.Location = new System.Drawing.Point(119, 530);
            this.btnPrevImage.Name = "btnPrevImage";
            this.btnPrevImage.Size = new System.Drawing.Size(30, 36);
            this.btnPrevImage.TabIndex = 16;
            this.btnPrevImage.UseVisualStyleBackColor = true;
            this.btnPrevImage.Click += new System.EventHandler(this.OnPrevImage);
            // 
            // btnNextImage
            // 
            this.btnNextImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNextImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNextImage.Image = global::KommFlex.Properties.Resources.arrow_right;
            this.btnNextImage.Location = new System.Drawing.Point(209, 529);
            this.btnNextImage.Name = "btnNextImage";
            this.btnNextImage.Size = new System.Drawing.Size(30, 36);
            this.btnNextImage.TabIndex = 16;
            this.btnNextImage.UseVisualStyleBackColor = true;
            this.btnNextImage.Click += new System.EventHandler(this.OnNextImage);
            // 
            // KommFlexMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(920, 634);
            this.Controls.Add(this.btnNextImage);
            this.Controls.Add(this.btnPrevImage);
            this.Controls.Add(this.lblImagePage);
            this.Controls.Add(this.pictureBackground);
            this.Controls.Add(this.btnStartClient);
            this.Controls.Add(this.btnExitCall);
            this.Controls.Add(this.btnSendMessage);
            this.Controls.Add(this.txtMsgBox);
            this.Controls.Add(this.btnStartCall);
            this.Controls.Add(this.btn2ndCamera);
            this.Controls.Add(this.panelMainBrowser);
            this.Controls.Add(this.picClientCamera);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "KommFlexMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "KommFlex";
            this.Activated += new System.EventHandler(this.OnActivated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.onFormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.onFormClosed);
            this.Load += new System.EventHandler(this.OnFormLoad);
            this.Resize += new System.EventHandler(this.OnWindowResize);
            this.panelMainBrowser.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBackground)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picClientCamera)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelMainBrowser;
        private System.Windows.Forms.Button btn2ndCamera;
        private System.Windows.Forms.Button btnStartCall;
        private System.Windows.Forms.TextBox txtMsgBox;
        private System.Windows.Forms.Button btnSendMessage;
        private System.Windows.Forms.Button btnExitCall;
        private System.Windows.Forms.Button btnStartClient;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.PictureBox pictureBackground;
        private System.Windows.Forms.PictureBox picClientCamera;
        private System.Windows.Forms.Label lblImagePage;
        private System.Windows.Forms.Button btnPrevImage;
        private System.Windows.Forms.Button btnNextImage;
    }
}

