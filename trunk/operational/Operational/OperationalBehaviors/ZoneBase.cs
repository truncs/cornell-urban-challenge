using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.Shapes;
using UrbanChallenge.Behaviors;
using UrbanChallenge.Common;
using OperationalLayer.Obstacles;
using OperationalLayer.Pose;

namespace OperationalLayer.OperationalBehaviors {
	abstract class ZoneBase : PlanningBase {
		protected Polygon zonePerimeter;
		protected Polygon[] zoneBadRegions;

		protected ScalarSpeedCommand recommendedSpeed;

		public override void Initialize(Behavior b) {
			Services.ObstaclePipeline.ExtraSpacing = 0;
			Services.ObstaclePipeline.UseOccupancyGrid = true;

			base.Initialize(b);
		}

		protected void HandleBaseBehavior(ZoneBehavior b) {
			// storing everything in absolute coordinates
			this.zonePerimeter = b.ZonePerimeter;
			this.zoneBadRegions = b.StayOutside;
			this.recommendedSpeed = b.RecommendedSpeed;

			Services.UIService.PushPolygons(b.StayOutside, b.TimeStamp, "zone bad regions", false);
			Services.UIService.PushPolygon(b.ZonePerimeter, b.TimeStamp, "zone perimeter", false);
		}

		
	}
}
