using System;
using System.Collections.Generic;
using System.Text;

//玩家类，对应着连接到服务器的所有玩家
namespace Server
{
    public class Player
    {
        public string id;
        public Conn conn;
        public PlayerData data;
        //临时数据
        public PlayerTempData tempData;
        public Player(string id,Conn conn)
        {
            this.id = id;
            this.conn = conn;
            tempData = new PlayerTempData();
        }

        public void Send(ProtocolBase proto)
        {
            if (conn == null)
            {
                return;
            }
            Serv.instance.Send(conn, proto);
        }

        public static bool KickOff(string id,ProtocolBase proto)
        {
            Conn[] conns = Serv.instance.conns;
            for(int i = 0; i < conns.Length; i++)
            {
                if (conns[i] == null) continue;
                if (!conns[i].isUse) continue;
                if (conns[i].player == null) continue;
                if(conns[i].player.id == id)
                {
                    lock(conns[i].player)
                    {
                        if (proto != null) conns[i].player.Send(proto);
                        return conns[i].player.Logout();
                    }
                }
            }
            return true;
        }

        public bool Logout()
        {
            Serv.instance.handlePlayerEvent.OnLogout(this);
            if (!DataMgr.instance.SavePlayer(this)) return false;
            conn.player = null;
            conn.Close();
            return true;
        }

    }
}
