using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.OperationalUI.Common.GraphicsWrapper;

namespace UrbanChallenge.OperationalUI.Common.Map {
	public interface IRenderable {
		void Render(IGraphics g, WorldTransform wt);

		string Name { get; }
	}
}
