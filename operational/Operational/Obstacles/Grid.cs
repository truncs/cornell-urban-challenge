using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UrbanChallenge.Common;
using System.Diagnostics;

namespace OperationalLayer.Obstacles {
	
	/// <summary>
	/// GridCoordinates structure
	/// </summary>
	struct GridCoordinates {
		public int X, Y;	// X and Y of grid coordinate
		
		/// <summary>
		/// GridCoordinates constructor
		/// </summary>
		/// <param name="X">X of grid coordinate</param>
		/// <param name="Y">Y of grid coordinate</param>
		public GridCoordinates(int X, int Y) {
			this.X = X;
			this.Y = Y;
		}

		/// <summary>
		/// Addition operator
		/// </summary>
		/// <param name="left">Left grid coordinate</param>
		/// <param name="right">Right grid coordinate</param>
		/// <returns>Summation of grid coordinates</returns>
		public static GridCoordinates operator +(GridCoordinates left, GridCoordinates right) {
			return new GridCoordinates(left.X + right.X, left.Y + right.Y);
		}

		/// <summary>
		/// Subtraction operator
		/// </summary>
		/// <param name="left">Left grid coordinate</param>
		/// <param name="right">Right grid coordinate</param>
		/// <returns>Subtraction of grid coordinates</returns>
		public static GridCoordinates operator -(GridCoordinates left, GridCoordinates right) {
			return new GridCoordinates(left.X - right.X, left.Y - right.Y);
		}

		/// <summary>
		/// Vector multiplication operator
		/// </summary>
		/// <param name="left">Left grid coordinate</param>
		/// <param name="right">Right grid coordinate</param>
		/// <returns>Vector multiplication of grid coordinates</returns>
		public static int operator *(GridCoordinates left, GridCoordinates right) {
			return (left.X * right.X + left.Y * right.Y);
		}

		/// <summary>
		/// Equality operator
		/// </summary>
		/// <param name="left">Left grid coordinate</param>
		/// <param name="right">Right grid coordinate</param>
		/// <returns>True if grid coordinates are equal</returns>
		public static bool operator ==(GridCoordinates left, GridCoordinates right) {
			return (left.X == right.X) && (left.Y == right.Y);
		}

		/// <summary>
		/// Inequality operator
		/// </summary>
		/// <param name="left">Left grid coordinate</param>
		/// <param name="right">Right grid coordinate</param>
		/// <returns>True if grid coordinates are not equal</returns>
		public static bool operator !=(GridCoordinates left, GridCoordinates right) {
			return !(left == right);
		}

		/// <summary>
		/// Negate operator
		/// </summary>
		/// <param name="left">Grid coordinate to negate</param>
		/// <returns>Negated grid coordinate</returns>
		public static GridCoordinates operator -(GridCoordinates left) {
			return new GridCoordinates(-left.X, -left.Y);
		}

		/// <summary>
		/// Euclidean distance between grid coordinates
		/// </summary>
		/// <param name="other">Other grid coordinate to measure distance to</param>
		/// <returns>Euclidean distance</returns>
		public double DistanceTo(GridCoordinates other) {
			return (other - this).VectorLength;
		}

		/// <summary>
		/// Norm of grid coordinate as a vector
		/// </summary>
		public double VectorLength {
			get {
				return Math.Sqrt((this.X * this.X) + (this.Y * this.Y));
			}
		}

		/// <summary>
		/// Equals function
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			if (obj is GridCoordinates) {
				GridCoordinates other = (GridCoordinates)obj;
				return Equals(other);
			}
			else
				return false;
		}

		/// <summary>
		/// Hash function
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return (this.X.GetHashCode() << 16) ^ (this.Y.GetHashCode());
		}
	}

	/// <summary>
	/// Grid class
	/// </summary>
	unsafe class Grid : IDisposable {
		// grid information
#if GRID_UNSAFE
		protected float* data;						// grid data, will be stored in row-major format
		protected int data_stride;				// size (int number of floats) per row of data
#else
		protected float[][] data;
#endif
		protected int xSize, ySize;				// grid size
		protected GridCoordinates middle; // grid middle coordinate
#if GRID_UNSAFE
		private bool disposed;						// indicates if we're disposed and the pointers have been deallocated
#endif

		// grid window
		public bool windowEnabled;
		public int windowLowerX, windowLowerY;
		public int windowUpperX, windowUpperY;

		// path information
		protected List<GridCoordinates> path;		 // path given in grid steps
		protected List<Coordinates> smoothPath;	 // smoothed path
		protected List<Coordinates> reducedPath; // reduced path, i.e. smoothed path with reduced points
		
		private float[][] costF, costG, costH;	// costs
		private GridCoordinates[][] parent;			// parents
		private Boolean[][] openListFlag;				// open list flags
		private Boolean[][] closedListFlag;			// closed list flags
		private List<GridCoordinates> openList; // open list

		/// <summary>
		/// Grid class constructor
		/// </summary>
		public Grid() {
			// empty constructor
		}

		/// <summary>
		/// Grid class constructor
		/// </summary>
		/// <param name="xSize">X size of grid</param>
		/// <param name="ySize">Y size of grid</param>
		public Grid(int xSize, int ySize) {
			Initialise(xSize, ySize);
		}

		/// <summary>
		/// Intialise grid with specified size
		/// </summary>
		/// <param name="xSize">X size of grid</param>
		/// <param name="ySize">Y size of grid</param>
		public void Initialise(int xSize, int ySize) {
			// x and y sizes of grid 
			this.xSize = xSize;
			this.ySize = ySize;

			// x and y middles of grid
			middle.X = (int)Math.Round(((double)xSize - 1) / 2);
			middle.Y = (int)Math.Round(((double)ySize - 1) / 2);

#if GRID_UNSAFE
			// create grid of data of size xSize by ySize
			// this is used to cache-align each row
			data_stride = (int)Math.Ceiling(ySize/16.0)*16;
			data = (float*)Memory.Allocate((uint)(data_stride*xSize*4));
			// inform that GC that we allocated this much space
			GC.AddMemoryPressure(data_stride*xSize*4);
#else
			data = new float[xSize][];
			for (int i=0; i < data.Length; i++) {
				data[i] = new float[ySize];
			} 
#endif

			// disable window mode
			WindowEnabled = false;
		}

		public void ZeroGrid(bool windowed) {
#if GRID_UNSAFE
			if (disposed)	throw new ObjectDisposedException("Grid");
			Memory.ZeroMemory(data, (uint)(data_stride*xSize*4));
#else
			if (windowed) {
				for (int x = windowLowerX; x <= windowUpperX; x++) {
					Array.Clear(data[x], windowLowerY, windowUpperY-windowLowerY+1);
				}
			}
			else {
				for (int x = 0; x < xSize; x++) {
					Array.Clear(data[x], 0, ySize);
				}
			}
#endif
		}

		public float[][] Data {
			get { return data; }
		}

		/// <summary>
		/// X size of grid
		/// </summary>
		public int XSize {
			get {
				return xSize;
			}
		}

		/// <summary>
		/// Y size of grid
		/// </summary>
		public int YSize {
			get {
				return ySize;
			}
		}

		/// <summary>
		/// X middle of grid
		/// </summary>
		public int XMiddle {
			get {
				return middle.X;
			}
		}

		/// <summary>
		/// Y middle of grid
		/// </summary>
		public int YMiddle {
			get {
				return middle.Y;
			}
		}

		/// <summary>
		/// Lower X of window
		/// </summary>
		public int WindowLowerX {
			get {
				return windowLowerX;
			}
			set {
				windowLowerX = Math.Max(value, 0);
			}
		}

		/// <summary>
		/// Lower Y of window
		/// </summary>
		public int WindowLowerY {
			get {
				return windowLowerY;
			}
			set {
				windowLowerY = Math.Max(value, 0);
			}
		}

		/// <summary>
		/// Upper X of window
		/// </summary>
		public int WindowUpperX {
			get {
				return windowUpperX;
			}
			set {
				windowUpperX = Math.Min(value, xSize - 1);
			}
		}

		/// <summary>
		/// Upper Y of window
		/// </summary>
		public int WindowUpperY {
			get {
				return windowUpperY;
			}
			set {
				windowUpperY = Math.Min(value, ySize - 1);
			}
		}

		/// <summary>
		/// Window enabled flag
		/// </summary>
		public Boolean WindowEnabled {
			get {
				return windowEnabled;
			}
			set {
				windowEnabled = value;
			}
		}

		/// <summary>
		/// Set window size
		/// </summary>
		/// <param name="lowerLeft">Lower left grid coordinate</param>
		/// <param name="upperRight">Upper right grid coordinate</param>
		public void SetWindow(GridCoordinates lowerLeft, GridCoordinates upperRight) {
			WindowLowerX = lowerLeft.X;
			WindowLowerY = lowerLeft.Y;
			WindowUpperX = upperRight.X;
			WindowUpperY = upperRight.Y;
		}

		/// <summary>
		/// Set window size and flag
		/// </summary>
		/// <param name="lowerLeft">Lower left grid coordinate</param>
		/// <param name="upperRight">Upper right grid coordinate</param>
		/// <param name="windowFlag">Window flag</param>
		public void SetWindow(GridCoordinates lowerLeft, GridCoordinates upperRight, bool windowFlag) {
			WindowLowerX = lowerLeft.X;
			WindowLowerY = lowerLeft.Y;
			WindowUpperX = upperRight.X;
			WindowUpperY = upperRight.Y;
			WindowEnabled = windowFlag;
		}

		/// <summary>
		/// Minimum grid date value
		/// </summary>
		public float Min {
			get {
#if GRID_UNSAFE
				if (disposed) throw new ObjectDisposedException("Grid");
#endif

				float minValue = float.MaxValue;
				if (windowEnabled == false) {
					// search for minimum value in grid
					for (int x = 0; x < xSize; x++) {
						for (int y = 0; y < ySize; y++) {
#if GRID_UNSAFE
							if (data[x*data_stride+y] < minValue) {
								minValue = data[x*data_stride+y];
							}
#else
							if (data[x][y] < minValue)
								minValue = data[x][y];
#endif
						}
					}
				}
				else {
					// search for minimum value in window
					for (int x = windowLowerX; x <= windowUpperX; x++) {
						for (int y = windowLowerY; y <= windowUpperY; y++) {
#if GRID_UNSAFE
							if (data[x*data_stride+y] < minValue) {
								minValue = data[x*data_stride+y];
							}
#else
							if (data[x][y] < minValue)
								minValue = data[x][y];
#endif
						}
					}
				}

				return minValue;
			}
		}

		/// <summary>
		/// Maximum grid date value
		/// </summary>
		public float Max {
			get {
#if GRID_UNSAFE
				if (disposed) throw new ObjectDisposedException("Grid");
#endif

				float maxValue = float.MinValue;
				if (windowEnabled == false) {
					// search for maximum value in grid
					for (int x = 0; x < xSize; x++) {
						for (int y = 0; y < ySize; y++) {
#if GRID_UNSAFE
							if (data[x*data_stride+y] > maxValue) {
								maxValue = data[x*data_stride+y];
							}
#else
							if (data[x][y] > maxValue)
								maxValue = data[x][y];
#endif
						}
					}
				}
				else {
					// search for maximum value in window
					for (int x = windowLowerX; x <= windowUpperX; x++) {
						for (int y = windowLowerY; y <= windowUpperY; y++) {
#if GRID_UNSAFE
							if (data[x*data_stride+y] > maxValue) {
								maxValue = data[x*data_stride+y];
							}
#else
							if (data[x][y] > maxValue)
								maxValue = data[x][y];
#endif
						}
					}
				}
				return maxValue;
			}
		}

		/// <summary>
		/// Check if grid location is within grid
		/// </summary>
		/// <param name="gridLoc">Grid location to check</param>
		/// <returns>True if grid location is within grid</returns>
		public Boolean InGrid(GridCoordinates gridLoc) {
			if (windowEnabled == false) {
				// check if within grid
				return (gridLoc.X >= 0 && gridLoc.X < xSize && gridLoc.Y >= 0 && gridLoc.Y < ySize);
			}
			else {
				// check if within window
				return (gridLoc.X >= windowLowerX && gridLoc.X <= windowUpperX &&
								gridLoc.Y >= windowLowerY && gridLoc.Y <= windowUpperY);
			}
		}

		/// <summary>
		/// Check if grid location is within grid
		/// </summary>
		/// <param name="x">X of grid location to check</param>
		/// <param name="y">Y of grid location to check</param>
		/// <returns>True if grid location is within grid</returns>
		public Boolean InGrid(int x, int y) {
			return InGrid(new GridCoordinates(x, y));
		}

		/// <summary>
		/// Get grid data value
		/// </summary>
		/// <param name="x">X of grid location to get from</param>
		/// <param name="y">Y of grid location to get from</param>
		/// <returns>Value at grid location</returns>
		public float GetValue(int x, int y) {
#if  GRID_UNSAFE
			return data[x*data_stride+y];
#else
			return data[x][y];
#endif
		}

		/// <summary>
		/// Get grid data value
		/// </summary>
		/// <param name="gridLoc">Grid location to get from</param>
		/// <returns>Value at grid location</returns>
		public float GetValue(GridCoordinates gridLoc) {
#if  GRID_UNSAFE
			return data[gridLoc.X*data_stride+gridLoc.Y];
#else
			return data[gridLoc.X][gridLoc.Y];
#endif
		}

		/// <summary>
		/// Set grid data value at a grid location
		/// </summary>
		/// <param name="x">X of grid location to set</param>
		/// <param name="y">Y of grid location to set</param>
		/// <param name="value">Value to set grid location to</param>
		/// <returns>True if grid location was set successfully</returns>
		public Boolean SetValue(int x, int y, float value) {
			// set value if within grid size
			if (InGrid(x,y) == true) {
#if GRID_UNSAFE
				data[x*data_stride+y] = value;
#else
				data[x][y] = value;
#endif
				return true;
			}
			else {
				return false;
			}
		}

		/// <summary>
		/// Set grid data value at a grid location
		/// </summary>
		/// <param name="gridLoc">Grid location to set</param>
		/// <param name="value">Value to set grid location</param>
		/// <returns>True if grid location was set successfully</returns>
		public Boolean SetValue(GridCoordinates gridLoc, float value) {
			return SetValue(gridLoc.X, gridLoc.Y, value);
		}

		/// <summary>
		/// Set grid data values at multiple grid locations
		/// </summary>
		/// <param name="gridLocs">Grid locations to set</param>
		/// <param name="value">Value to set grid locations</param>
		public void SetValues(List<GridCoordinates> gridLocs, float value) {
			int total = gridLocs.Count;
			for (int i = 0; i < total; i++) {
				SetValue(gridLocs[i].X, gridLocs[i].Y, value);
			}
		}

		/// <summary>
		/// Set grid data values at multiple grid locations
		/// </summary>
		/// <param name="x">X of grid locations to set</param>
		/// <param name="y">Y of grid locations to set</param>
		/// <param name="value">Value to set grid locations</param>
		public void SetValues(List<int> x, List<int> y, float value) {
			int total = x.Count;
			for (int i = 0; i < total; i++) {
				SetValue(x[i], y[i], value);
			}
		}

		public void FillConvexInterior(List<GridCoordinates> boundary, float value) {
			SortedList<int, Pair<int, int>> minmaxList = new SortedList<int, Pair<int, int>>();

			int total = boundary.Count;
			for (int i = 0; i < total; i++) {
				Pair<int, int> minmax;
				GridCoordinates val = boundary[i];
				if (minmaxList.TryGetValue(val.X, out minmax)) {
					if (val.Y < minmax.Left) {
						minmax.Left = val.Y;
					}

					if (val.Y > minmax.Right) {
						minmax.Right = val.Y;
					}
					minmaxList[val.X] = minmax;
				}
				else {
					minmaxList.Add(val.X, new Pair<int, int>(val.Y, val.Y));
				}
			}

			// iterate through the x values and set all the y values
			foreach (KeyValuePair<int, Pair<int, int>> fillLine in minmaxList) {
				for (int y = fillLine.Value.Left; y <= fillLine.Value.Right; y++) {
					SetValue(fillLine.Key, y, value);
				}
			}
		}

		/// <summary>
		/// Set grid data values along a line and returns grid locations which have been set 
		/// </summary>
		/// <param name="startLoc">Start grid location of line</param>
		/// <param name="endLoc">End grid location of line</param>
		/// <param name="value">Value to set grid locations</param>
		/// <param name="setLocs">Grid locations which have been set</param>
		public void SetLineValues(GridCoordinates startLoc, GridCoordinates endLoc, float value, 
															out List<GridCoordinates> setLocs) {
			// initialise locations which are successfully set
			setLocs = new List<GridCoordinates>();

			// find points along a line using bresenham line algorithm
			List<GridCoordinates> lineLocs;
			BresenhamLine(startLoc, endLoc, out lineLocs);

			Boolean setFlag;

			// set value for points along a line
			int total = lineLocs.Count;
			for (int i = 0; i < total; i++) {
				// set value for line point
				setFlag = SetValue(lineLocs[i], value);

				// save location if set is successful
				if (setFlag == true) {
					setLocs.Add(lineLocs[i]);
				}
			}
		}

		/// <summary>
		/// Set grid data values along a line 
		/// </summary>
		/// <param name="startLoc">Start grid location of line</param>
		/// <param name="endLoc">End grid location of line</param>
		/// <param name="value">Value to set grid locations</param>
		public void SetLineValues(GridCoordinates startLoc, GridCoordinates endLoc, float value) {
			List<GridCoordinates> setLocs;
			SetLineValues(startLoc, endLoc, value, out setLocs);
		}

		/// <summary>
		/// Set grid data values along a line
		/// </summary>
		/// <param name="sx">X of start grid location of line</param>
		/// <param name="sy">Y of start grid location of line</param>
		/// <param name="ex">X of end grid location of line</param>
		/// <param name="ey">Y of end grid location of line</param>
		/// <param name="value">Value to set grid locations</param>
		public void SetLineValues(int sx, int sy, int ex, int ey, float value) {
			List<GridCoordinates> setLocs;
			SetLineValues(new GridCoordinates(sx, sy), new GridCoordinates(ex, ey), value, out setLocs);
		}

		/// <summary>
		/// Set grid data values along a line and returns grid locations which have been set 
		/// </summary>
		/// <param name="sx">X of start grid location of line</param>
		/// <param name="sy">Y of start grid location of line</param>
		/// <param name="ex">X of end grid location of line</param>
		/// <param name="ey">Y of end grid location of line</param>
		/// <param name="value">Value to set grid locations</param>
		/// <param name="setLocs">Grid locations which have been set</param>
		public void SetLineValues(int sx, int sy, int ex, int ey, float value, 
															out List<GridCoordinates> setLocs) {
			SetLineValues(new GridCoordinates(sx, sy), new GridCoordinates(ex, ey), value, out setLocs);
		}

		/// <summary>
		/// Normalize grid data values between lower value and upper value
		/// </summary>
		/// <param name="lowerValue">Upper value to normalise to</param>
		/// <param name="upperValue">Lower value to normalise to</param>
		public void Normalize(float lowerValue, float upperValue) {
			float minValue = Min;	// minimum grid data value
			float maxValue = Max;	// maximum grid data value

			float factor = (upperValue - lowerValue) / (maxValue - minValue);

			if (windowEnabled == false) {
				// normalize all grid data values
				for (int x = 0; x < xSize; x++) {
					for (int y = 0; y < ySize; y++) {
#if GRID_UNSAFE
						data[x*data_stride+y] = lowerValue + factor*(data[x*data_stride+y] - minValue);
#else
						data[x][y] = lowerValue + factor * (data[x][y] - minValue);
#endif
					}
				}
			}
			else {
				// normalize all grid data values within window
				for (int x = windowLowerX; x <= windowUpperX; x++) {
					for (int y = windowLowerY; y <= WindowUpperY; y++) {
#if GRID_UNSAFE
						data[x*data_stride+y] = lowerValue + factor*(data[x*data_stride+y] - minValue);
#else
						data[x][y] = lowerValue + factor * (data[x][y] - minValue);
#endif
					}
				}
			}
		}

		/// <summary>
		/// Normalize grid data values between 0 to 1
		/// </summary>
		public void Normalize() {
			Normalize(0, 1);
		}

		/// <summary>
		/// Reverse grid data values
		/// </summary>
		public void Reverse() {
			float minValue = Min;	// minimum grid data value 
			float maxValue = Max;	// maximum grid data value

			if (windowEnabled == false) {
				// reverse all grid data values
				for (int x = 0; x < xSize; x++) {
					for (int y = 0; y < ySize; y++) {
#if GRID_UNSAFE
						data[x*data_stride+y] = maxValue - data[x*data_stride+y] + minValue;
#else
						data[x][y] = maxValue - data[x][y] + minValue;
#endif
					}
				}
			}
			else {
				// reverse all grid data values within window
				for (int x = windowLowerX; x <= windowUpperX; x++) {
					for (int y = windowLowerY; y <= windowUpperY; y++) {
#if GRID_UNSAFE
						data[x*data_stride+y] = maxValue - data[x*data_stride+y] + minValue;
#else
						data[x][y] = maxValue - data[x][y] + minValue;
#endif
					}
				}
			}
		}

		/// <summary>
		/// Replace grid data values
		/// </summary>
		/// <param name="oldValue">Old value to be replaced</param>
		/// <param name="newValue">New value to replace old value</param>
		public void Replace(float oldValue, float newValue) {
			if (windowEnabled == false) {
				// replace grid data values
				for (int x = 0; x < xSize; x++) {
					for (int y = 0; y < ySize; y++) {
#if GRID_UNSAFE
						if (data[x*data_stride+y] == oldValue)
							data[x*data_stride+y] = newValue;
#else
						if (data[x][y] == oldValue)
							data[x][y] = newValue;
#endif
					}
				}
			}
			else {
				// replace grid data values within window
				for (int x = windowLowerX; x <= windowUpperX; x++) {
					for (int y = windowLowerY; y <= windowUpperY; y++) {
#if GRID_UNSAFE
						if (data[x*data_stride+y] == oldValue)
							data[x*data_stride+y] = newValue;
#else
						if (data[x][y] == oldValue)
							data[x][y] = newValue;
#endif
					}
				}
			}
		}

		/// <summary>
		/// Set grid data values using wavefront propagation
		/// </summary>
		/// <param name="startValue">Start value to begin propagation</param>
		/// <param name="emptyValue">Empty value to propagate onto</param>
		/// <param name="stepValue">Step value for each propagation step</param>
		/// <param name="maxValue">Max value for propagation</param>
		public void WaveFront(float startValue, float emptyValue, float stepValue, float maxValue) {
			// initialise queue
			Queue<GridCoordinates> queue = new Queue<GridCoordinates>();

			// retrieve grid locations containing start value
			if (windowEnabled == false) {
				for (int x = 0; x < xSize; x++) {
					for (int y = 0; y < ySize; y++) {
#if GRID_UNSAFE
						if (data[x*data_stride+y] == startValue) {
#else
						if (data[x][y] == startValue) {
#endif
							// add grid location to queue
							queue.Enqueue(new GridCoordinates(x, y));
						}
					}
				}
			}
			else {
				for (int x = windowLowerX; x <= windowUpperX; x++) {
					for (int y = windowLowerY; y <= windowUpperY; y++) {
#if GRID_UNSAFE
						if (data[x*data_stride+y] == startValue) {
#else
						if (data[x][y] == startValue) {
#endif
							// add grid location to queue
							queue.Enqueue(new GridCoordinates(x, y));
						}
					}
				}
			}

			GridCoordinates adjLoc, gridLoc;
			float updateValue;

			// run wavefront propagation until queues are empty
			while (queue.Count != 0) {
				// retrieve a grid location from queue
				gridLoc = queue.Dequeue();

				// update value for adjacent grid locations
#if GRID_UNSAFE
				updateValue = data[gridLoc.X*data_stride+gridLoc.Y] + stepValue;
#else
				updateValue = data[gridLoc.X][gridLoc.Y] + stepValue;
#endif
				// stop wavefront propagation if update value reaches empty value
				if (updateValue <= emptyValue)
					break;
				// stop wavefront propagation if update value reaches maximum value
				if (updateValue > maxValue) {
					// set remaining grid locations with maximum value
					Replace(emptyValue, maxValue);
					break;
				}

				// update 4 adjacent grid locations
				Boolean validFlag;
				for (int i = 0; i < 4; i++) {
					// initialise adjacent location and valid flag
					adjLoc = gridLoc;
					validFlag = false;

					// find adjacent grid location
					switch (i) {
						case 0: // adjacent grid location to the left
							if ((gridLoc.X != 0 && !windowEnabled) || 
									(gridLoc.X != windowLowerX && windowEnabled)) {
								adjLoc.X--;
								validFlag = true;
							}
							break;
						case 1: // adjacent grid location to the right
							if ((gridLoc.X != xSize - 1 && !windowEnabled) || 
									(gridLoc.X != windowUpperX && windowEnabled)) {
								adjLoc.X++;
								validFlag = true;
							}
							break;
						case 2: // adjacent grid location to the bottom
							if ((gridLoc.Y != 0 && !windowEnabled) || 
									(gridLoc.Y != windowLowerY && windowEnabled)) {
								adjLoc.Y--;
								validFlag = true;
							}
							break;
						case 3: // adjacent grid location to the top
							if ((gridLoc.Y != ySize - 1 && !windowEnabled) || 
									(gridLoc.Y != windowUpperY && windowEnabled)) {
								adjLoc.Y++;
								validFlag = true;
							}
							break;
					}

					// update adjacent grid location if it is valid
					if (validFlag == true) {
						// check if adjacent grid location contains empty value
#if GRID_UNSAFE
						if (data[gridLoc.X*data_stride+gridLoc.Y]== emptyValue) {
							// update adjacent grid location
							data[gridLoc.X*data_stride+gridLoc.Y] = updateValue;
							// add adjacent grid location to queue
							queue.Enqueue(adjLoc);
						}
#else
						if (data[adjLoc.X][adjLoc.Y] == emptyValue) {
							// update adjacent grid location
							data[adjLoc.X][adjLoc.Y] = updateValue;
							// add adjacent grid location to queue
							queue.Enqueue(adjLoc);
						}

#endif
					}
				}
			}
		}

		/// <summary>
		/// Set grid data values using wavefront propagation
		/// </summary>
		/// <param name="startValue">Start value to begin propagation</param>
		/// <param name="emptyValue">Empty value to propagate onto</param>
		/// <param name="stepValue">Step value for each propagation step</param>
		public void WaveFront(float startValue, float emptyValue, float stepValue) {
			WaveFront(startValue, emptyValue, stepValue, float.MaxValue);
		}

		/// <summary>
		/// Set grid data values using floodfill
		/// </summary>
		/// <param name="startLoc">Start grid location to begin floodfill</param>
		/// <param name="fillValue">Fill value to update grid locations</param>
		/// <param name="emptyValue">Empty value for each filled location</param>
		public void FloodFill(GridCoordinates startLoc, float fillValue, float emptyValue) {
			// return if grid location is not valid
			if (InGrid(startLoc) == false)
				return;

			// return if start location is not empty
#if GRID_UNSAFE
			if (data[startLoc.X*data_stride+startLoc.Y] != emptyValue)
				return;
#else
			if (data[startLoc.X][startLoc.Y] != emptyValue)
				return;
#endif

			// initialise queue
			Queue<GridCoordinates> queue = new Queue<GridCoordinates>();

			// check if grid start location is empty
#if GRID_UNSAFE
			if (data[startLoc.X*data_stride+startLoc.Y] == emptyValue) {
				// update grid start location with fill value
				data[startLoc.X*data_stride+startLoc.Y] = fillValue;
#else
			if (data[startLoc.X][startLoc.Y] == emptyValue) {
				// update grid start location with fill value
				data[startLoc.X][startLoc.Y] = fillValue;
#endif
				// add grid start location to queue
				queue.Enqueue(startLoc);
			}
			else {
				// stop floodfill as grid start location is not empty
				return;
			}

			GridCoordinates adjLoc, gridLoc;

			// run floodfill until queues are empty
			while (queue.Count != 0) {
				// retrieve a grid location from queue
				gridLoc = queue.Dequeue();

				// update 4 adjacent grid locations
				Boolean validFlag;
				for (int i = 0; i < 4; i++) {
					// initialise adjacent location and valid flag
					adjLoc = gridLoc;
					validFlag = false;

					// find adjacent grid location
					switch (i) {
						case 0: // adjacent grid location to the left
							if ((gridLoc.X != 0 && !windowEnabled) ||
									(gridLoc.X != windowLowerX && windowEnabled)) {
								adjLoc.X--;
								validFlag = true;
							}
							break;
						case 1: // adjacent grid location to the right
							if ((gridLoc.X != xSize - 1 && !windowEnabled) ||
									(gridLoc.X != windowUpperX && windowEnabled)) {
								adjLoc.X++;
								validFlag = true;
							}
							break;
						case 2: // adjacent grid location to the bottom
							if ((gridLoc.Y != 0 && !windowEnabled) ||
									(gridLoc.Y != windowLowerY && windowEnabled)) {
								adjLoc.Y--;
								validFlag = true;
							}
							break;
						case 3: // adjacent grid location to the top
							if ((gridLoc.Y != ySize - 1 && !windowEnabled) ||
									(gridLoc.Y != windowUpperY && windowEnabled)) {
								adjLoc.Y++;
								validFlag = true;
							}
							break;
					}

					// update adjacent grid location if it is valid
					if (validFlag == true) {
						// check if adjacent grid location contains empty value
#if GRID_UNSAFE
						if (data[adjLoc.X*data_stride+adjLoc.Y] == emptyValue) {
							// update adjacent grid location
							data[adjLoc.X*data_stride+adjLoc.Y] = fillValue;
#else
						if (data[adjLoc.X][adjLoc.Y] == emptyValue) {
							// update adjacent grid location
							data[adjLoc.X][adjLoc.Y] = fillValue;
#endif
							// add adjacent grid location to queue
							queue.Enqueue(adjLoc);
						}
					}
				}
			}
		}

		/// <summary>
		/// Initialisation for A star search algorithm 
		/// </summary>
		public void AStarInitialise() {

			if (costF != null) {
			  AStarReset();
			  return;
			}

			// initialise A star paths
			path				= new List<GridCoordinates>();
			smoothPath  = new List<Coordinates>();
			reducedPath = new List<Coordinates>();

			Console.WriteLine("creating grid of size {0}x{1}", xSize, ySize);
			
			// initialise A star jagged arrays
			costF					 = new float[xSize][];
			costG					 = new float[xSize][];
			costH					 = new float[xSize][];
			parent				 = new GridCoordinates[xSize][];
			openListFlag	 = new Boolean[xSize][];
			closedListFlag = new Boolean[xSize][];
			for (int i = 0; i < xSize; i++) {
				costF[i]	= new float[ySize];
				costG[i]	= new float[ySize];
				costH[i]	= new float[ySize];
				parent[i] = new GridCoordinates[ySize];
				openListFlag[i]		= new Boolean[ySize];
				closedListFlag[i] = new Boolean[ySize];
			}
			
			
			// initialise A star open list
			openList = new List<GridCoordinates>(xSize*ySize);
		}

		public void AStarReset() {
			// clear A star paths
			path.Clear();
			smoothPath.Clear();
			reducedPath.Clear();
			
			// clear values in A star jagged arrays
			for (int x = 0; x < xSize; x++) {
				Array.Clear(costF[x], 0, costF[x].Length);
				Array.Clear(costG[x], 0, costG[x].Length);
				Array.Clear(costH[x], 0, costH[x].Length);
				Array.Clear(parent[x], 0, parent[x].Length);
				Array.Clear(openListFlag[x], 0, openListFlag[x].Length);
				Array.Clear(closedListFlag[x], 0, closedListFlag[x].Length);
			}

			// clear A star open list
			openList.Clear();
		}

		/// <summary>
		/// A star search algorithm
		/// </summary>
		/// <param name="startLoc">Start grid location</param>
		/// <param name="endLocs">End grid location(s)</param>
		/// <param name="desiredEndLoc">Desired end location</param>
		public void AStarSearch(GridCoordinates startLoc, List<GridCoordinates> endLocs, GridCoordinates desiredEndLoc) {
			GridCoordinates adjLoc, currLoc, pathLoc;

			Stopwatch sw = new Stopwatch();
			sw.Start();

			// initialise search variables
			AStarInitialise();

			sw.Stop();
			//Console.WriteLine("AStar Part 1 - {0}", sw.ElapsedMilliseconds);
			sw.Reset();
			sw.Start();

			// step 1 - add start node to open list
			OpenListAdd(startLoc);

			int openListIndex, openListTotal;
			float newG;
			Boolean pathFound = false;

			// step 2 - repeat until open list is empty
			while (openList.Count != 0) {

				// step 2a - look for lowest F cost node on open list (current node)
				OpenListRemove(out currLoc);

				// check if end node reached
				for (int i = 0; i < endLocs.Count; i++) {
					if (currLoc == endLocs[i]) {
						pathFound = true;
						path.Add(endLocs[i]);	// end end node to path
						break;
					}
				}
				if (pathFound == true)
					break;

				// step 2b - switch current node to closed list
				ClosedListAdd(currLoc);

				// step 2c - for each of 8 adjacent nodes to this current node
				for (int i = 0; i < 8; i++) {
					// adjacent node
					adjLoc = currLoc;
					switch (i) {
						case 0: adjLoc.X--; break;	// west node
						case 1: adjLoc.X--; adjLoc.Y++; break;	// northwest node
						case 2: adjLoc.Y++; break;	// north node
						case 3: adjLoc.X++; adjLoc.Y++; break;	// northeast node
						case 4: adjLoc.X++; break;	// east node
						case 5: adjLoc.X++; adjLoc.Y--; break;	// southeast node
						case 6: adjLoc.Y--; break;	// south node
						case 7: adjLoc.X--; adjLoc.Y--; break;	// southwest node
					}

					// check if adjacent node is walkable
					if (windowEnabled == false) {
						if (adjLoc.X < 0 || adjLoc.X >= xSize || adjLoc.Y < 0 || adjLoc.Y >= ySize)
							continue;
					}
					else {
						if (adjLoc.X < windowLowerX || adjLoc.X > windowUpperX ||
								adjLoc.Y < windowLowerY || adjLoc.Y > windowUpperY)
							continue;
					}

					// check if adjacent node is on closed list
					if (closedListFlag[adjLoc.X][adjLoc.Y] == true)// || data[adjLoc.X][adjLoc.Y] == 40)
						continue;

					// check if adjacent node is on open list
					if (openListFlag[adjLoc.X][adjLoc.Y] == false) {
						// adjacent node not on open list

						// current node is parent of adjacent node
						parent[adjLoc.X][adjLoc.Y] = currLoc;

						// record G cost of adjacent node
						// cost to move from start node to this adjacent node
						// i.e. add parent G and cost from parent
#if GRID_UNSAFE
						costG[adjLoc.X][adjLoc.Y] = costG[currLoc.X][currLoc.Y] +
																				data[adjLoc.X*data_stride+adjLoc.Y] * 2 *
																				(float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) +
																										  	 Math.Pow(adjLoc.Y - currLoc.Y, 2));
#else
						costG[adjLoc.X][adjLoc.Y] = costG[currLoc.X][currLoc.Y] +
																				data[adjLoc.X][adjLoc.Y] * 2 *
																				(float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) +
																										  	 Math.Pow(adjLoc.Y - currLoc.Y, 2));
#endif

						// record H cost of adjacent node
						// estimated or heuristic cost to move from this adjacent node to end node
						costH[adjLoc.X][adjLoc.Y] = (float)Math.Sqrt(Math.Pow(adjLoc.X - desiredEndLoc.X, 2) +
																												 Math.Pow(adjLoc.Y - desiredEndLoc.Y, 2));

						// record F cost of adjacent node
						costF[adjLoc.X][adjLoc.Y] = costG[adjLoc.X][adjLoc.Y] + costH[adjLoc.X][adjLoc.Y];

						// add adjacent node to open list
						OpenListAdd(adjLoc);
					}
					else {
						// adjacent node on open list

						// check if new path to node is better
						// lower G cost means a better path
#if GRID_UNSAFE
						newG = costG[currLoc.X][currLoc.Y] + data[adjLoc.X*data_stride+adjLoc.Y] * 2 * 
									 (float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) +
																	  Math.Pow(adjLoc.Y - currLoc.Y, 2));
#else
						newG = costG[currLoc.X][currLoc.Y] + data[adjLoc.X][adjLoc.Y] * 2 * 
									 (float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) +
																	  Math.Pow(adjLoc.Y - currLoc.Y, 2));
#endif
						if (newG < costG[adjLoc.X][adjLoc.Y]) {
							// change parent of node to current node
							parent[adjLoc.X][adjLoc.Y] = currLoc;

							// recalculate G and F cost of node
							costG[adjLoc.X][adjLoc.Y] = newG;
							costF[adjLoc.X][adjLoc.Y] = costG[adjLoc.X][adjLoc.Y] + costH[adjLoc.X][adjLoc.Y];

							// find index of adjacent node in open list
							openListIndex = 0;
							openListTotal = openList.Count;
							for (int j = 0; j < openListTotal; j++) {
								if (adjLoc == openList[j]) {
									openListIndex = j;
									break;
								}
							}
							// re-sort open list as F has changed
							OpenListReSort(openListIndex);
						}
					}
				}
			}

			sw.Stop();
			//Console.WriteLine("AStar Part 2 - {0}", sw.ElapsedMilliseconds);
			sw.Reset();
			sw.Start();

			// step 3 - save the path
			// save path if path was found
			if (pathFound == true) {
				// working backwards from end node

				// go from each node to its parent node until starting node reached 
				while (true) {
					// retrieve last node added to path
					pathLoc = path[path.Count - 1];
					// check if start node reached
					if (pathLoc == startLoc) {
						// path is completed, reverse path so that start node is in front
						path.Reverse();
						break;
					}
					// add parent node of current node to path
					path.Add(parent[pathLoc.X][pathLoc.Y]);
				}
			}

			// generate smoothed path from A star path
			SmoothPath();

			// generate reduced path from smooth path
			ReducePath();

			sw.Stop();
			//Console.WriteLine("AStar Part 3 - {0}", sw.ElapsedMilliseconds);
		}

		/// <summary>
		/// A star search algorithm
		/// </summary>
		/// <param name="startLoc">Start grid location</param>
		/// <param name="endLoc">End grid location</param>
		//public void AStarSearch(GridCoordinates startLoc, GridCoordinates endLoc) {
		//  GridCoordinates adjLoc, currLoc, pathLoc;

		//  Stopwatch sw = new Stopwatch();
		//  sw.Start();

		//  // initialise search variables
		//  AStarInitialise();

		//  sw.Stop();
		//  Console.WriteLine("T1 - {0}", sw.ElapsedMilliseconds);
		//  sw.Reset();
		//  sw.Start();

		//  // step 1 - add start node to open list
		//  OpenListAdd(startLoc);

		//  int openListIndex, openListTotal;
		//  float newG;
		//  Boolean pathFound = false;

		//  // step 2 - repeat until open list is empty
		//  while (openList.Count != 0) {

		//    // step 2a - look for lowest F cost node on open list (current node)
		//    OpenListRemove(out currLoc);

		//    // check if end node reached
		//    if (currLoc == endLoc) {
		//      pathFound = true;
		//      break;
		//    }

		//    // step 2b - switch current node to closed list
		//    ClosedListAdd(currLoc);

		//    // step 2c - for each of 8 adjacent nodes to this current node
		//    for (int i = 0; i < 8; i++) {
		//      // adjacent node
		//      adjLoc = currLoc;
		//      switch (i) {
		//        case 0: adjLoc.X--;							break;	// west node
		//        case 1: adjLoc.X--; adjLoc.Y++; break;	// northwest node
		//        case 2:							adjLoc.Y++;	break;	// north node
		//        case 3: adjLoc.X++; adjLoc.Y++; break;	// northeast node
		//        case 4: adjLoc.X++;							break;	// east node
		//        case 5: adjLoc.X++; adjLoc.Y--; break;	// southeast node
		//        case 6:							adjLoc.Y--; break;	// south node
		//        case 7: adjLoc.X--; adjLoc.Y--; break;	// southwest node
		//      }

		//      // check if adjacent node is walkable
		//      if (windowEnabled == false) {
		//        if (adjLoc.X < 0 || adjLoc.X >= xSize || adjLoc.Y < 0 || adjLoc.Y >= ySize)
		//          continue;
		//      }
		//      else {
		//        if (adjLoc.X < windowLowerX || adjLoc.X > windowUpperX ||
		//            adjLoc.Y < windowLowerY || adjLoc.Y > windowUpperY)
		//          continue;
		//      }

		//      // check if adjacent node is on closed list
		//      if (closedListFlag[adjLoc.X][adjLoc.Y] == true)
		//        continue;

		//      // check if adjacent node is on open list
		//      if (openListFlag[adjLoc.X][adjLoc.Y] == false) {
		//        // adjacent node not on open list

		//        // current node is parent of adjacent node
		//        parent[adjLoc.X][adjLoc.Y] = currLoc;

						// record G cost of adjacent node
						// cost to move from start node to this adjacent node
						// i.e. add parent G and cost from parent 
//#if GRID_UNSAFE
//            costG[adjLoc.X][adjLoc.Y] = costG[currLoc.X][currLoc.Y] +
//                                        data[adjLoc.X*data_stride+adjLoc.Y] * 
//                                        (float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) +
//                                                         Math.Pow(adjLoc.Y - currLoc.Y, 2));
//#else
//            costG[adjLoc.X][adjLoc.Y] = costG[currLoc.X][currLoc.Y] +
//                                        data[adjLoc.X][adjLoc.Y] * 
//                                        (float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) +
//                                                         Math.Pow(adjLoc.Y - currLoc.Y, 2));
//#endif

		//        // record H cost of adjacent node
		//        // estimated or heuristic cost to move from this adjacent node to end node
		//        costH[adjLoc.X][adjLoc.Y] = (float)Math.Sqrt(Math.Pow(adjLoc.X - endLoc.X, 2) + 
		//                                                     Math.Pow(adjLoc.Y - endLoc.Y, 2));

		//        // record F cost of adjacent node
		//        costF[adjLoc.X][adjLoc.Y] = costG[adjLoc.X][adjLoc.Y] + costH[adjLoc.X][adjLoc.Y];

		//        // add adjacent node to open list
		//        OpenListAdd(adjLoc);
		//      }
		//      else {
		//        // adjacent node on open list

						// check if new path to node is better
						// lower G cost means a better path
//#if GRID_UNSAFE
//            newG = costG[currLoc.X][currLoc.Y] + data[adjLoc.X*data_stride+adjLoc.Y] * 
//                   (float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) + 
//                                    Math.Pow(adjLoc.Y - currLoc.Y, 2));
//#else
//            newG = costG[currLoc.X][currLoc.Y] + data[adjLoc.X][adjLoc.Y] * 
//                   (float)Math.Sqrt(Math.Pow(adjLoc.X - currLoc.X, 2) + 
//                                    Math.Pow(adjLoc.Y - currLoc.Y, 2));
//#endif
//            if (newG < costG[adjLoc.X][adjLoc.Y]) {
//              // change parent of node to current node
//              parent[adjLoc.X][adjLoc.Y] = currLoc;

		//          // recalculate G and F cost of node
		//          costG[adjLoc.X][adjLoc.Y] = newG;
		//          costF[adjLoc.X][adjLoc.Y] = costG[adjLoc.X][adjLoc.Y] + costH[adjLoc.X][adjLoc.Y];

		//          // find index of adjacent node in open list
		//          openListIndex = 0;
		//          openListTotal = openList.Count;
		//          for (int j = 0; j < openListTotal; j++) {
		//            if (adjLoc == openList[j]) {
		//              openListIndex = j;
		//              break;
		//            }
		//          }
		//          // re-sort open list as F has changed
		//          OpenListReSort(openListIndex);
		//        }
		//      }
		//    }
		//  }

		//  sw.Stop();
		//  Console.WriteLine("T2 - {0}", sw.ElapsedMilliseconds);
		//  sw.Reset();
		//  sw.Start();

		//  // step 3 - save the path
		//  // save path if path was found
		//  if (pathFound == true) {
		//    // working backwards from end node, add end node to path
		//    path.Add(endLoc);

		//    // go from each node to its parent node until starting node reached 
		//    while (true) {
		//      // retrieve last node added to path
		//      pathLoc = path[path.Count - 1];
		//      // check if start node reached
		//      if (pathLoc == startLoc) {
		//        // path is completed, reverse path so that start node is in front
		//        path.Reverse();
		//        break;
		//      }
		//      // add parent node of current node to path
		//      path.Add(parent[pathLoc.X][pathLoc.Y]);
		//    }
		//  }

		//  // generate smoothed path from A star path
		//  SmoothPath();

		//  // generate reduced path from smooth path
		//  ReducePath();

		//  sw.Stop();
		//  Console.WriteLine("T3 - {0}", sw.ElapsedMilliseconds);
		//}

		/// <summary>
		/// Get original A star path (A star search must run beforehand)
		/// </summary>
		/// <param name="path">A star path</param>
		public void GetPath(out List<GridCoordinates> path) {
			path = new List<GridCoordinates>(this.path);
		}

		/// <summary>
		/// Get smoothed A star path (A star search must run beforehand)
		/// </summary>
		/// <param name="smoothPath">Smoothed path</param>
		public void GetSmoothPath(out List<Coordinates> smoothPath) {
			smoothPath = new List<Coordinates>(this.smoothPath);
		}

		/// <summary>
		/// Get reduced A star path (A star search must be run beforehand)
		/// </summary>
		/// <param name="reducedPath">Reduced path</param>
		public void GetReducedPath(out List<Coordinates> reducedPath) {
			reducedPath = new List<Coordinates>(this.reducedPath);
		}

		/// <summary>
		/// Add node to open list (for A star search)
		/// </summary>
		/// <param name="gridLoc">Grid location to add to open list</param>
		private void OpenListAdd(GridCoordinates gridLoc) {
			// add to open list
			openList.Add(gridLoc);
			openListFlag[gridLoc.X][gridLoc.Y] = true;

			// parent index and temporary storage
			int p;
			GridCoordinates tmpLoc;

			// index to start checking
			int i = openList.Count - 1;
			// loop until item at start index is sorted in binary heap
			while (i != 0) {
				// parent index
				p = (int)Math.Floor(((double)i - 1) / 2);

				// check if f cost of child is less than or equal to f cost of parent
				if (costF[openList[i].X][openList[i].Y] <= costF[openList[p].X][openList[p].Y]) {
					// swap child and parent
					tmpLoc			= openList[p];
					openList[p] = openList[i];
					openList[i] = tmpLoc;
					i = p;
				}
				else {
					// binary heap sort completed
					break;
				}
			}
		}

		/// <summary>
		/// Remove node from open list (for A star search)
		/// </summary>
		/// <param name="gridLoc">Grid location removed from open list</param>
		private void OpenListRemove(out GridCoordinates gridLoc) {
			// retrieve first item in open list
			gridLoc = openList[0];
			openListFlag[gridLoc.X][gridLoc.Y] = false;

			// move last item to start of open list, i.e. replace first item with last item
			openList[0] = openList[openList.Count - 1];

			// remove last item in open list
			openList.RemoveAt(openList.Count - 1);

			// children indexes, initial index, and temporary storages
			int c1, c2, i_init;
			GridCoordinates tmpLoc;

			// index to start checking
			int i = 0;

			// loop until item has shifted to proper place in binary heap
			while (true) {
				// save index for comparison
				i_init = i;

				// children indexes
				c1 = 2 * i + 1;
				c2 = 2 * i + 2;

				// compare f cost of parent with children if they exist
				if (c2 <= openList.Count - 1) {
					// both children exists
					// select lowest of two children
					if (costF[openList[i].X][openList[i].Y] >= costF[openList[c1].X][openList[c1].Y])
						i = c1;
					if (costF[openList[i].X][openList[i].Y] >= costF[openList[c2].X][openList[c2].Y])
						i = c2;
				}
				else if (c1 <= openList.Count - 1) {
					// only first child exists
					// check if f cost of parent is greater than f cost of child
					if (costF[openList[i].X][openList[i].Y] >= costF[openList[c1].X][openList[c1].Y])
						i = c1;
				}

				// check if swap if required
				if (i != i_init) {
					// item needs to be swap with one of its child
					tmpLoc = openList[i];
					openList[i] = openList[i_init];
					openList[i_init] = tmpLoc;
				}
				else {
					// item is less than or equal to both children, hence sort completed
					break;
				}
			}
		}

		/// <summary>
		/// Re-sort open list (for A star search)
		/// </summary>
		/// <param name="startIndex">Start index to start sorting from</param>
		private void OpenListReSort(int startIndex) {
			// parent index and temporary storage
			int p;
			GridCoordinates tmpLoc;

			// index to start checking
			int i = startIndex;
			// loop until item at start index is sorted in binary heap
			while (i != 0) {
				// parent index
				p = (int)Math.Floor(((double)i - 1) / 2);

				// check if f cost of child is less than or equal to f cost of parent
				if (costF[openList[i].X][openList[i].Y] <= costF[openList[p].X][openList[p].Y]) {
					// swap child and parent
					tmpLoc = openList[p];
					openList[p] = openList[i];
					openList[i] = tmpLoc;
					i = p;
				}
				else {
					// binary heap sort completed
					break;
				}
			}
		}

		/// <summary>
		/// Add node to closed list (for A star search)
		/// </summary>
		/// <param name="gridLoc">Grid location to add to closed list</param>
		private void ClosedListAdd(GridCoordinates gridLoc) {
			closedListFlag[gridLoc.X][gridLoc.Y] = true;
		}

		/// <summary>
		/// Smooth path using moving average filter with span of 5
		/// </summary>
		private void SmoothPath() {
			// initialise smooth path
			List<double> smoothPathX = new List<double>();
			List<double> smoothPathY = new List<double>();

			// copy path to smooth path
			int pathTotal = path.Count;
			for (int i = 0; i < pathTotal; i++) {
				smoothPathX.Add((double)path[i].X);
				smoothPathY.Add((double)path[i].Y);
			}

			// smooth path for 3 iterations, number of iterations was empircally selected
			for (int i = 0; i < 3; i++) {
				SmoothMovingAverage(smoothPathX, out smoothPathX);
				SmoothMovingAverage(smoothPathY, out smoothPathY);
			}

			// save smooth path points
			smoothPath = new List<Coordinates>();
			int smoothPathTotal = smoothPathX.Count;
			for (int i = 0; i < smoothPathTotal; i++) {
				smoothPath.Add(new Coordinates(smoothPathX[i], smoothPathY[i]));
			}
		}

		/// <summary>
		/// Smooth data using moving average filter with span of 5
		/// </summary>
		/// <param name="dataIn">List of data to filter</param>
		/// <param name="dataOut">List of filtered data</param>
		private void SmoothMovingAverage(List<double> dataIn, out List<double> dataOut) {
			// initialise smoothed data
			dataOut = new List<double>();

			// loop through all data
			int dataInTotal = dataIn.Count;
			for (int i = 0; i < dataInTotal; i++) {
				if (i == 0 || i == dataInTotal - 1) {
					// for first and last data 
					// no average
					dataOut.Add(dataIn[i]);
				}
				else if (i == 1 || i == dataInTotal - 2) {
					// for second and second last data
					// average across a span of 3
					dataOut.Add((dataIn[i - 1] + dataIn[i] + dataIn[i + 1]) / 3);
				}
				else {
					// for remaining data
					// average across a span of 5
					dataOut.Add((dataIn[i - 2] + dataIn[i - 1] + dataIn[i] + dataIn[i + 1] + dataIn[i + 2]) / 5);
				}
			}
		}

		/// <summary>
		/// Reduced path
		/// </summary>
		private void ReducePath() {
			Coordinates prevPoint, nextPoint;
			double prevAngle, nextAngle, diffAngle;

			// tolerance angle for pruning points
			double toleranceAngle = 10 * Math.PI / 180;

			// initialise reduced path
			reducedPath = new List<Coordinates>(smoothPath);

			int total = reducedPath.Count - 1;
			for (int i = 1; i < total; i++) {
				// find previous and next points with respect to current point
				prevPoint = reducedPath[i - 1] - reducedPath[i];
				nextPoint = reducedPath[i + 1] - reducedPath[i];
				
				// find previous and next angles with respect to current point
				prevAngle = prevPoint.ArcTan;
				nextAngle = nextPoint.ArcTan;
				if (prevAngle < 0) prevAngle += 2 * Math.PI;
				if (nextAngle < 0) nextAngle += 2 * Math.PI;

				// find difference angle between previous and next points
				if (prevAngle > nextAngle)
					diffAngle = prevAngle - nextAngle;
				else
					diffAngle = nextAngle - prevAngle;

				// remove path point if difference angle is tolerable
				if (Math.Abs(diffAngle - Math.PI) < toleranceAngle) {
					reducedPath.RemoveAt(i);
					i--; 
					total--;
				}
			}
		}

		/// <summary>
		/// Bresenham line algorithm
		/// </summary>
		/// <param name="startLoc">Start location of line</param>
		/// <param name="endLoc">End location of line</param>
		/// <param name="lineLocs">Locations along line</param>
		public void BresenhamLine(GridCoordinates startLoc, GridCoordinates endLoc,
															out List<GridCoordinates> lineLocs) {
			GridCoordinates origStartLoc = startLoc;

			// create empty list of points
			lineLocs = new List<GridCoordinates>();

			Boolean steep = Math.Abs(endLoc.Y - startLoc.Y) > Math.Abs(endLoc.X - startLoc.X);

			if (steep == true) {
				Swap(ref startLoc.X, ref startLoc.Y);
				Swap(ref endLoc.X, ref endLoc.Y);
			}

			if (startLoc.X > endLoc.X) {
				Swap(ref startLoc.X, ref endLoc.X);
				Swap(ref startLoc.Y, ref endLoc.Y);
			}

			int deltaX = endLoc.X - startLoc.X;
			int deltaY = Math.Abs(endLoc.Y - startLoc.Y);
			int error  = 0;

			int y = startLoc.Y;
			int yStep;
			if (startLoc.Y < endLoc.Y)
				yStep = 1;
			else
				yStep = -1;

			// find intermediate poitns
			for (int x = startLoc.X; x <= endLoc.X; x++) {
				if (steep == true) {
					lineLocs.Add(new GridCoordinates(y, x));
				}
				else {
					lineLocs.Add(new GridCoordinates(x, y));
				}
	    
				error = error + deltaY;
	    
				if (2 * error >= deltaX) {
					y = y + yStep;
					error = error - deltaX;
				}
			}

			// reverse order of points if start of array is not start point
			if (origStartLoc != lineLocs[0]) 
				lineLocs.Reverse();
		}

		/// <summary>
		/// Bresenham circle algorithm
		/// </summary>
		/// <param name="centerLoc">Center location of circle</param>
		/// <param name="radius">Radius of circle</param>
		/// <param name="circleLocs">Locations along circle</param>
		public void BresenhamCircle(GridCoordinates centerLoc, int radius,
			                          out List<GridCoordinates> circleLocs) {
			// create empty list of points
			circleLocs = new List<GridCoordinates>();

			int f = 1 - radius;
			int ddF_x = 0;
			int ddF_y = -2 * radius;
			int x = 0;
			int y = radius;

			circleLocs.Add(new GridCoordinates(centerLoc.X, centerLoc.Y + radius));
			circleLocs.Add(new GridCoordinates(centerLoc.X, centerLoc.Y - radius));
			circleLocs.Add(new GridCoordinates(centerLoc.X + radius, centerLoc.Y));
			circleLocs.Add(new GridCoordinates(centerLoc.X - radius, centerLoc.Y));

			while (x < y) {
				if (f >= 0) {
					y--;
					ddF_y += 2;
					f     += ddF_y;
				}

				x++;
				ddF_x += 2;
				f     += ddF_x + 1;

				circleLocs.Add(new GridCoordinates(centerLoc.X + x, centerLoc.Y + y));
				circleLocs.Add(new GridCoordinates(centerLoc.X - x, centerLoc.Y + y));
				circleLocs.Add(new GridCoordinates(centerLoc.X + x, centerLoc.Y - y));
				circleLocs.Add(new GridCoordinates(centerLoc.X - x, centerLoc.Y - y));
				circleLocs.Add(new GridCoordinates(centerLoc.X + y, centerLoc.Y + x));
				circleLocs.Add(new GridCoordinates(centerLoc.X - y, centerLoc.Y + x));
				circleLocs.Add(new GridCoordinates(centerLoc.X + y, centerLoc.Y - x));
				circleLocs.Add(new GridCoordinates(centerLoc.X - y, centerLoc.Y - x));
			}
		}

		/// <summary>
		/// Swap numbers (for Bresenham line algorithm)
		/// </summary>
		/// <param name="a">First value to swap</param>
		/// <param name="b">Second value to swap</param>
		private void Swap(ref int a, ref int b) {
			int t = a;
			a = b;
			b = t;
		}

		/// <summary>
		/// Write grid data to text file (for debugging purposes)
		/// </summary>
		/// <param name="fileName">File name write to</param>
		public void WriteGridToFile(string fileName) {
			// open file for writing
			TextWriter tw = new StreamWriter(fileName);
			
			// loop through entire grid data
			for (int x = 0; x < xSize; x++) {
				// write all y values for given x value
				for (int y = 0; y < ySize; y++) {
#if GRID_UNSAFE
					tw.Write(data[x*data_stride+y]);
#else
					tw.Write(data[x][y]);
#endif
					tw.Write(' ');
				}
				
				// move to next line
				tw.WriteLine();
			}
			
			// close file
			tw.Close();
		}

		/// <summary>
		/// Write A star path to text file (for debugging purposes)
		/// </summary>
		/// <param name="fileName">File name to write to</param>
		public void WritePathToFile(string fileName) {
			// open file for writing
			TextWriter tw = new StreamWriter(fileName);

			// loop through all path points
			int pathTotal = path.Count;
			for (int i = 0; i < pathTotal; i++) {
				// write A star path point
				tw.Write(path[i].X);
				tw.Write(' ');
				tw.Write(path[i].Y);
				tw.Write(' ');

				// write smoothed path point
				tw.Write(smoothPath[i].X);
				tw.Write(' ');
				tw.Write(smoothPath[i].Y);
				tw.Write(' ');

				// write reduced path point
				if (i < reducedPath.Count) {
					tw.Write(reducedPath[i].X);
					tw.Write(' ');
					tw.Write(reducedPath[i].Y);
					tw.Write(' ');
				}

				// move to next line
				tw.WriteLine();
			}

			// close file
			tw.Close();
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		protected virtual void Dispose(bool disposing) {
#if GRID_UNSAFE
			if (data != null) {
				Memory.Free((void*)data);
				GC.RemoveMemoryPressure(data_stride*xSize*4);
				disposed = true;
			}
#endif
		}

		~Grid() {
			Dispose(false);
		}
	}
}
