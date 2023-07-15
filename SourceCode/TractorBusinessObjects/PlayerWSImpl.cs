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

        public void CutCardShoeCards()
        {
            var args = new List<object>() { "dummy" };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_CutCardShoeCards, "", args);
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

        public void NotifyGameState(GameState gameState)
        {
            this.NotifyGameStateByType(gameState, "");
        }

        public void NotifyGameStateByType(GameState gameState,string gameStateType)
        {
            var args = new List<object>() { gameState, gameStateType };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyGameState, "", args);
        }

        public void NotifyCurrentHandState(CurrentHandState currentHandState)
        {
            this.NotifyCurrentHandStateByType(currentHandState, "");
        }

        public void NotifyCurrentHandStateByType(CurrentHandState currentHandState, string handStateType)
        {
            var args = new List<object>() { currentHandState, handStateType };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyCurrentHandState, "", args);
        }

        public void NotifyCurrentTrickState(CurrentTrickState currentTrickState)
        {
            this.NotifyCurrentTrickStateByType(currentTrickState, "");
        }

        public void NotifyCurrentTrickStateByType(CurrentTrickState currentTrickState, string trickStateType)
        {
            var args = new List<object>() { currentTrickState, trickStateType };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyCurrentTrickState, "", args);
        }

        public void NotifyDumpingValidationResult(ShowingCardsValidationResult result)
        {
            var args = new List<object>() { result };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyDumpingValidationResult, "", args);
        }

        public void NotifyEmoji(string playerID, int emojiType, int emojiIndex, bool isCenter, string msgString, bool noSpeaker)
        {
            var args = new List<object>() { playerID, emojiType, emojiIndex, isCenter, msgString, noSpeaker };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyEmoji, "", args);
        }

        public void NotifyGameHall(List<RoomState> roomStates, List<string> names)
        {
            var args = new List<object>() { roomStates, names };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyGameHall, "", args);
        }

        public void NotifyOnlinePlayerList(string playerID, bool isJoining)
        {
            var args = new List<object>() { isJoining };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyOnlinePlayerList, playerID, args);
        }

        public void NotifyDaojuInfo(DaojuInfo daojuInfo, bool updateQiandao, bool updateSkin)
        {
            var args = new List<object>() { daojuInfo, updateQiandao, updateSkin };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyDaojuInfo, "", args);
        }

        public void NotifyGameRoomPlayerList(string playerID, bool isJoining, string roomName)
        {
            var args = new List<object>() { isJoining, roomName };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyGameRoomPlayerList, playerID, args);
        }

        public void NotifyMessage(string[] msg)
        {
            var args = new List<object>() { msg };
            string messageType = WebSocketObjects.WebSocketMessageType_NotifyMessage;
            if (msg != null && msg.Length == 0)
            {
                messageType = WebSocketObjects.WebSocketMessageType_NotifyPing;
            }
            NotifyFinalHandler(messageType, "", args);
        }

        public void NotifyReplayState(ReplayEntity replayState)
        {
            var args = new List<object>() { replayState };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyReplayState, "", args);
        }

        public void NotifyRoomSetting(RoomSetting roomSetting, bool showMessage)
        {
            var args = new List<object>() { roomSetting,showMessage };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyRoomSetting, "", args);
        }

        public void NotifyShowAllHandCards()
        {
        }

        public void NotifyStartTimer(int timerLength, string playerID)
        {
            var args = new List<object>() { timerLength, playerID };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyStartTimer, "", args);
        }

        public void NotifyTryToDumpResult(ShowingCardsValidationResult result)
        {
            var args = new List<object>() { result };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyTryToDumpResult, "", args);
        }

        public void NotifySgcsPlayerUpdated(string content)
        {
            var args = new List<object>() { content };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifySgcsPlayerUpdated, "", args);
        }

        public void NotifyCreateCollectStar(WebSocketObjects.SGCSState state)
        {
            var args = new List<object>() { state };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyCreateCollectStar, "", args);
        }

        public void NotifyEndCollectStar(WebSocketObjects.SGCSState state)
        {
            var args = new List<object>() { state };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyEndCollectStar, "", args);
        }

        public void NotifyGrabStar(int playerIndex, int starIndex)
        {
            var args = new List<object>() { playerIndex, starIndex };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyGrabStar, "", args);
        }

        public void NotifyUpdateGobang(WebSocketObjects.SGGBState state)
        {
            var args = new List<object>() { state };
            NotifyFinalHandler(WebSocketObjects.WebSocketMessageType_NotifyUpdateGobang, "", args);
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
        public void Close()
        {
            try
            {
                this.Socket.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
