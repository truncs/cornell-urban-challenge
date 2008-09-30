using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;
using UrbanChallenge.Common.Path;
using System.Diagnostics;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common.Vehicle;

namespace OperationalLayer.Obstacles {
	class ObstacleManager : IDisposable {
		[Obsolete]
		public enum ObstacleType {
			Left,
			Right,
			Ignore
		}

		// grid information
		private float gridStep;	// distance per step (m)
		private float gridDist;	// half size of grid (m)
		private int gridSizeX;		// x size of grid
		private int gridSizeY;		// y size of grid
		private int gridMiddleX;	// x middle of grid
		private int gridMiddleY;	// y middle of grid
		private float maxCost;		// max cost value for grid

		// grid window
		private int gridWindowLowerX;
		private int gridWindowLowerY;
		private int gridWindowUpperX;
		private int gridWindowUpperY;

		// grid paths
		private List<GridCoordinates> gridBasePath, gridLaneBoundLeft, gridLaneBoundRight;

		// grids
		public Grid gridPath, gridPathScale, gridObstacle, gridObstacleID,
								gridLaneBound, gridCost, gridSearchPath;

		// processing times
		private long gridWindowTime, gridPathTime, gridPathScaleTime, gridObstacleTime, 
								 gridLaneBoundTime, gridCostTime, gridCostPathTime, gridSearchPathTime, 
								 obstaclePathTime, obstacleFlagsTime;

		// stopwatch
		private Stopwatch watch;

		// flag for write to file (for debugging)
		private Boolean writeToFileFlag;

		// flag for process time display (for debugging)
		private int timeDisplayFlag;

		/// <summary>
		/// ObstacleManager class constructor
		/// </summary>
		public ObstacleManager() {
			Initialise();
		}

		/// <summary>
		/// Initialise basic settings for obstacle manager
		/// </summary>
		private void Initialise() {
			// initialise max cost value in grids
			maxCost = 40.0f;

			// initialise stop watch
			watch = new Stopwatch();

			// initialise flag to write data to file (for debugging)
			writeToFileFlag = false;

			// initialise flag for process time display (for debugging)
			timeDisplayFlag = 1; // 0 - None, 1 - Total Time Only, 2 - Detailed Times

			InitialiseGrid(80);
		}

		/// <summary>
		/// Initialise grid settings for obstacle manager
		/// </summary>
		/// <param name="dist">half width of grid, in meters</param>
		private void InitialiseGrid(float dist) {
			// initialise grid information
			gridStep = 0.25f;
			gridDist = dist;	// half size of grid, in m, it must be large enough to enclose path
			gridSizeX = (int)(2 * Math.Round(gridDist / gridStep) + 1);
			gridSizeY = gridSizeX;

			// initialise grid paths
			gridBasePath = new List<GridCoordinates>();
			gridLaneBoundLeft = new List<GridCoordinates>();
			gridLaneBoundRight = new List<GridCoordinates>();

			// initialise grids
			gridPath = new Grid(gridSizeX, gridSizeY);
			gridPathScale = new Grid(gridSizeX, gridSizeY);
			gridObstacle = new Grid(gridSizeX, gridSizeY);
			gridObstacleID = new Grid(gridSizeX, gridSizeY);
			gridLaneBound = new Grid(gridSizeX, gridSizeY);
			gridCost = new Grid(gridSizeX, gridSizeY);
			gridSearchPath = new Grid(gridSizeX, gridSizeY);

			// initialise grid middle
			gridMiddleX = gridPath.XMiddle;
			gridMiddleY = gridPath.YMiddle;
		}

		public float Spacing {
			get { return gridStep; }
		}

		public float GridDist {
			get { return gridDist; }
		}

		/// <summary>
		/// Run obstacle manager
		/// </summary>
		/// <param name="curPath"></param>
		/// <param name="curLeftBound"></param>
		/// <param name="curRightBound"></param>
		/// <param name="obstacleClusters"></param>
		/// <param name="obstaclePath"></param>
		/// <param name="obstacleFlag"></param>
		/// <param name="successFlag"></param>
		public void ProcessObstacles(LinePath curPath, IList<LinePath> curLeftBounds, IList<LinePath> curRightBounds,
																 IList<Obstacle> obstacleClusters, double laneWidthAtPathEnd, bool reverse, bool sparse,
			out LinePath obstaclePath, out bool successFlag) {
			// start process stopwatch
			Stopwatch processWatch = new Stopwatch();
			processWatch.Start();

			// initialise grid windows
			InitialiseGridWindows(curPath, curLeftBounds, curRightBounds);

			// create grid for path and path scale
			UpdateGridPath(curPath, reverse, sparse);

			// create grid for lane bounds
			UpdateGridLaneBound(curLeftBounds, curRightBounds);

			// create grid for obstacles
			UpdateGridObstacle(obstacleClusters);

			// create grid for cost map to find path
			UpdateGridCost();

			// find search path using cost grid
			FindPath(curPath, laneWidthAtPathEnd);

			// generate obstacle path
			UpdateObstaclePath(out obstaclePath);

			// create grid for obstacle path
			UpdateGridSearchPath(obstaclePath);

			// set up obstacle types
			UpdateObstacleTypes(obstacleClusters, out successFlag);

			// stop process stopwatch
			processWatch.Stop();

			// display total processing time (for debugging)
			if (timeDisplayFlag != 0) {
				long totalTime = gridWindowTime + gridPathTime + gridPathScaleTime + gridObstacleTime + gridLaneBoundTime + 
												 gridCostTime + gridCostPathTime + gridSearchPathTime + obstaclePathTime + obstacleFlagsTime;
				Console.WriteLine("================================================================================");
				Console.WriteLine("ObstacleManager - Total Process (Exclude Writes) - Elapsed (ms): {0} of {1}", 
													totalTime, processWatch.ElapsedMilliseconds);
				Console.WriteLine("================================================================================");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="curPath"></param>
		/// <param name="curLeftBound"></param>
		/// <param name="curRightBound"></param>
		private void InitialiseGridWindows(LinePath curPath, IList<LinePath> curLeftBounds, IList<LinePath> curRightBounds) {
			StartWatch();	// start stopwatch

			// find min and max limits of path and lane bounds
			double minX = double.MaxValue;
			double minY = double.MaxValue;
			double maxX = double.MinValue;
			double maxY = double.MinValue;
			FindMinMax(curPath,			  ref minX, ref maxX, ref minY, ref maxY);
			foreach (LinePath curLeftBound in curLeftBounds) {
				FindMinMax(curLeftBound, ref minX, ref maxX, ref minY, ref maxY);
			}
			foreach (LinePath curRightBound in curRightBounds) {
				FindMinMax(curRightBound, ref minX, ref maxX, ref minY, ref maxY);
			}

			// create tight window
			gridWindowLowerX = (int)Math.Round(minX / gridStep) + gridMiddleX;
			gridWindowLowerY = (int)Math.Round(minY / gridStep) + gridMiddleY;
			gridWindowUpperX = (int)Math.Round(maxX / gridStep) + gridMiddleX;
			gridWindowUpperY = (int)Math.Round(maxY / gridStep) + gridMiddleY;

			// expand window
			int offset = (int)Math.Round(maxCost);
			gridWindowLowerX = Math.Max(gridWindowLowerX - offset, 0);
			gridWindowLowerY = Math.Max(gridWindowLowerY - offset, 0);
			gridWindowUpperX = Math.Min(gridWindowUpperX + offset, gridSizeX - 1);
			gridWindowUpperY = Math.Min(gridWindowUpperY + offset, gridSizeY - 1);

			// set up grid windows
			bool windowEnabled = true;
			GridCoordinates windowLowerLeft  = new GridCoordinates(gridWindowLowerX, gridWindowLowerY);
			GridCoordinates windowUpperRight = new GridCoordinates(gridWindowUpperX, gridWindowUpperY);
			gridPath.SetWindow(windowLowerLeft, windowUpperRight, windowEnabled);
			gridPathScale.SetWindow(windowLowerLeft, windowUpperRight, windowEnabled);
			gridObstacle.SetWindow(windowLowerLeft, windowUpperRight, windowEnabled);
			gridObstacleID.SetWindow(windowLowerLeft, windowUpperRight, windowEnabled);
			gridLaneBound.SetWindow(windowLowerLeft, windowUpperRight, windowEnabled);
			gridCost.SetWindow(windowLowerLeft, windowUpperRight, windowEnabled);
			gridSearchPath.SetWindow(windowLowerLeft, windowUpperRight, windowEnabled);

			gridPath.ZeroGrid(true);
			gridPathScale.ZeroGrid(true);
			gridObstacle.ZeroGrid(true);
			gridObstacleID.ZeroGrid(true);
			gridLaneBound.ZeroGrid(true);
			gridCost.ZeroGrid(true);
			gridSearchPath.ZeroGrid(true);

			// display time taken to process grid windows
			gridWindowTime = StopWatch("ObstacleManager - GridWindows - ");
		}

		/// <summary>
		/// Find minimum and maximum Xs and Ys for given path 
		/// </summary>
		/// <param name="path">Path to get limits from</param>
		/// <param name="minX">Minimum X of path</param>
		/// <param name="maxX">Maximum X of path</param>
		/// <param name="minY">Minimum Y of path</param>
		/// <param name="maxY">Maximum Y of path</param>
		private void FindMinMax(LinePath path, ref double minX, ref double maxX, ref double minY, ref double maxY) {
			int totalPoints = path.Count;
			for (int i = 0; i < totalPoints; i++) {
				minX = Math.Min(minX, path[i].X);
				minY = Math.Min(minY, path[i].Y);
				maxX = Math.Max(maxX, path[i].X);
				maxY = Math.Max(maxY, path[i].Y);
			}
		}

		/// <summary>
		/// Updates grid path and grid path scale with path information
		/// </summary>
		/// <param name="basePath">Base path points</param>
		private void UpdateGridPath(LinePath basePath, bool reverse, bool sparse) {
			StartWatch();	// start stopwatch

			// generate no-go regions near vehicle
			// no-go area in opposite direction of vehicle motion
			List<GridCoordinates> setLocs;
			if (reverse) {
				gridPath.SetLineValues(new GridCoordinates(gridMiddleX + 1, 0), new GridCoordinates(gridMiddleX + 1, gridSizeY - 1), maxCost, out setLocs);
				gridPath.FloodFill(new GridCoordinates(gridMiddleX + 2, gridMiddleY), maxCost, 0);
			}
			else {
				gridPath.SetLineValues(new GridCoordinates(gridMiddleX - 1, 0), new GridCoordinates(gridMiddleX - 1, gridSizeY - 1), maxCost, out setLocs);
				gridPath.FloodFill(new GridCoordinates(gridMiddleX - 2, gridMiddleY), maxCost, 0);
			}

			// radius for no-go circle regions
			float circleRadius = 7; // radius of circles beside rear axle, in meters
			int circleGridRadius = (int)Math.Round(circleRadius / gridStep);
			
			// generate no-go circle region to the left
			List<GridCoordinates> leftCircleLocs;
			GridCoordinates leftCircleCenterLoc = new GridCoordinates(gridMiddleX, gridMiddleY + circleGridRadius + 1);
			gridPath.BresenhamCircle(leftCircleCenterLoc, circleGridRadius, out leftCircleLocs);
			//gridPath.SetValues(leftCircleLocs, maxCost);
			//gridPath.FloodFill(leftCircleCenterLoc, maxCost, 0);
			gridPath.FillConvexInterior(leftCircleLocs, maxCost);

			// generate no-go circle region to the right
			List<GridCoordinates> rightCircleLocs;
			GridCoordinates rightCircleCenterLoc = new GridCoordinates(gridMiddleX, gridMiddleY - circleGridRadius - 1);
			gridPath.BresenhamCircle(rightCircleCenterLoc, circleGridRadius, out rightCircleLocs);
			//gridPath.SetValues(rightCircleLocs, maxCost);
			//gridPath.FloodFill(rightCircleCenterLoc, maxCost, 0);			
			gridPath.FillConvexInterior(rightCircleLocs, maxCost);

			float gridStartValue = 1;
			List<GridCoordinates> gridExtendLocs;

			// find grid base path points
			FindPoints(basePath, out gridBasePath);
			gridPath.SetValues(gridBasePath, gridStartValue);
			gridPathScale.SetValues(gridBasePath, gridStartValue);

			// extend start of base path to grid border, if necessary
			ExtendPoints(basePath[1], basePath[0], out gridExtendLocs);
			//gridPath.SetValues(gridExtendLocs, gridStartValue);
			gridPathScale.SetValues(gridExtendLocs, gridStartValue);

			// extend end of base path to grid border, if necessary
			int lastIndex = basePath.Count - 1;
			ExtendPoints(basePath[lastIndex - 1], basePath[lastIndex], out gridExtendLocs);
			gridPath.SetValues(gridExtendLocs, gridStartValue);
			gridPathScale.SetValues(gridExtendLocs, gridStartValue);

			// generate grid path using wavefront propagation
			gridPath.WaveFront(gridStartValue, 0, sparse ? 0.05f : 0.5f, maxCost);

			// display time taken to process grid path
			gridPathTime = StopWatch("ObstacleManager - GridPath - ");

			StartWatch();	// start stopwatch

			// grid path scale values
			float leftScaleValue  = gridStartValue; // scale value for region left of path
			float rightScaleValue = 0.75f;					// scale value for region right of path

			// set up left region for grid path scale using floodfill
			int offsetX = gridBasePath[1].X - gridBasePath[0].X;
			int offsetY = gridBasePath[1].Y - gridBasePath[0].Y;
			GridCoordinates leftStartLoc = new GridCoordinates(gridBasePath[0].X - offsetY, 
																												 gridBasePath[0].Y + offsetX);
			gridPathScale.FloodFill(leftStartLoc, leftScaleValue, 0);
			
			// set up right region for grid path scale by replacing remaining empty grid locations
			gridPathScale.Replace(0, rightScaleValue);

			// display time taken to process grid path scale
			gridPathScaleTime = StopWatch("ObstacleManager - GridPathScale - ");

			// write grid data to file (for debugging)
			if (writeToFileFlag == true) {
				// write grid path data to file
				StartWatch();
				gridPath.WriteGridToFile("GridPath.txt");
				StopWatch("ObstacleManager - GridPath File Write - ");

				// write grid path scale data to file
				StartWatch();
				gridPathScale.WriteGridToFile("GridPathScale.txt");
				StopWatch("ObstacleManager - GridPathScale File Write - ");
			}
		}

		/// <summary>
		/// Update grid lane bound 
		/// </summary>
		/// <param name="leftBoundPoints">Lane left bound points</param>
		/// <param name="rightBoundPoints">Lane right bound points</param>
		private void UpdateGridLaneBound(IList<LinePath> leftBounds, IList<LinePath> rightBounds) {
			StartWatch();	// start stopwatch

			Coordinates extPoint = new Coordinates();
			List<GridCoordinates> gridExtendLocs;

			foreach (LinePath leftBoundPoints in leftBounds) {
				// check if there is at least 2 left bound points
				if (leftBoundPoints.Count > 1) {
					// find grid lane bound left points
					FindPoints(leftBoundPoints, out gridLaneBoundLeft);
					gridLaneBound.SetValues(gridLaneBoundLeft, maxCost);

					// check if there are grid lane bound left points 
					if (gridLaneBoundLeft.Count != 0) {
					  // extend start of grid left lane bound to grid border
						extPoint = leftBoundPoints[0] - leftBoundPoints[1];
						extPoint = leftBoundPoints[0] + extPoint.RotateM90();
						ExtendPoints(leftBoundPoints[0], extPoint, out gridExtendLocs);
					  gridLaneBound.SetValues(gridExtendLocs, maxCost);

					  // extend end of grid left lane bound to grid border
					  int lastIndex = leftBoundPoints.Count - 1;
						extPoint = leftBoundPoints[lastIndex] - leftBoundPoints[lastIndex - 1];
						extPoint = leftBoundPoints[lastIndex] + extPoint.Rotate90().Normalize();
					  ExtendPoints(leftBoundPoints[lastIndex], extPoint, out gridExtendLocs);
					  gridLaneBound.SetValues(gridExtendLocs, maxCost);

						// floodfill lane bound area
						List<Coordinates> partialLaneBoundPoints = new List<Coordinates>();
						partialLaneBoundPoints.Add(leftBoundPoints[lastIndex]);
						partialLaneBoundPoints.Add(leftBoundPoints[lastIndex - 1]);
						partialLaneBoundPoints.Add(extPoint);
						Polygon partialLaneBoundArea = new Polygon(partialLaneBoundPoints);
						Coordinates partialLaneBoundAreaCenter = partialLaneBoundArea.Center;
						GridCoordinates partialGridLaneBoundAreaCenter = new GridCoordinates(
							(int)Math.Round(partialLaneBoundAreaCenter.X / gridStep) + gridMiddleX,
							(int)Math.Round(partialLaneBoundAreaCenter.Y / gridStep) + gridMiddleY);
						gridLaneBound.FloodFill(partialGridLaneBoundAreaCenter, maxCost, 0);
					}
				}
			}

			foreach (LinePath rightBoundPoints in rightBounds) {
				// check if there is at least 2 right bound points
				if (rightBoundPoints.Count > 1) {
					// find grid lane bound right points
					FindPoints(rightBoundPoints, out gridLaneBoundRight);
					gridLaneBound.SetValues(gridLaneBoundRight, maxCost);

					// check if there are grid lane bound right points 
					if (gridLaneBoundRight.Count != 0) {
						// extend start of grid right lane bound to grid border
						extPoint = rightBoundPoints[0] - rightBoundPoints[1];
						extPoint = rightBoundPoints[0] + extPoint.Rotate90();
						ExtendPoints(rightBoundPoints[0], extPoint, out gridExtendLocs);
						gridLaneBound.SetValues(gridExtendLocs, maxCost);

						// extend end of grid right lane bound to grid border
						int lastIndex = rightBoundPoints.Count - 1;
						extPoint = rightBoundPoints[lastIndex] - rightBoundPoints[lastIndex - 1];
						extPoint = rightBoundPoints[lastIndex] + extPoint.RotateM90().Normalize();
						ExtendPoints(rightBoundPoints[lastIndex], extPoint, out gridExtendLocs);
						gridLaneBound.SetValues(gridExtendLocs, maxCost);

						// floodfill lane bound area
						List<Coordinates> partialLaneBoundPoints = new List<Coordinates>();
						partialLaneBoundPoints.Add(rightBoundPoints[lastIndex]);
						partialLaneBoundPoints.Add(rightBoundPoints[lastIndex - 1]);
						partialLaneBoundPoints.Add(extPoint);
						Polygon partialLaneBoundArea = new Polygon(partialLaneBoundPoints);
						Coordinates partialLaneBoundAreaCenter = partialLaneBoundArea.Center;
						GridCoordinates partialGridLaneBoundAreaCenter = new GridCoordinates(
							(int)Math.Round(partialLaneBoundAreaCenter.X / gridStep) + gridMiddleX,
							(int)Math.Round(partialLaneBoundAreaCenter.Y / gridStep) + gridMiddleY);
						gridLaneBound.FloodFill(partialGridLaneBoundAreaCenter, maxCost, 0);
					}
				}
			}

			// generate grid lane bound using wavefront propagation
			float sepDist = 1;	// in m
			float step = maxCost * gridStep / sepDist;
			//gridLaneBound.WaveFront(maxCost, 0, -step);

			// display time to process grid lane bound
			gridLaneBoundTime = StopWatch("ObstacleManager - GridLaneBound - ");

			// write grid data to file (for debugging)
			if (writeToFileFlag == true) {
				StartWatch();
				gridLaneBound.WriteGridToFile("GridLaneBound.txt");
				StopWatch("ObstacleManager - GridLaneBound File Write - ");
			}
		}

		/// <summary>
		/// Update grid obstacle
		/// </summary>
		/// <param name="obstacleClusters">List of obstacle clusters</param>
		private void UpdateGridObstacle(IList<Obstacle> obstacleClusters) {
			StartWatch();	// start stopwatch

			// update grid with obstacle polygons
			int totalObstacleClusters = obstacleClusters.Count;
			List<GridCoordinates> gridLocs;
			//Coordinates obstacleCenter;
			//GridCoordinates obstacleGridCenter;
			for (int i = 0; i < totalObstacleClusters; i++) {
				FindPoints(obstacleClusters[i].cspacePolygon.GetSegmentEnumerator(), out gridLocs);

				gridObstacle.FillConvexInterior(gridLocs, maxCost);
				gridObstacleID.FillConvexInterior(gridLocs, i+1);

				// set grid obstacle points
				//gridObstacle.SetValues(gridLocs, maxCost);

				//// set grid obstacle IDs where ID number starts from 1
				//gridObstacleID.SetValues(gridLocs, i + 1);

				//// determine obstacle grid center using centroid of obstacle polygon
				//obstacleCenter = obstacleClusters[i].cspacePolygon.GetCentroid();
				//obstacleGridCenter.X = (int)Math.Round(obstacleCenter.X / gridStep) + gridMiddleX;
				//obstacleGridCenter.Y = (int)Math.Round(obstacleCenter.Y / gridStep) + gridMiddleY;

				//// flood fill inside of obstacle polygon
				//gridObstacle.FloodFill(obstacleGridCenter, maxCost, 0);
				//gridObstacleID.FloodFill(obstacleGridCenter, i + 1, 0);
			}

			// generate grid obstacle using wavefront propagation
			float sepDist = 2;	// in m
			float step = maxCost * gridStep / sepDist;
			//gridObstacle.WaveFront(maxCost, 0, -step);

			// display time to process grid obstacle
			gridObstacleTime = StopWatch("ObstacleManager - GridObstacle - ");

			// write grid data to file (for debugging)
			if (writeToFileFlag == true) {
				StartWatch();
				gridObstacle.WriteGridToFile("GridObstacle.txt");
				StopWatch("ObstacleManager - GridObstacle File Write - ");
			}
		}

		/// <summary>
		/// Update grid cost
		/// </summary>
		private void UpdateGridCost() {
			StartWatch();	// start stopwatch

			float gridPathValue, gridLaneBoundValue, gridObstacleValue;

			// update grid cost within window
			for (int x = gridWindowLowerX; x <= gridWindowUpperX; x++) {
				for (int y = gridWindowLowerY; y <= gridWindowUpperY; y++) {
					// update grid path value with scale factor and cap it at max cost
					gridPathValue = gridPath.GetValue(x, y);
					if (gridPathValue != maxCost)
						gridPathValue *= gridPathScale.GetValue(x, y);

					// get grid lane bound value
					gridLaneBoundValue = gridLaneBound.GetValue(x, y);

					// get grid obstacle value
					gridObstacleValue = gridObstacle.GetValue(x, y);

					// merge grid path, grid lane bound, and grid obstacle to obtain grid cost
					gridCost.SetValue(x, y, Math.Max(Math.Max(gridPathValue, gridLaneBoundValue), gridObstacleValue));
				}
			}

			// display time to process grid cost
			gridCostTime = StopWatch("ObstacleManager - GridCost - ");

			// write grid data to file (for debugging)
			if (writeToFileFlag == true) {
				StartWatch();
				gridCost.WriteGridToFile("GridCost.txt");
				StopWatch("ObstacleManager - GridCost File Write- ");
			}
		}

		/// <summary>
		/// Find lowest cost path
		/// </summary>
		/// <param name="pathPoints">Points for current path</param>
		/// <param name="leftBoundPoints">Points for left bound of current lane</param>
		/// <param name="rightBoundPoints">Points for right bound of current lane</param>
		private void FindPath(LinePath pathPoints, double laneWidthAtPathEnd) {
			StartWatch();	// start stopwatch

			// find left and right vectors from path end point
			//Coordinates pathEndPoint = pathPoints[pathPoints.Count - 1];
			//Coordinates pathLeftVec = leftBoundPoints.GetPoint(leftBoundPoints.GetClosestPoint(pathEndPoint)) - pathEndPoint;
			//Coordinates pathRightVec = rightBoundPoints.GetPoint(rightBoundPoints.GetClosestPoint(pathEndPoint)) - pathEndPoint;
			//pathLeftVec = pathLeftVec.Normalize(pathLeftVec.Length - 1.0);
			//pathRightVec = pathRightVec.Normalize(pathRightVec.Length - 1.0);

			// prepare multiple path end points for A star search
			//List<Coordinates> pathEndPoints = new List<Coordinates>();
			//pathEndPoints.Add(pathEndPoint + pathLeftVec.Normalize(Math.Max(pathLeftVec.Length - 1.0, 0)));
			//pathEndPoints.Add(pathEndPoint);
			//pathEndPoints.Add(pathEndPoint + pathRightVec.Normalize(Math.Max(pathRightVec.Length - 1.0, 0)));

			// find left and right vectors from path end point
			Coordinates pathEndPoint = pathPoints[pathPoints.Count - 1];
			Coordinates pathLeftVec = pathPoints.EndSegment.UnitVector.Rotate90().Normalize(Math.Max(laneWidthAtPathEnd / 2 - 1.0, 0));
			Coordinates pathRightVec = pathLeftVec.Rotate180();

			// prepare multiple path end points for A star search
			List<Coordinates> pathEndPoints = new List<Coordinates>();
			pathEndPoints.Add(pathEndPoint + pathLeftVec);
			pathEndPoints.Add(pathEndPoint);
			pathEndPoints.Add(pathEndPoint + pathRightVec);

			// find path end points on grid
			List<GridCoordinates> endLocs;
			FindPoints(pathEndPoints, out endLocs);

			// find path using A star search algorithm
			gridCost.AStarSearch(gridBasePath[0], endLocs, gridBasePath[gridBasePath.Count - 1]);

			// display time taken by function
			gridCostPathTime = StopWatch("ObstacleManager - GridCostPath - ");

			// write grid data to file (for debugging)
			if (writeToFileFlag == true) {
				StartWatch();
				gridCost.WritePathToFile("GridCostPath.txt");
				StopWatch("ObstacleManager - GridCostPath File Write - ");
			}
		}

		/// <summary>
		/// Updates obstacle path
		/// </summary>
		/// <param name="obstaclePath">Obstacle path from reduced path</param>
		private void UpdateObstaclePath(out LinePath obstaclePath) {
			StartWatch();	// start stopwatch

			// retrieve reduced path
			List<Coordinates> reducedPath;
			gridCost.GetReducedPath(out reducedPath);

			// prepare obstacle path
			obstaclePath = new LinePath();
			int totalPathPoints = reducedPath.Count;
			for (int i = 0; i < totalPathPoints; i++) {
				// convert grid coordinates back to vehicle relative coordinates
				obstaclePath.Add(new Coordinates((reducedPath[i].X - gridMiddleX) * gridStep,
																				 (reducedPath[i].Y - gridMiddleY) * gridStep));
			}

			// display time taken to process obstacle path
			obstaclePathTime = StopWatch("ObstacleManager - ObstaclePath - ");
		}

		/// <summary>
		/// Update grid search path
		/// </summary>
		/// <param name="obstaclePath">Path that avoids obstacles</param>
		private void UpdateGridSearchPath(LinePath obstaclePath) {
			StartWatch();	// start stopwatch

			float gridStartValue = 1;
			float gridExtValue = -1;
			List<GridCoordinates> gridExtendLocs;

			// retrieve A star search path
			List<GridCoordinates> searchPath;
			gridCost.GetPath(out searchPath);

			// extend start of path to grid border, if necessary
			ExtendPoints(obstaclePath[1], obstaclePath[0], out gridExtendLocs);
			gridSearchPath.SetValues(gridExtendLocs, gridExtValue);

			// extend end of path to grid border, if necessary
			int lastIndex = obstaclePath.Count - 1;
			ExtendPoints(obstaclePath[lastIndex - 1], obstaclePath[lastIndex], out gridExtendLocs);
			
			gridSearchPath.SetValues(gridExtendLocs, gridExtValue);

			// set grid search path points
			gridSearchPath.SetValues(searchPath, gridStartValue);

			// generate a line path with a 7.5 m extension off the end of the path
			//List<Coordinates> pathPoints = new List<Coordinates>();
			//pathPoints.Add(obstaclePath[obstaclePath.Count-1]);
			//pathPoints.Add(obstaclePath[obstaclePath.Count-1] + obstaclePath.EndSegment.Vector.Normalize(1.5*TahoeParams.VL));
			//FindPoints(pathPoints, out gridExtendLocs);
			//gridSearchPath.SetValues(gridExtendLocs, gridStartValue);

			// set up left region for grid search path using floodfill
			int offsetX = searchPath[1].X - searchPath[0].X;
			int offsetY = searchPath[1].Y - searchPath[0].Y;
			GridCoordinates leftStartLoc = new GridCoordinates(searchPath[0].X - offsetY,
																												 searchPath[0].Y + offsetX);
			gridSearchPath.FloodFill(leftStartLoc, gridStartValue + 1, 0);

			// set up right region for grid search path
			// do nothing, remains 0

			// display time to process grid search path
			gridSearchPathTime = StopWatch("ObstacleManager - GridSearchPath - ");

			// write grid data to file (for debugging)
			if (writeToFileFlag == true) {
				StartWatch();
				gridSearchPath.WriteGridToFile("GridSearchPath.txt");
				StopWatch("ObstacleManager - GridSearchPath File Write - ");
			}
		}

		/// <summary>
		/// Updates obstacle flags
		/// </summary>
		/// <param name="obstacleClustersFlag">Flags for obstacle clusters</param>
		/// <param name="successFlag">Flag for path success</param>
		private void UpdateObstacleTypes(IList<Obstacle> obstacles, out bool successFlag) {
			StartWatch();	// start stopwatch

			// initialise success
			successFlag = true;

			int totalObstacleClusters = obstacles.Count;

			int[] obstaclePointOnLeftSide  = new int[totalObstacleClusters];
			int[] obstaclePointOnRightSide = new int[totalObstacleClusters];

			// initialize avoidance status of all obstacles to unknown
			foreach (Obstacle obs in obstacles) {
				obs.avoidanceStatus = AvoidanceStatus.Unknown;
				obs.collisionPoints = null;
				obs.obstacleDistance = 0;
			}

			// iterate through all grid locations in window
			for (int x = gridWindowLowerX; x <= gridWindowUpperX; x++) {
				for (int y = gridWindowLowerY; y <= gridWindowUpperY; y++) {
					// iterate through all obstacle IDs
					for (int i = 0; i < totalObstacleClusters; i++) {
						// skip if obstacle ID do not match in current grid location
						if (i + 1 != gridObstacleID.GetValue(x, y))
							continue;

						// retrieve grid search path value in current grid location
						float pathValue = gridSearchPath.GetValue(x, y);

						// check if obstacle point is left of, right of, or on path
						if (pathValue == 2) {
							// obstacle point on left side of obstacle path
							obstaclePointOnLeftSide[i]++;
						}
						else if (pathValue == 0) {
							// obstacle point on right side of obstacle path
							obstaclePointOnRightSide[i]++;
						}
						else if (pathValue == 1) {
							// obstacle point on search path
							successFlag = false;

							obstacles[i].avoidanceStatus = AvoidanceStatus.Collision;

							if (obstacles[i].collisionPoints == null) {
								obstacles[i].collisionPoints = new List<Coordinates>();
							}

							obstacles[i].collisionPoints.Add(new Coordinates((x - gridMiddleX) * gridStep, (y - gridMiddleY) * gridStep));
						}
						else {
							// obstacle point on extended path 
							// for now, just don't do anything and it will be labelled as left or right later
							//if (obstacles[i].avoidanceStatus == AvoidanceStatus.Unknown) {
							//  obstacles[i].avoidanceStatus = AvoidanceStatus.Ignore;
							//}
						}
					}
				}
			}

			// update obstacle cluster flags based on obstacleClusterOnLeftSide counts
			for (int i = 0; i < totalObstacleClusters; i++) {
				if (obstacles[i].avoidanceStatus != AvoidanceStatus.Unknown)
					continue;

				if (obstaclePointOnLeftSide[i] > 0 || obstaclePointOnRightSide[i] > 0) {
					// obstacle cluster in grid
					// obstacle cluster considered to be left of path if half or more obstacle points are on the left
					if (obstaclePointOnLeftSide[i] - obstaclePointOnRightSide[i] >= 0) {
						obstacles[i].avoidanceStatus = AvoidanceStatus.Left;
					}
					else {
						obstacles[i].avoidanceStatus = AvoidanceStatus.Right;
					}
				}
				else {
					// obstacle cluster out of grid
					obstacles[i].avoidanceStatus = AvoidanceStatus.Ignore;
				}
			}

			// display time taken to process obstacle flags
			obstacleFlagsTime = StopWatch("ObstacleManager - ObstacleFlags - ");
		}

		/// <summary>
		/// Find points along path segments
		/// </summary>
		/// <param name="pathPoints">Points of path segments</param>
		/// <param name="gridLocs">Gird locations on path segments</param>
		private void FindPoints(List<Coordinates> pathPoints, out List<GridCoordinates> gridLocs) {
			// initialise points
			gridLocs = new List<GridCoordinates>();

			// start and end grid locations of path line segment
			GridCoordinates startLoc, endLoc;
			// grid locations of path line segment
			List<GridCoordinates> lineLocs;

			// find grid path points
			for (int i = 1; i < pathPoints.Count; i++) {
				// start grid location of path segment
				startLoc.X = (int)Math.Round(pathPoints[i - 1].X / gridStep) + gridMiddleX;
				startLoc.Y = (int)Math.Round(pathPoints[i - 1].Y / gridStep) + gridMiddleY;

				// end grid location of path segment
				endLoc.X = (int)Math.Round(pathPoints[i].X / gridStep) + gridMiddleX;
				endLoc.Y = (int)Math.Round(pathPoints[i].Y / gridStep) + gridMiddleY;

				// find grid points along path segment
				gridPath.BresenhamLine(startLoc, endLoc, out lineLocs);

				// save grid points
				for (int j = 0; j < lineLocs.Count; j++) {
					// skip first point for second segment onwards to prevent duplicates
					if (i > 1 && j == 0)
						continue;

					// save grid path points that are within grid
					if (InGrid(lineLocs[j]) == true)
						gridLocs.Add(lineLocs[j]);
				}

				// stop finding path points if path segment extends beyond grid
				if (endLoc.X >= gridSizeX || endLoc.Y >= gridSizeY)
					break;
			}
		}

		private void FindPoints(IEnumerable<LineSegment> pathSegs, out List<GridCoordinates> gridLocs) {
			// initialise points
			gridLocs = new List<GridCoordinates>();

			// start and end grid locations of path line segment
			GridCoordinates startLoc, endLoc;
			// grid locations of path line segment
			List<GridCoordinates> lineLocs;

			// find grid path points
			bool first = true;
			foreach (LineSegment ls in pathSegs) {
				// start grid location of path segment
				startLoc.X = (int)Math.Round(ls.P0.X / gridStep) + gridMiddleX;
				startLoc.Y = (int)Math.Round(ls.P0.Y / gridStep) + gridMiddleY;

				// end grid location of path segment
				endLoc.X = (int)Math.Round(ls.P1.X / gridStep) + gridMiddleX;
				endLoc.Y = (int)Math.Round(ls.P1.Y / gridStep) + gridMiddleY;

				// find grid points along path segment
				gridPath.BresenhamLine(startLoc, endLoc, out lineLocs);

				// save grid points
				for (int j = 0; j < lineLocs.Count; j++) {
					// skip first point for second segment onwards to prevent duplicates
					if (!first && j == 0)
						continue;

					// save grid path points that are within grid
					if (InGrid(lineLocs[j]) == true)
						gridLocs.Add(lineLocs[j]);
				}

				// stop finding path points if path segment extends beyond grid
				if (endLoc.X >= gridSizeX || endLoc.Y >= gridSizeY)
					break;

				first = false;
			}
		}

		/// <summary>
		/// Extend points to grid border given a line
		/// </summary>
		/// <param name="startPoint">Start point of line segment</param>
		/// <param name="endPoint">End point of line segment</param>
		/// <param name="gridLocs">Grid locations of line segment, includes locations outside grid</param>
		private void ExtendPoints(Coordinates startPoint, Coordinates endPoint, out List<GridCoordinates> gridLocs) {
			// redefine start and end points for extension
			Coordinates dirVec = endPoint - startPoint;
			//startPoint = endPoint;
			endPoint = endPoint + dirVec.Normalize(gridDist * 2.0);

			// start and end grid locations of line segment
			GridCoordinates startLoc, endLoc;

			// start grid location of line segment
			startLoc.X = (int)Math.Round(startPoint.X / gridStep) + gridMiddleX;
			startLoc.Y = (int)Math.Round(startPoint.Y / gridStep) + gridMiddleY;

			// end grid location of line segment
			endLoc.X = (int)Math.Round(endPoint.X / gridStep) + gridMiddleX;
			endLoc.Y = (int)Math.Round(endPoint.Y / gridStep) + gridMiddleY;

			// find grid locations of line segment, includes locations outside grid
			gridPath.BresenhamLine(startLoc, endLoc, out gridLocs);
		}

		/// <summary>
		/// Check if grid location is within grid
		/// </summary>
		/// <param name="gridLoc">Grid location to check</param>
		/// <returns></returns>
		public Boolean InGrid(GridCoordinates gridLoc) {
			if (gridLoc.X >= 0 && gridLoc.X < gridSizeX && gridLoc.Y >= 0 && gridLoc.Y < gridSizeY)
				return true;
			else
				return false;
		}

		/// <summary>
		/// Start stopwatch timer
		/// </summary>
		private void StartWatch() {
			if (timeDisplayFlag != 0) {
				watch.Reset();
				watch.Start();
			}
		}

		/// <summary>
		/// Stop stopwatch timer and display elasped time in milliseconds
		/// </summary>
		/// <param name="strValue">Optional string to concatenate in front of time display</param>
		/// <returns></returns>
		private long StopWatch(string strValue) {
			if (timeDisplayFlag != 0) {
				watch.Stop();
				if (timeDisplayFlag == 2) {
					Console.Write(strValue);
					Console.WriteLine("Elapsed (ms): {0}", watch.ElapsedMilliseconds);
				}
				return watch.ElapsedMilliseconds;
			}
			else
				return 0;
		}

		/// <summary>
		/// Stop stopwatch timer and display elasped time in milliseconds
		/// </summary>
		/// <returns></returns>
		private long StopWatch() {
			return StopWatch("");
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
		}

		#endregion

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (gridPath != null) gridPath.Dispose();
				if (gridPathScale != null) gridPathScale.Dispose();
				if (gridObstacle != null) gridObstacle.Dispose();
				if (gridObstacleID != null) gridObstacleID.Dispose();
				if (gridLaneBound != null) gridLaneBound.Dispose();
				if (gridCost != null) gridCost.Dispose();
				if (gridSearchPath != null) gridSearchPath.Dispose();
			}
		}
	}
}
