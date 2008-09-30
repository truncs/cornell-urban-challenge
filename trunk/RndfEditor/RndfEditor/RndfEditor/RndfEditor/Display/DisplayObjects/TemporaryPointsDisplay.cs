using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.EarthModel;
using System.IO;
using RndfToolkit;
using RndfEditor.Display.Utilities;
using System.Drawing;

namespace RndfEditor.Display.DisplayObjects
{
	/// <summary>
	/// Hodler for temporary points not part of the rndf
	/// </summary>
	[Serializable]
	public class TemporaryPointsDisplay : IDisplayObject
	{
		/// <summary>
		/// Points we are temporarily holding
		/// </summary>
		public Dictionary<string, Coordinates> Points;

		/// <summary>
		/// Constructor
		/// </summary>
		public TemporaryPointsDisplay()
		{
			this.Points = new Dictionary<string, Coordinates>();
		}

		/// <summary>
		/// Reads in a file with arc mins to coords
		/// </summary>
		/// <param name="fileName"></param>
		public void ReadArcMinutesFromFile(string fileName, PlanarProjection pp)
		{
			Points = new Dictionary<string, Coordinates>();
			FileStream fs = new FileStream(fileName, FileMode.Open);
			StreamReader sr = new StreamReader(fs);

			while (!sr.EndOfStream)
			{
				string lineStream = sr.ReadLine();

				string[] del1 = new string[] {"\t"};
				string[] nwSplit = lineStream.Split(del1, StringSplitOptions.RemoveEmptyEntries);

				/*string name = nwSplit[0];

				string[] del2 = new string[] { " " };
				string[] n = nwSplit[1].Split(del2, StringSplitOptions.RemoveEmptyEntries);
				string[] w = nwSplit[2].Split(del2, StringSplitOptions.RemoveEmptyEntries);
				 */
				string name = nwSplit[0];
				double degN = double.Parse(nwSplit[1]);
				double degE = double.Parse(nwSplit[2]);
				Coordinates c = pp.ECEFtoXY(WGS84.LLAtoECEF((new LLACoord(degN * Math.PI / 180.0, degE * Math.PI / 180.0, 0.0))));				
				Points.Add(name, c);
			}

			sr.Close();
			fs.Dispose();
		}

		/// <summary>
		/// Saves the temp points to a file of degrees
		/// </summary>
		/// <param name="fileName"></param>
		public void SaveToFileAsDegrees(string fileName, PlanarProjection pp)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);

			foreach (KeyValuePair<string, Coordinates> c in this.Points)
			{
				LLACoord lla = GpsTools.XyToLlaDegrees(c.Value, pp);
				//LLACoord lla = new LLACoord(c.Value.X, c.Value.Y, 0);
				sw.WriteLine(c.Key + "\t" + lla.lat.ToString("F6") + "\t" + lla.lon.ToString("F6"));
			}

			sw.Close();
			fs.Dispose();
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			Color c = DrawingUtility.ColorRndfEditorTemporaryPoint;

			foreach (KeyValuePair<string, Coordinates> point in Points)
			{
				if(!DrawingUtility.DrawRndfEditorTemporaryPointId)
					DrawingUtility.DrawControlPoint(point.Value, c, null, ContentAlignment.TopCenter, ControlPointStyle.SmallCircle, g, t);
				else
					DrawingUtility.DrawControlPoint(point.Value, c, point.Key, ContentAlignment.TopCenter, ControlPointStyle.SmallCircle, g, t);
			}
		}

		public bool MoveAllowed
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public void BeginMove(Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(Coordinates orig, Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public SelectionType Selected
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public IDisplayObject Parent
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public bool CanDelete
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public List<IDisplayObject> Delete()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDeselect(IDisplayObject newSelection)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool ShouldDraw()
		{
			return DrawingUtility.DrawRndfEditorTemporaryPoint;
		}

		#endregion
	}
}
