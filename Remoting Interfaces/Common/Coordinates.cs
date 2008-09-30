using System;

namespace UrbanChallenge.Common {

	/// <summary>
	/// 2-dimensional absolute position.
	/// </summary>
	[Serializable]
	public struct Coordinates : IComparable<Coordinates>, IEquatable<Coordinates> {
		public static Coordinates Zero { get { return new Coordinates(0, 0); } }
		public static Coordinates NaN { get { return new Coordinates(double.NaN, double.NaN); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="x">X coordinate.</param>
		/// <param name="y">Y coordinate.</param>
		public Coordinates(double x, double y) {
			this.X = x;
			this.Y = y;
		}

		/// <summary>
		/// Constructs a unit vector rotated by theta
		/// </summary>
		/// <param name="theta">Angle to rotate in radians</param>
		/// <returns></returns>
		public static Coordinates FromAngle(double theta) {
			return new Coordinates(Math.Cos(theta), Math.Sin(theta));
		}

		/// <summary>
		/// Vector addition.
		/// </summary>
		/// <param name="left">Left vector.</param>
		/// <param name="right">Right vector.</param>
		/// <returns>Vector sum.</returns>
		public static Coordinates operator +(Coordinates left, Coordinates right) {
			return new Coordinates(left.X + right.X, left.Y + right.Y);
		}

		/// <summary>
		/// Vector substraction.
		/// </summary>
		/// <param name="left">Left vector.</param>
		/// <param name="right">Right vector.</param>
		/// <returns>Vector difference.</returns>
		public static Coordinates operator -(Coordinates left, Coordinates right) {
			return new Coordinates(left.X - right.X, left.Y - right.Y);
		}

		/// <summary>
		/// Multiplication of vector with a scalar.
		/// </summary>
		/// <param name="c">The vector.</param>
		/// <param name="d">The scalar.</param>
		/// <returns>The scaled vector.</returns>
		public static Coordinates operator *(Coordinates c, double d) {
			return new Coordinates(c.X * d, c.Y * d);
		}

		/// <summary>
		/// Multiplication of vector with a scalar.
		/// </summary>
		/// <param name="c">The vector.</param>
		/// <param name="d">The scalar.</param>
		/// <returns>The scaled vector.</returns>
		public static Coordinates operator *(double d, Coordinates c) {
			return c * d;
		}

		/// <summary>
		/// Division of vector with a scalar.
		/// </summary>
		/// <param name="c">The vector.</param>
		/// <param name="d">The scalar.</param>
		/// <returns>The scaled vector.</returns>
		public static Coordinates operator /(Coordinates c, double d) {
			return new Coordinates(c.X / d, c.Y / d);
		}

		/// <summary>
		/// Vector multiplication.
		/// </summary>
		/// <param name="left">Left vector.</param>
		/// <param name="right">Right vector.</param>
		/// <returns>Scalar product.</returns>
		public static double operator *(Coordinates left, Coordinates right) {
			return (left.X * right.X + left.Y * right.Y);
		}

		/// <summary>
		/// Equality operator.
		/// </summary>
		/// <param name="left">Left vector.</param>
		/// <param name="right">Right vector.</param>
		/// <returns>True if both X and Y coordinates are equal.</returns>
		public static bool operator ==(Coordinates left, Coordinates right) {
			return (left.X == right.X) && (left.Y == right.Y);
		}

		/// <summary>
		/// Inequality operator.
		/// </summary>
		/// <param name="left">Left vector.</param>
		/// <param name="right">Right vector.</param>
		/// <returns>True if left and right are not equal.</returns>
		public static bool operator !=(Coordinates left, Coordinates right) {
			return !(left == right);
		}

		/// <summary>
		/// Negates the coordinates, returning a new instance (same as rotating by 180)
		/// </summary>
		public static Coordinates operator -(Coordinates left) {
			return new Coordinates(-left.X, -left.Y);
		}

		/// <summary>
		/// Euclidean distance between coordinates.
		/// </summary>
		/// <param name="other">Other Coordinates.</param>
		/// <returns>Euclidean distance.</returns>
		public double DistanceTo(Coordinates other) {
			return (other - this).VectorLength;
		}

		/// <summary>Modulus of vector in polar coordinates.</summary>
		public double VectorLength {
			get {
				return Math.Sqrt(this.X * this.X + this.Y * this.Y);
			}
		}

		/// <summary>
		/// Squared length of vector
		/// </summary>
		public double VectorLength2 {
			get {
				return this.X * this.X + this.Y * this.Y;
			}
		}

		/// <summary>The arcus tangens of the vector.</summary>
		public double ArcTan {
			get { return Math.Atan2(this.Y, this.X); }
		}

		// ToDgrees -> angle above x axis in terms of degrees: 0 - 360
		public double ToDegrees()
		{
			double arctan = (Math.Atan2(this.Y, this.X)) * 180 / Math.PI;

			if (arctan >= 0)
				return arctan;
			else
				return (360.0 + arctan);
		}

		/// <summary>
		/// Computes dot product of the two coordinates as vectors
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public double Dot(Coordinates other)
		{
			return ((this.X * other.X) + (this.Y * other.Y));
		}

		/// <summary>
		/// Computes the perpendicular product (2-D version of cross product) of this x other
		/// </summary>
		public double Cross(Coordinates other) {
			return X * other.Y - Y * other.X;
		}

		/// <summary>
		/// Length of coordinate as vector
		/// </summary>
		public double Length
		{
			get { return Math.Sqrt(Dot(this)); }
		}

		/// <summary>
		/// Scales the vector to be unit length
		/// </summary>
		/// <returns>Unit length vector</returns>
		public Coordinates Normalize()
		{
			return this / VectorLength;
		}

		/// <summary>
		/// Scales the vector to be length len
		/// </summary>
		/// <param name="len">Target length of vector</param>
		/// <returns>Normalized vector</returns>
		public Coordinates Normalize(double len)
		{
			return this * (len / VectorLength);
		}

		/// <summary>
		/// Rotates the coordinate
		/// </summary>
		/// <param name="radians">THETA IN RADIANS</param>
		/// <returns></returns>
		public Coordinates Rotate(double radians) {
			double ct = Math.Cos(radians), st = Math.Sin(radians);
			return new Coordinates(ct * X - st * Y, ct * Y + st * X);
		}

		/// <summary>
		/// Returns a new Coordinates rotated by 90 degrees
		/// </summary>
		public Coordinates Rotate90() {
			return new Coordinates(-Y, X);
		}

		/// <summary>
		/// Returns a new Coordinates rotated by -90 degrees
		/// </summary>
		public Coordinates RotateM90() {
			return new Coordinates(Y, -X);
		}

		/// <summary>
		/// Returns a new Coordinates rotated by 180 degrees
		/// </summary>
		public Coordinates Rotate180() {
			return new Coordinates(-X, -Y);
		}

		/// <summary>
		/// Comparison between Coordinates. Gives partial ordering.
		/// </summary>
		/// <param name="other">The other Coordinates.</param>
		/// <returns>
		/// True if X is lower, or X is equal other.X and Y is lower.
		/// </returns>
		public int CompareTo(Coordinates other) {
			double x = this.X - other.X;
			if (x == 0) {
				double y = this.Y - other.Y;
				if (y == 0)
					return 0;
				else if (y < 0)
					return -1;
				else
					return 1;
			} else if (x < 0)
				return -1;
			else
				return 1;
		}

		#region IEquatable<Coordinates> Members

		/// <summary>
		/// Analyzes if two coords are equal
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(Coordinates other) {
			return this.X.Equals(other.X) && this.Y.Equals(other.Y);
		}

		#endregion

		/// <summary>
		/// Comparison between Coordinates.
		/// </summary>
		/// <param name="obj">The other Coordinates.</param>
		/// <returns>True if X and Y are equal.</returns>
		public override bool Equals(object obj) {
			if (obj is Coordinates) {
				Coordinates other = (Coordinates)obj;
				return Equals(other);
			} else
				return false;
		}

		/// <summary>Hash function.</summary>
		/// <returns>A hash of X and Y.</returns>
		public override int GetHashCode() {
			return (this.X.GetHashCode() << 16) ^ (this.Y.GetHashCode());
		}

		/// <summary>
		/// string representation of coordinate
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return X.ToString() + "," + Y.ToString();
		}

		/// <summary>The transposed vector.</summary>
		public Coordinates Transposed {
			get { return new Coordinates(this.Y, this.X); }
		}

		/// <summary>
		/// Check for approximate equality within tol in both dimensions
		/// </summary>
		/// <param name="other">Coordinates to compare to</param>
		/// <param name="tol">Tolerance for equality</param>
		/// <returns>True if both the X and Y deviation from other by less than tol</returns>
		public bool ApproxEquals(Coordinates other, double tol) {
			return Math.Abs(X - other.X) < tol && Math.Abs(Y - other.Y) < tol;
		}

		public bool IsNaN {
			get { return double.IsNaN(X) || double.IsNaN(Y); }
		}

		/// <summary>
		/// The X coordinate.
		/// </summary>
		public double X;

		/// <summary>
		/// The Y coordinate.
		/// </summary>
		public double Y;
	}

}
