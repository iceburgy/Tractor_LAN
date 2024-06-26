﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    public class WebSocketObjects
    {
        public const string WebSocketMessageType_PlayerEnterHall = "PlayerEnterHall";
        public const string WebSocketMessageType_PlayerEnterRoom = "PlayerEnterRoom";
        public const string WebSocketMessageType_ReadyToStart = "ReadyToStart";
        public const string WebSocketMessageType_ToggleIsRobot = "ToggleIsRobot";
        public const string WebSocketMessageType_ToggleIsQiangliang = "ToggleIsQiangliang";
        public const string WebSocketMessageType_ObserveNext = "ObserveNext";
        public const string WebSocketMessageType_ExitRoom = "ExitRoom";
        public const string ExitRoom_REQUEST_TYPE_BootPlayer = "BootPlayer";

        public const string WebSocketMessageType_ExitAndEnterRoom = "ExitAndEnterRoom";
        public const string WebSocketMessageType_ExitAndObserve = "ExitAndObserve";
        public const string WebSocketMessageType_PlayerMakeTrump = "PlayerMakeTrump";
        public const string WebSocketMessageType_StoreDiscardedCards = "StoreDiscardedCards";
        public const string WebSocketMessageType_PlayerShowCards = "PlayerShowCards";
        public const string WebSocketMessageType_ValidateDumpingCards = "ValidateDumpingCards";
        public const string WebSocketMessageType_CardsReady = "CardsReady";
        public const string WebSocketMessageType_ResumeGameFromFile = "ResumeGameFromFile";
        public const string WebSocketMessageType_RandomSeat = "RandomSeat";
        public const string WebSocketMessageType_SwapSeat = "SwapSeat";
        public const string WebSocketMessageType_SendEmoji = "SendEmoji";
        public const string WebSocketMessageType_SendBroadcast = "SendBroadcast";
        public const string WebSocketMessageType_BuyNoDongtuUntil = "BuyNoDongtuUntil";
        public const string WebSocketMessageType_PlayerHasCutCards = "PlayerHasCutCards";
        public const string WebSocketMessageType_SgcsPlayerUpdated = "SgcsPlayerUpdated";
        public const string WebSocketMessageType_CreateCollectStar = "CreateCollectStar";
        public const string WebSocketMessageType_GrabStar = "GrabStar";
        public const string WebSocketMessageType_EndCollectStar = "EndCollectStar";
        public const string WebSocketMessageType_SaveRoomSetting = "SaveRoomSetting";
        public const string WebSocketMessageType_PlayerQiandao = "PlayerQiandao";
        public const string WebSocketMessageType_UsedShengbi = "UsedShengbi";
        public const string WebSocketMessageType_BuyUseSkin = "BuyUseSkin";
        public const string WebSocketMessageType_UpdateGobang = "UpdateGobang";
        public const string WebSocketMessageType_SendJoinOrQuitYuezhan = "SendJoinOrQuitYuezhan";
        public const string WebSocketMessageType_SendAwardOnlineBonus = "SendAwardOnlineBonus";

        public const string WebSocketMessageType_NotifyGameHall = "NotifyGameHall";
        public const string WebSocketMessageType_NotifyOnlinePlayerList = "NotifyOnlinePlayerList";
        public const string WebSocketMessageType_NotifyDaojuInfo = "NotifyDaojuInfo";
        public const string WebSocketMessageType_NotifyGameRoomPlayerList = "NotifyGameRoomPlayerList";
        public const string WebSocketMessageType_NotifyMessage = "NotifyMessage";
        public const string WebSocketMessageType_NotifyPing = "NotifyPing";
        public const string WebSocketMessageType_NotifyPong = "NotifyPong";
        public const string WebSocketMessageType_NotifyRoomSetting = "NotifyRoomSetting";
        public const string WebSocketMessageType_NotifyGameState = "NotifyGameState";
        public const string WebSocketMessageType_NotifyCurrentHandState = "NotifyCurrentHandState";
        public const string WebSocketMessageType_NotifyCurrentTrickState = "NotifyCurrentTrickState";
        public const string WebSocketMessageType_StartGame = "StartGame";
        public const string WebSocketMessageType_GetDistributedCard = "GetDistributedCard";
        public const string WebSocketMessageType_NotifyCardsReady = "NotifyCardsReady";
        public const string WebSocketMessageType_NotifyDumpingValidationResult = "NotifyDumpingValidationResult";
        public const string WebSocketMessageType_NotifyTryToDumpResult = "NotifyTryToDumpResult";
        public const string WebSocketMessageType_NotifyStartTimer = "NotifyStartTimer";
        public const string WebSocketMessageType_NotifyEmoji = "NotifyEmoji";
        public const string WebSocketMessageType_CutCardShoeCards = "CutCardShoeCards";
        public const string WebSocketMessageType_NotifyReplayState = "NotifyReplayState";
        public const string WebSocketMessageType_NotifySgcsPlayerUpdated = "NotifySgcsPlayerUpdated";
        public const string WebSocketMessageType_NotifyCreateCollectStar = "NotifyCreateCollectStar";
        public const string WebSocketMessageType_NotifyGrabStar = "NotifyGrabStar";
        public const string WebSocketMessageType_NotifyEndCollectStar = "NotifyEndCollectStar";
        public const string WebSocketMessageType_NotifyUpdateGobang = "NotifyUpdateGobang";

        public const string SmallGameName_CollectStar = "CatchStar";
        public const string SmallGameName_Gobang= "Gobang";

        [Serializable]
        public class WebSocketMessage
        {
            [DataMember]
            public string messageType;
            [DataMember]
            public string playerID;
            [DataMember]
            public string content;
        }

        [Serializable]
        public class PlayerEnterRoomWSMessage
        {
            [DataMember]
            public int roomID;
            [DataMember]
            public int posID;
        }

        [Serializable]
        public class SGCSBomb
        {
            [DataMember]
            public int X;
            [DataMember]
            public int Y;
            [DataMember]
            public int VelX;
        }

        [Serializable]
        public class SGCSDude
        {
            [DataMember]
            public string PlayerId = "";
            [DataMember]
            public int X = -1;
            [DataMember]
            public int Y = -1;
            [DataMember]
            public double Bounce = 0.2;
            [DataMember]
            public int Score = 0;
            [DataMember]
            public string Tint = "";
            [DataMember]
            public bool Enabled = false;
        }

        [Serializable]
        public class SGCSStar
        {
            [DataMember]
            public int X;
            [DataMember]
            public int Y;
            [DataMember]
            public double Bounce;
            [DataMember]
            public bool Enabled;
        }

        [Serializable]
        public class SGCSState
        {
            [DataMember]
            public string PlayerId;
            [DataMember]
            public bool IsGameOver;
            [DataMember]
            public int Score;
            [DataMember]
            public int Stage;
            [DataMember]
            public int ScoreHigh;
            [DataMember]
            public int StageHigh;
            [DataMember]
            public string ScreenshotHigh;
            [DataMember]
            public List<SGCSDude> Dudes;
            [DataMember]
            public List<SGCSStar> Stars;
            [DataMember]
            public List<SGCSBomb> Bombs;
        }

        [Serializable]
        public class SGGBState
        {
            [DataMember]
            public string PlayerId1;
            [DataMember]
            public string PlayerId2;
            [DataMember]
            public string PlayerIdMoved;
            [DataMember]
            public string PlayerIdMoving;
            [DataMember]
            public string PlayerIdWinner;

            // create, join, move, quit, restart
            [DataMember]
            public string GameAction;
            // created, joined, moved, over, restarted
            [DataMember]
            public string GameStage;

            [DataMember]
            public int[] LastMove = new int[] { -1, -1 };
            [DataMember]
            public int[] CurMove = new int[] { -1, -1 };
            [DataMember]
            public int[,] ChessBoard = new int[CommonMethods.gobangBoardSize, CommonMethods.gobangBoardSize];
        }
    }
}
