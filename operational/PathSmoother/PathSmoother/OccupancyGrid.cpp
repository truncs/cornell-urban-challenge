#include "OccupancyGrid\OccupancyGridInterface.h"

#ifdef __cplusplus_cli
#pragma managed(on)
#endif

using namespace System;
using namespace UrbanChallenge::Common;

namespace UrbanChallenge {
	namespace PathSmoothing {
		public enum class OccupancyStatus {
			Unknown = 0,
			Free = 1,
			Occupied = 2
		};

		public ref class OccupancyGrid {
		private:
			OccupancyGridInterface* occup_grid;

		public:
			OccupancyGrid() {
				occup_grid = new OccupancyGridInterface();
			}

			// destructor/dispose
			~OccupancyGrid() {
				delete occup_grid;
				occup_grid = NULL;
				GC::SuppressFinalize(this);
			}

			// finalizer
			!OccupancyGrid() {
				if (occup_grid != NULL) {
					OccupancyGrid::~OccupancyGrid();
				}
			}

			property bool IsDisposed {
				bool get() { return occup_grid == NULL; }
			}

			double LoadNewestGrid() {
				if (occup_grid == NULL)
					throw gcnew ObjectDisposedException("occupancy grid has been disposed");

				return occup_grid->LoadNewestAvailableGrid();
			}

			OccupancyStatus GetOccupancy(Coordinates pt) {
				return (OccupancyStatus)occup_grid->GetOccupancy_VehCoords((float)pt.X, (float)pt.Y);
			}
		};
	}
}