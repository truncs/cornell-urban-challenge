using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.Core.CoreIntelligence.Navigation;
using UrbanChallenge.Arbiter.ArbiterRoads;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.Tactical.Road
{
	/// <summary>
	/// Task types
	/// </summary>
	public enum TypeOfTasks
	{
		Straight,
		Left,
		Right
	}

	/// <summary>
	/// Inference about tasks
	/// </summary>
	public class TaskReasoning
	{
		/// <summary>
		/// Navigational planning information
		/// </summary>
		public RoadPlan navigationPlan;

		/// <summary>
		/// Current lane
		/// </summary>
		public ArbiterLane currentLane;

		/// <summary>
		/// Available tasks
		/// </summary>
		private Dictionary<TypeOfTasks, List<LanePlan>> tasks;

		/// <summary>
		/// Set the road plan and populate the tasks
		/// </summary>
		/// <param name="rp"></param>
		/// <param name="current"></param>
		public void SetRoadPlan(RoadPlan rp, ArbiterLane current)
		{
			Dictionary<ArbiterLaneId, LanePlan> plans = rp.LanePlans;

			// left
			List<LanePlan> left = new List<LanePlan>();

			// straight
			List<LanePlan> straight = new List<LanePlan>();

			// right
			List<LanePlan> right = new List<LanePlan>();

			// tmp
			ArbiterLane temp = current.LaneOnLeft;

			// left
			while (temp != null && temp.Way.Equals(current.Way))
			{
				if (plans.ContainsKey(temp.LaneId))
				{
					left.Add(plans[temp.LaneId]);
				}

				temp = temp.LaneOnLeft;
			}

			// right
			temp = current.LaneOnRight;
			while (temp != null && temp.Way.Equals(current.Way))
			{
				if (plans.ContainsKey(temp.LaneId))
				{
					right.Add(plans[temp.LaneId]);
				}

				temp = temp.LaneOnRight;
			}

			// straight
			if (plans.ContainsKey(current.LaneId))
			{
				straight.Add(plans[current.LaneId]);
			}

			// create tasks
			tasks = new Dictionary<TypeOfTasks, List<LanePlan>>();
			tasks.Add(TypeOfTasks.Left, left);
			tasks.Add(TypeOfTasks.Straight, straight);
			tasks.Add(TypeOfTasks.Right, right);
		}

		/// <summary>
		/// Sets the supra road plan
		/// </summary>
		/// <param name="rp"></param>
		/// <param name="current"></param>
		public void SetSupraRoadPlan(RoadPlan rp, SupraLane current)
		{
			this.navigationPlan = rp;

			// left
			List<LanePlan> left = new List<LanePlan>();

			// straight
			List<LanePlan> straight = new List<LanePlan>();

			// right
			List<LanePlan> right = new List<LanePlan>();

			Dictionary<ArbiterLaneId, LanePlan> plans = rp.LanePlans;

			// straight
			if (plans.ContainsKey(current.Initial.LaneId))
			{
				straight.Add(plans[current.Initial.LaneId]);
			}
			if (plans.ContainsKey(current.Final.LaneId))
			{
				straight.Add(plans[current.Final.LaneId]);
			}

			// create tasks
			tasks = new Dictionary<TypeOfTasks, List<LanePlan>>();
			tasks.Add(TypeOfTasks.Left, left);
			tasks.Add(TypeOfTasks.Straight, straight);
			tasks.Add(TypeOfTasks.Right, right);

			//s
			/*Dictionary<ArbiterLaneId, LanePlan> plans = rp.LanePlans;

			// left
			List<LanePlan> left = new List<LanePlan>();

			// straight
			List<LanePlan> straight = new List<LanePlan>();

			// right
			List<LanePlan> right = new List<LanePlan>();

			// tmp
			ArbiterLane temp = current.Initial.LaneOnLeft;

			// left
			while (temp != null && (temp.Way.Equals(current.Initial.Way) || temp.Way.Equals(current.Final.Way)))
			{
				if (plans.ContainsKey(temp.LaneId))
				{
					left.Add(plans[temp.LaneId]);
				}

				temp = temp.LaneOnLeft;
			}

			// right
			temp = current.Initial.LaneOnRight;
			while (temp != null && (temp.Way.Equals(current.Initial.Way) || temp.Way.Equals(current.Final.Way)))
			{
				if (plans.ContainsKey(temp.LaneId))
				{
					right.Add(plans[temp.LaneId]);
				}

				temp = temp.LaneOnRight;
			}

			// straight
			if (plans.ContainsKey(current.Initial.LaneId))
			{
				straight.Add(plans[current.Initial.LaneId]);
			}
			if (plans.ContainsKey(current.Final.LaneId))
			{
				straight.Add(plans[current.Final.LaneId]);
			}

			// create tasks
			tasks = new Dictionary<TypeOfTasks, List<LanePlan>>();
			tasks.Add(TypeOfTasks.Left, left);
			tasks.Add(TypeOfTasks.Straight, straight);
			tasks.Add(TypeOfTasks.Right, right);*/
		}

		/// <summary>
		/// Get the best task
		/// </summary>
		public TypeOfTasks Best
		{
			get
			{
				TypeOfTasks best = TypeOfTasks.Straight;
				double time = Double.MaxValue;

				foreach (KeyValuePair<TypeOfTasks, List<LanePlan>> plan in tasks)
				{
					foreach (LanePlan lp in plan.Value)
					{
						if (lp.laneWaypointOfInterest.TotalTime < time)
						{
							best = plan.Key;
							time = lp.laneWaypointOfInterest.TotalTime;
						}
					}
				}

				return best;
			}
		}

		/// <summary>
		/// Resets values held over time
		/// </summary>
		public void Reset()
		{
			currentLane = null;
			navigationPlan = null;
		}
	}
}
