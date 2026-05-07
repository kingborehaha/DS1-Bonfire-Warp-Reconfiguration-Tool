using SoulsFormats;

namespace BoreSoulsResource
{
    public static class EvdCommon
    {
        public static void InitEvent(long eventID, EMEVD emevd, List<object> parms = null)
        {
            InitEvent(eventID, emevd.Events.Find(e => e.ID == 0), parms);
        }
        public static void InitEvent(this EMEVD emevd, long eventID, List<object> parms = null)
        {
            InitEvent(eventID, emevd.Events.Find(e => e.ID == 0), parms);
        }
        public static void InitEvent(long eventID, EMEVD.Event constructor, List<object> parms = null)
        {
            if (parms == null)
                parms = new() { (int)0 };

            EMEVD.Instruction init = new();
            // InitializeEvent Instruction 2000[00]
            init.Bank = 2000;
            init.ID = 00;
            var args = new List<object> { -1, (uint)eventID };
            args.AddRange(parms);
            init.PackArgs(args);
            constructor.Instructions.Insert(0, init);
        }
        public static void InitEvent(this EMEVD.Event constructor, long eventID, List<object> parms = null)
        {
            InitEvent(eventID, constructor, parms);
        }

        /// <summary>
        /// Replaces an existing event with a replacement event.
        /// If it does not exist, nothing happens.
        /// </summary>
        /// <returns>True if successful, otherwise false</returns>
        public static bool ReplaceEvent(this EMEVD evd, EMEVD.Event replaceEvent)
        {
            for (var i = 0; i < evd.Events.Count; i++)
            {
                var eventID = evd.Events[i].ID;
                if (eventID == replaceEvent.ID)
                {
                    evd.Events[i] = replaceEvent;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replaces an existing event with a replacement event.
        /// If it does not exist, the event is added instead.
        /// </summary>
        /// <returns>True if the event was replaced, false if the event was added normally.</returns>
        public static bool AddReplaceEvent(this EMEVD evd, EMEVD.Event newEvent)
        {
            for (var i = 0; i < evd.Events.Count; i++)
            {
                var eventID = evd.Events[i].ID;
                if (eventID == newEvent.ID)
                {
                    evd.Events[i] = newEvent;
                    return true;
                }
            }

            evd.Events.Add(newEvent);
            return false;
        }

        /// <summary>
        /// Adds and initializes event.
        /// </summary>
        public static void AddInitEvent(EMEVD.Event newEvent, EMEVD evd, EMEVD.Event constructor = null, List<object> parms = null)
        {
            if (constructor == null)
                InitEvent(newEvent.ID, evd, parms);
            else
                InitEvent(newEvent.ID, constructor, parms);

            if (!evd.Events.Any(e => e.ID == newEvent.ID))
            {
                evd.Events.Add(newEvent);
            }
        }

        /// <summary>
        /// Makes the event with the specified ID non-functional.
        /// </summary>
        /// </summary>
        /// <returns>True if the event was found and cleansed, otherwise false.</returns>
        public static bool CleanseEvent(this EMEVD evd, uint eventID, bool preventEventEnd)
        {
            foreach (var evt in evd.Events)
            {
                if (evt.ID == eventID)
                {
                    //evt.Instructions.Clear(); // Can't do this because it fucks with args
                    evt.Parameters.Clear(); // sure hope event initializers dont cause issues because of this!
                    if (preventEventEnd)
                    {
                        /*
                        IfConditionGroup Instruction 0[00]
                        IfConditionGroup(
                            sbyte<ConditionGroup> resultConditionGroup, 
                            byte<ConditionState> desiredConditionGroupState, 
                            sbyte<ConditionGroup> targetConditionGroup)
                        */
                        EMEVD.Instruction cond = new();
                        cond.Bank = 0000;
                        cond.ID = 00;
                        cond.PackArgs(new List<object> { (sbyte)0, (byte)0, (sbyte)-1 }); // MAIN, FAIL, OR_01 (condition never continues)
                        evt.Instructions.Insert(0, cond);
                    }
                    else
                    {
                        /*
                        EndUnconditionally Instruction 1000[04]
                        EndUnconditionally(
                            byte < EventEndType > executionEndType)
                        */
                        EMEVD.Instruction cond = new();
                        cond.Bank = 1000;
                        cond.ID = 04;
                        cond.PackArgs(new List<object> { (byte)0 });
                        evt.Instructions.Insert(0, cond);
                    }

                    return true;
                }
            }

            return false;
        }

        public static void MergeDonorEvd(EMEVD targetEvd, bool replaceNormalEvents, bool autoInitialize, string? donorEvdPath = null)
        {
            if (string.IsNullOrEmpty(donorEvdPath))
            {
                donorEvdPath = $@"{UtilFile.GetWorkingDirectory()}\Resources\donor.emevd.dcx";
            }
            EMEVD donorEvd = EMEVD.Read(donorEvdPath);

            var con = targetEvd.Events.First(e => e.ID == 0);

            foreach (var ev in donorEvd.Events)
            {
                if (ev.ID == 0 || ev.ID == 50)
                {
                    // Constructor and Preconstructor. These will be merged instead.
                    var constructor = targetEvd.Events.First(e => e.ID == ev.ID);
                    constructor.Instructions.InsertRange(0, ev.Instructions);
                    continue;
                }

                var origIndex = targetEvd.Events.FindIndex(0, e => e.ID == ev.ID);
                if (origIndex == -1)
                {
                    targetEvd.Events.Add(ev);
                    if (autoInitialize)
                    {
                        InitEvent(ev.ID, con);
                    }
                    continue;
                }
                else
                {
                    // Event with that ID already exists
                    if (replaceNormalEvents)
                    {
                        targetEvd.Events[origIndex] = ev;
                        continue;
                    }
                    throw new Exception($"Donor event ID {ev.ID} is already in use.");
                }
            }
        }

        public static Dictionary<uint, EMEVD.Event> GetDonorEvents(string? donorEvdPath = null)
        {
            if (string.IsNullOrEmpty(donorEvdPath))
            {
                donorEvdPath = $@"{UtilFile.GetWorkingDirectory()}\Resources\donor.emevd.dcx";
            }
            Dictionary<uint, EMEVD.Event> events = new();
            EMEVD evd = EMEVD.Read(donorEvdPath);
            foreach (var e in evd.Events)
            {
                events.Add((uint)e.ID, e);
            }
            return events;
        }

        public static void AddBasePlayerSpEffects(int[] spEffectIDs, EMEVD emevd)
        {
            var constructor = emevd.Events.Find(e => e.ID == 0)!;

            for (var i = 0; i < spEffectIDs.Length; i++)
            {
                var bytes = new byte[8];
                Array.Copy(BitConverter.GetBytes(10000), 0, bytes, 0, 4);
                Array.Copy(BitConverter.GetBytes(spEffectIDs[i]), 0, bytes, 4, 4);
                constructor.Instructions.Insert(0, new(2004, 08, bytes)); // SetSpEffect Instruction 2004[08]
            }
        }

        public static EMEVD.Instruction CmdSetFlag(int flagId, bool setFlagState)
        {
            /*
            SetEventFlag Instruction 2003[02]
            SetEventFlag(
                int eventFlagId, 
                byte<ONOFF> flagState)
                */
            byte flag = (byte)0;
            if (setFlagState)
                flag = (byte)1;
            List<object> args = new() { flagId, flag };
            return new(2003, 02, args);
        }

        public static void AddOneOffLineSkipper(List<EMEVD.Instruction> instrs, int skipFlag)
        {
            for (var i = 0; i < instrs.Count; i += byte.MaxValue)
            {
                // Only 255 lines can be skipped per instruction, so loop as many times as needed.
                /*
                SkipIfEventFlag Instruction 1003[01]
                SkipIfEventFlag(
                    byte numberOfSkippedLines, 
                    byte<ONOFF> desiredFlagState, 
                    byte<TargetEventFlagType> targetEventFlagType, 
                    int targetEventFlagId)
                Condition function: EventFlag
                    */
                List<object> args = new() { (byte)Math.Min(byte.MaxValue, instrs.Count - i), (byte)1, (byte)1, skipFlag };
                EMEVD.Instruction lineSkipper = new(1003, 01, args);
                instrs.Insert(i, lineSkipper);
                i++; // skipper instruction was added, so index needs to increase
            }

            // Loops are finished, add one more skipper for the flag setter and we're done.

            /*
            SkipIfEventFlag Instruction 1003[01]
            SkipIfEventFlag(
                byte numberOfSkippedLines, 
                byte<ONOFF> desiredFlagState, 
                byte<TargetEventFlagType> targetEventFlagType, 
                int targetEventFlagId)
            Condition function: EventFlag
                */
            List<object> args55 = new() { (byte)(1), (byte)1, (byte)1, skipFlag };
            EMEVD.Instruction lineSkipper55 = new(1003, 01, args55);
            instrs.Add(lineSkipper55);
            /*
            SetEventFlag Instruction 2003[02]
            SetEventFlag(
                int eventFlagId, 
                byte<ONOFF> flagState)
                */
            List<object> args2 = new() { skipFlag, (byte)1 };
            EMEVD.Instruction setFlag = new(2003, 02, args2);
            instrs.Add(setFlag);
        }

        public static List<EMEVD.Instruction> CmdSetFlagsOnce(IEnumerable<int> flagIds, int skipFlag)
        {
            List<EMEVD.Instruction> instrs = new();

            foreach (var flag in flagIds)
            {
                instrs.Add(CmdSetFlag(flag, true));
            }

            AddOneOffLineSkipper(instrs, skipFlag);
            return instrs;
        }
    }
}
