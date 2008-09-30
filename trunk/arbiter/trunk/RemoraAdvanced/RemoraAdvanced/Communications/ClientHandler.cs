using System;
using System.Collections.Generic;
using System.Text;
using RemoraAdvanced.Common;
using System.Windows.Forms;

namespace RemoraAdvanced.Communications
{
	/// <summary>
	/// Available Clients
	/// </summary>
	public class ClientHandler
	{
		/// <summary>
		/// Available clients
		/// </summary>
		public Dictionary<string, int> AvailableClients = new Dictionary<string, int>();

		/// <summary>
		/// Viewable clients
		/// </summary>
		public List<ListViewItem> ViewableClients
		{
			get
			{
				List<ListViewItem> items = new List<ListViewItem>();
				foreach (KeyValuePair<string, int> kvp in AvailableClients)
				{
					items.Add(new ListViewItem(new string[] { kvp.Value.ToString(), kvp.Key}));
				}
				return items;
			}
		}

		/// <summary>
		/// Current client
		/// </summary>
		public string Current = "";
		
		/// <summary>
		/// Remove client
		/// </summary>
		/// <param name="s"></param>
		public void Remove(string s)
		{
			if (this.AvailableClients.ContainsKey(s))
				AvailableClients.Remove(s);

			if (Current == s)
			{
				this.Current = "";
				RemoraCommon.Communicator.Shutdown();
			}
		}

		/// <summary>
		/// Set machine we are connecting to
		/// </summary>
		/// <param name="s"></param>
		public void SetMachine(string s)
		{
			this.Current = s;
		}
	}
}
