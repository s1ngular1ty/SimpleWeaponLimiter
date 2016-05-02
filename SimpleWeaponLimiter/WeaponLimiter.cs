/* WeaponLimiter

    By S1ngular1ty

*/

using PRoCon.Core;
using PRoCon.Core.Maps;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Plugin;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;

namespace PRoConEvents
{
    using CapturableEvent = PRoCon.Core.Events.CapturableEvents;
    //Aliases
    using EventType = PRoCon.Core.Events.EventType;

    public class WeaponLimiter : PRoConPluginAPI, IPRoConPluginInterface
    {
        bool pluginEnabled = false;
        bool proconChatAllMsgs = false;
        bool disableGameMsgs = false;
        bool disableKillPunishments = false;
        bool disableKickBan = false;
        bool testing = false;
        bool whiteListAdmins = false;
        bool enableWhiteList = false;
        int debugLevel = 1;
        int tempBanDuration = 60;       // minutes 

        string PluginVersion = "1.0.0.0";
        // Command matching patterns
        static string InGameCommand_Pattern = @"^\s*([@!?]\S+)";
        static string Prefix_Pattern = @"^\s*([@!?])\s*";

        List<string> allowedWeaponCodes = new List<string>();
        List<string> allowedCategoryCodes = new List<string>();
        List<string> allowedCategories = new List<string>();
        List<string> disallowedCategories = new List<string>();
        List<string> whitelistAllowedWeapons = new List<string>();
        List<string> whitelistDisallowedWeapons = new List<string>();
        List<string> blacklistAllowedWeapons = new List<string>();
        List<string> blacklistDisallowedWeapons = new List<string>();
        List<string> wasPunishKicked = new List<string>();
        List<string> whiteList = new List<string>();
        // List allowable commands in lowercase
        static List<string> CommandList = new List<string> { "checkaccount", "testyell", "testplayeryell", "decrpunish", "incrpunish", "setpunish", "clearpunish",
                                                             "checkpunish"};


        Dictionary<string, PlayerInfo> PlayerList = new Dictionary<string, PlayerInfo>();
        Dictionary<string, DamageTypes> WeaponsDict = null;

        RestrictionMode restrictionMode = RestrictionMode.WhiteList;
        public enum RestrictionMode { WhiteList, BlackList };
        public enum MessageType { Warning, Error, Exception, Normal };
        public enum BanType { GUID, Name };
        public enum Color { Black, Maroon, Green, Orange, DarkBlue, LightBlue, Violet, Pink, Red, Gray };
        public enum Format { Bold, Normal, Italicized };

        Object PListBaton = new Object();
        ServerInfo server;

        public WeaponLimiter()
        {
            // Populate default WhiteList entries
            whiteList.Add("S1ngular1ty");
            whiteList.Add("Phogue");

            allowedCategories.Add("Melee");
            allowedCategories.Add("Handgun");

            disallowedCategories.Add("SniperRifle");
            disallowedCategories.Add("Shotgun");

            whitelistAllowedWeapons.Add("U_BallisticShield");
            whitelistAllowedWeapons.Add("dlSHTR");

            whitelistDisallowedWeapons.Add("U_SerbuShorty");
            whitelistDisallowedWeapons.Add("U_Glock18");
            whitelistDisallowedWeapons.Add("U_M93R");

            blacklistAllowedWeapons.Add("U_SerbuShorty");

            blacklistDisallowedWeapons.Add("U_XM25");
            blacklistDisallowedWeapons.Add("UCAV");

            // Allowed weapons codes to prevent unnecessary punishments
            allowedWeaponCodes.Add("SoldierCollision");
            allowedWeaponCodes.Add("Death");
            allowedWeaponCodes.Add("Suicide");
            allowedWeaponCodes.Add("RoadKill");

            // Allowed weapons categories to prevent unnecessary punishments
            allowedCategoryCodes.Add("Death");
            allowedCategoryCodes.Add("Suicide");
            allowedCategoryCodes.Add("VehicleWater");
            allowedCategoryCodes.Add("VehicleAir");
            allowedCategoryCodes.Add("VehicleStationary");
            allowedCategoryCodes.Add("VehiclePersonal");
            allowedCategoryCodes.Add("VehicleTransport");
            allowedCategoryCodes.Add("VehicleLight");
            allowedCategoryCodes.Add("VehicleHeavy");
            allowedCategoryCodes.Add("None");
        }

        #region Command Handling

        public void CommandHandler(string speaker, string message)
        {

            if (IsCommand(message) && IsAllowableCommand(ExtractCommandString(message)))
            {
                List<string> commandArgs = new List<string>();

                string commandString = ExtractCommandString(message);
                commandArgs = ExtractCommandArgs(commandString);
                string commandPrefix = ExtractCommandPrefix(message);
                string command = commandArgs[0];
                string target = "";
                int duration = 0;
                string commandText = "";

                if (commandArgs.Count > 1)
                    target = commandArgs[1];

                switch (command)
                {
                    case "checkaccount":
                        if (commandPrefix != "?")
                            OnCommandCheckAccount(speaker, target, commandPrefix);
                        else
                            SendPlayerMessage("", speaker);
                        break;
                    case "decrpunish":
                        if (commandPrefix != "?")
                            OnCommandDecrPunish(speaker, target, commandPrefix);
                        else
                            SendPlayerMessage("", speaker);
                        break;
                    case "incrpunish":
                        if (commandPrefix != "?")
                            OnCommandIncrPunish(speaker, target, commandPrefix);
                        else
                            SendPlayerMessage("", speaker);
                        break;
                    case "setpunish":
                        if (commandPrefix != "?")
                        {
                            string value = commandArgs.Count > 2 ? commandArgs[2] : "0";
                            OnCommandSetPunish(speaker, target, value, commandPrefix);
                        }
                        else
                            SendPlayerMessage("", speaker);
                        break;
                    case "clearpunish":
                        if (commandPrefix != "?")
                            OnCommandClearPunish(speaker, target, commandPrefix);
                        else
                            SendPlayerMessage("", speaker);
                        break;
                    case "checkpunish":
                        if (commandPrefix != "?")
                            OnCommandCheckPunish(speaker, target, commandPrefix);
                        else
                            SendPlayerMessage("", speaker);
                        break;
                    default:
                        break;
                }
            }

        }
        public void OnCommandCheckAccount(string speaker, string targetPlayer, string responseScope)
        {
            string player = "";

            if (targetPlayer == "")
                player = speaker;
            else
                player = MatchPlayer(targetPlayer);

            if (CheckAccount(GetPByName(player)))
                SendMessage(player + " is an admin.", responseScope, speaker);
            else
                SendMessage(player + " is NOT and admin.", responseScope, speaker);

        }
        public void OnCommandDecrPunish(string speaker, string targetPlayer, string responseScope)
        {
            if (CheckAccount(GetPByName(speaker)))
            {
                string player = MatchPlayer(targetPlayer);

                if (!PListContainsPlayer(player))
                {
                    SendMessage("Could not find player " + targetPlayer, responseScope, speaker);
                    return;
                }

                PlayerInfo Player = GetPByName(player);

                if (Player.Punishments > 0)
                {
                    SendMessage(Player.Name + " punishment: " + Player.Punishments + " -> " + (Player.Punishments - 1), responseScope, speaker);
                    Player.Punishments--;
                }
                else
                    SendMessage(Player.Name + " doesn't have any punishments.", responseScope, speaker);
            }
        }
        public void OnCommandIncrPunish(string speaker, string targetPlayer, string responseScope)
        {
            if (CheckAccount(GetPByName(speaker)))
            {
                string player = MatchPlayer(targetPlayer);

                if (!PListContainsPlayer(player))
                {
                    SendMessage("Could not find player " + targetPlayer, responseScope, speaker);
                    return;
                }

                PlayerInfo Player = GetPByName(player);

                if (Player.Punishments < 2)
                {
                    SendMessage(Player.Name + " punishment: " + Player.Punishments + " -> " + (Player.Punishments + 1), responseScope, speaker);
                    Player.Punishments++;
                }
                else if (Player.Punishments >= 2)
                    SendMessage("Punishments for " + Player.Name + " = " + Player.Punishments + " Can't manually set higher than 2.", responseScope, speaker);
            }
        }
        public void OnCommandSetPunish(string speaker, string targetPlayer, string value, string responseScope)
        {
            if (CheckAccount(GetPByName(speaker)))
            {
                string player = MatchPlayer(targetPlayer);
                int Value;

                if (!int.TryParse(value, out Value))
                {
                    SendMessage("Value not recognized.  Please try again.", responseScope, speaker);
                    return;
                }

                if (!PListContainsPlayer(player))
                {
                    SendMessage("Could not find player " + targetPlayer, responseScope, speaker);
                    return;
                }

                PlayerInfo Player = GetPByName(player);

                if (Value <= 2 && Value >= 0)
                {
                    SendMessage(Player.Name + " punishment: " + Player.Punishments + " -> " + Value, responseScope, speaker);
                    Player.Punishments = Value;
                }
                else if (Value > 2)
                    SendMessage("Can't manually set higher than 2.", responseScope, speaker);
                else if (Value < 0)
                    SendMessage("Can't set value less than 0.", responseScope, speaker);
            }
        }
        public void OnCommandClearPunish(string speaker, string targetPlayer, string responseScope)
        {

            if (CheckAccount(GetPByName(speaker)))
            {
                string player = MatchPlayer(targetPlayer);

                if (!PListContainsPlayer(player))
                {
                    SendMessage("Could not find player " + targetPlayer, responseScope, speaker);
                    return;
                }

                PlayerInfo Player = GetPByName(player);

                SendMessage(Player.Name + " punishment: " + Player.Punishments + " -> " + 0, responseScope, speaker);
                Player.Punishments = 0;
                if (wasPunishKicked.Contains(Player.Name))
                    wasPunishKicked.Remove(Player.Name);
            }
        }
        public void OnCommandCheckPunish(string speaker, string targetPlayer, string responseScope)
        {
            if (CheckAccount(GetPByName(speaker)))
            {
                string player = MatchPlayer(targetPlayer);

                if (!PListContainsPlayer(player))
                {
                    SendMessage("Could not find player " + targetPlayer, responseScope, speaker);
                    return;
                }

                PlayerInfo Player = GetPByName(player);

                SendMessage(Player.Name + " punishments = " + Player.Punishments, responseScope, speaker);
            }
        }
        public static bool IsCommand(string text)
        {
            bool match = Regex.Match(text, InGameCommand_Pattern).Success;

            return match;
        }
        public static bool IsAllowableCommand(string text)
        {
            bool isAllowableCommand;

            string commandString = ExtractCommandString(text);
            string command = ExtractCommandArgs(commandString)[0].ToLower();

            if (CommandList.Contains(command))
                isAllowableCommand = true;
            else
                isAllowableCommand = false;

            return isAllowableCommand;
        }
        public static string ExtractCommandString(string text)
        {
            return Regex.Replace(text, Prefix_Pattern, "").Trim();
        }
        public static string ExtractCommandPrefix(string text)
        {
            Match match = Regex.Match(text, Prefix_Pattern, RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups[1].Value;

            return string.Empty;
        }
        public static List<string> ExtractCommandArgs(string commandString)
        {
            MatchCollection matches = Regex.Matches(commandString, @"(\S+)");

            List<string> results = new List<string>();

            foreach (Match match in matches)
            {
                results.Add(match.Value);
            }

            return results;
        }
        public static string ExtractCommandText(List<string> commandArgs, int startIndex)
        {
            string commandText = "";

            if (commandArgs.Count > startIndex)
            {
                StringBuilder text = new StringBuilder();

                for (int i = startIndex; i < commandArgs.Count; i++)
                {
                    text.Append(commandArgs[i] + " ");
                }

                commandText = text.ToString();
                text = null;
            }

            return commandText;
        }

        #endregion

        #region Communication Methods

        public string FormatText(string msg, Color color, Format format)
        {
            string MSG = "";
            switch (color)
            {
                case Color.Black:
                    MSG = "^0" + msg + "^n";
                    break;
                case Color.Maroon:
                    MSG = "^1" + msg + "^n";
                    break;
                case Color.Green:
                    MSG = "^2" + msg + "^n";
                    break;
                case Color.Orange:
                    MSG = "^3" + msg + "^n";
                    break;
                case Color.DarkBlue:
                    MSG = "^4" + msg + "^n";
                    break;
                case Color.LightBlue:
                    MSG = "^5" + msg + "^n";
                    break;
                case Color.Violet:
                    MSG = "^6" + msg + "^n";
                    break;
                case Color.Pink:
                    MSG = "^7" + msg + "^n";
                    break;
                case Color.Red:
                    MSG = "^8" + msg + "^n";
                    break;
                case Color.Gray:
                    MSG = "^9" + msg + "^n";
                    break;
                default:
                    break;
            }

            switch (format)
            {
                case Format.Bold:
                    MSG = "^b" + MSG;
                    break;
                case Format.Normal:
                    break;
                case Format.Italicized:
                    MSG = "^i" + MSG;
                    break;
                default:
                    break;
            }

            return MSG;
        }

        public string FormatMessage(string msg, MessageType type)
        {
            String prefix = "[^bWeaponLimiter!^n] ";

            if (type.Equals(MessageType.Warning))
                prefix += FormatText("WARNING", Color.Orange, Format.Bold);
            else if (type.Equals(MessageType.Error))
                prefix += FormatText("ERROR", Color.Maroon, Format.Bold);
            else if (type.Equals(MessageType.Exception))
                prefix += FormatText("EXCEPTION", Color.Red, Format.Bold);

            return prefix + msg;
        }

        public void ChatWrite(string msg)
        {
            this.ExecuteCommand("procon.protected.chat.write", msg);
        }

        public void LogWrite(string msg)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
        }

        public void ConsoleWrite(string msg, MessageType type)
        {
            LogWrite(FormatMessage(msg, type));
        }

        public void ConsoleWrite(string msg)
        {
            ConsoleWrite(msg, MessageType.Normal);
        }

        public void ConsoleWarn(string msg)
        {
            ConsoleWrite(msg, MessageType.Warning);
        }

        public void ConsoleError(string msg)
        {
            ConsoleWrite(msg, MessageType.Error);
        }

        public void ConsoleException(string msg)
        {
            ConsoleWrite(msg, MessageType.Exception);
        }

        public void DebugWrite(int level, string msg)
        {
            if (debugLevel >= level) ConsoleWrite(msg, MessageType.Normal);
        }

        public void SendGlobalMessage(string message)
        {
            if (!disableGameMsgs)
            {
                ServerCommand("admin.say", message, "all");

                if (proconChatAllMsgs)
                    ChatWrite(message);
            }
            else if (disableGameMsgs)
                ChatWrite("NOT SENDING: (Global Message) " + message);
        }

        public void SendPlayerMessage(string message, string player)
        {
            if (!disableGameMsgs)
            {
                ServerCommand("admin.say", player + ": " + message, "player", player);

                if (proconChatAllMsgs)
                    ChatWrite("(PlayerSay: " + player + ") " + message);
            }
            else if (disableGameMsgs)
                ChatWrite("NOT SENDING: (PlayerSay: " + player + ") " + message);
        }

        public void SendMessage(string message, string responseScope, string speaker)
        {
            if (!disableGameMsgs)
            {
                if (responseScope == "!")
                    SendGlobalMessage(message);
                else if (responseScope == "@")
                    SendPlayerMessage(message, speaker);

                if (proconChatAllMsgs)
                    ChatWrite(message);
            }
            else if (disableGameMsgs)
            {
                if (responseScope == "!")
                    ChatWrite("NOT SENDING " + message);
                else if (responseScope == "@")
                    ChatWrite("NOT SENDING: (PlayerSay: " + speaker + ") " + message);
            }
        }

        public void SendAdminMessage(string message)
        {
            if (!disableGameMsgs)
            {
                List<PlayerInfo> tempList = PlayerList.Values.ToList();

                foreach (PlayerInfo Player in tempList)
                {
                    if (CheckAccount(Player))
                    {
                        if (PListContainsPlayer(Player.Name))
                        {
                            ServerCommand("admin.say", "Admins: " + message, "player", Player.Name);
                            ChatWrite("(ADMINS) " + message);
                        }
                    }
                }
            }
            else if (disableGameMsgs)
                ChatWrite("NOT SENDING: (ADMINS) " + message);
        }

        public void SendYell(string message, int duration)
        {
            if (!disableGameMsgs)
            {
                ServerCommand("admin.yell", message, duration.ToString(), "all");

                if (proconChatAllMsgs)
                    ChatWrite("(Yell) " + message);
            }
            else if (disableGameMsgs)
                ChatWrite("NOT SENDING: (Yell) " + message);
        }

        public void SendPlayerYell(string message, string player, int duration)
        {
            if (!disableGameMsgs)
            {
                ServerCommand("admin.yell", message, duration.ToString(), "player", player);
                if (proconChatAllMsgs)
                    ChatWrite("(PlayerYell: " + player + ") " + message);
            }
            else if (disableGameMsgs)
                ChatWrite("NOT SENDING: (PlayerYell: " + player + ") " + message);

        }

        public void ServerCommand(params string[] args)
        {
            List<string> list = new List<string>();
            list.Add("procon.protected.send");
            list.AddRange(args);
            this.ExecuteCommand(list.ToArray());
        }

        #endregion

        #region Main Plugin Methods   

        public void SyncPlayers(List<CPlayerInfo> players)
        {
            // Temporary player lists
            List<string> playersToAdd = new List<string>();
            List<string> playersToRemove = new List<string>();
            List<PlayerInfo> playersToUpdate = new List<PlayerInfo>();

            // Find player names in PlayerList that aren't in players so they can be removed
            lock (PListBaton)
                playersToRemove = PlayerList.Keys.Except(players.Select(x => x.SoldierName)).ToList();

            // Find players in "players" list that need to be added to plugin PlayerList
            lock (PListBaton)
                playersToAdd = players.Select(x => x.SoldierName).Except(PlayerList.Keys).ToList();

            // Remove players from PlayerList who are no longer on server
            foreach (string name in playersToRemove)
            {
                PlayerInfo player = new PlayerInfo();

                if (PListContainsPlayer(name))
                {
                    lock (PListBaton)
                    {
                        player = PlayerList[name];
                        PlayerList.Remove(name);
                        DebugWrite(3, FormatText("Removed " + name + " from player list.  No longer on server.", Color.Violet, Format.Bold));
                    }
                }
            }

            // Add new players to PlayerList and queue
            foreach (string name in playersToAdd)
            {
                if (!PListContainsPlayer(name))
                {
                    CPlayerInfo player = new CPlayerInfo();
                    player = players.Where(x => x.SoldierName == name).Select(x => x).FirstOrDefault();
                    PlayerInfo newPlayer = new PlayerInfo(player);

                    lock (PListBaton)
                        PlayerList.Add(name, newPlayer);

                    DebugWrite(3, "Added player " + newPlayer.Name + " to the player list.");
                }
            }

            // Update soldier CPlayerInfo if in PlayerList already
            List<PlayerInfo> playersInListAlready = new List<PlayerInfo>();

            foreach (KeyValuePair<string, PlayerInfo> kvp in PlayerList)
            {
                foreach (CPlayerInfo p in players)
                {
                    if (p.SoldierName == kvp.Value.Name)
                    {
                        kvp.Value.info = p;
                        kvp.Value.IsAdmin = CheckAccount(kvp.Value);
                        kvp.Value.IsInWhitelist = whiteList.Contains(kvp.Value.Name);
                        DebugWrite(5, kvp.Value.Name + " admin status: " + kvp.Value.IsAdmin);
                    }

                    if (!playersInListAlready.Contains(kvp.Value))
                        playersInListAlready.Add(kvp.Value);
                }
            }
        }

        public void InitWeapons()
        {
            // initialize values for all known weapons

            WeaponDictionary dic = GetWeaponDefines();
            WeaponsDict = new Dictionary<string, DamageTypes>();
            foreach (Weapon weapon in dic)
                if (weapon != null && !WeaponsDict.ContainsKey(weapon.Name))
                    WeaponsDict.Add(weapon.Name, weapon.Damage);
        }

        private string GetWeaponCategory(Kill info)
        {
            DamageTypes category = DamageTypes.None;

            if (info == null || String.IsNullOrEmpty(info.DamageType))
                return "None";

            if (!WeaponsDict.TryGetValue(info.DamageType, out category))
            {
                category = DamageTypes.None;
            }

            return category.ToString();
        }

        public KillReason FriendlyWeaponName(string killWeapon)
        {
            KillReason r = new KillReason();
            r.name = killWeapon;
            DamageTypes category = DamageTypes.None;
            bool hasCategory = false;

            if (WeaponsDict.TryGetValue(killWeapon, out category))
            {
                hasCategory = true;
            }

            if (killWeapon.StartsWith("U_")) // BF4 weapons
            {
                String[] tParts = killWeapon.Split(new[] { '_' });

                if (tParts.Length == 2)
                { // U_Name
                    r.name = tParts[1];
                }
                else if (tParts.Length == 3)
                { // U_Name_Detail
                    r.name = tParts[1];
                    r.detail = tParts[2];
                }
                else if (tParts.Length >= 4)
                { // U_AttachedTo_Name_Detail
                    r.name = tParts[2];
                    r.detail = tParts[3];
                    r.attachedTo = tParts[1];
                }
                else
                {
                    DebugWrite(1, "Warning: unrecognized weapon code: " + killWeapon);
                }
            }
            else if (killWeapon == "dlSHTR")
            {
                r.name = "Phantom Bow";
            }
            else if (killWeapon != "Death" && hasCategory) // BF4 vehicles?
            {
                if (category == DamageTypes.VehicleAir
                || category == DamageTypes.VehicleHeavy
                || category == DamageTypes.VehicleLight
                || category == DamageTypes.VehiclePersonal
                || category == DamageTypes.VehicleStationary
                || category == DamageTypes.VehicleTransport
                || category == DamageTypes.VehicleWater)
                {
                    r.name = "Death";
                    r.vName = killWeapon;
                    Match m = Regex.Match(killWeapon, @"/([^/]+)/([^/]+)$");
                    if (m.Success)
                    {
                        r.vName = m.Groups[1].Value;
                        r.vDetail = m.Groups[2].Value;
                    }

                    // Clean-up heuristics
                    String vn = r.vName;
                    if (vn.StartsWith("CH_"))
                        vn = vn.Replace("CH_", String.Empty);
                    else if (vn.StartsWith("Ch_"))
                        vn = vn.Replace("Ch_", String.Empty);
                    else if (vn.StartsWith("RU_"))
                        vn = vn.Replace("RU_", String.Empty);
                    else if (vn.StartsWith("US_"))
                        vn = vn.Replace("US_", String.Empty);

                    if (vn == "spec" && r.vDetail != null)
                    {
                        if (r.vDetail.Contains("Z-11w"))
                            vn = "Z-11w";
                        else if (r.vDetail.Contains("DV15"))
                            vn = "DV15";
                        else vn = r.vDetail;
                    }

                    if (vn.StartsWith("FAC_"))
                        vn = vn.Replace("FAC_", "Boat ");
                    else if (vn.StartsWith("FAC-"))
                        vn = vn.Replace("FAC-", "Boat ");
                    else if (vn.StartsWith("JET_"))
                        vn = vn.Replace("JET_", "Jet ");
                    else if (vn.StartsWith("FJET_"))
                        vn = vn.Replace("FJET_", "Jet ");

                    if (vn == "LAV25" && r.vDetail != null)
                    {
                        if (r.vDetail == "LAV_AD")
                        {
                            vn = "AA LAV_AD";
                        }
                        else
                        {
                            vn = "IFV LAV25";
                        }
                    }

                    switch (vn)
                    {
                        case "9K22_Tunguska_M": vn = "AA Tunguska"; break;
                        case "AC130": vn = "AC130 Gunship"; break;
                        case "AH1Z": vn = "Chopper AH1Z Viper"; break;
                        case "AH6": vn = "Chopper AH6 Littlebird"; break;
                        case "BTR-90": vn = "IFV BTR-90"; break;
                        case "F35": vn = "Jet F35"; break;
                        case "HIMARS": vn = "Artillery Truck M142 HIMARS"; break;
                        case "M1A2": vn = "MBT M1A2"; break;
                        case "Mi28": vn = "Chopper Mi28 Havoc"; break;
                        case "SU-25TM": vn = "Jet SU-25TM"; break;
                        case "Venom": vn = "Chopper Venom"; break;
                        case "Z-11w": vn = "Chopper Z-11w"; break;
                        case "KLR650": vn = "Bike KLR650"; break;
                        case "DPV": vn = "Jeep DPV"; break;
                        case "LTHE_Z-9": vn = "Chopper Z-9"; break;
                        case "FAV_LYT2021": vn = "Jeep LYT2021"; break;
                        case "GrowlerITV": vn = "Jeep Growler ITV"; break;
                        case "Ka-60": vn = "Chopper Ka-60"; break;
                        case "VDV Buggy": vn = "Jeep VDV Buggy"; break;
                        case "T90": vn = "MBT T90"; break;
                        case "A-10_THUNDERBOLT": vn = "Jet A-10 Thunderbolt"; break;
                        case "B1Lancer": vn = "Jet B1 Lancer"; break;
                        case "H6K": vn = "Jet H6K"; break;
                        case "Z-10w": vn = "Chopper Z-10w"; break;
                        case "RHIB": vn = "Boat RHIB"; break;
                        default: break;
                    }

                    r.vName = vn.Replace('_', ' ');
                }
            }
            return r;
        }

        public void ProcessKill(Kill killInfo)
        {            
            KillInfo kill = new KillInfo(killInfo, GetWeaponCategory(killInfo));
            bool killAllowed = true;
            PlayerInfo killer = GetPByName(kill.kill.Killer.SoldierName);
            PlayerInfo victim = GetPByName(kill.kill.Victim.SoldierName);

            // Check if player suicided or was killed by an unregulated weapon category or weapon then allow the kill.
            // These would include vehicles, death, soldiercollision, etc.
            if (killer == victim || allowedCategoryCodes.Contains(kill.Category) || allowedWeaponCodes.Contains(kill.Weapon))
                killAllowed = true;
            else
            {
                // Check whether plugin is in WhiteList or BlackList mode
                if (restrictionMode == RestrictionMode.WhiteList)
                {
                    // If in WhiteList mode check the kill category to see if it is white listed
                    if (!allowedCategories.Contains(kill.Category))
                        killAllowed = false;

                    // Check weapons that are specifically allowed or disallowed in the settings
                    if (whitelistDisallowedWeapons.Contains(kill.Weapon))
                        killAllowed = false;
                    if (whitelistAllowedWeapons.Contains(kill.Weapon))
                        killAllowed = true;
                }
                else if (restrictionMode == RestrictionMode.BlackList)
                {
                    // If in BlackList mode check the kill category to see if it is black listed
                    if (disallowedCategories.Contains(kill.Category))
                        killAllowed = false;

                    // Check weapons that are specifically allowed or disallowed in the settings
                    if (blacklistDisallowedWeapons.Contains(kill.Weapon))
                        killAllowed = false;
                    if (blacklistAllowedWeapons.Contains(kill.Weapon))
                        killAllowed = true;
                }
            }

            if (whiteListAdmins && killer.IsAdmin)
            {
                DebugWrite(1, killer.Name + " is an admin and admin whitelist is on.");
                killAllowed = true;
            }
            if (enableWhiteList && killer.IsInWhitelist)
            {
                DebugWrite(1, killer.Name + " is in the whitelist and the whitelist is enabled.");
                killAllowed = true;
            }

            DebugWrite(4, "Killinfo damage type: " + killInfo.DamageType);

            if (!killAllowed)
            {
                if (!disableKillPunishments)
                    PunishPlayer(killer, kill);

                string weapon = FriendlyWeaponName(kill.Weapon).Name;
                DebugWrite(1, "^8NOT ALLOWED -- " + killer.Name + " [Weapon: " + weapon + " - Category: " + kill.Category + "] " + victim.Name);
            }
            else
            {
                string weapon = FriendlyWeaponName(kill.Weapon).Name;
                DebugWrite(1, "^4ALLOWED -- " + killer.Name + " [Weapon: " + weapon + " - Category: " + kill.Category + "] " + victim.Name);
            }
        }

        public void PunishPlayer(PlayerInfo Player, KillInfo kill)
        {
            Player.Punishments++;

            string wp = FriendlyWeaponName(kill.Weapon).Name;
            string p = Player.Name;
            string v = kill.kill.Victim.SoldierName;
            double count = Player.Punishments;
            bool wasTempBanned = false;

            Dictionary<int, string> playerMsgs = new Dictionary<int, string>();
            Dictionary<int, string> victimMsgs = new Dictionary<int, string>();
            Dictionary<int, string> globalMsgs = new Dictionary<int, string>();

            playerMsgs.Add(1, String.Format("{0}, {1} is not allowed in this server! Use an allowed weapon.", p, wp));
            playerMsgs.Add(2, String.Format("{0}, {1} is not allowed in this server! Use an allowed weapon. This is your last warning.", p, wp));
            playerMsgs.Add(4, String.Format("{0} BANNED for repeatedly violating rules.", p));

            victimMsgs.Add(1, String.Format("{0} - {1} was KILLED for killing you with a weapon ({2}) that is not allowed.", v, p, wp));
            victimMsgs.Add(2, String.Format("{0} - {1} was KILLED for killing you with a weapon ({2}) that is not allowed.", v, p, wp));
            victimMsgs.Add(3, String.Format("{0} - {1} was KILLED for killing you with a weapon ({2}) that is not allowed.", v, p, wp));
            victimMsgs.Add(4, String.Format("{0} - {1} was BANNED for killing you with a weapon ({2}) that is not allowed.", v, p, wp));

            globalMsgs.Add(1, String.Format("{0} KILLED for using {1} - weapon not allowed!", p, wp));
            globalMsgs.Add(2, String.Format("{0} KILLED for using {1} - weapon not allowed!", p, wp));
            globalMsgs.Add(3, String.Format("{0} KICKED for using {1} - weapon not allowed!", p, wp));
            globalMsgs.Add(4, String.Format("{0} BANNED for using {1} - weapon not allowed!", p, wp));

            string kickMsg = String.Format("{0}, {1} is not allowed in this server! Use an allowed weapon. Next time you will be TEMP BANNED.", p, wp);
            string banMsg = String.Format("Disallowed weapon, {0} - TEMP BANNED for 60 minutes.", p);
            string proconMsg = String.Format("{0} was warned for using {1} - not allowed", p, wp);

            bool wasKicked = false;
            wasKicked = wasPunishKicked.Contains(Player.Name);

            if (wasKicked)
            {
                TempBanPlayer(Player, tempBanDuration, banMsg, BanType.Name);
                SendYell(globalMsgs[4], 10);
                SendPlayerMessage(victimMsgs[4], v);
                SendGlobalMessage(playerMsgs[4]);
                SendYell(playerMsgs[4], 10);
                ChatWrite(playerMsgs[4]);
                wasPunishKicked.Remove(Player.Name);
            }
            else
            {
                if (count == 1)
                {
                    KillPlayer(p);
                    SendYell(globalMsgs[1], 10);
                    SendPlayerMessage(victimMsgs[1], v);
                    SendPlayerYell(victimMsgs[1], v, 10);
                    SendPlayerMessage(playerMsgs[1], p);
                    SendPlayerYell(playerMsgs[1], p, 10);
                    ChatWrite(proconMsg);
                }
                if (count == 2)
                {
                    KillPlayer(p);
                    SendYell(globalMsgs[2], 10);
                    SendPlayerMessage(victimMsgs[2], v);
                    SendPlayerYell(victimMsgs[2], v, 10);
                    SendPlayerMessage(playerMsgs[2], p);
                    SendPlayerYell(playerMsgs[2], p, 10);
                    ChatWrite(proconMsg);
                }
                if (count == 3)
                {
                    KickPlayer(p, kickMsg);
                    SendYell(globalMsgs[3], 10);
                    SendPlayerMessage(victimMsgs[3], v);
                    ChatWrite(proconMsg);
                    wasPunishKicked.Add(Player.Name);
                    Player.Punishments = 0;
                }
            }
        }

        public void ClearRoundData()
        {
            wasPunishKicked.Clear();

            foreach (KeyValuePair<string, PlayerInfo> kvp in PlayerList)
            {
                kvp.Value.Punishments = 0;
            }
        }

        #endregion

        #region Helper Methods

        public List<string> GetPlayerNames()
        {
            List<string> playerNames = new List<string>();

            foreach (string key in PlayerList.Keys)
            {
                playerNames.Add(key);
            }

            return playerNames;
        }
        public PlayerInfo GetPByName(string name)
        {
            return PlayerList[name];
        }
        public bool PListContainsPlayer(string name)
        {
            return PlayerList.ContainsKey(name);
        }
        public bool CheckAccount(PlayerInfo Player)
        {
            bool ret = false;
            string name = Player.Name;

            try
            {
                if (!PListContainsPlayer(name))
                {
                    DebugWrite(2, "^1WARNING: Unable to CheckAccount for " + name + ": unrecognized name");
                    ret = false;
                }
                CPrivileges p = this.GetAccountPrivileges(name);
                if (p == null)
                    ret = false;
                else
                {
                    Player.CanKill = p.CanKillPlayers;
                    Player.CanKick = p.CanKickPlayers;
                    Player.CanBan = (p.CanTemporaryBanPlayers || p.CanPermanentlyBanPlayers);
                    Player.CanMove = p.CanMovePlayers;
                    Player.CanChangeLevel = p.CanUseMapFunctions;
                    Player.IsAdmin = Player.CanKill || Player.CanKick || Player.CanMove || Player.CanBan || Player.CanChangeLevel;

                    if (Player.IsAdmin) ret = true;
                }
            }
            catch (Exception e)
            {
                ConsoleException("EXCEPTION: CheckAccount(" + name + "): " + e.Message);
                ret = false;
            }
            return ret;
        }
        public string MatchPlayer(string targetPlayer)
        {
            List<string> search_list = new List<string>();

            foreach (string key in PlayerList.Keys)
            {
                search_list.Add(PlayerList[key].Name);
            }

            int best_distance = 0;
            string player = BestMatch(targetPlayer, search_list, out best_distance);

            return player;
        }

        #region Fuzzy Matching

        // modified algorithm to ignore insertions, and case
        public string BestMatch(string name, List<string> names, out int best_distance)
        {
            best_distance = int.MaxValue;

            //do the obvious check first
            if (names.Contains(name))
            {
                best_distance = 0;
                return name;
            }

            //name is not in the list, find the best match
            string best_match = null;

            // first try to see if any of the names contains target name as substring, so we can reduce the search
            Dictionary<string, string> sub_names = new Dictionary<string, string>();

            string name_lower = name.ToLower();

            for (int i = 0; i < names.Count; i++)
            {
                string cname = names[i].ToLower();
                if (cname.Equals(name_lower))
                    return names[i];
                else if (cname.Contains(name_lower) && !sub_names.ContainsKey(cname))
                    sub_names.Add(cname, names[i]);
            }

            if (sub_names.Count > 0)
                names = new List<string>(sub_names.Keys);

            if (sub_names.Count == 1)
            {
                // we can optimize, and exit early
                best_match = sub_names[names[0]];
                best_distance = Math.Abs(best_match.Length - name.Length);
                return best_match;
            }


            // find the best/fuzzy match using modified Leveshtein algorithm              
            foreach (string cname in names)
            {
                int distance = LevenshteinDistance(name, cname);
                if (distance < best_distance)
                {
                    best_distance = distance;
                    best_match = cname;
                }
            }


            if (best_match == null)
                return null;

            best_distance += Math.Abs(name.Length - best_match.Length);

            // if we searched through sub-names, get the actual match
            if (sub_names.Count > 0 && sub_names.ContainsKey(best_match))
                best_match = sub_names[best_match];

            return best_match;
        }
        public int LevenshteinDistance(string s, string t)
        {
            s = s.ToLower();
            t = t.ToLower();

            int n = s.Length;
            int m = t.Length;

            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;

            if (m == 0)
                return n;

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 0), d[i - 1, j - 1] + ((t[j - 1] == s[i - 1]) ? 0 : 1));

            return d[n, m];
        }

        #endregion

        #endregion

        #region Punishment Methods

        public void KillPlayer(string playerName)
        {
            if (!disableKickBan)
                ServerCommand("admin.killPlayer", playerName);
            else
                ConsoleWarn("^bNot killing " + playerName + " because kills, kicks, and bans are disabled.^n");
        }

        public void KillPlayer(string playerName, int delay)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Thread.Sleep(delay);
                KillPlayer(playerName);
            });
        }

        public void KickPlayer(string playerName, string reason)
        {
            if (!disableKickBan)
                ServerCommand("admin.kickPlayer", playerName, reason);
            else
                ConsoleWarn("^bNot kicking " + playerName + " because kills, kicks, and bans are disabled.^n");
        }

        public void KickPlayer(string playerName, string reason, int delay)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Thread.Sleep(delay);
                KickPlayer(playerName, reason);
            });
        }

        public void BanPlayer(PlayerInfo Player, string reason, BanType type)
        {
            if (!disableKickBan)
            {
                if (type == BanType.GUID)
                {
                    if (Player.EAGuid != null)
                    {
                        ServerCommand("banList.add", "guid", Player.EAGuid, "perm", reason);
                        ServerCommand("banList.save");
                        ServerCommand("banList.list");
                        DebugWrite(2, Player.Name + " was banned for reason: " + reason);
                    }
                    else
                        DebugWrite(2, "Players EAGUID is null.  Could not BAN.");
                }
                else if (type == BanType.Name)
                {
                    ServerCommand("banList.add", "name", Player.Name, "perm", reason);
                    ServerCommand("banList.save");
                    ServerCommand("banList.list");
                    DebugWrite(2, Player.Name + " was banned for reason: " + reason);
                }
            }
            else
                ConsoleWarn("^bNot banning " + Player.Name + " because kills, kicks, and bans are disabled.^n");
        }

        public void BanPlayer(PlayerInfo Player, string reason, BanType type, int delay)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Thread.Sleep(delay);
                BanPlayer(Player, reason, type);
            });
        }

        public void TempBanPlayer(PlayerInfo Player, int duration, string reason, BanType type)
        {
            if (!disableKickBan)
            {
                if (type == BanType.GUID)
                {
                    if (Player.EAGuid != null)
                    {
                        ServerCommand("banList.add", "guid", Player.EAGuid, "seconds", (duration * 60).ToString(), reason);
                        ServerCommand("banList.save");
                        ServerCommand("banList.list");
                        DebugWrite(2, Player.Name + " was temp banned for " + duration + " minutes for reason: " + reason);
                    }
                    else
                        DebugWrite(2, "Players EAGUID is null.  Could not BAN.");
                }
                else if (type == BanType.Name)
                {
                    ServerCommand("banList.add", "name", Player.Name, "seconds", (duration * 60).ToString(), reason);
                    ServerCommand("banList.save");
                    ServerCommand("banList.list");
                    DebugWrite(2, Player.Name + " was temp banned for " + duration + " minutes for reason: " + reason);
                }
            }
            else
                ConsoleWarn("^bNot temp banning " + Player.Name + " because kills, kicks, and bans are disabled.^n");
        }

        public void TempBanPlayer(PlayerInfo Player, int duration, string reason, BanType type, int delay)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Thread.Sleep(delay);
                TempBanPlayer(Player, duration, reason, type);
            });
        }

        #endregion

        #region Details tab details
        public string GetPluginName()
        {
            return "Simple Weapon Limiter";
        }

        public string GetPluginVersion()
        {
            return PluginVersion;
        }

        public string GetPluginAuthor()
        {
            return "S1ngular1ty";
        }

        public string GetPluginWebsite()
        {
            return "";
        }

        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
<p>This plugin was originally created to enforce weapon limits in a knife and pistol only server. I quickly realized it could be used to limit any weapon on any type of server.
    <br />
    <br />Unfortunately, because of the way Battlefield 4 reports vehicle kill information, vehicles can not be limited using this plugin.</p>
<p>This plugin limits players from using certain weapons, or categories of weapons, in game. Action is taken against players when they kill with a weapon or weapon category you have have restricted with this plugin. An escalating punishment system is built into the plugin. The punishments grow more severe until the player is temporarily banned for a time duration you can adjust. The punishment system is as follows:</p>
<p><strong>Punishment Pattern:</strong>
</p>
<ol>
    <li><strong>Warning 1</strong> - The offender is killed and warned that they used a restricted weapon via a admin yell directed to them and in game chat. The victim is also notified that the killer was punished to prevent victim rage.
        <br />
        <br />
    </li>
    <li><strong>Warning 2</strong> - The offender is warned again like they were warned in warning 1 and the victim is also notified.
        <br />
        <br />
    </li>
    <li><strong>Kick</strong> - The offender is kicked and notified in the kick message why they were kicked. Everyone in game is notified that the offender is kicked.
        <br />
        <br />
    </li>
    <li><strong>Temporary Ban</strong> - If the offender returns to the game and uses a restricted weapon again during the same game, they are temporarily banned from the server for a duration of your choosing. The ban duration can be set with the <strong>Temp Ban Duration</strong> setting in the plugin. The time you specify for the ban is in minutes.</li>
</ol>
<p>By default, this plugin restricts all weapons except those that you specifically allow. This makes it very easy to set up restrictions for a knife and pistol only server. The default configuration of the plugin is setup to allow only Melee weapons (knives), pistols, and the phantom bow (weapon code dlSHTR).</p>
<p>You can change this behavior if you wish with the <strong>Restriction Mode</strong> setting.&nbsp;</p>
<h2>&nbsp;</h2>
<h2>Settings</h2>
<ul>
    <li><strong>Enable White List? - </strong>This setting controls the player white list for the plugin. If set this setting is <strong>true</strong> then the player white list is activated. Any players in the player white list will be protected from punishments for using restricted weapons. If this setting is set to <strong>false</strong> then the white list is deactivated.
        <br />
        <br />
    </li>
    <li><strong>White List Admins? - </strong>This setting controls whether admins are immune to the weapon restrictions of the plugin. Setting this to <strong>true</strong> means admins are protected from punishments if they use a restricted weapon. Setting this to <strong>false</strong> means admins are restricted like all other players.
        <br />
        <br />
    </li>
    <li><strong>White List - </strong>This setting is a list of players you want to protect from being punished if they used a restricted weapon. Place one player name per line in the setting as shown below:
        <blockquote>S1ngular1ty
            <br /> Phogue
            <br /> RandonDude</blockquote>
    </li>
    <li><strong>Restriction Mode - </strong>This setting controls the how the plugin restricts weapons. There are two options. The first mode called <strong>WhiteList</strong> is the default behavior of the plugin.&nbsp; This mode restricts all weapons except those you explicitly allow. The second mode is called <strong>BlackList</strong> and it exhibits the opposite behavior. It allows all weapons except those that you explicitly restrict.
        <br />
        <br />
    </li>
    <li><strong>Allowed Weapon Categories - </strong>This setting is only available in the <strong>WhiteList</strong> restriction mode. Enter the weapon categories you wish to allow in this setting. You must enter one category per line as shown:
        <blockquote>Melee
            <br />Handgun
            <br />Impact</blockquote>
        <p>The allowable weapon categories are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Disallowed Weapon Categories - </strong>This setting is only available in the <strong>BlackList</strong> restriction mode. Enter the weapon categories you wish to restrict in this setting. You must enter one category per line as shown:
        <blockquote>SniperRifle
            <br />ProjectileExplosive
            <br />Shotgun</blockquote>
        <p>The allowable weapon categories are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Allowed Weapons - </strong>In the <strong>WhiteList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to allow from weapon categories that are not allowed in the <strong>Allowed Weapon Categories</strong> setting or that don't have a weapon category. For example, if you want to allow the Phantom Bow you must place the weapon code dlSHTR in this setting because the Phantom Bow does not have a weapon category.
        <br />
        <br />In the <strong>BlackList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to allow from weapon categories that you restricted in the <strong>Disallowed Weapon Categories</strong> setting. For example, if you wanted to limit sniper rifles to a few choices you could restrict the SniperRifle category and then allow the specific weapon codes for the rifles you want to allow by placing them in this setting.
        <br />
        <br />This setting requires you to enter 1 weapon per line as shown:
        <blockquote>dlSHTR
            <br />U_BallisticShield</blockquote>
        <p>The allowable weapon codes are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Disallowed Weapons - </strong>In the <strong>WhiteList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to restrict from weapon categories that are allowed in the <strong>Allowed Weapon Categories</strong> setting or that don't have a weapon category. A very common example of the usefulness of this setting would be to allow all handguns using the Handgun category in Allowed Weapon Categories while limiting the use of the G18 and M93 automatic pistols by by placing their weapon codes in this setting.
        <br />
        <br />In the <strong>BlackList</strong> restriction mode, this setting allows you to specify individual weapons that you wish to restrict from weapon categories that you have NOT restricted in the <strong>Disallowed Weapon Categories</strong> setting. For example, if you wish to restrict 1 sniper rifle out of all the sniper rifles you would enter the weapon code for that rifle in this setting.
        <br />
        <br />This setting requires you to enter 1 weapon per line as shown:
        <blockquote>U_Glock18
            <br />U_M93R
            <br />U_SerbuShorty</blockquote>
        <p>The allowable weapon categories are available in the BF4.def file in the \Configs folder of your Procon installation</p>
    </li>
    <li><strong>Temp Ban Duration (minutes) - </strong>This setting controls the temporary ban duration for players that repeatedly violate the restricted weapons rules during a game. The time show is in minutes.</li>
</ul>
<h2>&nbsp;</h2>
<h2>Weapon Categories</h2>
<p>These are the available weapon categories from the BF4.def file in the \Configs folder of your Procon installation.</p>
<blockquote>AssaultRifle
    <br />Carbine
    <br />DMR
    <br />Explosive
    <br />Handgun
    <br />Impact
    <br />LMG
    <br />Melee
    <br />Nonlethal
    <br />PDW
    <br />ProjectileExplosive
    <br />Shotgun
    <br />SMG
    <br />SniperRifle</blockquote>
<h2>&nbsp;</h2>
<h2>Commands</h2>
<blockquote>
    <h4>!checkaccount [playername]</h4>
    <ul>
        <li>Checks whether the player specified is an admin and sends message to you in chat</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!decrpunish [playername]</h4>
    <ul>
        <li>Decreases the punishment level of a player by 1</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!incrpunish [playername]</h4>
    <ul>
        <li>Increases the punishment level of a player by 1</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!checkpunish [playername]</h4>
    <ul>
        <li>Checks the current punishment level of a player</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!setpunish [playername] [value]</h4>
    <ul>
        <li>Sets the punishment level of a player to the value you specify</li>
        <li>Value can be between 0 and 2</li>
    </ul>
</blockquote>
<blockquote>
    <h4>!clearpunish [playername]</h4>
    <ul>
        <li>Clears the punishments for the player you specify</li>
    </ul>
</blockquote>
<h2>&nbsp;</h2>
<h2>Command Response Scopes</h2>
<blockquote>
    <h4>!</h4> Responses will be displayed to everyone in the server.</blockquote>
<blockquote>
    <h4>@</h4> Responses will only be displayed to the account holder that issued the command.</blockquote>
<p>All error messages are privately sent to the command issuer</p>
<h2>&nbsp;</h2>
<h2>Development</h2>
<h3>Changelog</h3>
<blockquote>
    <h4>1.0.0.0 (4-24-2016)</h4> - initial version</blockquote>
";
        }
        #endregion

        #region Plugin variables

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            var var_type = "enum.RestrictionMode" + "(" + String.Join("|", Enum.GetNames(typeof(RestrictionMode))) + ")";

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Testing|Testing?", testing.GetType(), testing));

            if (testing)
            {
                lstReturn.Add(new CPluginVariable("Testing|Disable In Game Messages", disableGameMsgs.GetType(), disableGameMsgs));
                lstReturn.Add(new CPluginVariable("Testing|Divert All Messages to Procon Chat", proconChatAllMsgs.GetType(), proconChatAllMsgs));
                lstReturn.Add(new CPluginVariable("Testing|Disable Kill Punishment System", disableKillPunishments.GetType(), disableKillPunishments));
                lstReturn.Add(new CPluginVariable("Testing|Disable Kills, Kicks, and Bans", disableKickBan.GetType(), disableKickBan));
            }
            else
            {
                proconChatAllMsgs = false;
                disableKillPunishments = false;
                disableKickBan = false;
            }
            lstReturn.Add(new CPluginVariable("WeaponLimiter|Debug Level", debugLevel.GetType(), debugLevel));
            lstReturn.Add(new CPluginVariable("WeaponLimiter|White List Admins?", whiteListAdmins.GetType(), whiteListAdmins));
            lstReturn.Add(new CPluginVariable("WeaponLimiter|Enable White List?", enableWhiteList.GetType(), enableWhiteList));
            lstReturn.Add(new CPluginVariable("WeaponLimiter|White List (one player per line)", typeof(string[]), whiteList.ToArray()));
            lstReturn.Add(new CPluginVariable("WeaponLimiter|Temp Ban Duration (minutes)", tempBanDuration.GetType(), tempBanDuration));
            lstReturn.Add(new CPluginVariable("WeaponLimiter|Restriction Mode", var_type, restrictionMode.ToString()));

            if (restrictionMode == RestrictionMode.WhiteList)
            {
                lstReturn.Add(new CPluginVariable("WeaponLimiter|Allowed Weapon Categories (one per line)", typeof(string[]), allowedCategories.ToArray()));
                lstReturn.Add(new CPluginVariable("WeaponLimiter|Allowed Weapons (one per line)", typeof(string[]), whitelistAllowedWeapons.ToArray()));
                lstReturn.Add(new CPluginVariable("WeaponLimiter|Disallowed Weapons (one per line)", typeof(string[]), whitelistDisallowedWeapons.ToArray()));
            }
            else if (restrictionMode == RestrictionMode.BlackList)
            {
                lstReturn.Add(new CPluginVariable("WeaponLimiter|Disallowed Weapon Categories (one per line)", typeof(string[]), disallowedCategories.ToArray()));
                lstReturn.Add(new CPluginVariable("WeaponLimiter|Allowed Weapons (one per line)", typeof(string[]), blacklistAllowedWeapons.ToArray()));
                lstReturn.Add(new CPluginVariable("WeaponLimiter|Disallowed Weapons (one per line)", typeof(string[]), blacklistDisallowedWeapons.ToArray()));
            }

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            var var_type = "enum.RestrictionMode" + "(" + String.Join("|", Enum.GetNames(typeof(RestrictionMode))) + ")";

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Debug Level", debugLevel.GetType(), debugLevel));

            lstReturn.Add(new CPluginVariable("Testing?", testing.GetType(), testing));
            lstReturn.Add(new CPluginVariable("Disable In Game Messages", disableGameMsgs.GetType(), disableGameMsgs));
            lstReturn.Add(new CPluginVariable("Divert All Messages to Procon Chat", proconChatAllMsgs.GetType(), proconChatAllMsgs));
            lstReturn.Add(new CPluginVariable("Disable Kill Punishment System", disableKillPunishments.GetType(), disableKillPunishments));
            lstReturn.Add(new CPluginVariable("Disable Kills, Kicks, and Bans", disableKickBan.GetType(), disableKickBan));

            lstReturn.Add(new CPluginVariable("White List Admins?", whiteListAdmins.GetType(), whiteListAdmins));
            lstReturn.Add(new CPluginVariable("Enable White List?", enableWhiteList.GetType(), enableWhiteList));
            lstReturn.Add(new CPluginVariable("White List (one player per line)", typeof(string[]), whiteList.ToArray()));
            lstReturn.Add(new CPluginVariable("Temp Ban Duration (minutes)", tempBanDuration.GetType(), tempBanDuration));

            lstReturn.Add(new CPluginVariable("Restriction Mode", var_type, restrictionMode.ToString()));
            lstReturn.Add(new CPluginVariable("Allowed Weapon Categories (one per line)", typeof(string[]), allowedCategories.ToArray()));
            lstReturn.Add(new CPluginVariable("Disallowed Weapon Categories (one per line)", typeof(string[]), disallowedCategories.ToArray()));
            lstReturn.Add(new CPluginVariable("Allowed Weapons (one per line)", typeof(string[]), whitelistAllowedWeapons.ToArray()));
            lstReturn.Add(new CPluginVariable("Disallowed Weapons (one per line)", typeof(string[]), whitelistDisallowedWeapons.ToArray()));
            lstReturn.Add(new CPluginVariable("Allowed Weapons (one per line)", typeof(string[]), blacklistAllowedWeapons.ToArray()));
            lstReturn.Add(new CPluginVariable("Disallowed Weapons (one per line)", typeof(string[]), blacklistDisallowedWeapons.ToArray()));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

            if (strVariable.CompareTo("Restriction Mode") == 0)
            {
                var value = (RestrictionMode)Enum.Parse(typeof(RestrictionMode), strValue);
                restrictionMode = value;
            }
            else if (strVariable.CompareTo("Debug Level") == 0)
            {
                int tmp = 0;
                int.TryParse(strValue, out tmp);
                debugLevel = tmp;
            }
            else if (strVariable.CompareTo("Testing?") == 0)
            {
                bool tmp;
                bool.TryParse(strValue, out tmp);
                testing = tmp;
            }
            else if (strVariable.CompareTo("Divert All Messages to Procon Chat") == 0)
            {
                bool tmp;
                bool.TryParse(strValue, out tmp);
                proconChatAllMsgs = tmp;
            }
            else if (strVariable.CompareTo("Disable Kill Punishment System") == 0)
            {
                bool tmp;
                bool.TryParse(strValue, out tmp);
                disableKillPunishments = tmp;
            }
            else if (strVariable.CompareTo("Disable Kills, Kicks, and Bans") == 0)
            {
                bool tmp;
                bool.TryParse(strValue, out tmp);
                disableKickBan = tmp;
            }
            else if (strVariable.CompareTo("Disable In Game Messages") == 0)
            {
                bool tmp;
                bool.TryParse(strValue, out tmp);
                disableGameMsgs = tmp;
            }
            else if (strVariable.CompareTo("White List Admins?") == 0)
            {
                bool tmp;
                bool.TryParse(strValue, out tmp);
                whiteListAdmins = tmp;
            }
            else if (strVariable.CompareTo("Enable White List?") == 0)
            {
                bool tmp;
                bool.TryParse(strValue, out tmp);
                enableWhiteList = tmp;
            }
            else if (strVariable.CompareTo("White List (one player per line)") == 0)
            {
                var values = CPluginVariable.DecodeStringArray(strValue);

                whiteList.Clear();
                foreach (var value in values)
                {
                    whiteList.Add(value.Trim());
                }
            }
            else if (strVariable.CompareTo("Allowed Weapon Categories (one per line)") == 0)
            {
                var values = CPluginVariable.DecodeStringArray(strValue);

                allowedCategories.Clear();
                foreach (var value in values)
                {
                    allowedCategories.Add(value.Trim());
                }
            }
            else if (strVariable.CompareTo("Disallowed Weapon Categories (one per line)") == 0)
            {
                var values = CPluginVariable.DecodeStringArray(strValue);

                disallowedCategories.Clear();
                foreach (var value in values)
                {
                    disallowedCategories.Add(value.Trim());
                }
            }
            else if (strVariable.CompareTo("Allowed Weapons (one per line)") == 0)
            {
                var values = CPluginVariable.DecodeStringArray(strValue);

                if (restrictionMode == RestrictionMode.WhiteList)
                {
                    whitelistAllowedWeapons.Clear();
                    foreach (var value in values)
                    {
                        whitelistAllowedWeapons.Add(value.Trim());
                    }
                }
                else if (restrictionMode == RestrictionMode.BlackList)
                {
                    blacklistAllowedWeapons.Clear();
                    foreach (var value in values)
                    {
                        blacklistAllowedWeapons.Add(value.Trim());
                    }
                }
            }
            else if (strVariable.CompareTo("Disallowed Weapons (one per line)") == 0)
            {
                var values = CPluginVariable.DecodeStringArray(strValue);

                if (restrictionMode == RestrictionMode.WhiteList)
                {
                    whitelistDisallowedWeapons.Clear();
                    foreach (var value in values)
                    {
                        whitelistDisallowedWeapons.Add(value.Trim());
                    }
                }
                else if (restrictionMode == RestrictionMode.BlackList)
                {
                    blacklistDisallowedWeapons.Clear();
                    foreach (var value in values)
                    {
                        blacklistDisallowedWeapons.Add(value.Trim());
                    }
                }
            }
            else if (strVariable.CompareTo("Temp Ban Duration (minutes)") == 0)
            {
                int tmp = 0;
                int.TryParse(strValue, out tmp);
                tempBanDuration = tmp;
            }

        }

        #endregion

        #region Procon events

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnServerInfo", "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnPlayerKilled",
                "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnRoundOverPlayers", "OnRoundOver", "OnLevelLoaded", "OnPunkbusterPlayerInfo");
        }

        public void OnPluginEnable()
        {
            pluginEnabled = true;
            ConsoleWrite("Enabled!");
            InitWeapons();
        }

        public void OnPluginDisable()
        {
            PlayerList.Clear();
            WeaponsDict.Clear();
            wasPunishKicked.Clear();

            pluginEnabled = false;
            ConsoleWrite("Disabled!");
        }

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            DebugWrite(5, "On List Players");

            if (subset.Subset == CPlayerSubset.PlayerSubsetType.All)
                SyncPlayers(players);
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo playerInfo)
        {
            if (PlayerList.Keys.Contains(playerInfo.SoldierName))
                PlayerList[playerInfo.SoldierName].pbInfo = playerInfo;
        }

        public override void OnPlayerJoin(string Name)
        {

        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {

        }

        public override void OnGlobalChat(string speaker, string message)
        {
            CommandHandler(speaker, message);
        }

        public override void OnTeamChat(string speaker, string message, int teamId)
        {
            CommandHandler(speaker, message);
        }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            CommandHandler(speaker, message);
        }

        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
        {

        } // use this one for BF4

        public override void OnServerInfo(CServerInfo serverInfo)
        {
            server.data = serverInfo;
            DebugWrite(4, "OnServerInfo - Current map is: " + server.CurrentMapName);
        }

        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            var killer = kKillerVictimDetails.Killer;
            var victim = kKillerVictimDetails.Victim;

            ProcessKill(kKillerVictimDetails);
        }

        public override void OnRoundOverPlayers(List<CPlayerInfo> players)
        {
            ConsoleWarn("OnRoundOverPlayers");
            SyncPlayers(players);
        }

        public override void OnRoundOver(int winningTeamId)
        {
            ConsoleWarn("OnRoundOver");
            ClearRoundData();
        }

        #endregion

        #region Unused Procon Events

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) { }
        public override void OnPlayerSpawned(string Name, Inventory spawnedInventory) { }
        public override void OnPlayerTeamChange(string Name, int teamId, int squadId) { }
        public override void OnPlayerSquadChange(string soldierName, int teamId, int squadId) { }
        public override void OnRoundOverTeamScores(List<TeamScore> teamScores) { }
        public override void OnVersion(string serverType, string version) { }

        #endregion

        public class PlayerInfo : CPlayerInfo
        {
            public int Punishments { get; set; }

            public bool IsAdmin { get; set; }
            public bool CanKill { get; set; }
            public bool CanKick { get; set; }
            public bool CanBan { get; set; }
            public bool CanMove { get; set; }
            public bool CanChangeLevel { get; set; }

            public bool IsInWhitelist { get; set; }

            public CPlayerInfo info;
            public CPunkbusterInfo pbInfo;

            public string Name { get { return info.SoldierName; } }
            public string FullName { get { return (ClanTag.Length > 0) ? "[" + ClanTag + "]" + Name : Name; } }
            public string FullDisplayName { get { return (ClanTag.Length > 0) ? "^b[^n" + ClanTag + "^b]^n^b" + Name + "^n" : "^b" + Name + "^n"; } }
            public string EAGuid { get { return info.GUID; } }
            public int TeamId { get { return info.TeamID; } set { info.TeamID = value; } }
            public int SquadId { get { return info.SquadID; } set { info.SquadID = value; } }
            public int Role { get { return info.Type; } }
            public string IPAddress { get { return pbInfo.Ip; } }
            public string CountryCode { get { return pbInfo.PlayerCountryCode; } }
            public string CountryName { get { return pbInfo.PlayerCountry; } }
            public string PBGuid { get { return pbInfo.GUID; } }

            public PlayerInfo()
            {
                IsAdmin = false;
                CanKill = false;
                CanKick = false;
                CanBan = false;
                CanMove = false;
                CanChangeLevel = false;
            }

            public PlayerInfo(CPlayerInfo pinfo)
            {
                info = pinfo;
                pbInfo = null;
            }
        }

        public class ServerInfo
        {
            WeaponLimiter Plugin { get; set; }
            public CServerInfo data = null;
            public List<MaplistEntry> MapList { get; set; }
            public Dictionary<string, string> FriendlyMaps { get; set; }

            int _WinTeamId = 0;

            public int CurrentRound { get { return data.CurrentRound; } }
            public int TotalRounds { get { return data.TotalRounds; } }
            public int RoundTime { get { return data.RoundTime; } }
            public string MapFileName { get { return (MapList == null) ? data.Map : MapList[CurrentMapIndex].MapFileName; } }
            public string CurrentMapName
            {
                get
                {
                    return (MapList == null) ? FriendlyMaps[data.Map] : FriendlyMaps[MapList[CurrentMapIndex].MapFileName];
                }
            }
            public string NextMapName
            {
                get
                {
                    return (MapList == null) ? "" : FriendlyMaps[MapList[NextMapIndex].MapFileName];
                }
            }
            public string CurrentMapFileName { get { return (MapList == null) ? "" : MapList[CurrentMapIndex].MapFileName; } }
            public string NextMapFileName { get { return (MapList == null) ? "" : MapList[NextMapIndex].MapFileName; } }
            public int CurrentMapIndex { get; set; }
            public int NextMapIndex { get; set; }
            public int PlayerCount { get { return data.PlayerCount; } }
            public double TimeUp { get { return data.ServerUptime; } }
            public int WinTeamId { get { return _WinTeamId; } internal set { _WinTeamId = value; } }
            private int team1Score;
            public int Team1Score { get { return data.TeamScores[0].Score; } internal set { team1Score = value; } }
            private int team2Score;
            public int Team2Score { get { return data.TeamScores[1].Score; } internal set { team2Score = value; } }

            public DateTime RoundStart
            {
                get
                {
                    DateTime ret = DateTime.MinValue;

                    TimeSpan ts = new TimeSpan(0, 0, RoundTime);
                    ret = DateTime.Now.Subtract(ts);

                    return ret;
                }
            }

            public double RoundTimeMinutes { get { return (DateTime.Now - RoundStart).TotalMinutes; } }

            public void ClearRoundData()
            {
                Team1Score = 0;
                Team2Score = 0;
            }
            public void UpdateMapDefinitions()
            {
                if (FriendlyMaps.Count > 0)
                    FriendlyMaps.Clear();
                List<CMap> bf3_defs = Plugin.GetMapDefines();
                foreach (CMap m in bf3_defs)
                {
                    if (!FriendlyMaps.ContainsKey(m.FileName)) FriendlyMaps[m.FileName] = m.PublicLevelName;
                }
            }

            public ServerInfo(WeaponLimiter plugin)
            {
                MapList = new List<MaplistEntry>();
                FriendlyMaps = new Dictionary<string, string>();
                Plugin = plugin;
            }
        }

        public class KillInfo
        {
            public Kill kill;
            public string category;
            public DateTime time = DateTime.Now;
            public string Weapon { get { return kill.DamageType; } }
            public bool Headshot { get { return kill.Headshot; } }
            public DateTime Time { get { return time; } }
            public string Category { get { return category; } }

            public KillInfo(Kill kill, string category)
            {
                this.kill = kill;
                this.category = category;
            }
        }

        public class KillReason
        {
            public string name = string.Empty;
            public string detail = null;
            public string attachedTo = null;
            public string vName = null;
            public string vDetail = null;

            public string Name { get { return name; } } // weapon name or reason, like "Suicide"
            public string Detail { get { return detail; } } // BF4: ammo or attachment
            public string AttachedTo { get { return attachedTo; } } // BF4: main weapon when Name is a secondary attachment, like M320
            public string VehicleName { get { return vName; } } // BF4: if Name is "Death", this is the vehicle's name
            public string VehicleDetail { get { return vDetail; } } // BF4: if Name is "Death", this is the vehicle's detail (stuff after final slash)            
        }

    } // end WeaponLimiter

} // end namespace PRoConEvents



