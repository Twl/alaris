using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using ICSharpCode.SharpZipLib;
using Mono.Math;
using System.Reflection;
using System.Threading;
using System.Timers;
using Alaris.Irc;
using Alaris.Core;

namespace Alaris.Extras
{
	/// <summary>
	/// The delegate used for <see cref="AlarisBot.ReadConfig"/>
	/// </summary>
	public delegate void ReadConfigDelegate(string configfile);
	
	/// <summary>
	/// A class providing functions to run specific method in an exception-handled environment.
	/// </summary>
	public class CrashHandler
	{
		/// <summary>
		/// Creates a new instance of <see cref="CrashHandler"/>
		/// </summary>
		private CrashHandler()
		{
			
		}
		
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="Alaris.Extras.CrashHandler"/> is reclaimed by garbage collection.
		/// </summary>
		~CrashHandler()
		{
			Log.Debug("CrashHandler", "~CrashHandler()");
		}
		
		/// <summary>
		/// Executes the specified <see cref="ReadConfigDelegate">ReadConfig</see> method in an exception-safe environment.
		/// </summary>
		/// <param name="confread">
		/// The ReadConfig method.
		/// </param>
		/// <param name="param">
		/// The parameter passed to the ReadConfig method.
		/// </param>
		/// <param name="caught">
		/// A list filled which will be filled with the exceptions caught inside the method.
		/// </param>
	
		public void HandleReadConfig(ReadConfigDelegate confread, string param, ref List<Exception> caught)
		{
			bool dolist = false;
			
			if(caught != null)
				dolist = true;
			
			try 
			{
				confread(param);
			}
			catch (FileNotFoundException ex)
			{
				Log.Error("CrashHandler", "FileNotFoundException: " + ex.ToString());
				if(dolist)
					caught.Add(ex);
			}
			catch (ConfigFileInvalidException ex)
			{
				Log.Error("CrashHandler", "ConfigFileInvalidException: " + ex.ToString());
				if(dolist)
					caught.Add(ex);
			}
			catch (Exception x)
			{
				Log.Error("CrashHandler", "Exception: " + x.ToString());
				
				if(dolist)
					caught.Add(x);
			}
		}
	}
}