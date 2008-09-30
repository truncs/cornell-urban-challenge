using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.Common;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Reasons about moving laterally across lanes
	/// </summary>
	public class LateralReasoning : ILateralReasoning
	{
		/// <summary>
		/// Monitors road ahead
		/// </summary>
		public ForwardQuadrantMonitor ForwardMonitor;

		/// <summary>
		/// Monitors the road beside us
		/// </summary>
		public LateralQuadrantMonitor LateralMonitor;

		/// <summary>
		/// Monitors the road behind us
		/// </summary>
		public RearQuadrantMonitor RearMonitor;

		/// <summary>
		/// Lane to monitor
		/// </summary>
		private ArbiterLane lane;

		/// <summary>
		/// Constructor
		/// </summary>
		public LateralReasoning(ArbiterLane lane, SideObstacleSide sos)
		{
			this.lane = lane;
			this.ForwardMonitor = new ForwardQuadrantMonitor();
			this.LateralMonitor = new LateralQuadrantMonitor(sos);
			this.RearMonitor = new RearQuadrantMonitor(lane, sos);
		}

		/// <summary>
		/// Resets held values
		/// </summary>
		public void Reset()
		{
			this.ForwardMonitor.Reset();
			this.LateralMonitor.Reset();
			this.RearMonitor.Reset();
		}

		#region ILateralReasoning Members

		/// <summary>
		/// Lane this is associated with
		/// </summary>
		public ArbiterLane LateralLane
		{
			get { return this.lane; }
		}
		
		public bool AdjacentAndRearClear(VehicleState state)
		{
			this.RearMonitor.Update(state);					
			bool adjOccupied = this.LateralMonitor.Occupied(lane, state);
			bool rearClear = this.RearMonitor.IsClear(state, CoreCommon.Communications.GetVehicleSpeed().Value);
			return !adjOccupied && rearClear;
		}

		public bool Exists
		{
			get { return this.lane != null; }
		}

		public VehicleAgent AdjacentVehicle
		{
			get { return this.LateralMonitor.CurrentVehicle; }
		}

		public bool ForwardClear(VehicleState state, double usDistanceToTravel, double usAvgSpeed, LaneChangeInformation information, Coordinates minReturn)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public bool IsOpposing
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// The forward vehicle laterally if exists
		/// </summary>
		public VehicleAgent ForwardVehicle(VehicleState state)
		{
			this.ForwardMonitor.ForwardVehicle.Update(lane, state);
			return this.ForwardMonitor.ForwardVehicle.CurrentVehicle;		
		}

		/// <summary>
		/// Side of the vehicle this reasoning component represents
		/// </summary>
		public SideObstacleSide VehicleSide
		{
			get
			{
				return this.LateralMonitor.VehicleSide;
			}
		}

		#endregion

		#region ILateralReasoning Members

		public bool ExistsExactlyHere(VehicleState state)
		{
			return this.lane.RelativelyInside(state.Front) && this.lane.IsInside(state.Front);
		}

		public bool ExistsRelativelyHere(VehicleState state)
		{
			return this.lane.RelativelyInside(state.Front);
		}


		#endregion
	}
}
