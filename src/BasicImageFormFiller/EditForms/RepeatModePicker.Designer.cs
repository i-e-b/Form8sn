using System.ComponentModel;

namespace BasicImageFormFiller.EditForms
{
    partial class RepeatModePicker
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RepeatModePicker));
            this.repeatsCheckbox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.dataPathLabel = new System.Windows.Forms.Label();
            this.pickDataButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // repeatsCheckbox
            // 
            this.repeatsCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.repeatsCheckbox.Location = new System.Drawing.Point(93, 114);
            this.repeatsCheckbox.Name = "repeatsCheckbox";
            this.repeatsCheckbox.Size = new System.Drawing.Size(419, 19);
            this.repeatsCheckbox.TabIndex = 0;
            this.repeatsCheckbox.Text = "Page is repeated";
            this.repeatsCheckbox.UseVisualStyleBackColor = true;
            this.repeatsCheckbox.CheckStateChanged += new System.EventHandler(this.repeatsCheckbox_CheckedChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(500, 102);
            this.label1.TabIndex = 1;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // saveButton
            // 
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Location = new System.Drawing.Point(437, 180);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 3;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(356, 180);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // dataPathLabel
            // 
            this.dataPathLabel.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.dataPathLabel.Location = new System.Drawing.Point(93, 153);
            this.dataPathLabel.Name = "dataPathLabel";
            this.dataPathLabel.Size = new System.Drawing.Size(100, 23);
            this.dataPathLabel.TabIndex = 5;
            this.dataPathLabel.Text = "<none>";
            // 
            // pickDataButton
            // 
            this.pickDataButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pickDataButton.Location = new System.Drawing.Point(12, 148);
            this.pickDataButton.Name = "pickDataButton";
            this.pickDataButton.Size = new System.Drawing.Size(75, 23);
            this.pickDataButton.TabIndex = 6;
            this.pickDataButton.Text = "Pick Data";
            this.pickDataButton.UseVisualStyleBackColor = true;
            this.pickDataButton.Click += new System.EventHandler(this.pickDataButton_Click);
            // 
            // RepeatModePicker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(524, 215);
            this.Controls.Add(this.pickDataButton);
            this.Controls.Add(this.dataPathLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.repeatsCheckbox);
            this.Name = "RepeatModePicker";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RepeatMode";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.RepeatModePicker_FormClosing);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label dataPathLabel;

        private System.Windows.Forms.Button pickDataButton;

        private System.Windows.Forms.Button cancelButton;

        private System.Windows.Forms.Button saveButton;

        private System.Windows.Forms.CheckBox repeatsCheckbox;

        private System.Windows.Forms.Label label1;

        #endregion
    }
}