﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Alaris.API;
using Alaris.Irc;
using NLog;


namespace Alaris
{
    /// <summary>
    /// Class used to manage (load, unload, reload) plugins dynamically.
    /// </summary>
    public static class AddonManager
    {
        /// <summary>
        /// The IRC connection.
        /// </summary>
        public static Connection Connection { get; private set; }
        /// <summary>
        /// IRC channel list.
        /// </summary>
        public static List<string> Channels { get; private set; }

        private static readonly List<IAlarisAddon> Plugins = new List<IAlarisAddon>();

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes the Plugin manager.
        /// </summary>
        /// <param name="con">IRC connection</param>
        /// <param name="channels">IRC channel list</param>
        public static void Initialize(ref Connection con, List<string> channels) 
        {
            Connection = con;
            Channels = channels;
            SetupAppDomainDebugHandlers();
        }

        /// <summary>
        /// Loads plugins from the specified directory.
        /// </summary>
        /// <param name="directory">The directory to check in</param>
        public static void LoadPluginsFromDirectory(DirectoryInfo directory) { LoadPluginsFromDirectory(directory.FullName);}


        /// <summary>
        /// Loads plugins from the specified directory.
        /// </summary>
        /// <param name="directory">The directory to check in</param>
        public static void LoadPluginsFromDirectory(string directory)
        {
            var dir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, directory));

            foreach(var dll in dir.GetFiles("*.dll").AsParallel())
            {
                var asm = Assembly.LoadFrom(dll.FullName);
                

                if(asm == null)
                    continue;

                var pl = Enumerable.OfType<IAlarisAddon>(asm.GetTypes().AsParallel()).FirstOrDefault();

                if (pl == null)
                    break; // not a plugin


                var connection = Connection;

                pl.Setup(ref connection, Channels);

                Plugins.Add(pl);
            }
        }

        /// <summary>
        /// Unloads the specified plugin.
        /// </summary>
        /// <param name="name">Name of the plugin.</param>
        public static void UnloadPlugin(string name)
        {
            foreach (var pl in Plugins.Where(pl => pl.Name == name))
            {
                pl.Destroy();
                Plugins.Remove(pl);
            }

            
        }

        private static void SetupAppDomainDebugHandlers()
        {
            AppDomain.CurrentDomain.DomainUnload +=
                (sender, args) => Log.Debug("PluginManager: AppDomain::DomainUnload, hash: {0}",
                                            AppDomain.CurrentDomain.GetHashCode());

            AppDomain.CurrentDomain.AssemblyLoad +=
                (sender, ea) =>
                Log.Debug("PluginManager: AppDomain::AssemblyLoad, sender is: {0}, loaded assembly: {1}."
                                        , sender.GetHashCode()
                                        , ea.LoadedAssembly.FullName);

           

            AppDomain.CurrentDomain.AssemblyResolve +=
                (sender, eargs) =>
                    {
                        Log.Debug("PluginManager: AppDomain::AssemblyResolve, sender: {0}, name: {1}, asm: {2}", 
                            sender.GetHashCode(), eargs.Name, eargs.RequestingAssembly.FullName );

                        return null;
                    };

        }
    }
}