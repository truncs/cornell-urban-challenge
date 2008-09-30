using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using RndfEditor.Forms;

namespace RndfEditor.Common
{
	/// <summary>
	/// Allows for output form the editor
	/// </summary>
	public static class EditorOutput
	{
		private static Editor editor;
		private static RichTextBox textBox;
		public static bool DisplayDateTimeOnOutput = true;

		/// <summary>
		/// Write a line to the output box and scroll down
		/// </summary>
		/// <param name="output"></param>
		public static void WriteLine(string output)
		{
			if (!textBox.InvokeRequired)
			{
				if (textBox != null)
				{
					string text = textBox.Text;

					if (DisplayDateTimeOnOutput)
						text += (DateTime.Now.ToLongTimeString() + ": " + output + "\n");
					else
						text += (output + "\n");

					textBox.Text = text;
					textBox.Select(textBox.Text.Length - 2, 0);
					textBox.ScrollToCaret();
				}
			}
			else if(editor != null && textBox != null)
			{
				// invoke
				editor.BeginInvoke(new MethodInvoker(delegate()
				{
					string text = textBox.Text;

					if (DisplayDateTimeOnOutput)
						text += (DateTime.Now.ToLongTimeString() + ": " + output + "\n");
					else
						text += (output + "\n");

					textBox.Text = text;
					textBox.Select(textBox.Text.Length - 2, 0);
					textBox.ScrollToCaret();
				}));
			}
		}

		public static void SetTextBox(RichTextBox box, Editor ed)
		{
			textBox = box;
			editor = ed;
		}
	}
}
