using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common;

namespace OperationalLayer.Obstacles {
	unsafe class GridOccupancy : Grid{

		// possible grid cell values
		public const float CELL_UNKNOWN = 0;
		public const float CELL_EMPTY	 = 1;
		public const float CELL_FULL		 = 2;

		private double gridStep;	// distance per step (m)
		private double gridDist;	// distance for grid (m)

		// GridOccupancy class constructor
		public GridOccupancy() {
			// empty constructor
		}

		// GridOccupancy class constructor
		public GridOccupancy(double step, double dist) {
			Initialise(step, dist);
		}

		// initialise grid given grid step and distance
		public void Initialise(double step, double dist) {
			// define grid step and distance
			gridStep = step;
			gridDist = dist;

			// define grid
			int gridSize = (int)(2 * Math.Round(gridDist / gridStep) + 1);

			// intialise grid data
			base.Initialise(gridSize, gridSize);
		}

		// return whether grid cell is unknown
		public Boolean IsUnknown(int x, int y) {
#if GRID_UNSAFE
			return (data[x*data_stride+y] == CELL_UNKNOWN);
#else
			return (data[x][y] == CELL_UNKNOWN);
#endif
		}

		// return whether grid cell is empty
		public Boolean IsEmpty(int x, int y) {
#if GRID_UNSAFE
			return (data[x*data_stride+y] == CELL_EMPTY);
#else
			return (data[x][y] == CELL_EMPTY);
#endif
		}

		// return whether grid cell is full or occupied
		public Boolean IsFull(int x, int y) {
#if GRID_UNSAFE
			return (data[x*data_stride+y] == CELL_FULL);
#else
			return (data[x][y] == CELL_FULL);
#endif
		}

		// get approximated obstacle points centered on grid cells
		public void GetObstacles(double vehicleHeading, out Coordinates[] obstaclePoints) {
			List<Coordinates> points = new List<Coordinates>();

			// convert occupancy grid data back to obstacle points (approximate using center of grid cell)
			Coordinates point;
			for (int x = 0; x < xSize; x++) {
				for (int y = 0; y < ySize; y++) {
					if (GetValue(x, y) == CELL_FULL) {
						// transform to vehicle relative coordinates
						point = new Coordinates((x - middle.X) * gridStep, (y - middle.Y) * gridStep);
						points.Add(point.Rotate(-vehicleHeading));
					}
				}
			}

			obstaclePoints = points.ToArray();
		}

		// shift grid given change in vehicle position since last update
		public void Shift(Coordinates vehicleShift) {
			// shift grid according to vehicle position offset
			int mx = (int)Math.Round(vehicleShift.X / gridStep);
			int my = (int)Math.Round(vehicleShift.Y / gridStep);

			// return if no shift required
			if (mx == 0 && my == 0)
				return;

			// shift is required
			int xLimit, yLimit;
			if (mx > 0) {
				// vehicle has moved in +ve x direction
				if (my > 0) {
					// vehicle has moved in +ve y direction
					// shift left and down (mx +ve, my +ve)
					xLimit = xSize - mx - 1;
					yLimit = ySize - my - 1;
					for (int x = 0; x < xSize; x++) {
						for (int y = 0; y < ySize; y++) {
							if (x < xLimit && y < yLimit)
								SetValue(x, y, GetValue(x + mx, y + my));
							else
								SetValue(x, y, CELL_UNKNOWN);
						}
					}
				}
				else {
					// vehicle has moved in -ve y direction or not moved in y direction
					// shift left and up (mx +ve, my -ve)
					xLimit = xSize - mx - 1;
					yLimit = - my;
					for (int x = 0; x < xSize; x++) {
						for (int y = ySize - 1; y >= 0; y--) {
							if (x < xLimit && y >= yLimit)
								SetValue(x, y, GetValue(x + mx, y + my));
							else
								SetValue(x, y, CELL_UNKNOWN);
						}
					}
				}
			}
			else {
				// vehicle has moved in -ve x direction or not moved in x direction
				if (my > 0) {
					// vehicle has moved in +ve y direction
					// shift right and down (mx -ve, my +ve)
					xLimit = -mx;
				  yLimit = ySize - my - 1;
					for (int x = xSize - 1; x >= 0; x--) {
						for (int y = 0; y < ySize; y++) {
							if (x >= xLimit && y < yLimit)
								SetValue(x, y, GetValue(x + mx, y + my));
							else
								SetValue(x, y, CELL_UNKNOWN);
						}
					}
				}
				else {
					// vehicle has moved in -ve y direction or not moved in y direction
					// shift right and up (mx -ve, my -ve)
					xLimit = -mx;
					yLimit = -my;
					for (int x = xSize - 1; x >= 0; x--) {
						for (int y = ySize - 1; y >= 0; y--) {
							if (x >= xLimit && y >= yLimit)
								SetValue(x, y, GetValue(x + mx, y + my));
							else
								SetValue(x, y, CELL_UNKNOWN);
						}
					}
				}
			}
		}

		// update grid with obstacle given as bearing and range with respect to vehicle
		public void Update(double[] obstacleBearings, double[] obstacleRanges, double vehicleHeading) {
			// vehicle frame
			Coordinates vec = new Coordinates(1, 0);
			
			// create obstacle points given bearings and ranges
			Coordinates[] obstaclePoints = new Coordinates[obstacleBearings.Length];
			for (int i = 0; i < obstaclePoints.Length; i++) {
				obstaclePoints[i] = (vec*obstacleRanges[i]).Rotate(obstacleBearings[i]);
			}

			// update grid using obstacle points
			Update(obstaclePoints, vehicleHeading);
		}

		// update grid with obstacle given as coordinate with respect to vehicle
		public void Update(Coordinates[] obstaclePoints, double vehicleHeading) {
			// convert obstacle points to grid locations with respect to vehicle heading 
			for (int i = 0; i < obstaclePoints.Length; i++) {
				// rotate obstacle point according to vehicle heading
				obstaclePoints[i] = obstaclePoints[i].Rotate(vehicleHeading);

				// convert obstacle point to grid locations
				obstaclePoints[i] = new Coordinates(Math.Round(obstaclePoints[i].X / gridStep) + middle.X,
																						Math.Round(obstaclePoints[i].Y / gridStep) + middle.Y);
			}

			// update empty grid cells in occupancy grid
			for (int i = 0; i < obstaclePoints.Length; i++) {
				SetLineValues(middle.X, middle.Y, (int)obstaclePoints[i].X, (int)obstaclePoints[i].Y, CELL_EMPTY);
			}

			// update full grid cells in occupancy grid
			for (int i = 0; i < obstaclePoints.Length; i++) {
				SetValue((int)obstaclePoints[i].X, (int)obstaclePoints[i].Y, CELL_FULL);
			}
		}
	}
}
