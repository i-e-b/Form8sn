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
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 33F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(726, 532);
            this.Font = new System.Drawing.Font("Calibri", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.Margin = new System.Windows.Forms.Padding(7, 8, 7, 8);
            this.MinimizeBox = false;
            this.Name = "BoxPlacer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Screen: drag to pan, scroll wheel to zoom;  Box: ctrl-drag to place, alt-click to edit, ctrl-shift to resize, ctrl-alt to move";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BoxPlacer_FormClosing);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BoxPlacer_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BoxPlacer_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BoxPlacer_MouseUp);
            this.ResumeLayout(false);
        }

        #endregion
    }
}