using System;
using System.Collections.Generic;
using System.Text;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public interface ISelectable : IHittable {
		void OnSelect();
		void OnDeselect();

		bool IsSelected { get; }
	}
}
