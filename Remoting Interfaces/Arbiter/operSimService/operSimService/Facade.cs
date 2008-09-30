using System;
using System.Collections.Generic;
using System.Text;

using UrbanChallenge.arbiter.strategic;
using UrbanChallenge.arbiter.strategic.state;

using UrbanChallenge.arbiter.behavioral.behaviors;
using UrbanChallenge.arbiter.behavioral.path;

namespace UrbanChallenge.operSimService
{
    public abstract class Facade : MarshalByRefObject
    {
        public abstract MapPosition getNewPosition(MapPosition position, State state, RouteOption routeOption);
        public abstract void setStrategic(Strategic strategic);
        public abstract State getNewState(State state, RouteOption routeOption);
    }
}
