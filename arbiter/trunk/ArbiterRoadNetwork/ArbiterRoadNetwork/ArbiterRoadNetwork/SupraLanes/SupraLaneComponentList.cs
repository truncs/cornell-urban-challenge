using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.Arbiter.ArbiterRoads
{
	public class SupraLaneComponentList : List<ISupraLaneComponent>
	{
		public SupraLaneComponentList() : base()
		{
		}

		public new void Add(ISupraLaneComponent component)
		{
			if (this.Count > 0)
			{
				component.PreviousComponent = this[this.Count - 1];
				this[this.Count - 1].NextComponent = component;
				base.Add(component);
			}
			else
			{
				base.Add(component);
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is SupraLaneComponentList)
			{
				SupraLaneComponentList other = (SupraLaneComponentList)obj;
				if (other.Count == this.Count)
				{
					for (int i = 0; i < this.Count; i++)
					{
						if (!this[i].Equals(other[i]))
							return false;
					}

					return true;
				}				
			}

			return false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}
