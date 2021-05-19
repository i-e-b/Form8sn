using System.ComponentModel;

namespace BasicImageFormFiller.EditForms
{
    partial class PickDataSource
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
            this.treeView = new System.Windows.Forms.TreeView();
            this.pickButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.pathPreview = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // treeView
            // 
            this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView.Location = new System.Drawing.Point(12, 12);
            this.treeView.Name = "treeView";
            this.treeView.Size = new System.Drawing.Size(595, 358);
            this.treeView.TabIndex = 0;
            this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
            // 
            // pickButton
            // 
            this.pickButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pickButton.Enabled = false;
            this.pickButton.Location = new System.Drawing.Point(532, 376);
            this.pickButton.Name = "pickButton";
            this.pickButton.Size = new System.Drawing.Size(75, 23);
            this.pickButton.TabIndex = 1;
            this.pickButton.Text = "Pick";
            this.pickButton.UseVisualStyleBackColor = true;
            this.pickButton.Click += new System.EventHandler(this.pickButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Location = new System.Drawing.Point(451, 376);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 2;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // pathPreview
            // 
            this.pathPreview.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.pathPreview.Location = new System.Drawing.Point(12, 376);
            this.pathPreview.Name = "pathPreview";
            this.pathPreview.Size = new System.Drawing.Size(433, 23);
            this.pathPreview.TabIndex = 3;
            this.pathPreview.Text = "Select a node";
            // 
            // PickDataSource
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(619, 411);
            this.Controls.Add(this.pathPreview);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.pickButton);
            this.Controls.Add(this.treeView);
            this.Name = "PickDataSource";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PickDataSource";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label pathPreview;

        private System.Windows.Forms.Button pickButton;
        private System.Windows.Forms.Button cancelButton;

        private System.Windows.Forms.Button button2;

        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.Button button1;

        #endregion
    }
}