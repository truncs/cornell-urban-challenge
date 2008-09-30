using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core.Common.Tools
{
	public static class PolygonToolkit
	{
		/// <summary>
		/// Takes union of a list of polygons
		/// </summary>
		/// <param name="inputs"></param>
		/// <returns></returns>
		public static UrbanChallenge.Common.Shapes.Polygon PolygonUnion(
			List<UrbanChallenge.Common.Shapes.Polygon> inputs)
		{
			GpcWrapper.Polygon final = null;

			foreach (UrbanChallenge.Common.Shapes.Polygon i in inputs)
			{
				if (final == null)
					final = ToGpcPolygon(i);
				else
					final = GpcWrapper.GpcWrapper.Clip(GpcWrapper.GpcOperation.Union, ToGpcPolygon(i), final);
			}

			return ToShapePolygon(final);
		}

		public static GpcWrapper.Polygon ToGpcPolygon(UrbanChallenge.Common.Shapes.Polygon input)
		{
			GraphicsPath gp = new GraphicsPath();
			PointF[] polyPoints = new PointF[input.Count];

			for (int i = 0; i < input.Count; i++)
			{
				polyPoints[i] = DrawingUtility.ToPointF(input[i]);
			}

			gp.AddPolygon(polyPoints);
			return new GpcWrapper.Polygon(gp);
		}

		public static UrbanChallenge.Common.Shapes.Polygon ToShapePolygon(GpcWrapper.Polygon input)
		{
			PointF[] polyPoints = input.ToGraphicsPath().PathPoints;
			List<Coordinates> polyCoords = new List<Coordinates>();

			for (int i = 0; i < polyPoints.Length; i++)
			{
				polyCoords.Add(DrawingUtility.ToCoord(polyPoints[i]));
			}

			return new UrbanChallenge.Common.Shapes.Polygon(polyCoords);
		}

		public static UrbanChallenge.Common.Shapes.Polygon PolygonIntersection(UrbanChallenge.Common.Shapes.Polygon polygon, UrbanChallenge.Common.Shapes.Polygon eePolygon)
		{
			GpcWrapper.Polygon one = ToGpcPolygon(polygon);
			GpcWrapper.Polygon two = ToGpcPolygon(eePolygon);
			GpcWrapper.Polygon final = GpcWrapper.GpcWrapper.Clip(GpcWrapper.GpcOperation.Intersection, one, two);

			return ToShapePolygon(final);
		}
	}
}
