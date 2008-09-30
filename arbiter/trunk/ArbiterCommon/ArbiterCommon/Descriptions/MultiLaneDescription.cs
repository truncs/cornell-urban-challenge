using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterCommon
{
	public class MultiLaneDescription : LaneDescription
	{
		private bool existMultipleLanes;

		public MultiLaneDescription(bool existMultiple, bool laneIsValid)
			: base(laneIsValid)
		{
			this.existMultipleLanes = existMultiple;
		}
	}
}
