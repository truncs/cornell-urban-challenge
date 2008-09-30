using System;
using System.Collections.Generic;
using System.Text;
using UrbanChallenge.Common.EarthModel;
using UrbanChallenge.Common;
using System.IO;

namespace RndfToolkit
{
	/// <summary>
	/// Defines a GPS Coordinate
	/// </summary>
	public struct GpsCoordinate
	{
		public double latitude;
		public double longitude;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="latitude"></param>
		/// <param name="longitude"></param>
		public GpsCoordinate(double latitude, double longitude)
		{
			this.latitude = latitude;
			this.longitude = longitude;
		}
	}

	/// <summary>
	/// Functions to help work with points in Lat/Lon Format
	/// </summary>
	public static class GpsTools
	{
		/// <summary>
		/// Constructs a planar projection based upon an origin latitude and longitude
		/// </summary>
		/// <param name="latitude"></param>
		/// <param name="longitude"></param>
		/// <returns></returns>
		public static PlanarProjection PlanarProjection(GpsCoordinate origin, bool isDegrees)
		{
			if (isDegrees)
			{
				// transform
				LLACoord llaOrigin = DegreesToLLA(origin);

				// returns a new planar projection based on origin
				return new PlanarProjection(llaOrigin.lat, llaOrigin.lon);
			}

			// returns a new planar projection based on origin
			return new PlanarProjection(origin.latitude, origin.longitude);
		}

		/// <summary>
		/// Calculates the optimal origin of a planar projection from a list of gps coordinates
		/// </summary>
		/// <param name="gpsCoordinates"></param>
		/// <returns></returns>
		/// <remarks>Takes mean of all coordiantes</remarks>
		public static GpsCoordinate CalculateOrigin(List<GpsCoordinate> gpsCoordinates)
		{
			double meanLatitude = 0.0;
			double meanLongitude = 0.0;

			// loop through coordinates
			foreach (GpsCoordinate gpsCoordinate in gpsCoordinates)
			{
				meanLatitude += gpsCoordinate.latitude;
				meanLongitude += gpsCoordinate.longitude;
			}

			// average
			meanLatitude = meanLatitude / ((double)gpsCoordinates.Count);
			meanLongitude = meanLongitude / ((double)gpsCoordinates.Count);

			// create and return coordinate
			return new GpsCoordinate(meanLatitude, meanLongitude);
		}

		/// <summary>
		/// Transoforms list of gps coordinates to xy coordinates
		/// </summary>
		/// <param name="gpsCoordinates"></param>
		/// <param name="projection"></param>
		/// <returns></returns>
		public static List<Coordinates> TransformToXy(List<GpsCoordinate> gpsCoordinates, PlanarProjection projection)
		{
			// final list of referenced coordinates
			List<Coordinates> xyCoordinates = new List<Coordinates>();

			// loop through coordinates
			foreach (GpsCoordinate gpsCooridnate in gpsCoordinates)
			{
				// generate xy coordinate from projection
				xyCoordinates.Add(projection.ECEFtoXY(WGS84.LLAtoECEF(new LLACoord(gpsCooridnate.latitude, gpsCooridnate.longitude, 0))));
			}

			// return final list
			return xyCoordinates;
		}

		/// <summary>
		/// Transoforms list of gps coordinates to xy coordinates
		/// </summary>
		/// <param name="gpsCoordinates"></param>
		/// <param name="projection"></param>
		/// <returns></returns>
		public static Coordinates TransformToXy(GpsCoordinate gpsCoordinate, PlanarProjection projection)
		{
			// generate xy coordinate from projection
			return projection.ECEFtoXY(WGS84.LLAtoECEF(new LLACoord(gpsCoordinate.latitude, gpsCoordinate.longitude, 0)));
		}

		/// <summary>
		/// Transforms a pose log into xy coordinates
		/// </summary>
		/// <param name="stream">filestream attached to the log</param>
		/// <param name="firstColumnTimeStamp">true if the first column of data contains a timestamp</param>
		/// <param name="ecef">if the data is in ecef format (usually true)</param>
		/// <param name="projection">projection we are using for the rest of the data</param>
		/// <returns></returns>
		public static List<Coordinates> TransformPoseLogToXy(FileStream dataStream, bool firstColumnTimeStamp, bool ecef, PlanarProjection projection)
		{
			// read in the line and split on the commas
			TextReader reader = new StreamReader(dataStream);

			// initialize list of final xy points
			List<Coordinates> xyCoordinates = new List<Coordinates>();

			// read data
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				int offset = 0;
				if (firstColumnTimeStamp)
					offset = 1;

				// split the line
				string[] parts = line.Split(',');
				double x = double.Parse(parts[0 + offset]);
				double y = double.Parse(parts[1 + offset]);
				double z = double.Parse(parts[2 + offset]);

				// perform the projection
				Coordinates xy = projection.ECEFtoXY(new Vector3(x, y, z));

				// add to final list of coordinates
				xyCoordinates.Add(xy);
			}

			// close reader
			reader.Dispose();
			reader.Close();

			// retrun list of coorinates
			return xyCoordinates;
		}

		/// <summary>
		/// Transforms a gps coordinate in degrees to an lla coordinate in radians
		/// </summary>
		/// <param name="gpsCoordinate"></param>
		/// <returns></returns>
		public static LLACoord DegreesToLLA(GpsCoordinate gpsCoordinate)
		{
			return new LLACoord(gpsCoordinate.latitude * Math.PI / 180.0, gpsCoordinate.longitude * Math.PI / 180.0, 0);
		}

		/// <summary>
		/// Transforms a coordinate in degrees to an lla coordinate in radians
		/// </summary>
		/// <param name="coordinate"></param>
		/// <returns></returns>
		public static LLACoord DegreesToLLA(Coordinates coordinate)
		{
			return new LLACoord(coordinate.X * Math.PI / 180.0, coordinate.Y * Math.PI / 180.0, 0);
		}

		/// <summary>
		/// LLA Coord in rad
		/// </summary>
		/// <param name="coordinate"></param>
		/// <param name="proj"></param>
		/// <returns></returns>
		public static LLACoord XyToLlaRadians(Coordinates coordinate, PlanarProjection proj)
		{
			// lla coord
			LLACoord llaRad = WGS84.ECEFtoLLA(proj.XYtoECEF(coordinate));

			// return 
			return llaRad;
		}

		/// <summary>
		/// LLA coord in degrees
		/// </summary>
		/// <param name="coordiante"></param>
		/// <param name="proj"></param>
		/// <returns></returns>
		public static LLACoord XyToLlaDegrees(Coordinates coordiante, PlanarProjection proj)
		{
			// radians
			LLACoord llaRad = XyToLlaRadians(coordiante, proj);

			// degrees
			LLACoord lla = new LLACoord(llaRad.lat * 180.0 / Math.PI, llaRad.lon * 180.0 / Math.PI, 0);

			// return
			return lla;
		}

		/// <summary>
		/// Returns a string representing the lla coord in arc minutes and seconds
		/// </summary>
		/// <param name="lla"></param>
		/// <returns></returns>
		public static string LlaDegreesToArcMinSecs(LLACoord lla)
		{
			double latDegFrac = lla.lat % 1;
			double latDeg = lla.lat - latDegFrac;
			double latMinFull = latDegFrac * 60.0;
			double latSecondsInMins = latMinFull % 1;
			double latMin = latMinFull - latSecondsInMins;
			double latSeconds = latSecondsInMins * 60.0;
			string nString = latDeg.ToString() + " " + latMin.ToString() + "' " + latSeconds.ToString("F6") + "\" N";

			double lonDegFrac = -lla.lon % 1;
			double lonDeg = -lla.lon - lonDegFrac;
			double lonMinFull = lonDegFrac * 60.0;
			double lonSecondsInMins = lonMinFull % 1;
			double lonMin = lonMinFull - lonSecondsInMins;
			double lonSeconds = lonSecondsInMins * 60.0;
			string eString = lonDeg.ToString() + " " + lonMin.ToString() + "' " + lonSeconds.ToString("F6") + "\" W";

			return nString + "\n" + eString;			
		}
	}
}

