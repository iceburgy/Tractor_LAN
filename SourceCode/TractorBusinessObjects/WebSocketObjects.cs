using System;
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
        public const string WebSocketMessageType_ObserveNext = "ObserveNext";
        public const string WebSocketMessageType_ExitRoom = "ExitRoom";
        public const string WebSocketMessageType_PlayerMakeTrump = "PlayerMakeTrump";
        public const string WebSocketMessageType_StoreDiscardedCards = "StoreDiscardedCards";
        public const string WebSocketMessageType_PlayerShowCards = "PlayerShowCards";
        public const string WebSocketMessageType_ValidateDumpingCards = "ValidateDumpingCards";
        public const string WebSocketMessageType_CardsReady = "CardsReady";
        public const string WebSocketMessageType_ResumeGameFromFile = "ResumeGameFromFile";
        public const string WebSocketMessageType_RandomSeat = "RandomSeat";
        
        public const string WebSocketMessageType_NotifyGameHall = "NotifyGameHall";
        public const string WebSocketMessageType_NotifyMessage = "NotifyMessage";
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
    }
}
