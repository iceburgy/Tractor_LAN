using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;

namespace Duan.Xiugang.Tractor.Objects
{
    [ServiceContract(CallbackContract = typeof (IPlayer))]
    public interface ITractorHost
    {
        CardsShoe CardsShoe { get; set; }
        Dictionary<string, IPlayer> PlayersProxy { get; set; }

        [OperationContract(IsOneWay = true)]
        void PlayerEnterHall(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerEnterRoom(string playerID, int roomID, int posID);

        [OperationContract(IsOneWay = true)]
        void PlayerIsReadyToStart(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerToggleIsRobot(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerExitRoom(string playerId);

        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginPlayerQuit(string playerId, AsyncCallback callback, object state);
        string EndPlayerQuit(IAsyncResult ar);

        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginPingHost(string playerId, AsyncCallback callback, object state);
        string EndPingHost(IAsyncResult ar);

        [OperationContract(IsOneWay = true)]
        void PlayerMakeTrump(TrumpExposingPoker trumpExposingPoker, Suit trump, string playerId);

        [OperationContract(IsOneWay = true)]
        void StoreDiscardedCards(int[] cards);

        //玩家出牌
        [OperationContract(IsOneWay = true)]
        void PlayerShowCards(CurrentTrickState currentTrickState);

        //更新玩家分数显示（用于甩牌失败后显示惩罚分数）
        [OperationContract(IsOneWay = true)]
        void RefreshPlayersCurrentHandState();

        //读取牌局
        [OperationContract(IsOneWay = true)]
        void RestoreGameStateFromFile(bool restoreCardsShoe);

        //设置从几打起
        [OperationContract(IsOneWay = true)]
        void SetBeginRank(string beginRankString);

        //随机组队
        [OperationContract(IsOneWay = true)]
        void TeamUp();

        //和下家互换座位
        [OperationContract(IsOneWay = true)]
        void MoveToNextPosition(string playerId);

        //旁观：选牌
        [OperationContract(IsOneWay = true)]
        void CardsReady(string playerId, ArrayList myCardIsReady);

        //旁观玩家 by id
        [OperationContract(IsOneWay = true)]
        void ObservePlayerById(string playerId, string observerId);

        //保存房间游戏设置
        [OperationContract(IsOneWay = true)]
        void SaveRoomSetting(RoomSetting roomSetting);

        //甩牌检查
        [OperationContract]
        ShowingCardsValidationResult ValidateDumpingCards(List<int> selectedCards, string playerId);
    }
}