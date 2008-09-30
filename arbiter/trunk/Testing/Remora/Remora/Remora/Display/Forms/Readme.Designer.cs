namespace Remora.Display.Forms
{
    partial class Readme
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Readme));
            this.readmeTextBox = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // readmeTextBox
            // 
            this.readmeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.readmeTextBox.Location = new System.Drawing.Point(0, 0);
            this.readmeTextBox.Name = "readmeTextBox";
            this.readmeTextBox.Size = new System.Drawing.Size(392, 516);
            this.readmeTextBox.TabIndex = 0;
            this.readmeTextBox.Text = "";
            // 
            // Readme
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(392, 516);
            this.Controls.Add(this.readmeTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Readme";
            this.ShowInTaskbar = false;
            this.Text = "Readme";
            this.Load += new System.EventHandler(this.Readme_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox readmeTextBox;
    }
}