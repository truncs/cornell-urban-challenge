using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace Publisher
{
	public class DebugTextObject : TraceListener
	{
		TextBox tb;
		public DebugTextObject(TextBox tb)
		{
			this.tb = tb;
		}
		public override void Write(string message)
		{
			//if ((tb.Handle!=null) && (tb.Visible))
			tb.BeginInvoke(new MethodInvoker(delegate()
			{
				tb.AppendText(message);
			}));
		}

		public override void WriteLine(string message)
		{
			//if ((tb.Handle != null) && (tb.Visible))
			tb.BeginInvoke(new MethodInvoker(delegate()
			{
				tb.AppendText(message + Environment.NewLine);
			}));
		}
	}
}
