using System.ComponentModel;

namespace BasicImageFormFiller.EditForms
{
    partial class BoxEdit
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
            this.wrapTextCheckbox = new System.Windows.Forms.CheckBox();
            this.shrinkToFitCheckbox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.boxKeyTextbox = new System.Windows.Forms.TextBox();
            this.saveButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.topLeft = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.topCentre = new System.Windows.Forms.RadioButton();
            this.topRight = new System.Windows.Forms.RadioButton();
            this.midRight = new System.Windows.Forms.RadioButton();
            this.midCentre = new System.Windows.Forms.RadioButton();
            this.midLeft = new System.Windows.Forms.RadioButton();
            this.bottomRight = new System.Windows.Forms.RadioButton();
            this.bottomCentre = new System.Windows.Forms.RadioButton();
            this.bottomLeft = new System.Windows.Forms.RadioButton();
            this.deleteButton = new System.Windows.Forms.Button();
            this.setMappingButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.dataPathLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.setDataFormatButton = new System.Windows.Forms.Button();
            this.displayFormatInfo = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.processOrderTextBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.fontSizeTextBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.dependsOnComboBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // wrapTextCheckbox
            // 
            this.wrapTextCheckbox.Location = new System.Drawing.Point(151, 73);
            this.wrapTextCheckbox.Name = "wrapTextCheckbox";
            this.wrapTextCheckbox.Size = new System.Drawing.Size(139, 22);
            this.wrapTextCheckbox.TabIndex = 0;
            this.wrapTextCheckbox.Text = "Wrap Text";
            this.wrapTextCheckbox.UseVisualStyleBackColor = true;
            // 
            // shrinkToFitCheckbox
            // 
            this.shrinkToFitCheckbox.Location = new System.Drawing.Point(296, 73);
            this.shrinkToFitCheckbox.Name = "shrinkToFitCheckbox";
            this.shrinkToFitCheckbox.Size = new System.Drawing.Size(139, 22);
            this.shrinkToFitCheckbox.TabIndex = 1;
            this.shrinkToFitCheckbox.Text = "Shrink to Fit";
            this.shrinkToFitCheckbox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(19, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 42);
            this.label1.TabIndex = 2;
            this.label1.Text = "Box Name\r\nmust be unique on page";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // boxKeyTextbox
            // 
            this.boxKeyTextbox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.boxKeyTextbox.Location = new System.Drawing.Point(151, 21);
            this.boxKeyTextbox.Name = "boxKeyTextbox";
            this.boxKeyTextbox.Size = new System.Drawing.Size(342, 20);
            this.boxKeyTextbox.TabIndex = 3;
            // 
            // saveButton
            // 
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Location = new System.Drawing.Point(418, 391);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 4;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(337, 391);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // topLeft
            // 
            this.topLeft.Location = new System.Drawing.Point(151, 289);
            this.topLeft.Name = "topLeft";
            this.topLeft.Size = new System.Drawing.Size(104, 24);
            this.topLeft.TabIndex = 6;
            this.topLeft.TabStop = true;
            this.topLeft.Text = "Top Left";
            this.topLeft.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(19, 280);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 42);
            this.label2.TabIndex = 7;
            this.label2.Text = "Text Alignment";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // topCentre
            // 
            this.topCentre.Location = new System.Drawing.Point(261, 289);
            this.topCentre.Name = "topCentre";
            this.topCentre.Size = new System.Drawing.Size(104, 24);
            this.topCentre.TabIndex = 8;
            this.topCentre.TabStop = true;
            this.topCentre.Text = "Top Centre";
            this.topCentre.UseVisualStyleBackColor = true;
            // 
            // topRight
            // 
            this.topRight.Location = new System.Drawing.Point(371, 289);
            this.topRight.Name = "topRight";
            this.topRight.Size = new System.Drawing.Size(104, 24);
            this.topRight.TabIndex = 9;
            this.topRight.TabStop = true;
            this.topRight.Text = "Top Right";
            this.topRight.UseVisualStyleBackColor = true;
            // 
            // midRight
            // 
            this.midRight.Location = new System.Drawing.Point(371, 319);
            this.midRight.Name = "midRight";
            this.midRight.Size = new System.Drawing.Size(104, 24);
            this.midRight.TabIndex = 12;
            this.midRight.TabStop = true;
            this.midRight.Text = "Midline Right";
            this.midRight.UseVisualStyleBackColor = true;
            // 
            // midCentre
            // 
            this.midCentre.Location = new System.Drawing.Point(261, 319);
            this.midCentre.Name = "midCentre";
            this.midCentre.Size = new System.Drawing.Size(104, 24);
            this.midCentre.TabIndex = 11;
            this.midCentre.TabStop = true;
            this.midCentre.Text = "Midline Centre";
            this.midCentre.UseVisualStyleBackColor = true;
            // 
            // midLeft
            // 
            this.midLeft.Location = new System.Drawing.Point(151, 319);
            this.midLeft.Name = "midLeft";
            this.midLeft.Size = new System.Drawing.Size(104, 24);
            this.midLeft.TabIndex = 10;
            this.midLeft.TabStop = true;
            this.midLeft.Text = "Midline Left";
            this.midLeft.UseVisualStyleBackColor = true;
            // 
            // bottomRight
            // 
            this.bottomRight.Location = new System.Drawing.Point(371, 349);
            this.bottomRight.Name = "bottomRight";
            this.bottomRight.Size = new System.Drawing.Size(104, 24);
            this.bottomRight.TabIndex = 15;
            this.bottomRight.TabStop = true;
            this.bottomRight.Text = "Bottom Right";
            this.bottomRight.UseVisualStyleBackColor = true;
            // 
            // bottomCentre
            // 
            this.bottomCentre.Location = new System.Drawing.Point(261, 349);
            this.bottomCentre.Name = "bottomCentre";
            this.bottomCentre.Size = new System.Drawing.Size(104, 24);
            this.bottomCentre.TabIndex = 14;
            this.bottomCentre.TabStop = true;
            this.bottomCentre.Text = "Bottom Centre";
            this.bottomCentre.UseVisualStyleBackColor = true;
            // 
            // bottomLeft
            // 
            this.bottomLeft.Location = new System.Drawing.Point(151, 349);
            this.bottomLeft.Name = "bottomLeft";
            this.bottomLeft.Size = new System.Drawing.Size(104, 24);
            this.bottomLeft.TabIndex = 13;
            this.bottomLeft.TabStop = true;
            this.bottomLeft.Text = "Bottom Left";
            this.bottomLeft.UseVisualStyleBackColor = true;
            // 
            // deleteButton
            // 
            this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.deleteButton.Location = new System.Drawing.Point(12, 391);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(75, 23);
            this.deleteButton.TabIndex = 16;
            this.deleteButton.Text = "Delete";
            this.deleteButton.UseVisualStyleBackColor = false;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            // 
            // setMappingButton
            // 
            this.setMappingButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.setMappingButton.Location = new System.Drawing.Point(441, 47);
            this.setMappingButton.Name = "setMappingButton";
            this.setMappingButton.Size = new System.Drawing.Size(52, 23);
            this.setMappingButton.TabIndex = 17;
            this.setMappingButton.Text = "Edit";
            this.setMappingButton.UseVisualStyleBackColor = true;
            this.setMappingButton.Click += new System.EventHandler(this.setMappingButton_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(45, 47);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 23);
            this.label3.TabIndex = 18;
            this.label3.Text = "Data Map";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // dataPathLabel
            // 
            this.dataPathLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.dataPathLabel.Location = new System.Drawing.Point(151, 47);
            this.dataPathLabel.Name = "dataPathLabel";
            this.dataPathLabel.Size = new System.Drawing.Size(284, 23);
            this.dataPathLabel.TabIndex = 19;
            this.dataPathLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(45, 134);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 31);
            this.label4.TabIndex = 20;
            this.label4.Text = "Display Format\r\n(optional)";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // setDataFormatButton
            // 
            this.setDataFormatButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.setDataFormatButton.Location = new System.Drawing.Point(418, 134);
            this.setDataFormatButton.Name = "setDataFormatButton";
            this.setDataFormatButton.Size = new System.Drawing.Size(75, 23);
            this.setDataFormatButton.TabIndex = 21;
            this.setDataFormatButton.Text = "Set Format";
            this.setDataFormatButton.UseVisualStyleBackColor = true;
            this.setDataFormatButton.Click += new System.EventHandler(this.setDataFormatButton_Click);
            // 
            // displayFormatInfo
            // 
            this.displayFormatInfo.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.displayFormatInfo.Location = new System.Drawing.Point(151, 134);
            this.displayFormatInfo.Name = "displayFormatInfo";
            this.displayFormatInfo.Size = new System.Drawing.Size(261, 58);
            this.displayFormatInfo.TabIndex = 22;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(45, 193);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 34);
            this.label5.TabIndex = 23;
            this.label5.Text = "Processing Order\r\n(optional)";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // processOrderTextBox
            // 
            this.processOrderTextBox.Location = new System.Drawing.Point(151, 190);
            this.processOrderTextBox.Name = "processOrderTextBox";
            this.processOrderTextBox.Size = new System.Drawing.Size(100, 20);
            this.processOrderTextBox.TabIndex = 24;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(45, 102);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 32);
            this.label6.TabIndex = 25;
            this.label6.Text = "Font Size\r\n(optional)";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // fontSizeTextBox
            // 
            this.fontSizeTextBox.Location = new System.Drawing.Point(151, 99);
            this.fontSizeTextBox.Name = "fontSizeTextBox";
            this.fontSizeTextBox.Size = new System.Drawing.Size(100, 20);
            this.fontSizeTextBox.TabIndex = 26;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(12, 236);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(133, 44);
            this.label7.TabIndex = 27;
            this.label7.Text = "Depends On\r\n(hide box if other is empty)";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // dependsOnComboBox
            // 
            this.dependsOnComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dependsOnComboBox.FormattingEnabled = true;
            this.dependsOnComboBox.Location = new System.Drawing.Point(151, 233);
            this.dependsOnComboBox.Name = "dependsOnComboBox";
            this.dependsOnComboBox.Size = new System.Drawing.Size(121, 21);
            this.dependsOnComboBox.TabIndex = 28;
            // 
            // BoxEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(505, 426);
            this.Controls.Add(this.dependsOnComboBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.fontSizeTextBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.processOrderTextBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.displayFormatInfo);
            this.Controls.Add(this.setDataFormatButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.dataPathLabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.setMappingButton);
            this.Controls.Add(this.deleteButton);
            this.Controls.Add(this.bottomRight);
            this.Controls.Add(this.bottomCentre);
            this.Controls.Add(this.bottomLeft);
            this.Controls.Add(this.midRight);
            this.Controls.Add(this.midCentre);
            this.Controls.Add(this.midLeft);
            this.Controls.Add(this.topRight);
            this.Controls.Add(this.topCentre);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.topLeft);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.boxKeyTextbox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.shrinkToFitCheckbox);
            this.Controls.Add(this.wrapTextCheckbox);
            this.MinimumSize = new System.Drawing.Size(521, 420);
            this.Name = "BoxEdit";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BoxEdit";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BoxEdit_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ComboBox dependsOnComboBox;

        private System.Windows.Forms.Label label7;

        private System.Windows.Forms.TextBox fontSizeTextBox;

        private System.Windows.Forms.Label label6;

        private System.Windows.Forms.TextBox processOrderTextBox;

        private System.Windows.Forms.Label label5;

        private System.Windows.Forms.Label displayFormatInfo;

        private System.Windows.Forms.Button setDataFormatButton;

        private System.Windows.Forms.Label label4;

        private System.Windows.Forms.Label dataPathLabel;

        private System.Windows.Forms.Label label3;

        private System.Windows.Forms.Button setMappingButton;

        private System.Windows.Forms.Button deleteButton;


        private System.Windows.Forms.RadioButton topLeft;
        private System.Windows.Forms.RadioButton topCentre;
        private System.Windows.Forms.RadioButton topRight;
        private System.Windows.Forms.RadioButton midCentre;
        private System.Windows.Forms.RadioButton midRight;
        private System.Windows.Forms.RadioButton midLeft;
        private System.Windows.Forms.RadioButton bottomLeft;
        private System.Windows.Forms.RadioButton bottomRight;
        private System.Windows.Forms.RadioButton bottomCentre;
        
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label2;

        private System.Windows.Forms.Button saveButton;

        private System.Windows.Forms.TextBox boxKeyTextbox;

        private System.Windows.Forms.CheckBox shrinkToFitCheckbox;
        private System.Windows.Forms.CheckBox wrapTextCheckbox;
        private System.Windows.Forms.Label label1;

        #endregion
    }
}