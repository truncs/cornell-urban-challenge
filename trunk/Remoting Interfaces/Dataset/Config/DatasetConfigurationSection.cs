using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Xml;
using System.IO;

namespace Dataset.Config {
	public class DatasetConfigurationSection : ConfigurationSection {
		[ConfigurationProperty("sourceConfig", IsRequired = false)]
		public SourceConfigurationElement SourceConfig {
			get { return (SourceConfigurationElement)this["sourceConfig"]; }
			set { this["sourceConfig"] = value; }
		}

		[ConfigurationProperty("clientConfig", IsRequired = false)]
		public ClientConfigurationElement ClientConfig {
			get { return (ClientConfigurationElement)this["clientConfig"]; }
			set { this["clientConfig"] = value; }
		}

		public static DatasetConfigurationSection Deserialize(string file) {
			using (FileStream fs = new FileStream(file, FileMode.Open)) {
				XmlReader reader = XmlReader.Create(fs);
				DatasetConfigurationSection dsconfig = new DatasetConfigurationSection();
				reader.ReadToFollowing("datasetConfig");
				dsconfig.DeserializeSection(reader.ReadSubtree());
				return dsconfig;
			}
		}
	}

	public class SourceConfigurationElement : ConfigurationElement {
		[ConfigurationProperty("bindTo", IsRequired = false, DefaultValue = "0.0.0.0")]
		[RegexStringValidator(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
		public string BindTo {
			get { return (string)this["bindTo"]; }
			set { this["bindTo"] = value; }
		}

		[ConfigurationProperty("port", DefaultValue = 0, IsRequired = false)]
		[IntegerValidator(MinValue = 0, MaxValue = 65535, ExcludeRange = false)]
		public int Port {
			get { return (int)this["port"]; }
			set { this["port"] = value; }
		}
	}

	public class ClientConfigurationElement : ConfigurationElement {
		[ConfigurationProperty("bindTo", IsRequired = false, DefaultValue = "0.0.0.0")]
		[RegexStringValidator(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
		public string BindTo {
			get { return (string)this["bindTo"]; }
			set { this["bindTo"] = value; }
		}

		[ConfigurationProperty("port", IsRequired = false, DefaultValue = 0)]
		[IntegerValidator(MinValue = 0, MaxValue = 65535, ExcludeRange = false)]
		public int Port {
			get { return (int)this["port"]; }
			set { this["port"] = value; }
		}

		[ConfigurationProperty("useMulticast", IsRequired = false, DefaultValue = false)]
		public bool Multicast {
			get { return (bool)this["useMulticast"]; }
			set { this["useMulticast"] = value; }
		}

		[ConfigurationProperty("multicastAddress", IsRequired = false, DefaultValue = "0.0.0.0")]
		[RegexStringValidator(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
		public string MulticastAddress {
			get { return (string)this["multicastAddress"]; }
			set { this["multicastAddress"] = value; }
		}
	}
}
