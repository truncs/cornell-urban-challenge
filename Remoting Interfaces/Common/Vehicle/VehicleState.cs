using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Path;

namespace UrbanChallenge.Common.Vehicle
{
	/// <summary>
	/// Type of area vehicle is in
	/// </summary>
	public enum StateAreaType
	{
		Lane=0,
		Interconnect=1,
		Zone=2
	}

	/// <summary>
	/// Estimate of the vehicle in a cetrain area
	/// </summary>
	[Serializable]
	public struct AreaEstimate
	{
		/// <summary>
		/// Id of the area
		/// </summary>
		public string AreaId;

		/// <summary>
		/// Type of the area
		/// </summary>
		public StateAreaType AreaType;

		/// <summary>
		/// Probability of being in this area
		/// </summary>
		public double Probability;
	}

	/// <summary>
	/// The full current state of the vehicle
	/// </summary>
	[Serializable]
	public class VehicleState
	{
		public static string ChannelName = "ArbiterSceneEstimatorPositionChannel";

		/// <summary>
		/// Absolute xy location in RNDF coordinates (units: meters)
		/// </summary>
		public Coordinates Position;

		/// <summary>
		/// Absolute heading in RNDF coordinates (East, North) where the x axis is 0
		/// </summary>		
		public Coordinates Heading;

		/// <summary>
		/// Area estimates
		/// </summary>		
		public List<AreaEstimate> Area;

		/// <summary>
		/// 0 indicates that the waypoints and local sensing line up well
		/// 1 indicates that we are in a sparse waypoint condition
		/// </summary>
		public double IsSparseWaypoints;

		/// <summary>
		/// Synchronized car time in seconds
		/// </summary>
		public double Timestamp;

		/// <summary>
		/// East North Covariance Matrix (4 doubles)
		/// </summary>
		public double[] ENCovariance;
		
		/// <summary>
		/// Default constructor
		/// </summary>
		public VehicleState()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position"></param>
		/// <param name="heading"></param>
		public VehicleState(Coordinates position, Coordinates heading)
		{
			this.Position = position;
			this.Heading = heading;	
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="position"></param>
		/// <param name="heading"></param>
		/// <param name="area"></param>
		public VehicleState(Coordinates position, Coordinates heading, List<AreaEstimate> area)
		{
			this.Position = position;
			this.Heading = heading;
			this.Area = area;
		}

		/// <summary>
		/// Position of front of car
		/// </summary>
		public Coordinates Front
		{
			get
			{
				return this.Position + this.Heading.Normalize(TahoeParams.FL);
			}
		}

		public Coordinates Rear
		{
			get
			{
				return this.Position - this.Heading.Normalize(TahoeParams.RL);
			}
		}

		public LinePath VehicleLinePath
		{
			get
			{
				return new LinePath(new Coordinates[]{
					this.Rear - this.Heading.Normalize(TahoeParams.VL * 3.0),
					this.Front + this.Heading.Normalize(TahoeParams.VL * 3.0)});
			}
		}

		public Polygon VehiclePolygon
		{
			get
			{
				Coordinates backAxle = this.Position;
				Coordinates toFront = this.Heading.Normalize(TahoeParams.FL);
				Coordinates toBack = this.Heading.Normalize(TahoeParams.RL).Rotate180();
				Coordinates toRight = this.Heading.Normalize(TahoeParams.T / 2.0).RotateM90();
				Coordinates fl = backAxle + toFront - toRight;
				Coordinates fr = backAxle + toFront + toRight;
				Coordinates bl = backAxle + toBack - toRight;
				Coordinates br = backAxle + toBack + toRight;
				Coordinates[] cs = new Coordinates[] { fl, fr, bl, br };
				return new Polygon(cs, CoordinateMode.AbsoluteProjected);
			}
		}

		public Polygon ForwardPolygon
		{
			get
			{
				/*LinePath lp1 = new LinePath(new Coordinates[] { this.Front, this.Front + this.Heading.Normalize(50.0) });
				LinePath lpL = lp1.ShiftLateral(50.0);
				LinePath lpR = lp1.ShiftLateral(-50.0);
				List<Coordinates> polyCoors = new List<Coordinates>();
				polyCoors.AddRange(lpL);
				polyCoors.AddRange(lpR);
				Polygon p = Polygon.GrahamScan(polyCoors);
				return p;*/
				return this.GetForwardPolygon(50.0, 50.0);
			}
		}

		public Polygon GetForwardPolygon(double distanceForward, double halfWidth)
		{
			LinePath lp1 = new LinePath(new Coordinates[] { this.Front, this.Front + this.Heading.Normalize(distanceForward) });
			LinePath lpL = lp1.ShiftLateral(halfWidth);
			LinePath lpR = lp1.ShiftLateral(-halfWidth);
			List<Coordinates> polyCoors = new List<Coordinates>();
			polyCoors.AddRange(lpL);
			polyCoors.AddRange(lpR);
			Polygon p = Polygon.GrahamScan(polyCoors);
			return p;
		}

		public Polygon SmallForwardPolygon
		{
			get
			{
				return this.GetForwardPolygon(20.0, 20.0);
			}
		}

		public Polygon RearPolygon
		{
			get
			{
				LinePath lp1 = new LinePath(new Coordinates[] { this.Rear, this.Rear - this.Heading.Normalize(50.0) });
				LinePath lpL = lp1.ShiftLateral(50.0);
				LinePath lpR = lp1.ShiftLateral(-50.0);
				List<Coordinates> polyCoors = new List<Coordinates>();
				polyCoors.AddRange(lpL);
				polyCoors.AddRange(lpR);
				Polygon p = Polygon.GrahamScan(polyCoors);
				return p;
			}
		}

		public DirectionAlong DirectionAlongSegment(LinePath lp)
		{
			// get heading of the lane path there
			Coordinates pathVector = lp.GetSegment(0).UnitVector;

			// get vehicle heading
			Coordinates unit = new Coordinates(1, 0);
			Coordinates headingVector = unit.Rotate(this.Heading.ArcTan);

			// rotate vehicle heading
			Coordinates relativeVehicle = headingVector.Rotate(-pathVector.ArcTan);

			// get path heading
			double relativeVehicleDegrees = relativeVehicle.ToDegrees() >= 180.0 ? Math.Abs(relativeVehicle.ToDegrees() - 360.0) : Math.Abs(relativeVehicle.ToDegrees());

			if (relativeVehicleDegrees < 70)
				return DirectionAlong.Forwards;
			else if (relativeVehicleDegrees > 70 && relativeVehicleDegrees < 110)
				return DirectionAlong.Perpendicular;
			else
				return DirectionAlong.Reverse;
		}

	}

	public enum DirectionAlong
	{
		Forwards,
		Perpendicular,
		Reverse
	}
}
