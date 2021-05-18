using System.ComponentModel;

namespace BasicImageFormFiller.EditForms
{
    sealed partial class BoxPlacer
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
            this.SuspendLayout();
            // 
            // BoxPlacer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.MinimizeBox = false;
            this.Name = "BoxPlacer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Drag to pan, Ctrl-drag to place box, alt-click to edit, scroll wheel to zoom";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BoxPlacer_FormClosing);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BoxPlacer_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BoxPlacer_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BoxPlacer_MouseUp);
            this.ResumeLayout(false);
        }

        #endregion
    }
}