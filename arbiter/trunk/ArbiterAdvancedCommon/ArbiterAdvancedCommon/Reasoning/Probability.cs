using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.Core.Common.Reasoning
{
	/// <summary>
	/// Represents a probablistic observation
	/// </summary>
	[Serializable]
	public struct Probability : IComparable<Probability>, IEquatable<Probability>
	{
		private double t;
		private double f;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="t">Probability true</param>
		/// <param name="f">Probability false</param>
		public Probability(double t, double f)
		{
			this.t = t;
			this.f = f;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="t">Probability true</param>
		/// <param name="f">Probability false</param>
		/// <param name="normalize">True if want to normalize</param>
		public Probability(double t, double f, bool normalize)
		{
			if (normalize)
			{
				double tot = t + f;
				this.t = t / tot;
				this.f = f / tot;
			}
			else
			{
				this.t = t;
				this.f = f;
			}
		}

		/// <summary>
		/// Normalizes the probability
		/// </summary>
		/// <returns></returns>
		public Probability Normalize()
		{
			double tot = t + f;
			return new Probability(t / tot, f / tot);
		}
		
		/// <summary>
		/// Multiplies probability by a scalar
		/// </summary>
		/// <param name="p"></param>
		/// <param name="d"></param>
		/// <returns></returns>
		public static Probability operator *(Probability p, double d)
		{
			return new Probability(p.T * d, p.F * d);
		}

		/// <summary>
		/// Multiplies two probabilities
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static Probability operator *(Probability p1, Probability p2)
		{
			return new Probability(p1.T * p2.T, p1.F * p2.F);
		}

		/// <summary>
		/// Adds two probabilities
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		public static Probability operator +(Probability p1, Probability p2)
		{
			return new Probability(p1.T + p2.T, p1.F + p2.F);
		}

		/// <summary>
		/// Probability true
		/// </summary>
		public double T
		{
			get
			{
				return t;
			}
		}

		/// <summary>
		/// Probability false
		/// </summary>
		public double F
		{
			get
			{
				return f;
			}
		}

		/// <summary>
		/// returns inverse of the probability
		/// </summary>
		/// <returns></returns>
		public Probability Invert()
		{
			return new Probability(f, t);
		}

		#region IComparable<Probability> Members

		/// <summary>
		/// COmpares truthfulness of two probabilities
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(Probability other)
		{
			if (this.t == other.T)
			{
				return 0;
			}
			else if (this.t > other.T)
			{
				return 1;
			}
			else
			{
				return -1;
			}
		}

		#endregion

		#region IEquatable<Probability> Members

		/// <summary>
		/// Checks if two probabilities are equal
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(Probability other)
		{
			return this.t == other.T && this.f == other.F;
		}

		#endregion

		public override string ToString()
		{
			return "<" + this.T.ToString("F3") + ", " + this.F.ToString("F3") + ">";
		}
	}
}
