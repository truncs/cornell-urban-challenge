using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Common;

namespace UrbanChallenge.Arbiter.Core.Common.State
{
	/// <summary>
	/// Traveling in a zone from a specific start point
	/// </summary>
	public class ZoneOrientationState : ZoneState, IState
	{
		#region Zone Orientation State Members

		public NavigableEdge final;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="zone"></param>
		public ZoneOrientationState(ArbiterZone zone, NavigableEdge final)
			: base(zone)
		{
			this.final = final;
		}

		#endregion

		#region IState Members

		public string ShortDescription()
		{
			return "ZoneOrientationState";
		}

		public string LongDescription()
		{
			return "ZoneOrientation State:" + this.Zone.ToString();
		}

		public string StateInformation()
		{
			return "Zone: " + this.Zone.ToString();
		}

		public UrbanChallenge.Behaviors.Behavior Resume(UrbanChallenge.Common.Vehicle.VehicleState currentState, double speed)
		{
			List<Polygon> stayOuts = new List<Polygon>();
			foreach(Polygon so in this.Zone.StayOutAreas)
			{
				if(!so.IsInside(currentState.Front) && !so.IsInside(currentState.Position))
					stayOuts.Add(so);
			}
			return new UTurnBehavior(this.Zone.Perimeter.PerimeterPolygon, new LinePath(new Coordinates[] { this.final.Start.Position, this.final.End.Position }), null, new ScalarSpeedCommand(1.7), stayOuts);
		}

		public bool CanResume()
		{
			return true;
		}

		public List<UrbanChallenge.Behaviors.BehaviorDecorator> DefaultStateDecorators
		{
			get { return TurnDecorators.NoDecorators; }
		}

		public bool UseLaneAgent
		{
			get { return false; }
		}

		public UrbanChallenge.Arbiter.Core.Common.Reasoning.InternalState InternalLaneState
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

		public bool ResetLaneAgent
		{
			get
			{
				return true;
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}
}