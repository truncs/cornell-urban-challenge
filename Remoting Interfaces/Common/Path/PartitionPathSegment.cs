using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.RndfNetwork;

namespace UrbanChallenge.Common.Path
{
	[Serializable]
	public class PartitionPathSegment : LinePathSegment
	{
		UserPartition partition;

		internal PartitionPathSegment(Coordinates start, Coordinates end, UserPartition partition) : base(start, end)
		{
			this.partition = partition;
		}

		public UserPartition UserPartition
		{
			get { return partition; }
		}

		public override Coordinates Start
		{
			get { return base.Start; }
			set { throw new NotSupportedException(); }
		}

		public override Coordinates End
		{
			get { return base.End; }
			set { throw new NotSupportedException(); }
		}

		public override bool Equals(IPathSegment other)
		{
			return Equals(other);	
		}

		public override bool Equals(object other)
		{
			if (other is PartitionPathSegment)
			{
				return partition.Equals(((PartitionPathSegment)other).partition);
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return partition.GetHashCode();
		}
	}
}
