using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace UrbanChallenge.Common.RndfNetwork
{
	public class MdfHandler
	{
		private Mdf mdf;

		/// <summary>
		/// Default constructor
		/// </summary>
		public MdfHandler()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="mdf">Network object to handle</param>
		public MdfHandler(Mdf mdf)
		{
			this.mdf = mdf;
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
				bf.Serialize(str, mdf);
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
		public Mdf Load(string filename)
		{
			Mdf s = new Mdf();

			try
			{
				FileStream str = File.OpenRead(filename);
				BinaryFormatter bf = new BinaryFormatter();
				s = (Mdf)bf.Deserialize(str);
				str.Close();
			}
			catch (SerializationException e)
			{
				Console.WriteLine("");
				Console.WriteLine("LOAD ERROR");
				Console.WriteLine("Serialization Error: " + e.ToString());
			}

			mdf = s;
			return s;
		}

		/// <summary>
		/// Serializes the Mdf object from constructor and returns as a byte array
		/// </summary>
		/// <returns></returns>
		public byte[] Serialize()
		{
			byte[] b = { };

			try
			{
				MemoryStream str = new MemoryStream();
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(str, mdf);
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
		/// De-Serialize this byte-string and cast as a Mdf object
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public Mdf DeSerialize(byte[] b)
		{
			Mdf s = new Mdf();

			try
			{
				MemoryStream str = new MemoryStream(b);
				BinaryFormatter bf = new BinaryFormatter();
				s = (Mdf)bf.Deserialize(str);
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
