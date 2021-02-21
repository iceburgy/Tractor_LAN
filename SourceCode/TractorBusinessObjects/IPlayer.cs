using System.Collections.Generic;
using System.ServiceModel;

namespace Duan.Xiugang.Tractor.Objects
{
    public interface IPlayer
    {
        CurrentHandState CurrentHandState { get; set; }
        string PlayerId { get; set; }
        int Rank { get; set; }
        void ClearAllCards();
        void Ready();
        void ReadyToStart();
        void Quit();
        void ShowCards(List<int> cards);
        void ExposeTrump(TrumpExposingPoker trumpExposingPoker, Suit trump);

        //从新开始打
        [OperationContract(IsOneWay = true)]
        void StartGame();

        //有玩家退出，重新组队
        [OperationContract(IsOneWay = true)]
        void ClearTeamState();

        //房间已满
        [OperationContract(IsOneWay = true)]
        void RoomIsFull(string msg);

        [OperationContract(IsOneWay = true)]
        void GetDistributedCard(int number);

        /// <summary>
        ///     更新玩家状态
        /// </summary>
        /// <param name="currentHandState"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyCurrentHandState(CurrentHandState currentHandState);

        /// <summary>
        ///     更新当前这回合牌的状态
        /// </summary>
        /// <param name="currentTrickState"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyCurrentTrickState(CurrentTrickState currentTrickState);


        /// <summary>
        ///     更新游戏状态
        /// </summary>
        /// <param name="gameState"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyGameState(GameState gameState);

        /// <summary>
        ///     甩牌检查结果
        /// </summary>
        /// <param name="result"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyDumpingValidationResult(ShowingCardsValidationResult result);

        void DiscardCards(int[] cards);
    }
}