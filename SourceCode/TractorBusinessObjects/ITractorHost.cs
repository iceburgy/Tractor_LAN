using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;

namespace Duan.Xiugang.Tractor.Objects
{
    [ServiceContract(CallbackContract = typeof (IPlayer))]
    public interface ITractorHost
    {
        [OperationContract(IsOneWay = true)]
        void PlayerEnterHall(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerEnterRoom(string playerID, int roomID, int posID);

        [OperationContract(IsOneWay = true)]
        void PlayerIsReadyToStart(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerToggleIsRobot(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerSendEmoji(string playerId, int emojiType, int emojiIndex, bool isCenter);

        [OperationContract(IsOneWay = true)]
        void PlayerExitRoom(string playerId);

        [OperationContract(IsOneWay = true)]
        void MarkPlayerOffline(string playerId);

        [OperationContract(IsOneWay = true)]
        void SpecialEndGameRequest(string playerID);

        [OperationContract(IsOneWay = true)]
        void SpecialEndGame(string playerID, SpecialEndingType endType);

        [OperationContract(IsOneWay = true)]
        void SpecialEndGameDeclined(string playerID);

        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginPlayerQuit(string playerId, AsyncCallback callback, object state);
        string EndPlayerQuit(IAsyncResult ar);

        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginPingHost(string playerId, AsyncCallback callback, object state);
        string EndPingHost(IAsyncResult ar);

        [OperationContract(IsOneWay = true)]
        void PlayerMakeTrump(TrumpExposingPoker trumpExposingPoker, Suit trump, string playerId, int usedShengbi);

        [OperationContract(IsOneWay = true)]
        void StoreDiscardedCards(string playerId, int[] cards);

        //玩家出牌
        [OperationContract(IsOneWay = true)]
        void PlayerShowCards(string playerId, CurrentTrickState currentTrickState);

        //更新玩家分数显示（用于甩牌失败后显示惩罚分数）
        [OperationContract(IsOneWay = true)]
        void RefreshPlayersCurrentHandState(string playerId);

        //继续牌局
        [OperationContract(IsOneWay = true)]
        void ResumeGameFromFile(string playerId);

        //设置从几打起
        [OperationContract(IsOneWay = true)]
        void SetBeginRank(string playerId, string beginRankString);

        //随机组队
        [OperationContract(IsOneWay = true)]
        void TeamUp(string playerId);

        //换座
        [OperationContract(IsOneWay = true)]
        void SwapSeat(string playerId, int offset);

        //旁观：选牌
        [OperationContract(IsOneWay = true)]
        void CardsReady(string playerId, ArrayList myCardIsReady);

        //旁观玩家 by id
        [OperationContract(IsOneWay = true)]
        void ObservePlayerById(string playerId, string observerId, bool skipUpdateGameState);

        //保存房间游戏设置
        [OperationContract(IsOneWay = true)]
        void SaveRoomSetting(string playerId, RoomSetting roomSetting);

        //甩牌检查
        [OperationContract(IsOneWay = true)]
        void ValidateDumpingCards(List<int> selectedCards, string playerId);

        //切牌
        [OperationContract(IsOneWay = true)]
        void PlayerHasCutCards(string playerID, string cutInfo);
    }
}