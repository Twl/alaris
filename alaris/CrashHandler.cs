using System;
using System.IO;
using Alaris.API;
using Alaris.Exceptions;
using Alaris.Irc;

namespace Alaris
{
    /// <summary>
    ///   The delegate used for <see cref = "AlarisBot.ReadConfig" />
    /// </summary>
    public delegate void ReadConfigDelegate(string configfile);

    /// <summary>
    ///   A class providing functions to run specific method in an exception-handled environment.
    /// </summary>
    public class CrashHandler : IAlarisComponent
    {
        private readonly Guid _guid;
        /// <summary>
        ///   Creates a new instance of <see cref = "CrashHandler" />
        /// </summary>
        private CrashHandler()
        {
            _guid = Guid.NewGuid();
        }

        /// <summary>
        ///   Releases unmanaged resources and performs other cleanup operations before the
        ///   <see cref = "CrashHandler" /> is reclaimed by garbage collection.
        /// </summary>
        ~CrashHandler()
        {
            Log.Debug("CrashHandler", "~CrashHandler()");
        }


        /// <summary>
        /// Gets the current instance's GUID.
        /// </summary>
        /// <returns>The GUID</returns>
        public Guid GetGuid()
        {
            return _guid;
        }

        /// <summary>
        ///   Executes the specified <see cref = "ReadConfigDelegate">ReadConfig</see> method in an exception-safe environment.
        /// </summary>
        /// <param name = "confread">
        ///   The ReadConfig method.
        /// </param>
        /// <param name = "param">
        ///   The parameter passed to the ReadConfig method.
        /// </param>
        public static void HandleReadConfig(ReadConfigDelegate confread, string param)
        {
            try
            {
                confread(param);
            }
            catch (FileNotFoundException ex)
            {
                Log.Error("CrashHandler", "FileNotFoundException: " + ex);
            }
            catch (ConfigFileInvalidException ex)
            {
                Log.Error("CrashHandler", "ConfigFileInvalidException: " + ex);
            }
            catch (Exception x)
            {
                Log.Error("CrashHandler", "Exception: " + x);
            }
        }
    }
}