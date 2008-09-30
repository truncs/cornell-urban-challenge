using System;
using System.Collections.Generic;
using System.Text;

namespace Simulator.Engine
{
	/// <summary>
	/// Identification information for a vehicle
	/// </summary>
	[Serializable]
	public class SimVehicleId
	{
		/// <summary>
		/// the vehicle's number
		/// </summary>
		private int number;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="number"></param>
		public SimVehicleId(int number)
		{
			this.number = number;
		}

		/// <summary>
		/// The vehicle's number
		/// </summary>
		public int Number
		{
			get { return number; }
			set { number = value; }
		}

		/// <summary>
		/// Check if two id's are equal
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj is SimVehicleId)
			{
				SimVehicleId other = (SimVehicleId)obj;
				return (this.number == other.Number);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Hash code of the Id
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return number.GetHashCode();
		}

		/// <summary>
		/// String representation of the Id
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return (number.ToString());
		}
	}
}
