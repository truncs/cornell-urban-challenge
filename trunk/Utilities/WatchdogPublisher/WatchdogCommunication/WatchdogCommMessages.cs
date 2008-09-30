using System;
using System.Collections.Generic;
using System.Text;

namespace WatchdogCommunication
{
	[Serializable]
	public abstract class WatchdogCommMessage
	{
		public string senderName;
		public abstract string GetName();
	}

	[Serializable]
	public class StartPublishMessage : WatchdogCommMessage
	{
		public StartPublishMessage(string machineName, string publishName)
		{
			this.machineName = machineName; this.publishName = publishName;
		}
		public string machineName;
		public string publishName;
		public override string GetName() { return StartPublishMessage.GetClassName(); }
		public static string GetClassName()
		{
			return "StartPublishMessage";
		}
	}

	[Serializable]
	public class WatchdogTerminateMessage : WatchdogCommMessage
	{
		public WatchdogTerminateMessage()
		{
		}
		public override string GetName() { return WatchdogTerminateMessage.GetClassName(); }
		public static string GetClassName()
		{
			return "WatchdogTerminateMessage";
		}
	}

	[Serializable]
	public class StopPublishMessage : WatchdogCommMessage
	{
		public StopPublishMessage(string machineName, string publishName)
		{
			this.machineName = machineName; this.publishName = publishName;
		}
		public string machineName;
		public string publishName;
		public override string GetName() { return StopPublishMessage.GetClassName(); }
		public static string GetClassName()
		{
			return "StopPublishMessage";
		}
	}

	
	[Serializable]
	public class StartStopPublishMessageReply : WatchdogCommMessage
	{
		public StartStopPublishMessageReply(string machineName, string publishName, bool ok, string status)
		{
			this.machineName = machineName; this.publishName = publishName; this.ok = ok; this.status = status;

		}
		public string machineName;
		public string publishName;
		public bool ok;
		public string status;
		public override string GetName() { return StartStopPublishMessageReply.GetClassName(); }
		public static string GetClassName()
		{
			return "StartStopPublishMessageReply";
		}
	}


	[Serializable]
	public class CommandMessage : WatchdogCommMessage
	{
		public enum WatchdogCommand
		{
			AddServiceRight,
			DisableWatchdogAutoReset,
			EnableWatchdogAutoReset,
			StartRemoteDebugger,			
			RefreshPublishes,
			AutoDetectRunningPublish,
			Quit
		}

		public CommandMessage(WatchdogCommand command, string args, string machine)
		{
			this.command = command; this.args = args; this.machineName = machine;
		}
		[NonSerialized]
		public const string ALLMACHINES = "%%allmachine%%";
		public string machineName;
		public string args;
		public WatchdogCommand command;
		public override string GetName() { return CommandMessage.GetClassName(); }
		public static string GetClassName()
		{
			return "CommandMessage";
		}
	}

	[Serializable]
	public class WatchdogStatusMessage : WatchdogCommMessage
	{
		public enum StatusLevel
		{
			Running,
			Error,
			NotRunning,
			NoConnection
		}
		public WatchdogStatusMessage(string machineName, string curPublishName, string statusText, StatusLevel status, List<string> availablePublishes)
		{
			this.machineName = machineName; this.curPublishName = curPublishName; this.statusText = statusText; this.statusLevel = status; this.availablePublishes = availablePublishes;
		}
		public string machineName;
		public string curPublishName;
		public string statusText;
		public StatusLevel statusLevel;
		public List<string> availablePublishes;
		public override string GetName() { return WatchdogStatusMessage.GetClassName(); }
		public static string GetClassName()
		{
			return "WatchdogStatusMessage";
		}
	}

	public class WatchdogMessageEventArgs<MessageType> : EventArgs where MessageType : WatchdogCommMessage
	{
		public WatchdogMessageEventArgs(MessageType msg)
		{
			this.msg = msg;
		}
		public MessageType msg;
	}

}
