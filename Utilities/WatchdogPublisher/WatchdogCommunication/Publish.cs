using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace PublishCommon
{
	public static class PublishManager
	{
		public static void Dummy() { }
		public static PublishSettings publishSettings;
		private static XmlSerializer s = new XmlSerializer(typeof(PublishSettings));
		private const string settingsFile = @"publisher.xml";
		public static void LoadSettings()
		{
			if (File.Exists(settingsFile) == false)
			{
				publishSettings = new PublishSettings();
				publishSettings.publishes = new List<Publish>();
				publishSettings.publishLocation = new List<PublishLocation>();
				return;
			}
			TextReader r = new StreamReader(settingsFile);
			publishSettings = (PublishSettings)s.Deserialize(r);
			r.Close();
		}

		public static void SaveSettings()
		{
			TextWriter w = new StreamWriter(settingsFile);
			s.Serialize(w, publishSettings);
			w.Close();
		}
	}

	[XmlRoot("PublishSettings")]
	[Serializable]
	public class PublishSettings
	{
		[XmlElement("publishes")]
		public List<Publish> publishes;

		[XmlElement("publishLocations")]
		public List<PublishLocation> publishLocation;

		public bool PublishNameExists(string name)
		{
			foreach (Publish p in publishes)
				if (p.name.ToLower() == name.ToLower()) return true;
			return false;
		}
	}

	[XmlRoot("PublishLocation")]
	[Serializable]
	public class PublishLocation
	{
		[XmlAttribute("name")]
		public string name;
		[XmlAttribute("localDrive")]
		public string localDrive;
		[XmlAttribute("remoteShare")]
		public string remoteShare;
		[XmlAttribute("username")]
		public string username="labuser";
		[XmlAttribute("password")]
		public string password="dgcee05";		
	}

	[Serializable]	
	public class Publish
	{
		[XmlAttribute("name")]
		public string name;
		[XmlAttribute("files")]
		public List<string> files;
		[XmlAttribute("relativeLocation")]
		public string relativeLocation;
		[XmlAttribute("lastPublish")]
		public DateTime lastPublish;
		[XmlAttribute("commands")]
		public List<string> commands;
	}
}
