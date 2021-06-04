using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Duan.Xiugang.Tractor.Objects
{
    [DataContract]
    public class RoomState
    {
        [DataMember]
        public int RoomID;
        [DataMember]
        public string RoomName;
        [DataMember]
        public GameState CurrentGameState;
        [DataMember]
        public CurrentHandState CurrentHandState;
        [DataMember]
        public CurrentTrickState CurrentTrickState;

        public RoomSetting roomSetting;

        public RoomState(int roomID, string roomName)
        {
            RoomID = roomID;
            RoomName = roomName;
            CurrentGameState = new GameState();
            CurrentHandState = new CurrentHandState(this.CurrentGameState);
            CurrentTrickState = new CurrentTrickState();
            roomSetting = new RoomSetting();
        }
    }
}