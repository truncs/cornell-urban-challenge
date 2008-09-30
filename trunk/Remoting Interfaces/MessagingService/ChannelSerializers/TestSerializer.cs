using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace UrbanChallenge.MessagingService.ChannelSerializers
{
	/// <summary>
	/// This is a sample implementation of a custom serializer. It actually uses binary serialization as its underlying
	/// serialization, so don't use it for anything real.
	/// 
	/// NB: 
	/// You will need to add your serializer to three places:
	/// 1) The Enum in ChannelSerializerInfo
	/// 2) The Switch statement in PublishUnreliably
	/// 3) The Switch statement in RecieveCallback
	/// </summary>
	public static class TestSerializer
	{
		private static BinaryFormatter bf;
		
		static TestSerializer ()
		{
			bf = new BinaryFormatter();
		}
		public static void Serialize (Stream stream, Object o)
		{			
			bf.Serialize(stream, o);
		}

		public static Object Deserialize(Stream stream)
		{			
			return bf.Deserialize(stream);
		}
	}
}
