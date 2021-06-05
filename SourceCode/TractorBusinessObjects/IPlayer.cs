using System.Collections;
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
        void PlayerEnterRoom(string playerID, int roomID, int posID);
        void ReadyToStart();
        void ExitRoom(string playerID);
        void Quit();
        void ShowCards(List<int> cards);
        void ExposeTrump(TrumpExposingPoker trumpExposingPoker, Suit trump);

        //从新开始打
        [OperationContract(IsOneWay = true)]
        void StartGame();

        //广播消息
        [OperationContract(IsOneWay = true)]
        void NotifyMessage(string[] msg);

        //广播倒计时
        [OperationContract(IsOneWay = true)]
        void NotifyStartTimer(int timerLength);

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
        ///     更新房间游戏设置状态
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyRoomSetting(RoomSetting roomSetting, bool isRoomSettingModified);

        ///     更新游戏游戏大厅状态
        [OperationContract(IsOneWay = true)]
        void NotifyGameHall(List<RoomState> roomStates, List<string> names);

        /// <summary>
        ///     甩牌检查结果
        /// </summary>
        /// <param name="result"></param>
        [OperationContract(IsOneWay = true)]
        void NotifyDumpingValidationResult(ShowingCardsValidationResult result);

        //旁观：选牌
        [OperationContract(IsOneWay = true)]
        void NotifyCardsReady(ArrayList myCardIsReady);

        /// <summary>
        ///     所有人展示手牌
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyShowAllHandCards();

        void DiscardCards(int[] cards);
    }
}