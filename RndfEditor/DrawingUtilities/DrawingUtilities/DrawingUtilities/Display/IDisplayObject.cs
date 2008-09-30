using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using UrbanChallenge.Common;

namespace RndfEditor.Display.Utilities
{
	/// <summary>
	/// Filters display objects by type
	/// </summary>
	/// <param name="dispObj"></param>
	/// <returns></returns>
	public delegate bool DisplayObjectFilter(IDisplayObject dispObj);

	/// <summary>
	/// Interface for all objects to be displayed
	/// </summary>
	public interface IDisplayObject
	{
		RectangleF GetBoundingBox(WorldTransform wt);
		HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter);
		void Render(Graphics g, WorldTransform t);
		bool MoveAllowed { get; }
		void BeginMove(Coordinates orig, WorldTransform t);
		void InMove(Coordinates orig, Coordinates offset, WorldTransform t);
		void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t);
		void CancelMove(Coordinates orig, WorldTransform t);
		SelectionType Selected { get; set; }
		IDisplayObject Parent { get; }
		bool CanDelete { get;}

		/// <summary>
		/// Deleter the object
		/// </summary>
		/// <returns>List of objects to remove from the display objects</returns>
		List<IDisplayObject> Delete();


		bool ShouldDeselect(IDisplayObject newSelection);
		bool ShouldDraw();
	}
}
