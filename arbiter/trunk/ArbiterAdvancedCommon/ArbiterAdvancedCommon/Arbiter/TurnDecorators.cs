using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Behaviors;

namespace UrbanChallenge.Arbiter.Core.Common
{
	/// <summary>
	/// Some default turn decorators
	/// </summary>
	public class TurnDecorators
	{
		/// <summary>
		/// No decorators
		/// </summary>
		public static List<BehaviorDecorator> NoDecorators;

		/// <summary>
		/// Left turn
		/// </summary>
		public static List<BehaviorDecorator> LeftTurnDecorator;

		/// <summary>
		/// Right turn
		/// </summary>
		public static List<BehaviorDecorator> RightTurnDecorator;

		/// <summary>
		/// Hazard
		/// </summary>
		public static List<BehaviorDecorator> HazardDecorator;

		/// <summary>
		/// Initialize decorators
		/// </summary>
		public static void Initialize()
		{
			// no decorators
			List<BehaviorDecorator> nd = new List<BehaviorDecorator>();
			nd.Add(new TurnSignalDecorator(TurnSignal.Off));
			TurnDecorators.NoDecorators = nd;

			// left decorators
			List<BehaviorDecorator> ltd = new List<BehaviorDecorator>();
			ltd.Add(new TurnSignalDecorator(TurnSignal.Left));
			TurnDecorators.LeftTurnDecorator = ltd;

			// right decorators
			List<BehaviorDecorator> rtd = new List<BehaviorDecorator>();
			rtd.Add(new TurnSignalDecorator(TurnSignal.Right));
			TurnDecorators.RightTurnDecorator = rtd;

			// hazard decorators
			List<BehaviorDecorator> hd = new List<BehaviorDecorator>();
			hd.Add(new TurnSignalDecorator(TurnSignal.Hazard));
			TurnDecorators.HazardDecorator = hd;
		}
	}
}
