using System.ComponentModel;

namespace BasicImageFormFiller.EditForms
{
    partial class FilterEditor
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
            this.label1 = new System.Windows.Forms.Label();
            this.filterNameTextbox = new System.Windows.Forms.TextBox();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.dataPathLabel = new System.Windows.Forms.Label();
            this.dataButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.filterTypeComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.filterPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "Filter Name";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // filterNameTextbox
            // 
            this.filterNameTextbox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.filterNameTextbox.Location = new System.Drawing.Point(139, 11);
            this.filterNameTextbox.Name = "filterNameTextbox";
            this.filterNameTextbox.Size = new System.Drawing.Size(531, 20);
            this.filterNameTextbox.TabIndex = 1;
            // 
            // saveButton
            // 
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Location = new System.Drawing.Point(595, 348);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 2;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(514, 348);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // dataPathLabel
            // 
            this.dataPathLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dataPathLabel.Location = new System.Drawing.Point(139, 35);
            this.dataPathLabel.Name = "dataPathLabel";
            this.dataPathLabel.Size = new System.Drawing.Size(531, 23);
            this.dataPathLabel.TabIndex = 4;
            this.dataPathLabel.Text = "< data path >";
            this.dataPathLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataButton
            // 
            this.dataButton.Location = new System.Drawing.Point(58, 35);
            this.dataButton.Name = "dataButton";
            this.dataButton.Size = new System.Drawing.Size(75, 23);
            this.dataButton.TabIndex = 5;
            this.dataButton.Text = "Pick Data";
            this.dataButton.UseVisualStyleBackColor = true;
            this.dataButton.Click += new System.EventHandler(this.dataButton_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(33, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 23);
            this.label2.TabIndex = 6;
            this.label2.Text = "Filter Type";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // filterTypeComboBox
            // 
            this.filterTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.filterTypeComboBox.FormattingEnabled = true;
            this.filterTypeComboBox.Location = new System.Drawing.Point(139, 63);
            this.filterTypeComboBox.Name = "filterTypeComboBox";
            this.filterTypeComboBox.Size = new System.Drawing.Size(239, 21);
            this.filterTypeComboBox.TabIndex = 8;
            this.filterTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.filterTypeComboBox_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(33, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 23);
            this.label3.TabIndex = 9;
            this.label3.Text = "Filter Parameters";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // filterPropertyGrid
            // 
            this.filterPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.filterPropertyGrid.Location = new System.Drawing.Point(139, 97);
            this.filterPropertyGrid.Name = "filterPropertyGrid";
            this.filterPropertyGrid.Size = new System.Drawing.Size(531, 245);
            this.filterPropertyGrid.TabIndex = 10;
            // 
            // FilterEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(682, 383);
            this.Controls.Add(this.filterPropertyGrid);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.filterTypeComboBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.dataButton);
            this.Controls.Add(this.dataPathLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.filterNameTextbox);
            this.Controls.Add(this.label1);
            this.Name = "FilterEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FilterEditor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FilterEditor_FormClosing);
            this.Load += new System.EventHandler(this.FilterEditor_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.PropertyGrid filterPropertyGrid;

        private System.Windows.Forms.ComboBox filterTypeComboBox;
        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.Label dataPathLabel;
        private System.Windows.Forms.Button dataButton;

        private System.Windows.Forms.Label label2;

        private System.Windows.Forms.Button cancelButton;

        private System.Windows.Forms.Button saveButton;

        private System.Windows.Forms.TextBox filterNameTextbox;

        private System.Windows.Forms.Label label1;
        #endregion
    }
}