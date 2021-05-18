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
            this.SuspendLayout();
            // 
            // wrapTextCheckbox
            // 
            this.wrapTextCheckbox.Location = new System.Drawing.Point(151, 58);
            this.wrapTextCheckbox.Name = "wrapTextCheckbox";
            this.wrapTextCheckbox.Size = new System.Drawing.Size(139, 33);
            this.wrapTextCheckbox.TabIndex = 0;
            this.wrapTextCheckbox.Text = "Wrap Text";
            this.wrapTextCheckbox.UseVisualStyleBackColor = true;
            // 
            // shrinkToFitCheckbox
            // 
            this.shrinkToFitCheckbox.Location = new System.Drawing.Point(151, 97);
            this.shrinkToFitCheckbox.Name = "shrinkToFitCheckbox";
            this.shrinkToFitCheckbox.Size = new System.Drawing.Size(139, 44);
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
            this.saveButton.Location = new System.Drawing.Point(418, 282);
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
            this.cancelButton.Location = new System.Drawing.Point(337, 282);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // topLeft
            // 
            this.topLeft.Location = new System.Drawing.Point(151, 166);
            this.topLeft.Name = "topLeft";
            this.topLeft.Size = new System.Drawing.Size(104, 24);
            this.topLeft.TabIndex = 6;
            this.topLeft.TabStop = true;
            this.topLeft.Text = "Top Left";
            this.topLeft.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(19, 157);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 42);
            this.label2.TabIndex = 7;
            this.label2.Text = "Text Alignment";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // topCentre
            // 
            this.topCentre.Location = new System.Drawing.Point(261, 166);
            this.topCentre.Name = "topCentre";
            this.topCentre.Size = new System.Drawing.Size(104, 24);
            this.topCentre.TabIndex = 8;
            this.topCentre.TabStop = true;
            this.topCentre.Text = "Top Centre";
            this.topCentre.UseVisualStyleBackColor = true;
            // 
            // topRight
            // 
            this.topRight.Location = new System.Drawing.Point(371, 166);
            this.topRight.Name = "topRight";
            this.topRight.Size = new System.Drawing.Size(104, 24);
            this.topRight.TabIndex = 9;
            this.topRight.TabStop = true;
            this.topRight.Text = "Top Right";
            this.topRight.UseVisualStyleBackColor = true;
            // 
            // midRight
            // 
            this.midRight.Location = new System.Drawing.Point(371, 196);
            this.midRight.Name = "midRight";
            this.midRight.Size = new System.Drawing.Size(104, 24);
            this.midRight.TabIndex = 12;
            this.midRight.TabStop = true;
            this.midRight.Text = "Midline Right";
            this.midRight.UseVisualStyleBackColor = true;
            // 
            // midCentre
            // 
            this.midCentre.Location = new System.Drawing.Point(261, 196);
            this.midCentre.Name = "midCentre";
            this.midCentre.Size = new System.Drawing.Size(104, 24);
            this.midCentre.TabIndex = 11;
            this.midCentre.TabStop = true;
            this.midCentre.Text = "Midline Centre";
            this.midCentre.UseVisualStyleBackColor = true;
            // 
            // midLeft
            // 
            this.midLeft.Location = new System.Drawing.Point(151, 196);
            this.midLeft.Name = "midLeft";
            this.midLeft.Size = new System.Drawing.Size(104, 24);
            this.midLeft.TabIndex = 10;
            this.midLeft.TabStop = true;
            this.midLeft.Text = "Midline Left";
            this.midLeft.UseVisualStyleBackColor = true;
            // 
            // bottomRight
            // 
            this.bottomRight.Location = new System.Drawing.Point(371, 226);
            this.bottomRight.Name = "bottomRight";
            this.bottomRight.Size = new System.Drawing.Size(104, 24);
            this.bottomRight.TabIndex = 15;
            this.bottomRight.TabStop = true;
            this.bottomRight.Text = "Bottom Right";
            this.bottomRight.UseVisualStyleBackColor = true;
            // 
            // bottomCentre
            // 
            this.bottomCentre.Location = new System.Drawing.Point(261, 226);
            this.bottomCentre.Name = "bottomCentre";
            this.bottomCentre.Size = new System.Drawing.Size(104, 24);
            this.bottomCentre.TabIndex = 14;
            this.bottomCentre.TabStop = true;
            this.bottomCentre.Text = "Bottom Centre";
            this.bottomCentre.UseVisualStyleBackColor = true;
            // 
            // bottomLeft
            // 
            this.bottomLeft.Location = new System.Drawing.Point(151, 226);
            this.bottomLeft.Name = "bottomLeft";
            this.bottomLeft.Size = new System.Drawing.Size(104, 24);
            this.bottomLeft.TabIndex = 13;
            this.bottomLeft.TabStop = true;
            this.bottomLeft.Text = "Bottom Left";
            this.bottomLeft.UseVisualStyleBackColor = true;
            // 
            // deleteButton
            // 
            this.deleteButton.Location = new System.Drawing.Point(12, 282);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(75, 23);
            this.deleteButton.TabIndex = 16;
            this.deleteButton.Text = "Delete";
            this.deleteButton.UseVisualStyleBackColor = false;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            // 
            // BoxEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(505, 317);
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
            this.Name = "BoxEdit";
            this.Text = "BoxEdit";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BoxEdit_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

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