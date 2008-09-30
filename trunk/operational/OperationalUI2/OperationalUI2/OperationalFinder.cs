using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using UrbanChallenge.OperationalUIService;
using System.Runtime.Remoting;
using System.Threading;

namespace UrbanChallenge.OperationalUI {
	class OperationalFinder {
		public enum ResponseStatus {
			Unknown,
			Down,
			Alive
		}

		public struct OperationalStatus {
			public string name;
			public ResponseStatus responseStatus;

			public OperationalStatus(string name, ResponseStatus status) {
				this.name = name;
				this.responseStatus = status;
			}
		}

		private class OperationalInstance {
			private MethodInvoker pingInvoker;
			public ResponseStatus responseStatus;
			public string name;
			private string uri;
			private DateTime lastPingTime;
			private OperationalFinder parent;

			public OperationalInstance(string name, OperationalFinder parent) {
				this.name = name;
				this.responseStatus = ResponseStatus.Unknown;
				this.parent = parent;

				StartPing();
			}

			public bool IsSameUri() {
				try {
					OperationalUIFacade facade = (OperationalUIFacade)OperationalInterface.ObjectDirectory.Resolve(name);
					return uri == RemotingServices.GetObjectUri(facade);
				}
				catch (Exception) {
					responseStatus = ResponseStatus.Down;
					return false;
				}
			}

			// return true if the uri is different
			public bool StartPing() {
				lastPingTime = DateTime.Now;
				bool isNewUri = false;
				try {
					OperationalUIFacade facade = (OperationalUIFacade)OperationalInterface.ObjectDirectory.Resolve(name);
					string newUri = RemotingServices.GetObjectUri(facade);
					if (newUri != uri) {
						isNewUri = true;
						uri = newUri;
						if (responseStatus != ResponseStatus.Unknown) {
							responseStatus = ResponseStatus.Unknown;
							parent.OnPingCompleted(this);
						}
					}

					pingInvoker = facade.Ping;
					ThreadPool.QueueUserWorkItem(DoPing, pingInvoker);
				}
				catch (Exception) {
					if (responseStatus != ResponseStatus.Down) {
						responseStatus = ResponseStatus.Down;
						parent.OnPingCompleted(this);
					}
				}

				return isNewUri;
			}

			private void DoPing(object shits) {
				MethodInvoker target = (MethodInvoker)shits;
				if (target == pingInvoker) {
					try {
						target();

						if (target == pingInvoker) {
							if (responseStatus != ResponseStatus.Alive) {
								responseStatus = ResponseStatus.Alive;
								parent.OnPingCompleted(this);
							}
						}
					}
					catch (Exception) {
						if (target == pingInvoker) {
							if (responseStatus != ResponseStatus.Down) {
								responseStatus = ResponseStatus.Down;
								parent.OnPingCompleted(this);
							}
						}
					}
				}
			}
		}

		public event EventHandler StatusChanged;

		private List<OperationalInstance> instances;

		public OperationalFinder() {
			instances = new List<OperationalInstance>();
		}

		public void RefreshInstances(bool forcePing) {
			ICollection<string> names = null;

			names = OperationalInterface.ObjectDirectory.GetNames();

			bool didChange = false;

			foreach (string name in names) {
				if (name.StartsWith(OperationalUIFacade.ServiceName)) {
					OperationalInstance instance = instances.Find(delegate(OperationalInstance inst) {
						return inst.name == name;
					});

					if (instance != null) {
						try {
							if (!instance.IsSameUri()) {
								instance.responseStatus = ResponseStatus.Unknown;
								instance.StartPing();
								didChange = true;
							}
							else if (forcePing) {
								instance.StartPing();
							}
						}
						catch (Exception) {
						}
					}
					else {
						// new item
						didChange = true;
						instance = new OperationalInstance(name, this);
						instance.StartPing();
						instances.Add(instance);
					}
				}
			}

			if (didChange && StatusChanged != null) {
				StatusChanged(this, EventArgs.Empty);
			}
		}

		private void OnPingCompleted(OperationalInstance instance) {
			if (StatusChanged != null) {
				StatusChanged(this, EventArgs.Empty);
			}
		}

		public ICollection<OperationalStatus> GetStatus() {
			List<OperationalStatus> statusList = new List<OperationalStatus>();
			foreach (OperationalInstance op in instances) {
				statusList.Add(new OperationalStatus(op.name, op.responseStatus));
			}

			return statusList;
		}
	}
}
