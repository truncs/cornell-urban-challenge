using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Collections;
using UrbanChallenge.DarpaRndf;

namespace Parser
{
    // Parses the Rndf and Mdf into the common format    
    public class RndfParser
    {
		public int numWps;

		public RndfParser()
		{
			numWps = 0;
		}

        // Creates Mdf from an input FileStream
        public IMdf createMdf(FileStream fileStream)
        {
            // File in Read Only mode, convert to stream
            StreamReader r = new StreamReader(fileStream, Encoding.UTF8);

            // Create new queue for input buffer
            Queue q = new Queue();
            string word = "";

			

            // Create the Mdf
            IMdf mdf = new IMdf();
            mdf.SpeedLimits = new List<SpeedLimit>();
            mdf.CheckpointOrder = new List<string>();

            // Loop until reach end of file marker
            while ((word.Length < 8) || (word.Substring(0, 8) != "end_file"))
            {
                // get next word
                word = parseWord(r, q);

                if (word == "MDF_name")
                {
                    word = parseWord(r, q);
                    mdf.Name = word;
                }
                else if (word == "RNDF")
                {
                    word = parseWord(r, q);
                    mdf.RndfName = word;
                }
                else if (word == "format_version")
                {
                    word = parseWord(r, q);
                    mdf.Version = word;
                }
                else if (word == "creation_date")
                {
                    word = parseWord(r, q);
                    mdf.CreationDate = word;
                }
                else if (word == "checkpoints")
                {
                    // get number of checkpoints
                    word = parseWord(r, q);
                    word = parseWord(r, q);
                    mdf.NumberCheckpoints = word;

                    // create checkpoint list
                    word = parseWord(r, q);

                    // loop until end of checkpoints
                    while (word != "end_checkpoints")
                    {
                        // add checkpoint
                        mdf.CheckpointOrder.Add(word);

                        // get next word
                        word = parseWord(r, q);
                    }
                }
                else if (word == "speed_limits")
                {
                    // set number of speed limits
                    word = parseWord(r, q);
                    word = parseWord(r, q);
                    mdf.NumberSpeedLimits = word;

                    // get next word
                    word = parseWord(r, q);

                    // loop until end of speedlimits
                    while (word != "end_speed_limits")
                    {
                        // create new speed limit
                        SpeedLimit sl = new SpeedLimit();

                        // id
                        sl.SegmentID = word;

                        // min speed
                        word = parseWord(r, q);
                        sl.MinimumVelocity = Double.Parse(word);

                        // max speed
                        word = parseWord(r, q);
                        sl.MaximumVelocity = Double.Parse(word);
                        
                        // add speed limit to list of speed limits
                        mdf.SpeedLimits.Add(sl);

                        // get next word
                        word = parseWord(r, q);
                    }
                }
                else if (word == "end_file")
                {
                    Console.WriteLine("Mdf Parse :: Successful");
                }
                else
                {
                    Console.WriteLine("Unknown identifier: " + word);
                }
            }
            return mdf;        
        }

        // Creates Rndf from an input FileStream
        public IRndf createRndf(FileStream fileStream)
        {
			numWps = 0;

            // File in Read Only mode, convert to stream
            StreamReader r = new StreamReader(fileStream, Encoding.UTF8);

            // Create new queue for input buffer
            Queue q = new Queue();
            string word = "";

            // Create the Rndf (with only segments for now, no zones)
            IRndf rndf = new IRndf();
            rndf.Segments = new List<SimpleSegment>();
			rndf.Zones = new List<SimpleZone>();

            // Loop until reach end of file marker
            while ((word.Length < 8) || (word.Substring(0, 8) != "end_file"))
            {
                // get the next word
                word = parseWord(r, q);

                if (word == "RNDF_name")
                {
                    word = parseWord(r, q);
                    rndf.Name = word;
                }
                else if (word == "num_segments")
                {
                    word = parseWord(r, q);
                    rndf.NumSegs = int.Parse(word);
                }
                else if (word == "num_zones")
                {
                    word = parseWord(r, q);
                    rndf.NumZones = int.Parse(word);
                }
                else if (word == "format_version")
                {
                    word = parseWord(r, q);
                    rndf.FormatVersion = word;
                }
                else if (word == "creation_date")
                {
                    word = parseWord(r, q);
                    rndf.CreationDate = word;
                }
                else if (word == "segment")
                {
                    // create new segment
                    SimpleSegment seg = new SimpleSegment();
                    seg.Lanes = new List<SimpleLane>();
                    
                    word = parseWord(r, q);
                    seg.Id = word;

                    // run until reach end of segment marker
                    while (word != "end_segment")
                    {
                        // get next word
                        word = parseWord(r, q);

                        if (word == "segment_name")
                        {
                            word = parseWord(r, q);
                            seg.Name = word;
                        }
                        else if (word == "num_lanes")
                        {
                            word = parseWord(r, q);
                            seg.NumLanes = int.Parse(word);
                        }
                        else if (word == "end_segment")
                        {
                            // do nothing if at the end
                        }
                        else if (word == "lane")
                        {
                            // Create new lane
                            SimpleLane ln = new SimpleLane();
                            ln.Checkpoints = new List<SimpleCheckpoint>();
                            ln.Waypoints = new List<SimpleWaypoint>();
                            ln.Stops = new List<string>();
                            ln.ExitEntries = new List<SimpleExitEntry>();

                            word = parseWord(r, q);
                            ln.Id = word;

                            // run until reach end of lane
                            while (word != "end_lane")
                            {
                                // get next word
                                word = parseWord(r, q);

                                if (word == "num_waypoints")
                                {
                                    word = parseWord(r, q);
                                    ln.NumWaypoints = int.Parse(word);
                                }
                                else if (word == "checkpoint")
                                {
                                    // create checkpoint
                                    SimpleCheckpoint cp = new SimpleCheckpoint();

                                    // get waypoint id
                                    string wp = parseWord(r, q);
                                    cp.WaypointId = wp;

                                    // get checkpoint id
                                    string id = parseWord(r, q);
                                    cp.CheckpointId = id;

                                    // add to collection of checkpoints within lane
                                    ln.Checkpoints.Add(cp);
                                }
                                else if (word == "lane_width")
                                {
                                    word = parseWord(r, q);
                                    ln.LaneWidth = double.Parse(word);
                                }
                                else if (word == "stop")
                                {
                                    word = parseWord(r, q);
                                    ln.Stops.Add(word);
                                }
                                else if (word == "left_boundary")
                                {
                                    word = parseWord(r, q);
                                    ln.LeftBound = word;
                                }
                                else if (word == "right_boundary")
                                {
                                    word = parseWord(r, q);
                                    ln.RightBound = word;
                                }
                                else if (word == "exit")
                                {
                                    // create exit-entry pair
                                    SimpleExitEntry exitEntry = new SimpleExitEntry();

                                    // get the exit id
                                    string exit = parseWord(r, q);
                                    exitEntry.ExitId = exit;

                                    // get the entry id
                                    string entry = parseWord(r, q);
                                    exitEntry.EntryId = entry;

                                    // add to collection of exit-entry pairs within lane
                                    ln.ExitEntries.Add(exitEntry);
                                }
                                else if (word == "end_lane")
                                {
                                    // do nothing
                                }
                                // Otherwise we probably have a waypoint
                                else
                                {
                                    // check to make sure a wp by matching lane id to lane identifier of waypoint                                    
                                    int laneIdLength = ln.Id.Length;

                                    // if waypoint matches then create the waypoint
                                    if (word.Length >= laneIdLength + 2 && (word.Substring(0, laneIdLength)).CompareTo(ln.Id) == 0)
                                    {
                                        // create a new waypoint
                                        SimpleWaypoint wp = new SimpleWaypoint();
                                        wp.Position = new UrbanChallenge.Common.Coordinates();

                                        // set its id
                                        wp.ID = word;

                                        // get latitude or X
                                        string lat = parseWord(r, q);
										wp.Position.X = double.Parse(lat);

                                        // get longitude or y
                                        string lon = parseWord(r, q);
										wp.Position.Y = double.Parse(lon);

                                        // add to lane's collection of waypoints
                                        ln.Waypoints.Add(wp);

										numWps += 1;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Unknown identifier: " + word);
                                    }
                                }
                            }
                            seg.Lanes.Add(ln);
                        }
                        else
                        {
                            Console.WriteLine("Unknown identifier: " + word);
                        }
                    }
                    rndf.Segments.Add(seg);
                }
                else if (word == "zone")
                {
                    // create new zone
                    SimpleZone zone = new SimpleZone();
                    zone.ParkingSpots = new List<ParkingSpot>();
                    
                    // get ID                    
                    word = parseWord(r, q);
                    zone.ZoneID = word;

                    // run until reach end of segment marker
                    while (word != "end_zone")
                    {
                        // get next word
                        word = parseWord(r, q);

                        if (word == "num_spots")
                        {
                            // get next word
                            word = parseWord(r, q);

                            // set num of parking spots
                            zone.NumParkingSpots = int.Parse(word);
                        }
                        else if (word == "zone_name")
                        {
                            // get next word
                            word = parseWord(r, q);

                            // set zone name
                            zone.Name = word;
                        }
                        else if (word == "perimeter")
                        {
                            // create perimeter
                            zone.Perimeter = new ZonePerimeter();
                            zone.Perimeter.ExitEntries = new List<SimpleExitEntry>();
                            zone.Perimeter.PerimeterPoints = new List<PerimeterPoint>();

                            // set perimeter id
                            zone.Perimeter.PerimeterID = parseWord(r, q);

                            while (word != "end_perimeter")
                            {
                                // get next word
                                word = parseWord(r, q);

                                if (word == "num_perimeterpoints")
                                {
                                    // set num of perimeter points
                                    zone.Perimeter.NumPerimeterPoints = int.Parse(parseWord(r, q));
                                }
                                else if (word == "exit")
                                {
                                    // create new exit,entry
                                    SimpleExitEntry ee = new SimpleExitEntry();

                                    // set exit
                                    ee.ExitId = parseWord(r, q);

                                    // set entry
                                    ee.EntryId = parseWord(r, q);

                                    // add to perimeter exit entries
                                    zone.Perimeter.ExitEntries.Add(ee);
                                }
                                else if (word == "end_perimeter")
                                {
                                    // Do Nothing
                                }
                                else
                                {
                                    // create new perimeter point
                                    PerimeterPoint p = new PerimeterPoint();

                                    // set id
                                    p.ID = word;

                                    // create new coordinate
                                    p.position = new UrbanChallenge.Common.Coordinates();

                                    // setX
                                    p.position.X = Double.Parse(parseWord(r, q));

                                    // setY
                                    p.position.Y = Double.Parse(parseWord(r, q));

									// add to perimeter points
									zone.Perimeter.PerimeterPoints.Add(p);
                                }
                            }
                        }
                        else if (word == "spot")
                        {
                            // create a new spot
                            ParkingSpot ps = new ParkingSpot();

                            // set spot id
                            ps.SpotID = parseWord(r, q);

                            while (word != "end_spot")
                            {
                                // get next word
                                word = parseWord(r, q);

                                if (word == "spot_width")
                                {
                                    // set spot width                                    
                                    ps.SpotWidth = parseWord(r, q);
                                }
                                else if (word == "checkpoint")
                                {
                                    // get waypoint id that corresponds with checkpoint
                                    ps.CheckpointWaypointID = parseWord(r, q);

                                    // get checkpoint id
                                    ps.CheckpointID = parseWord(r, q);
                                }
                                else if (word == "end_spot")
                                {
									// add spot to zone
									zone.ParkingSpots.Add(ps);
                                }
                                else
                                {
                                    // SimpleWaypoint 1
                                    #region

                                    // create new waypoint for waypoint1
                                    ps.Waypoint1 = new SimpleWaypoint();
                                    ps.Waypoint1.Position = new UrbanChallenge.Common.Coordinates();

                                    // set id
                                    ps.Waypoint1.ID = word;

                                    // check if id is checkpointWaypointID
                                    if (ps.Waypoint1.ID == ps.CheckpointWaypointID)
                                    {
                                        ps.Waypoint1.IsCheckpoint = true;
										ps.Waypoint1.CheckpointID = ps.CheckpointID;
                                    }

                                    // setX
                                    ps.Waypoint1.Position.X = Double.Parse(parseWord(r, q));

                                    // setY
                                    ps.Waypoint1.Position.Y = Double.Parse(parseWord(r, q));

                                    #endregion

                                    // SimpleWaypoint 2
                                    #region

                                    // create new waypoint for waypoint2
                                    ps.Waypoint2 = new SimpleWaypoint();
                                    ps.Waypoint2.Position = new UrbanChallenge.Common.Coordinates();

                                    // set id
                                    ps.Waypoint2.ID = parseWord(r, q);

                                    // check if id is checkpointWaypointID
                                    if (ps.Waypoint2.ID == ps.CheckpointWaypointID)
                                    {
                                        ps.Waypoint2.IsCheckpoint = true;
										ps.Waypoint2.CheckpointID = ps.CheckpointID;
                                    }

                                    // setX
                                    ps.Waypoint2.Position.X = Double.Parse(parseWord(r, q));

                                    // setY
                                    ps.Waypoint2.Position.Y = Double.Parse(parseWord(r, q));

                                    #endregion
                                }
                            }
                        }
                        else if (word == "end_zone")
                        {
                            // Do Nothing
                        }
                        else
                        {
                            Console.WriteLine("Unrecognized: " + word);
                        }
                    }

					// Add zones to zone
					rndf.Zones.Add(zone);
                }
                else
                {
                    if (word == "end_file")
                        Console.WriteLine("Rndf Parse :: Successful");
                    else
                        Console.WriteLine("Unknown identifier: " + word);
                }
            }
            return rndf;
        }

        // Retreives next actual word in the filestream
        private string parseWord(StreamReader r, Queue q)
        {
            // Word to return as the next word
            string word;

            // If queue length not zero, then simply pull next word from queue
            if (q.Count != 0)
            {
                word = (String)(q.Dequeue());
            }
            // Otherwise split current line into separate words an analyze
            else
            {
                // Holds separate words
                string[] buffer;

                // Define which characters seperate fields
                char[] delimiters = { ' ', '\n', '\r', '\t' };

                // Take in line and split into words
                string line = r.ReadLine();
                buffer = line.Split(delimiters);

                // Make sure we're not inputting empty words before putting them in word queue
                foreach (string s in buffer)
                {
                    if (s != " " && s != "")
                        q.Enqueue(s);
                }

                // If the queue isn't full now, just dequeue the first word
                if (q.Count != 0)
                {
                    word = (String)(q.Dequeue());
                }
                // Otherwise just search the next line
                else
                {
                    word = parseWord(r, q);
                }
            }
            return removeComments(word, r, q);
        }

        // removes comments from the words the parseWord method returns
        public string removeComments(string word, StreamReader r, Queue q)
        {
            // word contains enclosed comment: /*word*/
            if (word.Length >= 4 && word[0] == '/' && word[1] == '*' && 
                word[word.Length - 2] == '*' && word[word.Length - 1] == '/')
            {
                //Console.WriteLine(word);
                return(parseWord(r, q));
            }
            // beginning of word or whole word contains begin-comment
            else if (word.Length >= 2 && word[0] == '/' && word[1] == '*')
            {
                //Console.WriteLine(word);
                string tmp = "";
                while (tmp != "CornellDarpaUrbanChallengeRocksMySocksOff")
                {
                    tmp = parseWord(r, q);
                    //Console.WriteLine(tmp);
                }
                return (parseWord(r, q));
            }
            // end of word or whole word contains end-comment
            else if (word.Length >= 2 && word[word.Length - 2] == '*' && word[word.Length - 1] == '/')
            {
                //Console.WriteLine(word);
                return ("CornellDarpaUrbanChallengeRocksMySocksOff");
            }
            else
            {
                //Console.WriteLine("Returning: " + word);
                return word;
            }
        }
    }
}
