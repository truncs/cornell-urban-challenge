using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Remora.Display
{
	/// <summary>
	/// Every object to be displayed must inplement this interface
	/// </summary>
	public interface IDisplayObject
	{
		void Render(Graphics g, WorldTransform t);
	}
}
