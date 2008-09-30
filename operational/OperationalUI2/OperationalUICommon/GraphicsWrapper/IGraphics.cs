using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace UrbanChallenge.OperationalUI.Common.GraphicsWrapper {
	public interface IGraphics {
		IPen CreatePen();

		void DrawBezier(IPen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4);
		void DrawBeziers(IPen pen, PointF[] pts);
		void DrawEllipse(IPen pen, RectangleF rect);
		void DrawRectangle(IPen pen, RectangleF rect);
		void DrawLine(IPen pen, PointF pt1, PointF pt2);
		void DrawLines(IPen pen, PointF[] pts);
		void DrawPolygon(IPen pen, PointF[] pts);
		void DrawCross(IPen pen, PointF pt, float size);

		void FillRectangle(Color color, RectangleF rect);
		void FillPolygon(Color color, PointF[] pts);
		void FillEllipse(Color color, RectangleF rect);

		SizeF MeasureString(string str, Font font);
		void DrawString(string str, Font font, Color color, PointF loc);

		void InitScene(WorldTransform wt, Color background);

		void GoToVehicleCoordinates(PointF loc, float heading);
		void ComeBackFromVehicleCoordinates();

		void PushMatrix();
		void PopMatrix();

		void Translate(float dx, float dy);
		/// <summary>
		/// Pushes a rotation matrix onto the current transformation stack
		/// </summary>
		/// <param name="theta">Angle to rotate in degrees</param>
		void Rotate(float theta);
	}
}
