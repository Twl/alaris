﻿using System;
using System.Text;
using Alaris.API.Crypt;
using Alaris.API.Database;

namespace Alaris.Commands
{
    /// <summary>
    /// Advanced Alaris commands
    /// </summary>
    public static class AdvancedCommands
    {
        /// <summary>
        /// Handles admin commands.
        /// </summary>
        /// <param name="mp"></param>
        /// <param name="action"></param>
        /// <param name="user"></param>
        /// <param name="nick"></param>
        /// <param name="host"></param>
        [ParameterizedAlarisCommand("admin", CommandPermission.Admin, 4)]
        public static void HandleAdminCommands(AlarisMainParameter mp, string action, string user, string nick, string host)
        {
            if (string.IsNullOrEmpty(action))
            {
                mp.Bot.SendMsg(mp.Channel, "Sub-commands: list | delete | add");
                return;
            }


            if(action.Equals("add", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(user)
                                                                                 && !string.IsNullOrEmpty(nick)
                                                                                 && !string.IsNullOrEmpty(host))
            {
                DatabaseManager.Query(string.Format("INSERT INTO admins(user,nick,hostname) VALUES('{0}', '{1}', '{2}')", user, nick, host));

                mp.Bot.SendMsg(mp.Channel, string.Format("Admin {0} has been added.", nick));
            }
            else if(action.Equals("list", StringComparison.InvariantCultureIgnoreCase))
            {
                if (AdminManager.GetAdmins() == null)
                    mp.Bot.SendMsg(mp.Channel, "No admins."); // shouldn't happen.
                else
                {
                    foreach (var adm in AdminManager.GetAdmins())
                        mp.Bot.SendMsg(mp.Channel, adm);
                }
            }
            else if(action.Equals("delete", StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(user))
            {
                AdminManager.DeleteAdmin(user);
                mp.Bot.SendMsg(mp.Channel, string.Format("Admin {0} deleted.", user));      
            }
        }

        /// <summary>
        /// Handles AES commands.
        /// </summary>
        /// <param name="mp"></param>
        /// <param name="parameters"></param>
        [ParameterizedAlarisCommand("aes", CommandPermission.Normal, 0, true)]
        public static void HandleAesCommands(AlarisMainParameter mp, params string[] parameters)
        {
            var action = parameters[0];
            var sb = new StringBuilder();
            for(var i = 1; i < parameters.Length; ++i)
                sb.AppendFormat("{0} ", parameters[i]);

            var text = sb.ToString();
            if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(text))
                return;


            if (action.Equals("decrypt", StringComparison.InvariantCultureIgnoreCase))
            {
                mp.Bot.SendMsg(mp.Channel, Rijndael.DecryptString(text));
            }
            else if(action.Equals("encrypt", StringComparison.InvariantCultureIgnoreCase))
                HandleAesEncryptCommand(mp, text);
        }

        /// <summary>
        /// Handles the Rijndael encrypt command.
        /// </summary>
        /// <param name="mp"></param>
        /// <param name="text"></param>
        [AlarisSubCommand("aes encrypt")]
        public static void HandleAesEncryptCommand(AlarisMainParameter mp, string text)
        {
            mp.Bot.SendMsg(mp.Channel, Rijndael.EncryptString(text));   
        }
    }
}