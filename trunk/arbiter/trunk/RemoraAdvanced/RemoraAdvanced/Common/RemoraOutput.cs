using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RemoraAdvanced.Common
{
	/// <summary>
	/// Type of output
	/// </summary>
	public enum OutputType
	{
		Remora,
		Arbiter
	}

	/// <summary>
	/// Output for remora
	/// </summary>
	public static class RemoraOutput
	{
		public static Remora RemoraMain;
		private static RichTextBox textBox;
		public static bool DisplayDateTimeOnOutput = true;

		/// <summary>
		/// Write a line to the output box and scroll down
		/// </summary>
		/// <param name="output"></param>
		public static void WriteLine(string output, OutputType outputType)
		{
			if ((outputType == OutputType.Arbiter && RemoraMain.ArbiterOutputButton.CheckState == CheckState.Checked) ||
				(outputType == OutputType.Remora && RemoraMain.RemoraOutputButton.CheckState == CheckState.Checked))
			{
				if (textBox != null)
				{
					// InvokeRequired required compares the thread ID of the
					// calling thread to the thread ID of the creating thread.
					// If these threads are different, it returns true.
					if (!textBox.InvokeRequired)
					{
						string text = textBox.Text;

						if (DisplayDateTimeOnOutput)
							text += (DateTime.Now.ToShortTimeString() + ": " + outputType.ToString() + ": " + output + "\n");
						else
							text += (output + "\n");

						if (text.Length > 5000)
							text = text.Substring(text.Length - 5000);

						textBox.Text = text;
						textBox.Select(textBox.Text.Length - 2, 0);
						textBox.ScrollToCaret();
					}
					else
					{
						// invoke
						RemoraMain.BeginInvoke(new MethodInvoker(delegate()
						{
							string text = textBox.Text;

							if (DisplayDateTimeOnOutput)
								text += (DateTime.Now.ToShortTimeString() + ": " + outputType.ToString() + ": " + output + "\n");
							else
								text += (output + "\n");

							if (text.Length > 5000)
								text = text.Substring(text.Length - 5000);

							textBox.Text = text;
							textBox.Select(textBox.Text.Length - 2, 0);
							textBox.ScrollToCaret();
						}));
					}
				}
			}
		}

		public static void SetTextBox(RichTextBox box)
		{
			textBox = box;
		}
	}
}
