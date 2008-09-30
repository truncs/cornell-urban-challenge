using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace UrbanChallenge.Common.RndfNetwork
{
	/// <summary>
	/// Handles the RndfNetwork
	/// </summary>
	public class RndfNetworkHandler
	{	
		private RndfNetwork rndfNetwork;

		/// <summary>
		/// Default constructor
		/// </summary>
		public RndfNetworkHandler()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rndfNetwork">Network object to handle</param>
		public RndfNetworkHandler(RndfNetwork rndfNetwork)
		{
			this.rndfNetwork = rndfNetwork;
		}

		/// <summary>
		/// Serialize and save to a file
		/// </summary>
		/// <param name="filename"></param>
		public void Save(string filename)
		{
			try
			{
				FileStream str = File.Create(filename);
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(str, rndfNetwork);
				str.Flush();
				str.Close();
				str.Dispose();
			}
			catch (SerializationException e)
			{
				Console.WriteLine("");
				Console.WriteLine("SAVE ERROR");
				Console.WriteLine("Serialization Error: " + e.ToString());
			}
		}

		/// <summary>
		/// Deserialize the roadNetwork from a file
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public RndfNetwork Load(string filename)
		{
			RndfNetwork s = new RndfNetwork();

			try
			{
				FileStream str = File.OpenRead(filename);
				BinaryFormatter bf = new BinaryFormatter();
				s = (RndfNetwork)bf.Deserialize(str);
				str.Close();
			}
			catch (SerializationException e)
			{
				Console.WriteLine("");
				Console.WriteLine("LOAD ERROR");
				Console.WriteLine("Serialization Error: " + e.ToString());
			}

			rndfNetwork = s;
			return s;
		}

		/// <summary>
		/// Serializes the RndfNetwork object from constructor and returns as a byte array
		/// </summary>
		/// <returns></returns>
		public byte[] Serialize()
		{
			byte[] b = { };

			try
			{
				MemoryStream str = new MemoryStream();
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(str, rndfNetwork);
				b = str.ToArray();
				str.Close();
				return b;
			}
			catch (SerializationException e)
			{
				Console.WriteLine("");
				Console.WriteLine("");
				Console.WriteLine("Serialization Error: " + e.ToString());
			}

			return b;
		}

		/// <summary>
		/// De-Serialize this byte-string and cast as a RndfNetwork object
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public RndfNetwork DeSerialize(byte[] b)
		{
			RndfNetwork s = new RndfNetwork();

			try
			{
				MemoryStream str = new MemoryStream(b);
				BinaryFormatter bf = new BinaryFormatter();
				s = (RndfNetwork)bf.Deserialize(str);
				str.Close();
			}
			catch (SerializationException e)
			{
				Console.WriteLine("");
				Console.WriteLine("LOAD ERROR");
				Console.WriteLine("Serialization Error: " + e.ToString());
			}

			return s;
		}
	}	
}
