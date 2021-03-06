﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Alaris.Framework.Extensions;
using Alaris.Irc;
using NLog;

namespace Alaris.Framework.Commands
{
    /// <summary>
    /// Class used to manage and load Alaris commands.
    /// </summary>
    public sealed class CommandManager
    {
        #region Properties
        /// <summary>
        /// Prefix of IRC commands.
        /// </summary>
        public string CommandPrefix { get; set; }

        private static readonly object MapLock = new object();

        #endregion

        #region Private Members

        private readonly Dictionary<AlarisCommandWrapper, AlarisMethod> _commandMethodMap = new Dictionary<AlarisCommandWrapper, AlarisMethod>();
        private readonly Dictionary<AlarisCommandWrapper, AlarisMethod> _subCommandMethodMap = new Dictionary<AlarisCommandWrapper, AlarisMethod>();

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion

        #region Command Mappings

        /// <summary>
        /// This methods loads every method marked as a command and maps it to the specified command.
        /// </summary>
        public void CreateMappings()
        {
            _commandMethodMap.Clear();

            var tasm = Assembly.GetExecutingAssembly();

            var asms = AlarisBase.Instance.AddonManager.Assemblies.ToList();
            asms.Add(tasm);
            asms.AddRange(from asm in AppDomain.CurrentDomain.GetAssemblies()
                              where asm.GetName().FullName.ToLower(CultureInfo.InvariantCulture).Contains("alaris")
                              select asm);

            Parallel.ForEach(asms, asm =>
                                       {
                                           var types = asm.GetTypes();

                                           Parallel.ForEach(types, type =>
                                                                       {
                                                                           var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

                                                                           //Utilities.ExecuteSafely(
                                                                           //    () => ProcessMethods(methods));

                                                                           ProcessMethods(methods);
                                                                       });
                                       });


            Log.Info("Created {0} command mapping(s) and {1} sub-command mapping(s)", _commandMethodMap.Count, _subCommandMethodMap.Count);
        }

        /// <summary>
        /// Deletes every loaded mapping.
        /// <para>Used for unloading stuff.</para>
        /// </summary>
        public void DeleteMappings()
        {
            lock(MapLock)
            {
                _commandMethodMap.Clear();
                _subCommandMethodMap.Clear();
            }
        }

        private void ProcessMethods(IEnumerable<MethodInfo> methods)
        {
            Parallel.ForEach(methods, method =>
                                          {
                                              var passEverything = false;
                                              foreach (var attribute in Attribute.GetCustomAttributes(method))
                                              {
                                                  if (attribute.IsOfType(typeof(AlarisCommandAttribute)) ||
                                                      attribute.IsOfType(typeof(ParameterizedAlarisCommand)))
                                                  {
                                                      if (attribute.IsOfType(typeof(ParameterizedAlarisCommand)))
                                                      {
                                                          var patt = attribute.Cast<ParameterizedAlarisCommand>();

                                                          if (patt.IsParameterCountUnspecified)
                                                              passEverything = true;

                                                          else if (method.GetParameters().Length != patt.ParameterCount + 1)
                                                              continue;
                                                      }


                                                      var attr = (AlarisCommandAttribute)attribute;

                                                      lock (MapLock)
                                                      {

                                                          _commandMethodMap.Add(new AlarisCommandWrapper
                                                                                   {
                                                                                       Command = attr.Command,
                                                                                       Permission = attr.Permission,
                                                                                       IsParameterCountUnspecified =
                                                                                           passEverything
                                                                                   },
                                                                               new AlarisMethod(method, attr,
                                                                                                attr.CanBeCastedTo
                                                                                                    <
                                                                                                    ParameterizedAlarisCommand
                                                                                                    >()));
                                                      }

                                                  }
                                                  else if (attribute.IsOfType(typeof(AlarisSubCommandAttribute)) ||
                                                           attribute.IsOfType(typeof(ParameterizedAlarisSubCommand)))
                                                  {
                                                      if (attribute.IsOfType(typeof(ParameterizedAlarisSubCommand)))
                                                      {
                                                          var patt = (ParameterizedAlarisSubCommand)attribute;

                                                          if (patt.IsParameterCountUnspecified)
                                                              passEverything = true;
                                                          else if (method.GetParameters().Length != patt.ParameterCount + 1)
                                                              continue;
                                                      }

                                                      var attr = (AlarisSubCommandAttribute)attribute;

                                                      lock (MapLock)
                                                      {

                                                          _commandMethodMap.Add(new AlarisCommandWrapper
                                                                                   {
                                                                                       Command = attr.Command,
                                                                                       Permission = attr.Permission,
                                                                                       IsParameterCountUnspecified =
                                                                                           passEverything
                                                                                   },
                                                                               new AlarisMethod(method, attr,
                                                                                                attr.CanBeCastedTo
                                                                                                    <
                                                                                                    ParameterizedAlarisSubCommand
                                                                                                    >()));
                                                      }
                                                  }
                                              }
                                          });
        }

        #endregion

        #region Command Handler

        /// <summary>
        /// Handles the command.
        /// </summary>
        /// <param name="user">The irc user</param>
        /// <param name="channel">IRC channel</param>
        /// <param name="message">IRC msg.</param>
        public void HandleCommand(UserInfo user, string channel, string message)
        {
            try
            {
                if (!message.StartsWith(CommandPrefix))
                    return;
                
                var commandText = message.Remove(0, 1); // remove prefix
                Log.Info("Checking for command: {0}", commandText);

                if(_commandMethodMap.Count <= 5)
                    foreach (var entry in _commandMethodMap)
                        Log.Info("Mapping contains command: {0}", entry.Key.Command);


                var wr = commandText.Split(' ');
                var command = wr[0];

                var parameters = new List<string>();

                for (var i = 1; i < wr.Length; ++i)
                {
                    parameters.Add(wr[i]);
                }

                var perm = CommandPermission.Normal;
                AlarisMethod handler = null;
                AlarisCommandWrapper cmd = null;

                foreach (var entry in _commandMethodMap.AsParallel().Where(entry => entry.Key.Command.Equals(command, StringComparison.InvariantCultureIgnoreCase)))
                {
                    perm = entry.Key.Permission;
                    handler = entry.Value;
                    cmd = entry.Key;
                }

                if (handler == null)
                    return;

                if (parameters.Count != 0 && !handler.IsParameterized)
                    return;

                if (perm == CommandPermission.Admin && !Utility.IsAdmin(user))
                    return;

                Log.Info("The handler {0} parameterized. It is about to be called with {1} params",
                         (handler.IsParameterized ? "is" : "is not"), parameters.Count);

                var mp = new AlarisMainParameter
                             {
                                 Channel = channel,
                                 Channels = AlarisBase.Instance.Channels,
                                 IrcConnection = AlarisBase.Instance.Connection,
                                 User = user
                                 
                             };

                var parl = new List<object>();

                if(handler.IsParameterized)
                {
                    parl.Add(mp);

                    if (!cmd.IsParameterCountUnspecified)
                    {
                        parl.AddRange(parameters);
                        var pdiff = handler.Method.GetParameters().Length - (parl.Count + 1);

                        if (pdiff > 0)
                        {
                            for (var i = 0; i <= pdiff; ++i)
                                parl.Add(null);
                        }
                    }

                }
                else
                    parl.Add(mp);

                /*object[] passParams = handler.IsParameterized
                                          ? new object[] {mp, parameters.ToArray()}
                                          : new object[] {mp};*/


                if(handler.IsParameterized && cmd.IsParameterCountUnspecified)
                {
                    var prms = new ArrayList {mp, parameters.ToArray()};
                    Log.Info("Invoking command handler method ({0}) ({1})", handler.Method.Name, prms.Count);
                    handler.Method.Invoke(null, prms.ToArray());
                    return;
                }

                Log.Info("Invoking command handler method ({0}) ({1})", handler.Method.Name, parl.Count);
                
                handler.Method.Invoke(null, parl.ToArray());

            }
            catch(Exception x)
            {
                Log.ErrorException(string.Format("Exception thrown during command recognition ({0})", x), x);
                return;
            }
        }

        #endregion

    }

    ///<summary>
    ///</summary>
    public static class ManagerCommands
    {
        ///<summary>
        ///</summary>
        ///<param name="mp"></param>
        [AlarisCommand("ListCmds")]
        public static void HandleListCmdsCommand(AlarisMainParameter mp)
        {
            
        }
    }
}
