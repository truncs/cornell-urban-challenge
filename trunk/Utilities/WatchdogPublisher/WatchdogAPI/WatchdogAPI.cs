using System;
using System.Collections.Generic;
using System.Text;
using WatchdogCommunication;
using System.Threading;
using System.Diagnostics;

namespace WatchdogAPI
{
	public static class WatchdogCommAPI
	{
		static string SceneEstimatorMachineName = "skynet4";
		static string LocalMapMachineName = "skynet3";
		static string SceneEstimatorPublishName = "Scene Estimator 2";
		static string LocalMapPublishName = "Local Map 2";

		static bool isLocalMapRunning = false;
		static bool isSceneEstimatorRunning = false;

		static WatchdogComm comm = new WatchdogComm();

		public static void Initialize() {
			try {
				comm.Init();
			}
			catch {
				Console.WriteLine("WATCHDOG COMM ERROR: No Network Detected!");
			}
			comm.GotWatchdogStatusMessage += new EventHandler<WatchdogMessageEventArgs<WatchdogStatusMessage>>(comm_GotWatchdogStatusMessage); 
		}

		static void comm_GotWatchdogStatusMessage(object sender, WatchdogMessageEventArgs<WatchdogStatusMessage> e)
		{
			if (e.msg.curPublishName.ToLower() == LocalMapPublishName)
			{
				isLocalMapRunning = (e.msg.statusLevel == WatchdogStatusMessage.StatusLevel.Running);
			}

			if (e.msg.curPublishName.ToLower() == SceneEstimatorPublishName)
			{
				isSceneEstimatorRunning = (e.msg.statusLevel == WatchdogStatusMessage.StatusLevel.Running);
			}
		}

		public static bool RestartSkeetEstimator()
		{
			Stopwatch sw = new Stopwatch();
			isSceneEstimatorRunning = true;
			comm.SendMessage(new StopPublishMessage(SceneEstimatorMachineName , SceneEstimatorPublishName));
			
			sw.Start();
			while (isSceneEstimatorRunning == true && sw.Elapsed < TimeSpan.FromSeconds (5))
				Thread.Sleep(1);
			if (sw.Elapsed > TimeSpan.FromSeconds(5))
			{
				Console.WriteLine("Timed out waiting for Scene Estimator to stop!");				
			}
			sw.Stop();
			sw.Reset();
			comm.SendMessage(new StartPublishMessage(SceneEstimatorMachineName, SceneEstimatorPublishName));
			sw.Start();
			while (isSceneEstimatorRunning == false && sw.Elapsed < TimeSpan.FromSeconds(5))
				Thread.Sleep(1);
			if (sw.Elapsed > TimeSpan.FromSeconds(5))
			{
				Console.WriteLine("Timed out waiting for Scene Estimator to start!");
				return false;
			}
			Console.WriteLine("Succesfully Restarted Scene Estimator");
			return true;
		}

		public static bool RestartLocalMap()
		{
			Stopwatch sw = new Stopwatch();
			isLocalMapRunning = true;
			comm.SendMessage(new StopPublishMessage(LocalMapMachineName ,LocalMapPublishName));

			sw.Start();
			while (isLocalMapRunning == true && sw.Elapsed < TimeSpan.FromSeconds(5))
				Thread.Sleep(1);
			if (sw.Elapsed > TimeSpan.FromSeconds(5))
			{
				Console.WriteLine("Timed out waiting for Local Map to stop!");
			}
			Console.WriteLine("Stopped Local map OK");
			sw.Stop();
			sw.Reset();
			comm.SendMessage(new StartPublishMessage(LocalMapMachineName, LocalMapPublishName));
			sw.Start();
			while (isLocalMapRunning == false && sw.Elapsed < TimeSpan.FromSeconds(5))
				Thread.Sleep(1);
			if (sw.Elapsed > TimeSpan.FromSeconds(5))
			{
				Console.WriteLine("Timed out waiting for LocalMap to start!");
				return false;
			}
			Console.WriteLine("Succesfully Restarted Local Map");
			return true;
		}
	}
}
