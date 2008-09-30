using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Remora.Display.Forms
{
    public partial class Readme : Form
    {
        public Readme()
        {
            InitializeComponent();
        }

        /// <summary>
        /// On loading, we want to load up the readme file located in the top-level remora directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Readme_Load(object sender, EventArgs e)
        {
            // path to readme
            string relativePath = "..\\..\\Readme.txt";

            try
            {
                using (StreamReader sr = new StreamReader(relativePath)) 
                {
                    this.readmeTextBox.Text = sr.ReadToEnd();
                    sr.Dispose();
                    sr.Close();
                }
            }
            catch (Exception ex)
            {
                RemoraOutput.WriteLine(ex.ToString());
            }
        }
    }
}