using System.Diagnostics;
using System.Numerics;

using BoreSoulsResource;

using SoulsFormats;

using static BoreSoulsResource.UtilEnums;

namespace SoulsModInstaller
{
    public partial class MainForm : Form
    {
        public enum InstallResult
        {
            Failed = 0,
            Success = 1,
            Ignore = 2,
        }

        public static readonly string ModName = $"DS1 Bonfire Warp Reconfiguration Tool";
        public const string BackupExt = ".BonfireReconfiguration.temp.bak";
        public static readonly string Version = Application.ProductVersion;
        public static readonly string ProgramTitle = $"{ModName} v{Version}";

        public static readonly string LocalLogsDir = $@"{UtilFile.GetWorkingDirectory()}\Logs";
        public static readonly string LocalBackupsDir = $@"{UtilFile.GetWorkingDirectory()}\Backups";

        public static string InstallDir = "";
        public static GameType Game = GameType.Undefined;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CheckEnableActivateButton();
            this.Text = ProgramTitle;
            //Directory.CreateDirectory(LocalLogsDir);
            Directory.CreateDirectory(LocalBackupsDir);
        }

        private void CheckEnableActivateButton()
        {
            if (FileDialog_Browse.FileName != "")
            {
                Button_Install.Enabled = true;
            }
            else
            {
                Button_Install.Enabled = false;
            }
            return;
        }

        private void RunProgram()
        {
            Button_Install.Enabled = false;

            Log = new();

            InstallResult result = InstallResult.Failed;
            try
            {
                result = TryInstall();
            }
            catch (Exception ex)
            {
                Log.AddLog("\r\n" + ex.ToString());
                UtilFile.RestoreBackups(InstallDir, BackupExt, false);
#if DEBUG
                throw;
#else
                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show(ex.Message, "Installation Error");
#endif
            }

            switch (result)
            {
                case InstallResult.Success:
                    Log.AddLog($"Installation Succeeded");
                    break;
                case InstallResult.Failed:
                    Log.AddLog($"Installation Failed");
                    break;
            }

            var backupTimeName = Log.StartTime.ToString("MM-dd-yyyy HH.mm.ss");
            var backupDir = $@"{LocalBackupsDir}\Backup {backupTimeName}";

            var logPath = Log.CreateLogFile(backupDir);

            Button_Install.Enabled = true;

            if (result == InstallResult.Success)
            {
                MoveBackupsToLocalBackupFolder(InstallDir, backupDir);

                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show("All done!\r\n(Backups were stored in the tool's backup folder)", "Installation Successful", MessageBoxButtons.OK);
            }
            else if (result == InstallResult.Failed)
            {
                UtilFile.RestoreBackups(InstallDir, BackupExt, false);

                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show("Installation failed. Backups made during installation have been restored.", "Installation Failed", MessageBoxButtons.OK);
            }

            System.Media.SystemSounds.Exclamation.Play();
            var openLogResult = MessageBox.Show("Open install log file for details?", "Open log?", MessageBoxButtons.YesNo);
            if (openLogResult == DialogResult.Yes)
                UtilFile.OpenInExplorer(logPath);
        }

        public InstallLog Log;

        public record InstallLog
        {
            public int? RngSeed = null;

            public DateTime StartTime = DateTime.Now;

            public InstallLog(int rngSeed)
            {
                RngSeed = rngSeed;
            }
            public InstallLog()
            { }

            public List<string> Log = new();

            private object _lock = new();

            public void AddLog(string text)
            {
                lock (_lock)
                {
                    Log.Add(text);
                }
            }

            /// <summary>
            /// Creates a text file for the log.
            /// </summary>
            /// <returns>Path to the text file</returns>
            public string CreateLogFile(string? logDir = null)
            {
                logDir ??= LocalLogsDir;

                var time = StartTime.ToString("MM/dd/yyyy HH:mm:ss");
                List<string> workingLog = [];
                if (RngSeed != null)
                    workingLog.Add($"Version {Version}\r\nStarted: {time}\r\nSeed: {RngSeed}");
                else
                    workingLog.Add($"Version {Version}\r\nStarted: {time}");

                workingLog.Add($"Target: {InstallDir}\r\n");

                workingLog.AddRange(Log);

                Directory.CreateDirectory(logDir);
                string logPath;
                if (RngSeed != null)
                    logPath = $@"{logDir}\LOG {RngSeed}.txt";
                else
                    logPath = $@"{logDir}\LOG {StartTime.ToString("MM-dd-yyyy HH.mm.ss")}.txt";

                File.WriteAllLines(logPath, workingLog);
                return logPath;
            }
        }

        /// <summary>
        /// Resource containing bonfire name info.
        /// </summary>
        internal class WarpResource
        {
            /// <summary>
            /// Information about a warpable bonfire derived from the resource txt file.
            /// </summary>
            /// <param name="UnlockOffset">Unlock event flag offset</param>
            /// <param name="AlwaysUnlocked">If bonfire warp will be unlocked by default, not requiring player intereacting with the relevant bonfire.</param>
            /// <param name="EntityId">Bonfire object Entity ID</param>
            /// <param name="Name">Bonfire warp name</param>
            internal record WarpInfo(int UnlockOffset, bool AlwaysUnlocked, int EntityId, string Name);

            /// <summary>
            /// Bonfire warp info in order it will appear in menus.
            /// </summary>
            public List<WarpInfo> OrderedWarps { get; set; } = new();

            public WarpResource()
            { }

            public WarpResource(string localResourcePath)
            {
                var resource = UtilFile.LoadLocalTextResource(localResourcePath, 4);

                for (var i = 0; i < resource.Count; i++)
                {
                    var l = resource[i];

                    if (int.TryParse(l[0], out var unlockFlagOffset)
                        && bool.TryParse(l[1], out var alwaysUnlocked)
                        && int.TryParse(l[2], out var id))
                    {
                        string name = l[3].Trim();
                        WarpInfo warp = new(unlockFlagOffset, alwaysUnlocked, id, name);
                        OrderedWarps.Add(warp);
                    }
                    else
                    {
                        throw new Exception($"Text resource load error: \"{localResourcePath}\" (Line {i + 1} has invalid formatting)");
                    }
                }
            }
        }

        internal class BonfireInfo
        {
            public BonfireInfo(string mapName, int bonfireEnt, int unlockFlagBase, int warpRequestedFlag)
            {
                var mapIdSplit = mapName.Replace("m", "").Split('_');
                MapId = (byte.Parse(mapIdSplit[0]), byte.Parse(mapIdSplit[1]));
                WarpRequestFlag = warpRequestedFlag;
                BonfireEnt = bonfireEnt;

                WarpUnlockedFlagBase = unlockFlagBase;
            }

            public (byte, byte) MapId { get; set; }

            public int BonfireEnt { get; set; }
            public int WarpPlayerEnt => BonfireEnt - 980;
            public int SpawnPointEnt => BonfireEnt + 1000;

            /// <summary>
            /// Warp info for this bonfire. Null if bonfire will be unwarpable.
            /// </summary>
            public WarpResource.WarpInfo? Warp { get; set; } = null;
            private int WarpUnlockedFlagBase { get; set; }
            public int WarpUnlockedFlag => WarpUnlockedFlagBase + Warp!.UnlockOffset;

            public int WarpRequestFlag { get; set; }

            public int TextId { get; set; }
            public int TalkId { get; set; }
        }

        private void ReconfigureBonfireWarps(string gameDir, WarpResource warpResource, int eventFlagRangeStart, int eventFmgRangeStart)
        {
            const int WarpNumMax = 199;
            int unlockFlagRangeStart = eventFlagRangeStart;
            int warpFlagRangeStart = eventFlagRangeStart + WarpNumMax + 1;
            int eventIdRangeStart = eventFlagRangeStart + 400;
            int dummyUnlockFlag = eventFlagRangeStart + 450;

            int highestWarpId = warpResource.OrderedWarps.Max(e => e.UnlockOffset);
            if (highestWarpId > WarpNumMax)
                throw new Exception($"Cannot have a warp unlock ID higher than {WarpNumMax}");

            if (warpResource.OrderedWarps.Count > WarpNumMax)
                throw new Exception($"Cannot have more than {WarpNumMax} warpable bonfires.");

            Log.AddLog($"Bonfire warp unlocked flag range: {unlockFlagRangeStart} - {unlockFlagRangeStart + WarpNumMax}");
            Log.AddLog($"Bonfire warp request flag range: {warpFlagRangeStart} - {warpFlagRangeStart + WarpNumMax}");
            Log.AddLog($"Bonfire warp managers event IDs: {eventIdRangeStart}, {eventIdRangeStart + 1}");
            Log.AddLog($"Bonfire warp unlocked dummy flag: {dummyUnlockFlag}");
            Log.AddLog("");

            string dcxExt = "";
            if (Game == GameType.DS1R)
            {
                dcxExt = ".dcx";
            }

            string msbDir = $@"{gameDir}\map\MapStudio";
            string talkDir = $@"{gameDir}\script\talk";
            string evdDir = $@"{gameDir}\event";
            string msgbndDir = $@"{gameDir}\msg";

            // BonfireInfo keyed by entity ID
            Dictionary<int, BonfireInfo> bonfireInfos = new();
            var nextWarpFlag = warpFlagRangeStart;

            foreach (var path in Directory.GetFiles(evdDir, "*.emevd" + dcxExt))
            {
                string mapName = Path.GetFileNameWithoutExtension(path);

                if (mapName.Split("_").Length == 4)
                {
                    EMEVD evd = EMEVD.Read(path);

                    foreach (var e in evd.Events)
                    {
                        foreach (var i in e.Instructions)
                        {
                            if (i.Bank == 2009 && i.ID == 03)
                            {
                                /*
                                RegisterBonfire Instruction 2009[03]
                                RegisterBonfire(
                                    int eventFlagId, 
                                    int entityId, 
                                    float reactionDistance, 
                                    float reactionAngle, 
                                    int setStandardKindlingLevel)
                                 */

                                var args = i.UnpackArgs([
                                    EMEVD.Instruction.ArgType.Int32,
                                    EMEVD.Instruction.ArgType.Int32,
                                    EMEVD.Instruction.ArgType.Single,
                                    EMEVD.Instruction.ArgType.Single,
                                    EMEVD.Instruction.ArgType.Int32,
                                ]);

                                //var bonfireFlag = (int)args[0]; // This isn't actually set when bonfire is lit. No idea what it does. Scared to mess with it.
                                var entityId = (int)args[1];

                                if (entityId <= 0)
                                {
                                    // This is likely because of event parameters. Does not happen in vanilla, but a mod might.
                                    // Todo: could probably just figure out how to read event parameters.
                                    Log.AddLog($"{mapName} emevd contains RegisterBonfire() with unreadable EntityID, and was skipped.");
                                    continue;
                                }

                                bonfireInfos.TryAdd(entityId, new(mapName, entityId, unlockFlagRangeStart, nextWarpFlag++));
                            }
                        }
                    }
                }
            }

            // Common EMEVD
            {
                var donorEvents = EvdCommon.GetDonorEvents($@"{UtilFile.GetWorkingDirectory()}\Resources\data\donor.emevd.dcx");

                var commonEvdPath = evdDir + "\\common.emevd" + dcxExt;
                var commonEvd = EMEVD.Read(commonEvdPath);

                var warpCheckEvent = donorEvents[51321000];
                warpCheckEvent.ID = eventIdRangeStart++;
                var warpActiveEvent = donorEvents[51321001];
                warpActiveEvent.ID = eventIdRangeStart++;

                foreach (var b in bonfireInfos.Values)
                {
                    // IfEventFlag(OR_01, ON, TargetEventFlagType.EventFlag, WarpFlag);
                    warpCheckEvent.Instructions.Add(new(3, 00,
                        [(sbyte)-1, (byte)1, (byte)0, (int)b.WarpRequestFlag]));
                }

                // IfConditionGroup(main, pass, OR_01)
                warpCheckEvent.Instructions.Add(new(0, 00,
                    [(sbyte)0, (byte)1, (sbyte)-1]));

                foreach (var b in bonfireInfos.Values)
                {
                    // SkipIfEventFlag(3, OFF, TargetEventFlagType.EventFlag, DoThisWarp)
                    warpCheckEvent.Instructions.Add(new(1003, 01,
                        new List<object>() { (byte)3, (byte)0, (byte)0, (int)b.WarpRequestFlag }));

                    // $InitializeEvent(-1, ActiveWarpEventId, warpTargetEnt, respawnEnt, warp1, warp2);
                    warpCheckEvent.Instructions.Add(new(2000, 00,
                        new List<object>() { (int)-1, (uint)warpActiveEvent.ID,
                        (int)b.WarpPlayerEnt, (int)b.SpawnPointEnt, (byte)b.MapId.Item1, (byte)b.MapId.Item2}));

                    // SetEventFlag(DoThisWarp, OFF);
                    warpCheckEvent.Instructions.Add(new(2003, 02,
                        new List<object>() { (int)b.WarpRequestFlag, (byte)0 }));

                    // EndUnconditionally(EventEndType.End);
                    warpCheckEvent.Instructions.Add(new(1000, 04,
                        new List<object>() { (byte)0 }));
                }

                var replaced = commonEvd.AddReplaceEvent(warpCheckEvent);
                if (!replaced)
                {
                    commonEvd.InitEvent(warpCheckEvent.ID); // only init warp check. active warp is initialized by warp check. 
                }
                replaced |= commonEvd.AddReplaceEvent(warpActiveEvent);

                if (replaced)
                {
                    Log.AddLog("\r\nCommon EMEVD already contained bonfire warp reconfiguration events. Make sure to update darkscript .js files.");
                }


                CreateBackup(commonEvdPath);
                commonEvd.Write(commonEvdPath);
            }


            foreach (var path in Directory.GetFiles(msbDir, "*.msb"))
            {
                string mapName = Path.GetFileNameWithoutExtension(path);
                if (mapName.StartsWith("m99"))
                    continue;

                var msb = MSB1.Read(path);
                bool modified = false;

                foreach (var o in msb.Parts.Objects)
                {
                    if (bonfireInfos.TryGetValue(o.EntityID, out var b))
                    {
                        // Bonfire (derived from EMEVD)

                        var talk = msb.Parts.Enemies.FirstOrDefault(e => e.EntityID == o.EntityID - 1000);
                        if (talk == null)
                            throw new Exception($"{mapName} bonfire {o.EntityID} does not have an associated talk character {o.EntityID - 1000}");

                        var playerWarp = msb.Parts.Players.FirstOrDefault(e => e.EntityID == o.EntityID - 980);
                        if (playerWarp == null)
                        {
                            // No player warp exists for this bonfire. Make a new one.

                            var spawnP = msb.Events.SpawnPoints.FirstOrDefault(e => e.EntityID == o.EntityID + 1000);
                            if (spawnP == null)
                                throw new Exception($"{mapName} bonfire {o.EntityID} does not have an associated SpawnPoint {o.EntityID + 1000}");

                            var spawnPRegion = msb.Regions.Regions.First(e => e.Name == spawnP.SpawnPointName);

                            playerWarp = new()
                            {
                                Name = "c0000_NewWarp_" + o.Name,
                                Scale = Vector3.One,
                                EntityID = o.EntityID - 980,
                                Position = spawnPRegion.Position,
                                Rotation = spawnPRegion.Rotation,

                                // These probably don't matter but whatever
                                ModelName = "c0000",
                                DrawGroups = new uint[4],
                                DispGroups = new uint[4],
                                IsShadowDest = 1,
                                IsShadowSrc = 1,
                                DrawByReflectCam = 1
                            };
                            msb.Parts.Players.Add(playerWarp);

                            modified = true;
                        }

                        if (talk.TalkID <= 0)
                        {
                            throw new Exception($"{mapName} bonfire {o.EntityID} talk character {o.EntityID - 1000} does not have a talkID");
                        }

                        b.TalkId = talk.TalkID;
                    }
                }

                if (modified)
                {
                    CreateBackup(path);
                    msb.Write(path);
                }
            }


            List<BonfireInfo> warpableBonfires = new();
            List<FMG.Entry> fmgWarpLocationNames = new();
            for (var i = 0; i < warpResource.OrderedWarps.Count; i++)
            {
                var warp = warpResource.OrderedWarps[i];
                if (bonfireInfos.TryGetValue(warp.EntityId, out var b))
                {
                    b.Warp = warp;
                    warpableBonfires.Add(b);
                    fmgWarpLocationNames.Add(new(eventFmgRangeStart + i, b.Warp.Name));
                    b.TextId = eventFmgRangeStart + i;
                }
                else
                {
                    Log.AddLog($"Error: Cannot find bonfire '{warp.Name}' in game data with Entity ID {warp.EntityId}");
                }
            }


            foreach (var path in Directory.GetFiles(msgbndDir, "*.msgbnd" + dcxExt, SearchOption.AllDirectories))
            {
                bool modified = false;
                var localPath = "msg\\" + path.Split("msg\\").Last();
                var bnd = BND3.Read(path);
                foreach (var f in bnd.Files)
                {
                    bool entriesAlreadyExisted = false;
                    if (f.ID == (int)FmgIDType.Event_Patch)
                    {
                        var fmg = FMG.Read(f.Bytes);

                        foreach (var newEntry in fmgWarpLocationNames)
                        {
                            var i = fmg.Entries.FindIndex(e => e.ID == newEntry.ID);
                            if (i == -1)
                            {
                                fmg.Entries.Add(newEntry);
                            }
                            else
                            {
                                // FMG already contains this entry ID
                                if (fmg.Entries[i].Text != newEntry.Text)
                                {
                                    if (!entriesAlreadyExisted)
                                    {
                                        Log.AddLog($"\r\n{localPath} Event_Patch already contained entry ID(s) between {eventFmgRangeStart} and {eventFmgRangeStart + fmgWarpLocationNames.Count}");
                                        entriesAlreadyExisted = true;
                                    }

                                    Log.AddLog($"FMG Event_Patch entry {newEntry.ID} was overwritten: {fmg.Entries[i].Text} -> {newEntry.Text}");
                                    fmg.Entries[i] = newEntry;
                                }
                            }
                        }

                        f.Bytes = fmg.Write();
                        modified = true;
                    }
                }
                if (modified)
                {
                    CreateBackup(path);
                    bnd.Write(path);
                }
            }


            // ESD

            var donor = ESD.Read($@"{UtilFile.GetWorkingDirectory()}\resources\data\t550055.esd");
            var targetStatesTxt = UtilFile.LoadLocalTextResource(@"resources\data\TalkEsdTargetStates.txt", 7)[0];
            long promptMenuStateId = long.Parse(targetStatesTxt[0]);
            long setFlagTemplateStateId = long.Parse(targetStatesTxt[1]);
            long oldResultStateIdStart = long.Parse(targetStatesTxt[2]);
            long postWarpSetFlagStateId = long.Parse(targetStatesTxt[3]);
            long bonfireInterruptedStateId = long.Parse(targetStatesTxt[4]);
            long leftBonfireStateId = long.Parse(targetStatesTxt[5]);
            long resultStateIdStart = long.Parse(targetStatesTxt[6]);

            var states = donor.StateGroups.First().Value;
            var promptMenuState = states[promptMenuStateId];
            var addPromptCmdBase = promptMenuState.EntryCommands[1];
            promptMenuState.EntryCommands.Remove(addPromptCmdBase); // Unneeded.
            var promptResultCondBase = promptMenuState.Conditions[2];
            int nextPromptSlot = 10;
            var promptSetFlagState = states[setFlagTemplateStateId];
            var setFlagCmdBase = promptSetFlagState.EntryCommands[0];
            long nextResultStateId = resultStateIdStart;

            foreach (var b in warpableBonfires)
            {
                // new state: new promp result state, tell it to set new flag
                ESD.State promptDestState = new();
                ESD.CommandCall setFlagCommand = new()
                {
                    CommandBank = setFlagCmdBase.CommandBank,
                    CommandID = setFlagCmdBase.CommandID,
                    Arguments = setFlagCmdBase.Arguments.ConvertAll(e => (byte[])e.Clone()),
                };
                BitConverter.GetBytes(b.WarpRequestFlag).CopyTo(setFlagCommand.Arguments[0], 1);  // Set warp flag
                promptDestState.EntryCommands = [setFlagCommand];
                promptDestState.Conditions = promptSetFlagState.Conditions; // goto "warp was requested, relax now" state 3150

                states.Add(nextResultStateId, promptDestState);

                // promptsState: add command to add new prompt
                ESD.CommandCall addPrompt = new()
                {
                    CommandBank = addPromptCmdBase.CommandBank,
                    CommandID = addPromptCmdBase.CommandID,
                    Arguments = addPromptCmdBase.Arguments.ConvertAll(e => (byte[])e.Clone()),
                };
                BitConverter.GetBytes(nextPromptSlot).CopyTo(addPrompt.Arguments[0], 1); // slot
                BitConverter.GetBytes(b.TextId).CopyTo(addPrompt.Arguments[1], 1); // text ID
                if (b.Warp!.AlwaysUnlocked)
                {
                    BitConverter.GetBytes(-1).CopyTo(addPrompt.Arguments[2], 1); // unlocked flag
                }
                else
                {
                    BitConverter.GetBytes(b.WarpUnlockedFlag).CopyTo(addPrompt.Arguments[2], 1); // unlocked flag
                }
                promptMenuState.EntryCommands.Add(addPrompt);

                // promptsState: add condition to check new prompt result, then jump to new prompt result state
                byte[] promptCheckEval = (byte[])promptResultCondBase.Evaluator.Clone();
                BitConverter.GetBytes(nextPromptSlot).CopyTo(promptCheckEval, 3);
                ESD.Condition promptCheck = new(nextResultStateId, promptCheckEval);
                promptCheck.TargetState = nextResultStateId;
                promptMenuState.Conditions.Add(promptCheck);

                nextResultStateId++;
                nextPromptSlot++;
            }

            // ESD: inject new warp states into EVERY bonfire talkESD
            foreach (var path in Directory.GetFiles(talkDir, "*.talkesdbnd" + dcxExt))
            {
                bool modified = false;
                var bnd = BND3.Read(path);
                foreach (var f in bnd.Files)
                {
                    var fName = Path.GetFileNameWithoutExtension(f.Name);
                    var talkId = int.Parse(fName[1..]);
                    var b = bonfireInfos.Values.FirstOrDefault(e => e.TalkId == talkId);
                    if (b != null)
                    {
                        modified = true;
                        bool preReconfigured = false;

                        var esd = ESD.Read(f.Bytes);

                        foreach (var groups in esd.StateGroups)
                        {
                            void CheckAndRemoveOldStates(long id)
                            {
                                var result = groups.Value.Remove(id);
                                if (result)
                                {
                                    if (!preReconfigured)
                                    {
                                        Log.AddLog($"\r\nESD {fName} has state IDs showing it was already reconfigured. Updating...");
                                        preReconfigured = true;
                                    }

                                    //Log.AddLog($"Removed old reconfiguration state {id} from ESD {fName}");
                                }
                            }
                            CheckAndRemoveOldStates(promptMenuStateId);
                            CheckAndRemoveOldStates(setFlagTemplateStateId);
                            for (var i = 0; i <= WarpNumMax; i++)
                            {
                                CheckAndRemoveOldStates(resultStateIdStart + i);
                            }
                            for (var i = 0; i <= WarpNumMax; i++)
                            {
                                CheckAndRemoveOldStates(oldResultStateIdStart + i);
                            }
                            CheckAndRemoveOldStates(postWarpSetFlagStateId);
                            CheckAndRemoveOldStates(bonfireInterruptedStateId);
                            CheckAndRemoveOldStates(leftBonfireStateId);

                            foreach (var state in donor.StateGroups.First().Value)
                            {
                                if (state.Key <= 999)
                                    continue;

                                groups.Value.Add(state.Key, state.Value);
                            }

                            foreach (var state in groups.Value.Values)
                            {

                                for (var i = 0; i < state.EntryCommands.Count; i++)
                                {
                                    var c = state.EntryCommands[i];

                                    const bool enableWarpOnBonfireLit = false; // Not vanilla behavior. todo: implement as option

                                    if ((c.CommandBank == 1 && c.CommandID == 50) // RequestSave (used when resting at a bonfire)
                                        || (enableWarpOnBonfireLit && c.CommandBank == 1 && c.CommandID == 38)) // SetUpdateDistance (used when bonfire is actively lit)
                                    {
                                        // Set new bonfire lit flag if bonfire lit state is active

                                        ESD.CommandCall setFlagCommand = new()
                                        {
                                            CommandBank = setFlagCmdBase.CommandBank,
                                            CommandID = setFlagCmdBase.CommandID,
                                            Arguments = setFlagCmdBase.Arguments.ConvertAll(e => (byte[])e.Clone()),
                                        };

                                        int unlockFlag;
                                        if (b.Warp != null)
                                            unlockFlag = b.WarpUnlockedFlag;
                                        else
                                            unlockFlag = dummyUnlockFlag;

                                        BitConverter.GetBytes(unlockFlag).CopyTo(setFlagCommand.Arguments[0], 1);

                                        if (preReconfigured)
                                        {
                                            // Bonfire has been reconfigured in the past, update instead of adding.
                                            if (i == 0
                                                || state.EntryCommands[i - 1].CommandBank != setFlagCmdBase.CommandBank
                                                || state.EntryCommands[i - 1].CommandID != setFlagCmdBase.CommandID)
                                            {
                                                throw new Exception($"ESD {fName} seems reconfigured in the past, but bonfire unlocked set flag cmd was not found where expected (before RequestSave() )");
                                            }

                                            state.EntryCommands[i - 1] = setFlagCommand;
                                        }
                                        else
                                        {
                                            state.EntryCommands.Insert(i, setFlagCommand);
                                        }
                                        i++;
                                    }
                                    else if (c.CommandBank == 1 && c.CommandID == 41) // StartWarpMenuInit
                                    {
                                        // Remove old bonfire warp menu command
                                        // If this never triggers, then ESD was probably already reconfigured, and the pre-existing state jump should work.
                                        state.EntryCommands.RemoveAt(i);
                                        state.Conditions.Insert(0, new(promptMenuStateId, [65, 161])); // auto jump to new warp menu state

                                        break;
                                    }
                                }
                            }
                        }

                        f.Bytes = esd.Write();
                    }
                }

                if (modified)
                {
                    CreateBackup(path);
                    bnd.Write(path);
                }
            }

            foreach (var b in warpableBonfires)
            {
                string unlocked = b.Warp!.AlwaysUnlocked 
                    ? $"\tWarp Always Unlocked\r\n" 
                    : $"\tWarp unlock flag: {b.WarpUnlockedFlag} (Warp ID #{b.Warp!.UnlockOffset})\r\n";

                Log.AddLog($"m{b.MapId.Item1:D2}_{b.MapId.Item2:D2} Bonfire {b.Warp!.Name}\r\n" +
                    $"\tEntity ID: {b.BonfireEnt}\r\n" +
                    unlocked +
                    $"\tWarp request flag: {b.WarpRequestFlag}\r\n" +
                    $"\tFMG entry ID: {b.TextId}\r\n" +
                    $"\tTalk ID: {b.TalkId}\r\n");
            }

        }

        public InstallResult TryInstall()
        {
            if (UtilFile.BackupFilesExist(InstallDir, BackupExt))
            {
                var msg = MessageBox.Show("Temporary backup files have been found in the game folder.\r\n\r\n" +
                    "This may indicate a catastrophic failure from a previous installation attempt, where backups failed to be restored afterwards.\r\n\r\n" +
                    "Be aware that these temporary backups will replace actual game files if installation fails.\r\n\r\n" +
                    "Be aware that these temporary backups will be deleted upon installation success.\r\n\r\n" +
                    $"If the installation failure was recent and you have not made additional changes to game files: you can probably ignore this warning and proceed.\r\n\r\n" +
                    $"If the installation failure was NOT recent, please check these backups yourself to make sure proceeding will not result in your work being lost. (Backup files end with \"{BackupExt}\").\r\n\r\n" +
                    "Do you want to continue?"
                    , "WARNING: TEMPORARY BACKUP FILES FOUND", MessageBoxButtons.OKCancel);
                if (msg == DialogResult.Cancel)
                {
                    return InstallResult.Ignore;
                }
            }

            string paramPath = UtilFile.GetGameParamPath(Game, InstallDir);
            if (!UtilFile.CheckUnpackedFileExists(Game, paramPath))
                return InstallResult.Failed;

            WarpResource bonfireNames = new($@"Resources\BonfireWarps.txt");
            var eventTxt = UtilFile.LoadLocalTextResource(@"Resources\EventFlags.txt", 1);
            var textTxt = UtilFile.LoadLocalTextResource(@"Resources\EventTextIds.txt", 1);
            int eventId;
            int textId;

            try
            {
                eventId = int.Parse(eventTxt[0][0]);
            }
            catch (Exception ex)
            {
                throw new Exception($"Text resource load error: failed to parse value from EventFlags.txt\r\n{ex.Message}");
            }

            try
            {
                textId = int.Parse(textTxt[0][0]);
            }
            catch (Exception ex)
            {
                throw new Exception($"Text resource load error: failed to parse value from EventTextIds.txt\r\n{ex.Message}");
            }

            ReconfigureBonfireWarps(InstallDir, bonfireNames, eventId, textId);

            return InstallResult.Success;
        }

        private void CreateBackup(string path, bool overwrite = false)
        {
            if (!File.Exists(path + BackupExt))
                File.Copy(path, path + BackupExt, false);
            else if (overwrite)
                File.Copy(path + BackupExt, path, true);
        }

        private void MoveBackupsToLocalBackupFolder(string gameDir, string backupDir)
        {
            var backups = UtilFile.GetBackupFiles(gameDir, BackupExt);
            foreach (var path in backups)
            {
                var localPath = UtilFile.GetVirtualPath(gameDir, path);
                localPath = localPath.Replace(BackupExt, "");
                var destPath = backupDir + localPath;
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Move(path, destPath);
            }
        }

        private void Button_Browse_Click(object sender, EventArgs e)
        {
            var result = FileDialog_Browse.ShowDialog();
            if (result != DialogResult.OK)
                return;

            InstallDir = Path.GetDirectoryName(FileDialog_Browse.FileName);

            if (Game == GameType.DES && !File.Exists(InstallDir + "\\EBOOT.BIN"))
            {
                return;
            }
            else if (Path.GetFileName(FileDialog_Browse.FileName) == "DARKSOULS.exe")
            {
                Game = GameType.DS1;
            }
            else
            {
                Game = GameType.DS1R;
            }

            CheckEnableActivateButton();
        }

        private void Button_Install_Click(object sender, EventArgs e)
        {
            RunProgram();
        }

        private void b_openBackups_Click(object sender, EventArgs e)
        {
            UtilFile.OpenInExplorer(LocalBackupsDir);
        }

        private void b_editBonfireWarps_Click(object sender, EventArgs e)
        {
            UtilFile.OpenInExplorer($@"{UtilFile.GetWorkingDirectory()}\Resources\BonfireWarps.txt");
        }
    }
}