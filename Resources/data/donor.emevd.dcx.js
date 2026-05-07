// ==EMEVD==
// @docs    ds1-common.emedf.json
// @compress    DCX_DFLT_10000_24_9
// @game    DarkSouls1
// @string    ""
// @linked    []
// @version    3.5
// ==/EMEVD==

// Bonfire Warp Check
Event(51321000, Restart, function() {
    
    /*
    IfEventFlag(OR_01, ON, TargetEventFlagType.EventFlag, 9999); // start warp flag (template)
    
    IfConditionGroup(MAIN, PASS, OR_01);
    
    SkipIfEventFlag(3, OFF, TargetEventFlagType.EventFlag, DoThisWarp) // start warp flag (template)
        $InitializeEvent(-1, 51321001, warpTargetEnt, respawnEnt, warp1, warp2);
        SetEventFlag(DoThisWarp, OFF);
        EndUnconditionally(EventEndType.End);
    */
    
});

// Bonfire Active Warp
$Event(51321001, Default, function(warpTargetEnt, respawnEnt, warp1, warp2) {
    
    ForceAnimationPlayback(10000, 7725, false, false, false); // warp-in anim
    //WaitFor(CharacterHasEventMessage(10000, 40)); // time to warp event message. PTDE lua events hijack it and ruin the custom warp so it's not an option.
    WaitFixedTimeFrames(118); // Time for anim 7725 to turn player completely invisible, but a few frames BEFORE the time to warp event message.
    ForceAnimationPlayback(10000, 8284, false, false, false); // invis and prepping for warp
    SetPlayerRespawnPoint(respawnEnt);
    WarpPlayer(warp1, warp2, warpTargetEnt);
});
