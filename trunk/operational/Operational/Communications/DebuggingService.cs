using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUIService.Debugging;
using System.Threading;
using UrbanChallenge.Simulator.Client;
using OperationalLayer.Obstacles;
using OperationalLayer.OperationalBehaviors;
using OperationalLayer.Tracking;
using UrbanChallenge.Operational.Common;

namespace OperationalLayer.Communications {
	class DebuggingService : DebuggingFacade {
		private bool stepMode;

		private List<KeyValuePair<Type, AutoResetEvent>> sequencerEvents;
		private volatile bool inSequence;

		public DebuggingService(bool registerWithSim) {
			SetupSequencerEvents();

			if (registerWithSim) {
				SimulatorClientFacade simClient = (SimulatorClientFacade)CommBuilder.GetObject("SimulationClient");
				simClient.RegisterSteppableClient(this, ClientRunControlFacade.OperationalStepOrder);
			}
		}

		private void SetupSequencerEvents() {
			sequencerEvents = new List<KeyValuePair<Type, AutoResetEvent>>();

			// create the stages we want to sequence
			sequencerEvents.Add(new KeyValuePair<Type, AutoResetEvent>(typeof(ObstaclePipeline), new AutoResetEvent(false)));
			sequencerEvents.Add(new KeyValuePair<Type, AutoResetEvent>(typeof(BehaviorManager), new AutoResetEvent(false)));
			sequencerEvents.Add(new KeyValuePair<Type, AutoResetEvent>(typeof(TrackingManager), new AutoResetEvent(false)));
			sequencerEvents.Add(new KeyValuePair<Type, AutoResetEvent>(typeof(DebuggingService), new AutoResetEvent(false)));
		}

		public override bool StepMode {
			get { return stepMode; }
		}

		public void WaitOnSequencer(Type type) {
			if (!stepMode) {
				return;
			}

			// find the sequencer event
			KeyValuePair<Type, AutoResetEvent> sequencerEvent = sequencerEvents.Find(delegate(KeyValuePair<Type, AutoResetEvent> kvp) { 
				return kvp.Key == type; 
			});

			if (sequencerEvent.Key == type) {
				sequencerEvent.Value.WaitOne();
			}
		}

		public void SetCompleted(Type type) {
			if (!stepMode) {
				return;
			}

			int sequencerEventIndex = sequencerEvents.FindIndex(delegate(KeyValuePair<Type, AutoResetEvent> kvp) {
				return kvp.Key == type;
			});

			if (sequencerEventIndex != -1) {
				if (sequencerEventIndex == sequencerEvents.Count-1) {
					// this was the last dude to run, set that we're not longer in a sequence
					inSequence = false;
				}
				else {
					// there are more left in the chain, set the next event to go through
					sequencerEvents[sequencerEventIndex+1].Value.Set();
				}
			}
		}

		public override void SetStepMode() {
			foreach (KeyValuePair<Type, AutoResetEvent> ev in sequencerEvents) {
				ev.Value.Reset();
			}

			stepMode = true;
		}

		public override void SetContinuousMode(double realtimeFactor) {
			stepMode = false;

			// let all waiting dudes through
			foreach (KeyValuePair<Type, AutoResetEvent> ev in sequencerEvents) {
				ev.Value.Set();
			}
		}

		public override void Step() {
			if (inSequence) {
				return;
			}
			else {
				inSequence = true;
				sequencerEvents[0].Value.Set();

				WaitOnSequencer(typeof(DebuggingService));
				// we're the last dude
				inSequence = false;
			}
		}

		public override PlanningGrid GetPlanningGrid(PlanningGrids requestedGrid) {
			Grid grid = null;
			switch (requestedGrid) {
				case PlanningGrids.CostGrid:
					grid = Services.ObstacleManager.gridCost;
					break;

				case PlanningGrids.LaneBoundGrid:
					grid = Services.ObstacleManager.gridLaneBound;
					break;

				case PlanningGrids.ObstacleGrid:
					grid = Services.ObstacleManager.gridObstacle;
					break;

				case PlanningGrids.ObstacleIDGrid:
					grid = Services.ObstacleManager.gridObstacleID;
					break;

				case PlanningGrids.PathGrid:
					grid = Services.ObstacleManager.gridPath;
					break;

				case PlanningGrids.PathScaleGrid:
					grid = Services.ObstacleManager.gridPathScale;
					break;

				case PlanningGrids.SearchPathGrid:
					grid = Services.ObstacleManager.gridSearchPath;
					break;
			}

			if (grid != null) {
				PlanningGrid ret = new PlanningGrid(grid.XSize, grid.YSize, grid.Data, Services.ObstacleManager.Spacing, -Services.ObstacleManager.Spacing*grid.XMiddle, -Services.ObstacleManager.Spacing*grid.YMiddle);

				if (grid.WindowEnabled) {
					ret.SetWindow(grid.WindowLowerX, grid.WindowLowerY, grid.WindowUpperX, grid.WindowUpperY);
				}

				return ret;
			}
			else {
				return null;
			}
		}

		public override object InitializeLifetimeService() {
			return null;
		}

		public override AvoidanceDetails GetAvoidanceDetails() {
			return PathPlanning.PathPlanner.LastAvoidanceDetails;
		}

		public override bool GenerateAvoidanceDetails {
			get { return PathPlanning.PathPlanner.GenerateDetails; }
			set { PathPlanning.PathPlanner.GenerateDetails = value; }
		}
	}
}
