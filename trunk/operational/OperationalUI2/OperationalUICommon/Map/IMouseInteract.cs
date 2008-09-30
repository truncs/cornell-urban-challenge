using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public interface IMouseInteract {
		void OnMouseDown(MouseEvent e);
		void OnMouseMove(MouseEvent e);
		void OnMouseUp(MouseEvent e);
	}
}
