using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Mapack;
using RndfEditor.Display.Utilities;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	/// <summary>
	/// Represents a cost map for a zone
	/// </summary>
	[Serializable]
	public class ArbiterZoneCostMap : IDisplayObject, INetworkObject
	{
		/// <summary>
		/// Top Left Point of the Map
		/// </summary>
		public Coordinates MapTopLeft;

		/// <summary>
		/// Spacing, in meters of the grid
		/// </summary>
		public double Spacing;

		/// <summary>
		/// The actual cost map
		/// </summary>
		public Matrix CostMap;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mapTopLeft"></param>
		/// <param name="spacing"></param>
		/// <param name="mapBottomRight"></param>
		public ArbiterZoneCostMap(Coordinates mapTopLeft, double spacing, Coordinates mapBottomRight)
		{
			this.MapTopLeft = mapTopLeft;
			this.Spacing = spacing;
			this.CostMap = new Matrix((int)Math.Ceiling(Math.Abs((mapTopLeft.Y - mapBottomRight.Y) / spacing)), (int)Math.Ceiling(Math.Abs((mapBottomRight.X - mapTopLeft.X) / spacing)));
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mapTopLeft"></param>
		/// <param name="spacing"></param>
		/// <param name="costMap"></param>
		public ArbiterZoneCostMap(Coordinates mapTopLeft, double spacing, Matrix costMap)
		{
			this.MapTopLeft = mapTopLeft;
			this.Spacing = spacing;
			this.CostMap = costMap;
		}

		/// <summary>
		/// Projects the current matrix of some spacing and position onto the input matrix of a separate spacing and position
		/// </summary>
		/// <param name="input"></param>
		/// <param name="inputTopLeft"></param>
		/// <param name="inputSpacing"></param>
		/// <returns></returns>
		public Matrix ProjectCostMap(Matrix input, Coordinates inputTopLeft, double inputSpacing)
		{
			// returns the input for now
			return input;
		}

		/// <summary>
		/// Projects the input matrix of some top left corner and spacing onto the internal cost map
		/// </summary>
		/// <param name="input"></param>
		/// <param name="inputTopLeft"></param>
		/// <param name="inputSpacing"></param>
		public void ProjectOntoMap(Matrix input, Coordinates inputTopLeft, double inputSpacing)
		{
		}

		/// <summary>
		/// Projects cost onto the map given some point, dropoff distance from point, and furthest bound distance
		/// </summary>
		/// <param name="point"></param>
		/// <param name="mu"></param>
		/// <param name="distance"></param>
		public void ProjectPointCostOntoMap(Coordinates point, double mu, double distance)
		{
		}

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
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
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}
}
