using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Remora
{
	public static class RemoraOutput
	{
		private static RichTextBox textBox;
        public static bool DisplayDateTimeOnOutput = true;

		/// <summary>
		/// Write a line to the output box and scroll down
		/// </summary>
		/// <param name="output"></param>
		public static void WriteLine(string output)
		{
			if (textBox != null)
			{
				string text = textBox.Text;

                if(DisplayDateTimeOnOutput)
				    text += (DateTime.Now.ToLongTimeString() + ": " + output + "\n");
                else
                    text += (output + "\n");

				textBox.Text = text;
				textBox.Select(textBox.Text.Length - 2, 0);
                textBox.ScrollToCaret();
			}
		}

		public static void SetTextBox(RichTextBox box)
		{
			textBox = box;
		}
	}
}
