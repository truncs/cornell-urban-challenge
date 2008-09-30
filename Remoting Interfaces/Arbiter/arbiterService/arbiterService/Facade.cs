using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

// Rndf
using UrbanChallenge.arbiter.apriori.parser;
using UrbanChallenge.arbiter.apriori.parser.rndf;
using UrbanChallenge.arbiter.apriori.parser.mdf;

// Strategic
using UrbanChallenge.arbiter.strategic;
using UrbanChallenge.arbiter.strategic.state;
using UrbanChallenge.arbiter.strategic.route;
using UrbanChallenge.arbiter.strategic.goals;

namespace UrbanChallenge.arbiterService
{
    public enum arbiterType { rndf, strategic, mdf, state, routes, goal, position }

    [Serializable]
    public struct UpdatedProxy
    {
        private bool state;
        public bool State
        {
            get { return state; }
            set { state = value; }
        }

        private bool routes;
        public bool Routes
        {
            get { return routes; }
            set { routes = value; }
        }

        private bool goal;
        public bool Goal
        {
            get { return goal; }
            set { goal = value; }
        }

        private bool position;
        public bool Position
        {
            get { return position; }
            set { position = value; }
        }

        private bool routeIDs;
        public bool RouteIDs
        {
            get { return routeIDs; }
            set { routeIDs = value; }
        }
        
    }

    public abstract class Facade : MarshalByRefObject
    {
        // ******** Single Use Items ********** //

        // Rndf
        public abstract Parse_Rndf GetRndf();
        public abstract void SetRndf(Parse_Rndf myRndf);
        public abstract Parse_Mdf GetMdf();
        public abstract void SetMdf(Parse_Mdf myMdf);

        // Strategic Layer
        public abstract Strategic GetStrategic();
        public abstract void SetStrategic(Strategic myStrategic);

        // State
        public abstract State GetState();
        public abstract void SetState(State myState);

        // Route List
        public abstract List<Route> GetRoutes();
        public abstract void SetRoutes(List<Route> myRoutes);
        public abstract List<List<string>> GetRouteIDs();
        public abstract void SetRouteIds(List<List<string>> myRouteIDs);

        // Goal
        public abstract Goal GetGoal();
        public abstract void SetGoal(Goal myGoal);

        // MapPosition
        public abstract MapPosition GetPosition();
        public abstract void SetPosition(MapPosition myPosition);

        // Update
        public abstract UpdatedProxy GetUpdated();

        // For ArbiterGui
        public abstract void arbiterGuiReady();
        public abstract void arbiterGuiDisconnect();
    }
}