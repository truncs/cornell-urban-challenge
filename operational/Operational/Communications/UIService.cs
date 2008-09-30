using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUIService;
using UrbanChallenge.OperationalUIService.Parameters;
using Dataset.Source;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Common;
using OperationalLayer.Pose;
using UrbanChallenge.Common.Pose;
using NameService;
using UrbanChallenge.OperationalUIService.Debugging;
using UrbanChallenge.Common.Sensors;

namespace OperationalLayer.Communications {
	class UIService : OperationalUIFacade, IPingable {
		private DatasetSourceFacade datasetFacade;
		private TunableParameterFacade paramFacade;

		public UIService() {
			if (Services.Dataset == null) {
				throw new InvalidOperationException("Dataset must be initialized before creating UIService");
			}

			if (Services.Params == null) {
				throw new InvalidOperationException("TunableParameterTable must be intialized before creating UIService");
			}

			datasetFacade = new DatasetSourceFacade(Services.Dataset);
			paramFacade = new TunableParameterFacade(Services.Params);

			// list in the object directory
			if (!Settings.TestMode) {
				CommBuilder.BindObject(OperationalUIFacade.ServiceName, this);
			}
		}

		public override DatasetSourceFacade DatasetFacade {
			get { return datasetFacade; }
		}

		public override TunableParameterFacade TunableParamFacade {
			get { return paramFacade; }
		}

		public override void Ping() {
			// nothing to do
		}

		public void PushRelativePath(LineList list, CarTimestamp timestamp, string name) {
			try {
				// convert to the current time
				// get the absolute transform
				AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();

				list = list.Transform(absTransform);

				Services.Dataset.ItemAs<LineList>(name).Add(list, absTransform.Timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send line data to ui: {0}", ex.Message);
			}
		}

		public void PushAbsolutePath(LineList list, CarTimestamp timestamp, string name) {
			Services.Dataset.ItemAs<LineList>(name).Add(list, timestamp);
		}

		public void PushLineList(LineList list, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (relative) {
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;
					list = list.Transform(absTransform);
				}

				Services.Dataset.ItemAs<LineList>(name).Add(list, timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send line data to ui: {0}", ex.Message);
			}
		}

		public void PushPoint(Coordinates point, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (relative) {
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;

					point = absTransform.TransformPoint(point);
				}

				Services.Dataset.ItemAs<Coordinates>(name).Add(point, timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send point data to ui: {0}", ex.Message);
			}
		}

		public void PushPoints(Coordinates[] points, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (relative) {
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;

					points = absTransform.TransformPoints(points);
				}

				Services.Dataset.ItemAs<Coordinates[]>(name).Add(points, timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send points data to ui: {0}", ex.Message);
			}
		}

		public void PushPolygon(Polygon polygon, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (relative) {
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;

					polygon = polygon.Transform(absTransform);
				}

				Services.Dataset.ItemAs<Polygon>(name).Add(polygon, timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send polygon data to ui: {0}", ex.Message);
			}
		}

		public void PushPolygons(Polygon[] polygons, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (polygons == null)
					return;

				if (relative) {
					Polygon[] transPolygons = new Polygon[polygons.Length];
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;
					for (int i = 0; i < polygons.Length; i++) {
						transPolygons[i] = polygons[i].Transform(absTransform);
					}

					polygons = transPolygons;
				}

				Services.Dataset.ItemAs<Polygon[]>(name).Add(polygons, timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send polygon data to ui: {0}", ex.Message);
			}
		}

		public void PushObstacles(OperationalObstacle[] obstacles, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (obstacles == null || obstacles.Length == 0)
					return;

				if (relative) {
					OperationalObstacle[] transformObstacles = new OperationalObstacle[obstacles.Length];
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;
					for (int i = 0; i < obstacles.Length; i++) {
						transformObstacles[i] = obstacles[i].ShallowClone();
						transformObstacles[i].poly = obstacles[i].poly.Transform(absTransform);
					}

					obstacles = transformObstacles;
				}

				Services.Dataset.ItemAs<OperationalObstacle[]>(name).Add(obstacles, timestamp);
			}
			catch (Exception) {
			}
		}

		public void PushCircle(Circle circle, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (relative) {
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;

					circle = circle.Transform(absTransform);
				}

				Services.Dataset.ItemAs<Circle>(name).Add(circle, timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send circle data to ui: {0}", ex.Message);
			}
		}

		public void PushCircleSegment(CircleSegment circle, CarTimestamp timestamp, string name, bool relative) {
			try {
				if (relative) {
					AbsoluteTransformer absTransform = Services.StateProvider.GetAbsoluteTransformer(timestamp).Invert();
					timestamp = absTransform.Timestamp;

					circle = circle.Transform(absTransform);
				}

				Services.Dataset.ItemAs<CircleSegment>(name).Add(circle, timestamp);
			}
			catch (Exception ex) {
				OperationalLayer.Tracing.OperationalTrace.WriteWarning("could not send circle data to ui: {0}", ex.Message);
			}
		}

		public override object InitializeLifetimeService() {
			return null;
		}

		public override DebuggingFacade DebuggingFacade {
			get { return Services.DebuggingService; }
		}
	}
}
