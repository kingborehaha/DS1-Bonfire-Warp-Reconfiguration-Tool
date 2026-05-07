# -*- coding: utf-8 -*-
def t550055_1():
    """State 0"""
    Label('L0')
    while True:
        """State 3000"""
        ShowShopMessage(TalkOptionsType.Old, False, False)
        
        # Template command: AddTalkListData for each bonfire warp is added via code
        AddTalkListData(1, 15000005, -1)
        
        def ExitPause():
            ClearTalkListData()
        if (CompareBonfireState(0) or IsPlayerDead() or HasPlayerBeenAttacked() or (IsTalkingToSomeoneElse()
            or CheckSelfDeath() or IsCharacterDisabled() or IsClientPlayer() or GetRelativeAngleBetweenPlayerAndSelf()
            > 180 or GetDistanceToPlayer() > 8)):
            """State 3350"""
            Label('L1')
            ForceEndTalk(0)
            ClearTalkProgressData()
            CloseShopMessage()
            DebugEvent('リスト強制開放')
            EndBonfireKindleAnimLoop()
            ClearTalkDisabledState()
            Goto('L0')
        elif GetTalkListEntryResult() == 0 or GetTalkListEntryResult() == 1:
            """State 3360"""
            Goto('L1')
        elif GetTalkListEntryResult() == 9999:
            """State 3250"""
            # TEMPLATE ROW
            SetEventFlag(22222, FlagState.On)
            Goto('L500')
    '''State 3150'''
    Label('L500')
    ForceEndTalk(0)
    ClearTalkProgressData()
    CloseShopMessage()
    #ClearTalkDisabledState()