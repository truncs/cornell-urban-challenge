using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Arbiter.ArbiterRoads;
using UrbanChallenge.Common.Vehicle;
using UrbanChallenge.Arbiter.Core.Common.Reasoning;
using UrbanChallenge.Arbiter.Core.Common.State;
using UrbanChallenge.Arbiter.Core.Communications;
using UrbanChallenge.Arbiter.Core.Common.Arbiter;

namespace UrbanChallenge.Arbiter.Core.CoreIntelligence.State
{
	/// <summary>
	/// Provides reasoning and filtering over the scene estimator's lane estimate
	/// </summary>
	public class LaneAgent
	{
		private List<PosteriorEvidence> PosteriorEvidence;
		private List<InternalState> InternalState;
		private List<Probability> FilteredEstimate;
		private int maxSteps = 50;
		private ArbiterLane sceneLikelyLane;

		/// <summary>
		/// Constructor
		/// </summary>
		public LaneAgent()
		{
			this.PosteriorEvidence = new List<PosteriorEvidence>();
			this.InternalState = new List<InternalState>();
			this.FilteredEstimate = new List<Probability>();
		}

		/// <summary>
		/// Updates teh internal state
		/// </summary>
		/// <param name="updatedState"></param>
		/// <param name="reset"></param>
		public void UpdateInternal(InternalState updatedState, bool reset)
		{
			if (reset)
			{
				this.PosteriorEvidence = new List<PosteriorEvidence>();
				this.PosteriorEvidence.Add(new PosteriorEvidence(new Dictionary<ArbiterLane, double>()));

				this.InternalState = new List<InternalState>();
				this.InternalState.Add(updatedState);
				this.InternalState.Add(updatedState);

				this.FilteredEstimate = new List<Probability>();
				this.FilteredEstimate.Add(updatedState.Confidence);
			}
			else
			{
				if (this.PosteriorEvidence.Count >= maxSteps)
					this.PosteriorEvidence.RemoveAt(0);

				if (this.InternalState.Count >= maxSteps)
					this.InternalState.RemoveAt(0);

				if (this.FilteredEstimate.Count >= maxSteps)
					this.FilteredEstimate.RemoveAt(0);

				this.InternalState.Add(updatedState);
			}
			
			CoreCommon.CurrentInformation.LAInitial = updatedState.Target != null ? updatedState.Initial.ToString() : "";
			CoreCommon.CurrentInformation.LATarget = updatedState.Target != null ? updatedState.Target.ToString() : "";
		}

		/// <summary>
		/// Updates evidence
		/// </summary>
		/// <param name="observations"></param>
		public void UpdateEvidence(List<AreaEstimate> observations)
		{
			// dictionary mapping lanes to values
			Dictionary<ArbiterLane, double> areaEstimates = new Dictionary<ArbiterLane, double>();

			// loop over area possibilities
			foreach (AreaEstimate ae in observations)
			{
				// do nothing for now and return first for nav tests
				if (ae.AreaType == StateAreaType.Lane)
				{
					// get lane
					ArbiterLane al = this.GetLane(ae.AreaId);

					// assign
					if (areaEstimates.ContainsKey(al))
					{
						// update
						areaEstimates[al] = areaEstimates[al] + ae.Probability;
					}
					else
					{
						// add new
						areaEstimates.Add(al, ae.Probability);
					}
				}
			}

			// output
			if(this.InternalState.Count > 0)
			{
				foreach(KeyValuePair<ArbiterLane, double> kvp in areaEstimates)
				{
					if (kvp.Key.LaneId.Equals(this.InternalState[this.InternalState.Count - 1].Initial))
					{
						CoreCommon.CurrentInformation.LAPosteriorProbInitial = kvp.Value.ToString("F6");
					}

					if (kvp.Key.LaneId.Equals(this.InternalState[this.InternalState.Count - 1].Target))
					{
						CoreCommon.CurrentInformation.LAPosteriorProbTarget = kvp.Value.ToString("F6");
					}					 
				}
			}
			if (this.FilteredEstimate.Count > 0)
			{
				Probability p = this.FilteredEstimate[this.FilteredEstimate.Count - 1];
				CoreCommon.CurrentInformation.LAProbabilityCorrect = p.T.ToString("F6");
			}


			// new estimate
			PosteriorEvidence pe = new PosteriorEvidence(areaEstimates);

			// add
			PosteriorEvidence.Add(pe);
		}

		/// <summary>
		/// Filter the estiamtes
		/// </summary>
		public IState UpdateFilter()
		{
			// forward filter
			this.FilteredEstimate.Add(this.Forward());

			// check again if we should use lane agent for state
			if (CoreCommon.CorePlanningState.UseLaneAgent)
			{
				if (FilterSteady)
					return CoreCommon.CorePlanningState;
				else
				{
					ArbiterOutput.Output("Lane Agent Filter Diverged, Resetting Planning State");
					return new StartUpState();
				}
			}
			else
			{
				return CoreCommon.CorePlanningState;
			}
		}

		// transition matrix
		private Probability trans = new Probability(0.98, 0.02);

		// Does the posterior evidence at time t agree with the internal state at time t?
		private bool Ut(PosteriorEvidence et, int t)
		{
			if (et.LaneProbabilities.Count != 0)
			{
				double with = 0.0;
				double against = 0.0;

				// get probabilities
				foreach (KeyValuePair<ArbiterLane, double> est in et.LaneProbabilities)
				{
					if (est.Key.LaneId.Equals(InternalState[t].Initial) || est.Key.LaneId.Equals(InternalState[t].Target))
					{
						with += est.Value;
					}
					else
					{
						against += est.Value;
					}
				}

				// make prob
				Probability agreement = (new Probability(with, against, true)).Normalize();

				// consistency
				bool ct = this.Ct(this.PosteriorEvidence);
				CoreCommon.CurrentInformation.LAConsistent = ct.ToString();

				// check if this is greater than, say 40% in agreement
				if (ct)
					return agreement.T > 0.2 ? true : false;
				else
					return true;
			}
			else
			{
				return true;
			}
		}

		// Is the posterior evidence consistent over time?
		private bool Ct(List<PosteriorEvidence> e1t)
		{
			ArbiterLane lane = null;

			if (e1t.Count == 50)
			{
				// check if previous are consistent
				for (int i = 0; i < e1t.Count; i++)
				{
					if (e1t[i].LaneProbabilities.Count > 0)
					{
						double max = Double.MinValue;
						ArbiterLane curLane = null;

						foreach (KeyValuePair<ArbiterLane, double> est in e1t[i].LaneProbabilities)
						{
							if (est.Value > max || curLane == null)
							{
								max = est.Value;
								curLane = est.Key;
							}
						}

						if (lane == null)
							lane = curLane;
						else
						{
							if (!lane.Equals(curLane))
							{
								return false;
							}
						}
					}
				}

				if (lane == null)
					return false;
				else
				{
					this.sceneLikelyLane = lane;
					CoreCommon.CurrentInformation.LASceneLikelyLane = this.sceneLikelyLane.ToString();
					return true;
				}
			}
			else
			{
				this.sceneLikelyLane = null;
				return false;
			}
		}

		// calculates sensor model
		private Probability Et(int tp1, bool ut)
		{
			// standard probability of posterior agreeing with internal lane est
			Probability eStandard = new Probability(0.65, 0.35);

			// modify probabilities off of consistency
			Probability et = eStandard;

			// return
			return et;
		}

		// f1:t+1 = sFORWARD(f1:t, et+1)
		private Probability Forward()
		{
			// get t and t+1
			int tp1 = this.FilteredEstimate.Count;
			int t = tp1 - 1;

			// check numbering
			if (t < 0)
				return new Probability(0.9, 0.2);

			// transition
			Probability transition  = new Probability(0.7, 0.3);

			// calculate prediction from t-1 to t
			// sum(xt): P(Xt+1|xt)P(xt|e1:t)			
			Probability prediction = transition * FilteredEstimate[t];
			prediction = new Probability(Math.Max(prediction.T, 0.00001), Math.Max(prediction.F, 0.00001), true);

			// calculate boolean evidence for t+1
			bool ut = this.Ut(PosteriorEvidence[tp1], tp1);
			//Probability ct = this.Ct(this.PosteriorEvidence, ut);

			// calculate sensor model given the estiamtes agree
			Probability evidence = ut ? new Probability(0.9, 0.2) : new Probability(0.2, 0.9);
			
			//update with evidence for time t, calculate P(et+1|Xt+1) * (PreviousStep)
			//Probability filtered = ut ? et * prediction : et.Invert() * prediction;
			Probability filtered = evidence * prediction;

			// normalize
			Probability nFiltered = filtered.Normalize();

			// return 
			return nFiltered;
		}

		/// <summary>
		/// Determines most likely lane we are in given evidence
		/// </summary>
		/// <param name="partitionProbabilities"></param>
		/// <returns></returns>
		public ArbiterLane MostLikelyLane()
		{
			// dictionary mapping lanes to values
			Dictionary<ArbiterLane, double> areaEstimates = new Dictionary<ArbiterLane, double>();

			// loop over area possibilities
			foreach (PosteriorEvidence pe in this.PosteriorEvidence)
			{				
				// do nothing for now and return first for nav tests
				foreach (KeyValuePair<ArbiterLane, double> lp in pe.LaneProbabilities)
				{
					if (areaEstimates.ContainsKey(lp.Key))
					{
						areaEstimates[lp.Key] = areaEstimates[lp.Key] + lp.Value;
					}
					else
					{
						areaEstimates.Add(lp.Key, lp.Value);
					}
				}
			}

			KeyValuePair<ArbiterLane, double> best = new KeyValuePair<ArbiterLane, double>(null, 0);

			// get best
			foreach (KeyValuePair<ArbiterLane, double> lp in areaEstimates)
			{
				if (lp.Value > best.Value)
					best = lp;
			}

			// return most likely
			return best.Key;
		}

		/// <summary>
		/// Gets lane from a partition id as a string
		/// </summary>
		/// <param name="partitionId"></param>
		/// <returns></returns>
		public ArbiterLane GetLane(string partitionId)
		{
			// split id along delimeters
			string[] delimeters = new string[] { "." };
			string[] ids = partitionId.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

			// waypoint id
			ArbiterWaypointId awi =
				new ArbiterWaypointId(int.Parse(ids[3]),
				new ArbiterLaneId(int.Parse(ids[2]),
				new ArbiterWayId(int.Parse(ids[1]),
				new ArbiterSegmentId(int.Parse(ids[0])))));

			// get wp
			ArbiterWaypoint aw = (ArbiterWaypoint)CoreCommon.RoadNetwork.ArbiterWaypoints[awi];

			// get lane
			return aw.Lane;
		}

		/// <summary>
		/// Checks if hte filtered estimate agrees with the internal lane states
		/// </summary>
		public bool FilterSteady
		{
			get
			{
				return FilteredEstimate.Count > 0 ? FilteredEstimate[FilteredEstimate.Count - 1].T > 0.1 : true;
			}
		}

		/// <summary>
		/// Lane to switch to if can
		/// </summary>
		public ArbiterLane FilteredLane
		{
			get
			{
				return this.sceneLikelyLane;
			}
		}
	}
}
