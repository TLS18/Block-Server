using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Text;

//玩家数据类，用于保存玩家的信息，如金币，等级等
namespace Server
{
    //可序列化，用于储存在数据库中
    [Serializable]
    public class PlayerData
    {
        public string name;
        public PlayerData()
        {
            name = "Common";
        }
    }
}
