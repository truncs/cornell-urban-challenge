using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors.CompletionReport;

namespace UrbanChallenge.Arbiter.Core.Common
{
	/// <summary>
	/// Blockage interface
	/// </summary>
	public interface ITacticalBlockage
	{
		/// <summary>
		/// The blockage report that was sent to the ai
		/// </summary>
		TrajectoryBlockedReport BlockageReport
		{
			get;
		}
	}

	/// <summary>
	/// Blockage in a lane
	/// </summary>
	public class LaneBlockage : ITacticalBlockage
	{
		/// <summary>
		/// base blockage report
		/// </summary>
		private TrajectoryBlockedReport blockageReport;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blockageReport"></param>
		public LaneBlockage(TrajectoryBlockedReport blockageReport)
		{
			this.blockageReport = blockageReport;
		}

		#region ITacticalBlockage Members

		/// <summary>
		/// Base blockage report
		/// </summary>
		public TrajectoryBlockedReport BlockageReport
		{
			get { return this.blockageReport; }
		}

		#endregion

		/// <summary>
		/// String information about blockage
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "LaneBlockage, dist " + this.blockageReport.DistanceToBlockage.ToString("f2");
		}
	}

	/// <summary>
	/// Blockage in a lane
	/// </summary>
	public class OpposingLaneBlockage : ITacticalBlockage
	{
		/// <summary>
		/// base blockage report
		/// </summary>
		private TrajectoryBlockedReport blockageReport;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blockageReport"></param>
		public OpposingLaneBlockage(TrajectoryBlockedReport blockageReport)
		{
			this.blockageReport = blockageReport;
		}

		#region ITacticalBlockage Members

		/// <summary>
		/// Base blockage report
		/// </summary>
		public TrajectoryBlockedReport BlockageReport
		{
			get { return this.blockageReport; }
		}

		#endregion

		/// <summary>
		/// String information about blockage
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "OpposingLaneBlockage, dist " + this.blockageReport.DistanceToBlockage.ToString("f2");
		}
	}

	/// <summary>
	/// Blockage in a zone
	/// </summary>
	public class ZoneBlockage : ITacticalBlockage
	{
		/// <summary>
		/// base blockage report
		/// </summary>
		private TrajectoryBlockedReport blockageReport;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blockageReport"></param>
		public ZoneBlockage(TrajectoryBlockedReport blockageReport)
		{
			this.blockageReport = blockageReport;
		}

		#region ITacticalBlockage Members

		/// <summary>
		/// Base blockage report
		/// </summary>
		public TrajectoryBlockedReport BlockageReport
		{
			get { return this.blockageReport; }
		}

		#endregion

		/// <summary>
		/// String information about blockage
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "ZoneBlockage, dist " + this.blockageReport.DistanceToBlockage.ToString("f2");
		}
	}

	/// <summary>
	/// Blockage during a lane change
	/// </summary>
	public class LaneChangeBlockage : ITacticalBlockage
	{
		/// <summary>
		/// base blockage report
		/// </summary>
		private TrajectoryBlockedReport blockageReport;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blockageReport"></param>
		public LaneChangeBlockage(TrajectoryBlockedReport blockageReport)
		{
			this.blockageReport = blockageReport;
		}

		#region ITacticalBlockage Members

		/// <summary>
		/// Base blockage report
		/// </summary>
		public TrajectoryBlockedReport BlockageReport
		{
			get { return this.blockageReport; }
		}

		#endregion

		/// <summary>
		/// String information about blockage
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "LaneChangeBlockage, dist " + this.blockageReport.DistanceToBlockage.ToString("f2");
		}
	}

	/// <summary>
	/// Blockage during a turn
	/// </summary>
	public class TurnBlockage : ITacticalBlockage
	{
		/// <summary>
		/// base blockage report
		/// </summary>
		private TrajectoryBlockedReport blockageReport;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blockageReport"></param>
		public TurnBlockage(TrajectoryBlockedReport blockageReport)
		{
			this.blockageReport = blockageReport;
		}

		#region ITacticalBlockage Members

		/// <summary>
		/// Base blockage report
		/// </summary>
		public TrajectoryBlockedReport BlockageReport
		{
			get { return this.blockageReport; }
		}

		#endregion

		/// <summary>
		/// String information about blockage
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "TurnBlockage, dist " + this.blockageReport.DistanceToBlockage.ToString("f2");
		}
	}

	/// <summary>
	/// Blockage during blockage recovery
	/// </summary>
	public class BlockageRecoveryBlockage : ITacticalBlockage
	{
		/// <summary>
		/// base blockage report
		/// </summary>
		private TrajectoryBlockedReport blockageReport;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blockageReport"></param>
		public BlockageRecoveryBlockage(TrajectoryBlockedReport blockageReport)
		{
			this.blockageReport = blockageReport;
		}

		#region ITacticalBlockage Members

		/// <summary>
		/// Base blockage report
		/// </summary>
		public TrajectoryBlockedReport BlockageReport
		{
			get { return this.blockageReport; }
		}

		#endregion

		/// <summary>
		/// String information about blockage
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "BlockageRecoveryBlockage, dist " + this.blockageReport.DistanceToBlockage.ToString("f2");
		}
	}
}
