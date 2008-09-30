using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Reasoning;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Quadrants;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Common;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common.Path;
using UrbanChallenge.Arbiter.Core.Common.Common;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Vehicles;
using UrbanChallenge.Common.Sensors;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road.Opposing.Reasoning
{
	/// <summary>
	/// Monitors opposing lane
	/// </summary>
	public class OpposingLateralReasoning : ILateralReasoning
	{
		/// <summary>
		/// Monitors opposing lane forward of us
		/// </summary>
		public OpposingForwardQuadrantMonitor ForwardMonitor;

		/// <summary>
		/// monitors opposing lane next to us
		/// </summary>
		public OpposingLateralQuadrantMonitor LateralMonitor;

		/// <summary>
		/// monitors opposing lane behind us
		/// </summary>
		public OpposingRearQuadrantMonitor RearMonitor;

		/// <summary>
		/// the opposing lane
		/// </summary>
		public ArbiterLane lane;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lane"></param>
		public OpposingLateralReasoning(ArbiterLane lane, SideObstacleSide sos)
		{
			this.lane = lane;
			this.ForwardMonitor = new OpposingForwardQuadrantMonitor();
			this.LateralMonitor = new OpposingLateralQuadrantMonitor(sos);
			this.RearMonitor = new OpposingRearQuadrantMonitor(lane, sos);
		}

		#region ILateralReasoning Members

		public ArbiterLane LateralLane
		{
			get { return this.lane; }
		}

		public void Reset()
		{
			if (this.LateralMonitor != null && this.ForwardMonitor != null && this.RearMonitor != null)
			{
				this.LateralMonitor.Reset();
				this.ForwardMonitor.Reset();
				this.RearMonitor.Reset();
			}
		}

		public bool Exists
		{
			get { return this.lane != null; }
		}

		public VehicleAgent AdjacentVehicle
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public bool AdjacentAndRearClear(VehicleState state)
		{
			this.RearMonitor.Update(state);
			return !this.LateralMonitor.Occupied(lane, state) && this.RearMonitor.IsClear(state, CoreCommon.Communications.GetVehicleSpeed().Value);
		}

		public bool ForwardClear(VehicleState state, double usDistanceToTravel, double usAvgSpeed, LaneChangeInformation information, Coordinates minReturn)
		{			
			if (information.Reason == LaneChangeReason.FailedForwardVehicle)
				return this.ForwardMonitor.ClearForDisabledVehiclePass(lane, state, CoreCommon.Communications.GetVehicleSpeed().Value, minReturn);
			else
				throw new Exception("standard forward clear in opposing lateral reasoning not imp");
		}

		public bool IsOpposing
		{
			get
			{
				return true;
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
		/// Side of hte vehicle this is on
		/// </summary>
		public UrbanChallenge.Common.Sensors.SideObstacleSide VehicleSide
		{
			get { return this.LateralMonitor.VehicleSide; }
		}

		#endregion

		#region ILateralReasoning Members

		public bool ExistsExactlyHere(VehicleState state)
		{
			bool b = this.lane.RelativelyInside(state.Front) && this.lane.IsInside(state.Front);
			return b;
		}

		public bool ExistsRelativelyHere(VehicleState state)
		{
			return this.lane.RelativelyInside(state.Front);
		}

		#endregion
	}
}
