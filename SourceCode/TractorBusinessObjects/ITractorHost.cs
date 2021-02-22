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
        void PlayerIsReady(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerIsReadyToStart(string playerId);

        [OperationContract(IsOneWay = true)]
        void PlayerQuit(string playerId);

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
        void RestoreGameStateFromFile();

        //设置从几打起
        [OperationContract(IsOneWay = true)]
        void SetBeginRank(string beginRankString);

        //甩牌检查
        [OperationContract]
        ShowingCardsValidationResult ValidateDumpingCards(List<int> selectedCards, string playerId);
    }
}