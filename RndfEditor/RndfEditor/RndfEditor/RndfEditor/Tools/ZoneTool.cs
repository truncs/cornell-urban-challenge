using System;
using System.Collections.Generic;
using System.Text;
using RndfEditor.Display.Utilities;
using UrbanChallenge.Arbiter.ArbiterRoads;
using RndfEditor.Forms;
using UrbanChallenge.Common;
using System.Drawing;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;

namespace RndfEditor.Tools
{
	/// <summary>
	/// Tools to help with zones
	/// </summary>
	public class ZoneTool : IDisplayObject, IEditorTool
	{
		#region Zone Tool

		// items to help with intersections
		public ArbiterRoadNetwork arn;
		public RoadDisplay rd;
		public Editor ed;
		public ZoneToolbox zt;
		public Coordinates CurrentMouse;
				
		// items to help with wrapping
		public List<Coordinates> WrappingHelpers;		
		public INavigableNode PreviousNode;	
	
		// right click crap
		public INavigableNode rightClickNode;
		public NavigableEdge rightClickEdge;

		// moving nodes crap
		public Coordinates moveOriginalCoords;
		public INavigableNode moveNode;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="arn"></param>
		/// <param name="rd"></param>
		/// <param name="ed"></param>
		public ZoneTool(ArbiterRoadNetwork arn, RoadDisplay rd, Editor ed)
		{
			// set helpers we can access
			this.arn = arn;
			this.rd = rd;
			this.ed = ed;

			// helpers to wrap intersections for polygons
			this.WrappingHelpers = new List<Coordinates>();

			// create toolbox
			zt = new ZoneToolbox(this);
			zt.Show();
		}

		/// <summary>
		/// Zone toolbox
		/// </summary>
		public ZoneToolbox Toolbox
		{
			get { return zt; }
			set { zt = value; }
		}

		/// <summary>
		/// the intersection toolbox mode
		/// </summary>
		public ZoneToolboxMode Mode
		{
			get { return zt.Mode; }
		}

		public void ShutDown()
		{
			if (!zt.IsDisposed)
			{
				zt.Close();
			}
		}

		public void NavigationHitTest(Coordinates c, out INavigableNode node, out NavigableEdge edge)
		{
			if (this.zt.current != null)
			{
				// Determine size of bounding box
				float scaled_offset = 1 / this.rd.WorldTransform.Scale;

				// invert the scale
				float scaled_size = DrawingUtility.cp_large_size;

				// assume that the world transform is currently applied correctly to the graphics
				RectangleF rect = new RectangleF((float)c.X - scaled_size / 2, (float)c.Y - scaled_size / 2, scaled_size, scaled_size);

				foreach (ArbiterParkingSpotWaypoint apsw in this.zt.current.ParkingSpotWaypoints.Values)
				{
					if (apsw.Position.DistanceTo(c) < 1.0)
					{
						node = apsw;
						edge = null;
						return;
					}
				}

				foreach (ArbiterPerimeterWaypoint apw in this.zt.current.Perimeter.PerimeterPoints.Values)
				{
					if ((apw.IsExit || apw.IsEntry) && rect.Contains(DrawingUtility.ToPointF(apw.Position)) && 
						(this.PreviousNode == null || !this.PreviousNode.Equals(apw)) &&
						apw.Position.DistanceTo(c) < 1.0)
					{
						node = apw;
						edge = null;
						return;
					}
				}

				foreach (INavigableNode inn in this.zt.current.NavigationNodes)
				{
					if (rect.Contains(DrawingUtility.ToPointF(inn.Position)) &&
						(this.PreviousNode == null || !this.PreviousNode.Equals(inn)) &&
						inn.Position.DistanceTo(c) < 1.0)
					{
						node = inn;
						edge = null;
						return;
					}
				}

				NavigableEdge closest = null;
				double distance = double.MaxValue;
				foreach (NavigableEdge ne in this.zt.current.NavigableEdges)
				{
					LinePath lp = new LinePath(new Coordinates[] { ne.Start.Position, ne.End.Position });
					double dist = lp.GetClosestPoint(c).Location.DistanceTo(c);
					if (dist < rect.Width && (dist < distance || closest == null) && dist < 1.0)
					{
						closest = ne;
						distance = dist;
					}
				}

				if (closest != null)
				{
					node = null;
					edge = closest;
					return;
				}
			}

			node = null;
			edge = null;
			return;
		}

		public void Click(Coordinates c)
		{
			if (this.zt.Mode == ZoneToolboxMode.Selection)
			{
				this.zt.SelectZone(c);
			}
			else if (this.Mode == ZoneToolboxMode.NavNodes && this.zt.current != null)
			{
				// save undo point
				this.ed.SaveUndoPoint();

				// check if we hit any of hte edges or nodes part of the zone
				ArbiterZone az = this.zt.current;

				// check if hit node or edge
				NavigableEdge ne;
				INavigableNode nn;
				this.NavigationHitTest(c, out nn, out ne);

				if (nn != null)
				{
					// create new node
					INavigableNode aznn = nn;

					if (this.PreviousNode != null)
					{
						// create new edges						
						NavigableEdge newE1 = new NavigableEdge(true, this.zt.current, false, null, new List<IConnectAreaWaypoints>(), this.PreviousNode, aznn);
						this.PreviousNode.OutgoingConnections.Add(newE1);
						this.zt.current.NavigableEdges.Add(newE1);
					}

					this.PreviousNode = aznn;
				}
				else if (ne != null)
				{
					// remove old
					this.zt.current.NavigableEdges.Remove(ne);

					// remove all references
					ne.Start.OutgoingConnections.Remove(ne);

					// create new node
					ArbiterZoneNavigableNode aznn = new ArbiterZoneNavigableNode(c);

					// create new edges
					NavigableEdge newE1 = new NavigableEdge(true, this.zt.current, false, null, new List<IConnectAreaWaypoints>(), ne.Start, aznn);
					NavigableEdge newE2 = new NavigableEdge(true, this.zt.current, false, null, new List<IConnectAreaWaypoints>(), aznn, ne.End);

					// add edges
					ne.Start.OutgoingConnections.Add(newE1);
					aznn.OutgoingConnections.Add(newE2);

					// add all to lists
					this.zt.current.NavigableEdges.Add(newE1);
					this.zt.current.NavigableEdges.Add(newE2);
					this.zt.current.NavigationNodes.Add(aznn);

					if (this.PreviousNode != null)
					{
						NavigableEdge newE3 = new NavigableEdge(true, this.zt.current, false, null, new List<IConnectAreaWaypoints>(), this.PreviousNode, aznn);
						this.PreviousNode.OutgoingConnections.Add(newE3);
						this.zt.current.NavigableEdges.Add(newE3);	
					}

					this.PreviousNode = aznn;
				}
				else
				{
					// create new node
					ArbiterZoneNavigableNode aznn = new ArbiterZoneNavigableNode(c);

					if (this.PreviousNode != null)
					{
						// create new edges						
						NavigableEdge newE1 = new NavigableEdge(true, this.zt.current, false, null, new List<IConnectAreaWaypoints>(), this.PreviousNode, aznn);
						this.PreviousNode.OutgoingConnections.Add(newE1);
						this.zt.current.NavigableEdges.Add(newE1);
					}

					this.PreviousNode = aznn;
					this.zt.current.NavigationNodes.Add(aznn);
				}
			}
			else if (this.zt.Mode == ZoneToolboxMode.StayOut && this.zt.current != null)
			{
				if (this.WrappingHelpers.Count == 0)
				{
					this.WrappingHelpers.Add(c);
				}
				else
				{
					// Determine size of bounding box
					float scaled_offset = 1 / this.rd.WorldTransform.Scale;

					// invert the scale
					float scaled_size = DrawingUtility.cp_large_size;

					// assume that the world transform is currently applied correctly to the graphics
					RectangleF rect = new RectangleF((float)c.X - scaled_size / 2, (float)c.Y - scaled_size / 2, scaled_size, scaled_size);

					if (rect.Contains(DrawingUtility.ToPointF(this.WrappingHelpers[0])) &&
						c.DistanceTo(this.WrappingHelpers[0]) < 1)
					{
						ed.SaveUndoPoint();
						Polygon p = new Polygon(this.WrappingHelpers);
						this.zt.current.StayOutAreas.Add(p);
						this.WrappingHelpers = new List<Coordinates>();
					}
					else
						this.WrappingHelpers.Add(c);
				}

				this.rd.Invalidate();
			}
		}

		public void RightClick(Coordinates c)
		{
			if (this.Mode == ZoneToolboxMode.NavNodes && this.zt.current != null)
			{
				// nullify
				this.Reset(false);

				// determine what we selected
				NavigableEdge ne;
				INavigableNode nn;
				this.NavigationHitTest(c, out nn, out ne);

				if (nn != null)
				{
					this.rightClickNode = nn;
				}
				else if (ne != null)
				{
					this.rightClickEdge = ne;
				}
			}
		}

		public void MoveMouse(Coordinates c)
		{
			if (this.moveNode != null)
			{
				this.moveNode.Position = c;
			}
		}

		public void BeginMove(Coordinates c)
		{
			if (this.Mode == ZoneToolboxMode.NavNodes && this.zt.current != null)
			{
				// nullify
				this.Reset(false);

				// determine what we selected
				NavigableEdge ne;
				INavigableNode nn;
				this.NavigationHitTest(c, out nn, out ne);

				if (nn != null && !(nn is ArbiterPerimeterWaypoint))
				{
					this.moveNode = nn;
					this.moveOriginalCoords = this.moveNode.Position;
				}
			}
		}		

		public void DeleteSelected()
		{
			if (this.rightClickNode != null)
			{
				this.ed.SaveUndoPoint();
				this.zt.current.NavigationNodes.Remove(this.rightClickNode);
				foreach(NavigableEdge ne in this.rightClickNode.OutgoingConnections)
					ne.Zone.NavigableEdges.Remove(ne);
				List<NavigableEdge> toRemove = new List<NavigableEdge>();
				foreach (NavigableEdge ne in this.zt.current.NavigableEdges)
				{
					if (ne.End.Equals(this.rightClickNode))
					{
						ne.Start.OutgoingConnections.Remove(ne);
						toRemove.Add(ne);						
					}
				}
				foreach(NavigableEdge ne in toRemove)
					ne.Zone.NavigableEdges.Remove(ne);
			}
			else if(this.rightClickEdge != null)
			{
				this.ed.SaveUndoPoint();
				this.rightClickEdge.Start.OutgoingConnections.Remove(this.rightClickEdge);
				this.rightClickEdge.Zone.NavigableEdges.Remove(this.rightClickEdge);				
			}

			this.Reset(false);
		}

		public void Reset(bool resetPrevious)
		{	
			this.WrappingHelpers = new List<Coordinates>();
			this.PreviousNode = null;		

			if (resetPrevious)
				this.zt.Mode = ZoneToolboxMode.None;

			this.rightClickNode = null;
			this.rightClickEdge = null;

			if (this.moveNode != null)
				this.moveNode.Position = this.moveOriginalCoords;
			this.moveNode = null;
		}

		#endregion

		#region IDisplayObject Members

		public System.Drawing.RectangleF GetBoundingBox(WorldTransform wt)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public HitTestResult HitTest(UrbanChallenge.Common.Coordinates loc, float tol, WorldTransform wt, DisplayObjectFilter filter)
		{
			return new HitTestResult(this, false, float.MaxValue);
		}

		public void Render(System.Drawing.Graphics g, WorldTransform t)
		{
			if (this.Mode == ZoneToolboxMode.StayOut)
			{
				if (this.WrappingHelpers.Count > 0)
				{
					for (int i = 0; i < this.WrappingHelpers.Count; i++)
					{
						DrawingUtility.DrawControlPoint(this.WrappingHelpers[i], Color.SteelBlue, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);

						if (i + 1 < this.WrappingHelpers.Count)
						{
							DrawingUtility.DrawColoredControlLine(Color.SteelBlue, System.Drawing.Drawing2D.DashStyle.Solid,
								this.WrappingHelpers[i], this.WrappingHelpers[i + 1], g, t);
						}
					}

					if (this.WrappingHelpers.Count > 0)
					{
						DrawingUtility.DrawColoredControlLine(Color.SteelBlue, System.Drawing.Drawing2D.DashStyle.Solid,
							this.WrappingHelpers[this.WrappingHelpers.Count - 1], this.CurrentMouse, g, t);
					}
				}
			}
			else if (this.Mode == ZoneToolboxMode.NavNodes)
			{
				if (this.rightClickNode != null)
				{
					DrawingUtility.DrawControlPoint(this.rightClickNode.Position, Color.Red, null, ContentAlignment.MiddleCenter, ControlPointStyle.SmallCircle, g, t);
				}
				else if (this.rightClickEdge != null)
				{
					DrawingUtility.DrawColoredArrowControlLine(Color.Red, System.Drawing.Drawing2D.DashStyle.Solid,
							this.rightClickEdge.Start.Position, this.rightClickEdge.End.Position, g, t);
				}

				if (this.PreviousNode != null)
				{
					DrawingUtility.DrawColoredArrowControlLine(Color.DarkBlue, System.Drawing.Drawing2D.DashStyle.Solid,
							this.PreviousNode.Position, this.CurrentMouse, g, t);
				}
			}
		}

		public bool MoveAllowed
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public void BeginMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void InMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CompleteMove(UrbanChallenge.Common.Coordinates orig, UrbanChallenge.Common.Coordinates offset, WorldTransform t)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public void CancelMove(UrbanChallenge.Common.Coordinates orig, WorldTransform t)
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
			return true;
		}

		#endregion
	}
}
