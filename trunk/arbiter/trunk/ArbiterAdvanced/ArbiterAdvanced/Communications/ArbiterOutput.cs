using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UrbanChallenge.Arbiter.Core.CoreIntelligence;
using System.Runtime.Remoting.Messaging;

namespace UrbanChallenge.Arbiter.Core.Communications
{
	/// <summary>
	/// Output functions to help split between logging and console writing
	/// </summary>
	[Serializable]
	public static class ArbiterOutput
	{
		/// <summary>
		/// Whether logging is on for default output
		/// </summary>
		public static bool DefaultOutputLoggingEnabled = true;

		/// <summary>
		/// Whether default output is pushed over messaging channels
		/// </summary>
		public static bool DefaultOutputMessagingEnabled = true;

		/// <summary>
		/// File stream for the log
		/// </summary>
		private static FileStream logStream;

		/// <summary>
		/// The actual log writer
		/// </summary>
		private static StreamWriter logWriter;

		/// <summary>
		/// Generate the log
		/// </summary>
		public static void BeginLog()
		{
			// log name
			string s = "ArbiterLog "
				+ DateTime.Now.Month.ToString() + "-"
				+ DateTime.Now.Day.ToString() + "-"
				+ DateTime.Now.Year.ToString() + " "
				+ DateTime.Now.Hour.ToString() + "-"
				+ DateTime.Now.Minute.ToString() + "-"
				+ DateTime.Now.Second.ToString()
				+ ".txt";

			// create file
			ArbiterOutput.logStream = new FileStream(s, FileMode.Create);

			// create writer
			ArbiterOutput.logWriter = new StreamWriter(ArbiterOutput.logStream);

			// write general information
			ArbiterOutput.logWriter.WriteLine("Arbiter Log Created: " + DateTime.Now.ToString());
			ArbiterOutput.logWriter.WriteLine("");
		}

		/// <summary>
		/// Output to console as well as log file
		/// </summary>
		/// <param name="s">String to output</param>
		public static void Output(string s)
		{
			try
			{
				if (DefaultOutputLoggingEnabled)
				{
					// write to log
					ArbiterOutput.logWriter.WriteLine(DateTime.Now.ToString() + ": " + s);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error writing to log: \n" + e.ToString());
			}

			// write to console
			Console.WriteLine(s);

			try
			{
				if (DefaultOutputMessagingEnabled)
				{
					if (CoreCommon.Communications != null)
					{					
						// output
						CoreCommon.Communications.SendOutput(s);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error outputting to arbiter message channel: \n" + e.ToString());
			}			
		}

		public static void OutputNoLog(string s)
		{
			// write to console
			Console.WriteLine(s);

			try
			{
				if (DefaultOutputMessagingEnabled)
				{
					if (CoreCommon.Communications != null)
					{
						// output
						CoreCommon.Communications.SendOutput(s);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error outputting to arbiter message channel: \n" + e.ToString());
			}			
		}

		/// <summary>
		/// Output error to console as well as log file
		/// </summary>
		/// <param name="s">String to output</param>
		/// <param name="e">Exception to output</param>
		public static void Output(string s, Exception e)
		{
			s = "\n" + s + ": \n" + e.ToString() + "\n\n";

			try
			{
				// write to log
				ArbiterOutput.WriteToLog(DateTime.Now.ToString() + ": " + s);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error writing to log: \n" + ex.ToString());
			}

			// write to console
			Console.WriteLine(s);

			try
			{
				if (CoreCommon.Communications != null)
				{
					// output
					CoreCommon.Communications.SendOutput(s);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error outputting to arbiter message channel: \n" + ex.ToString());
			}
		}

		/// <summary>
		/// Write string only to log
		/// </summary>
		/// <param name="s">String to output</param>
		[OneWay]
		public static void WriteToLog(string s)
		{
			try
			{
				if (DefaultOutputLoggingEnabled)
				{
					if (logWriter != null)
					{
						// write to log
						ArbiterOutput.logWriter.WriteLine(DateTime.Now.ToString() + ": " + s);
					}
				}
			}
			catch (Exception e)
			{
				Output("Error writing to log: \n" + e.ToString());
				DefaultOutputLoggingEnabled = false;
				Output("Error writing to log: \n" + e.ToString());
				Output("Error Writing to LOG, disabling logging");				
			}
		}

		/// <summary>
		/// Write string only to console
		/// </summary>
		/// <param name="s">String to Output</param>
		public static void WriteToConsole(string s)
		{
			Console.WriteLine(s);
		}

		/// <summary>
		/// Closes the output
		/// </summary>
		public static void ShutDownOutput()
		{
			lock (logWriter)
			{
				// write final
				ArbiterOutput.logWriter.Dispose();
				logWriter = null;

				// release holds
				ArbiterOutput.logStream.Close();
			}
		}
	}
}
