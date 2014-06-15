namespace istatServer
{
    partial class Settings
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.pinGroupLabel = new System.Windows.Forms.Label();
            this.pinText = new System.Windows.Forms.TextBox();
            this.instructionLabel = new System.Windows.Forms.Label();
            this.cancelLabel = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.resetGroupLabel = new System.Windows.Forms.Label();
            this.resetAuthButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(54)))), ((int)(((byte)(80)))), ((int)(((byte)(128)))));
            this.panel1.Location = new System.Drawing.Point(-1, 14);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(10, 168);
            this.panel1.TabIndex = 0;
            // 
            // pinGroupLabel
            // 
            this.pinGroupLabel.AutoSize = true;
            this.pinGroupLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pinGroupLabel.Location = new System.Drawing.Point(28, 9);
            this.pinGroupLabel.Name = "pinGroupLabel";
            this.pinGroupLabel.Size = new System.Drawing.Size(108, 30);
            this.pinGroupLabel.TabIndex = 1;
            this.pinGroupLabel.Text = "PIN CODE";
            // 
            // pinText
            // 
            this.pinText.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pinText.Location = new System.Drawing.Point(33, 72);
            this.pinText.Name = "pinText";
            this.pinText.Size = new System.Drawing.Size(146, 25);
            this.pinText.TabIndex = 2;
            this.pinText.TextChanged += new System.EventHandler(this.PinTextChanged);
            // 
            // instructionLabel
            // 
            this.instructionLabel.AutoSize = true;
            this.instructionLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.instructionLabel.Location = new System.Drawing.Point(30, 52);
            this.instructionLabel.Name = "instructionLabel";
            this.instructionLabel.Size = new System.Drawing.Size(123, 17);
            this.instructionLabel.TabIndex = 3;
            this.instructionLabel.Text = "Enter a 5 digit code";
            // 
            // cancelLabel
            // 
            this.cancelLabel.AutoSize = true;
            this.cancelLabel.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelLabel.ForeColor = System.Drawing.Color.Red;
            this.cancelLabel.Location = new System.Drawing.Point(231, 9);
            this.cancelLabel.Name = "cancelLabel";
            this.cancelLabel.Size = new System.Drawing.Size(39, 13);
            this.cancelLabel.TabIndex = 4;
            this.cancelLabel.Text = "cancel";
            this.cancelLabel.Click += new System.EventHandler(this.CancelClick);
            // 
            // saveButton
            // 
            this.saveButton.BackColor = System.Drawing.Color.Transparent;
            this.saveButton.Enabled = false;
            this.saveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.saveButton.Location = new System.Drawing.Point(195, 72);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 5;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = false;
            this.saveButton.Click += new System.EventHandler(this.SaveClick);
            // 
            // resetGroupLabel
            // 
            this.resetGroupLabel.AutoSize = true;
            this.resetGroupLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.resetGroupLabel.Location = new System.Drawing.Point(28, 123);
            this.resetGroupLabel.Name = "resetGroupLabel";
            this.resetGroupLabel.Size = new System.Drawing.Size(255, 30);
            this.resetGroupLabel.TabIndex = 6;
            this.resetGroupLabel.Text = "RESET AUTHORIZATIONS";
            // 
            // resetAuthButton
            // 
            this.resetAuthButton.BackColor = System.Drawing.Color.Transparent;
            this.resetAuthButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.resetAuthButton.Location = new System.Drawing.Point(195, 159);
            this.resetAuthButton.Name = "resetAuthButton";
            this.resetAuthButton.Size = new System.Drawing.Size(75, 23);
            this.resetAuthButton.TabIndex = 7;
            this.resetAuthButton.Text = "Reset";
            this.resetAuthButton.UseVisualStyleBackColor = false;
            this.resetAuthButton.Click += new System.EventHandler(this.ResetAuthButtonClick);
            // 
            // Settings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(285, 196);
            this.Controls.Add(this.resetAuthButton);
            this.Controls.Add(this.resetGroupLabel);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.cancelLabel);
            this.Controls.Add(this.instructionLabel);
            this.Controls.Add(this.pinText);
            this.Controls.Add(this.pinGroupLabel);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Settings";
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label pinGroupLabel;
        private System.Windows.Forms.TextBox pinText;
        private System.Windows.Forms.Label instructionLabel;
        private System.Windows.Forms.Label cancelLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Label resetGroupLabel;
        private System.Windows.Forms.Button resetAuthButton;
    }
}