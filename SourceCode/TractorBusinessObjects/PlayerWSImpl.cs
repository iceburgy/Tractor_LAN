using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json;

namespace Duan.Xiugang.Tractor.Objects
{
    public class PlayerWSImpl : IPlayer
    {
        public IWebSocketConnection Socket;
        public CurrentHandState CurrentHandState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string PlayerId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Rank { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PlayerWSImpl(IWebSocketConnection s)
        {
            this.Socket = s;
        }

        public void NotifyFinalHandler(string messageType, string playerID, List<object> args)
        {
            string body = JsonConvert.SerializeObject(args);
            WebSocketObjects.WebSocketMessage messageObj = new WebSocketObjects.WebSocketMessage();
            messageObj.messageType = messageType;
            messageObj.playerID = playerID;
            messageObj.content = body;
            this.Socket.Send(JsonConvert.SerializeObject(messageObj));
        }

        public void ClearAllCards()
        {
        }

        public string CutCardShoeCards()
        {
            return "";
        }

        public void ExitRoom(string playerID)
        {
        }

        public void ExposeTrump(TrumpExposingPoker trumpExposingPoker, Suit trump)
        {
        }

        public void GetDistributedCard(int number)
        {
            var args = new List<object>() { number };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_GetDistributedCard, "", args);
        }

        public void NotifyCardsReady(ArrayList myCardIsReady)
        {
            var args = new List<object>() { myCardIsReady };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyCardsReady, "", args);
        }

        public void NotifyCurrentHandState(CurrentHandState currentHandState)
        {
            var args = new List<object>() { currentHandState };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyCurrentHandState, "", args);
        }

        public void NotifyCurrentTrickState(CurrentTrickState currentTrickState)
        {
            var args = new List<object>() { currentTrickState };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyCurrentTrickState, "", args);
        }

        public void NotifyDumpingValidationResult(ShowingCardsValidationResult result)
        {
            var args = new List<object>() { result };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyDumpingValidationResult, "", args);
        }

        public void NotifyEmoji(string playerID, int emojiType, int emojiIndex, bool isCenter)
        {
        }

        public void NotifyGameHall(List<RoomState> roomStates, List<string> names)
        {
            var args = new List<object>() { roomStates, names };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyGameHall, "", args);
        }

        public void NotifyGameState(GameState gameState)
        {
            var args = new List<object>() { gameState };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyGameState, "", args);
        }

        public void NotifyMessage(string[] msg)
        {
            var args = new List<object>() { msg };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyMessage, "", args);
        }

        public void NotifyReplayState(ReplayEntity replayState)
        {
        }

        public void NotifyRoomSetting(RoomSetting roomSetting, bool showMessage)
        {
            var args = new List<object>() { roomSetting,showMessage };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyRoomSetting, "", args);
        }

        public void NotifyShowAllHandCards()
        {
        }

        public void NotifyStartTimer(int timerLength)
        {
            var args = new List<object>() { timerLength };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyStartTimer, "", args);
        }

        public void NotifyTryToDumpResult(ShowingCardsValidationResult result)
        {
            var args = new List<object>() { result };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyTryToDumpResult, "", args);
        }

        public void PlayerEnterRoom(string playerID, int roomID, int posID)
        {
        }

        public void Quit()
        {
        }

        public void ReadyToStart()
        {
        }

        public void SpecialEndGameShouldAgree()
        {
        }

        public void StartGame()
        {
            var args = new List<object>();
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_StartGame, "", args);
        }
    }
}
