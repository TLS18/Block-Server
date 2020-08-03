using System;
using System.Collections.Generic;
using System.Text;

//玩家消息处理类
namespace Server
{
    class HandlePlayerMsg
    {
        //创建角色
        public void MsgCreatePlayer(Player player,ProtocolBase protoBase)
        {
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protoBase;
            string protoName = protocol.GetString(start, ref start);
            string Cname = protocol.GetString(start, ref start);
            Console.WriteLine("[收到创建角色协议]" + player.id);
            //构建返回协议
            protocol = new ProtocolBytes();
            protocol.AddString("CreatePlayer");
            PlayerData playerData = new PlayerData
            {
                name = Cname
            };
            if (DataMgr.instance.CreatePlayer(player.id,playerData))
            {
                protocol.AddInt(0);
                player.conn.player.data = playerData;
                Serv.instance.handlePlayerEvent.OnLogin(player);
            }
            else
            {
                protocol.AddInt(-1);
            }
            player.Send(protocol);
        }


        //获取名字
        public void MsgGetName(Player player,ProtocolBase protoBase)
        {
            ProtocolBytes protocol = (ProtocolBytes)protoBase;
            protocol.AddString("GetName");
            protocol.AddString(player.data.name);
            player.Send(protocol);
            
        }

        //设置名字
        public void MsgEditName(Player player,ProtocolBase protocolBase)
        {
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protocolBase;
            string protoName = protocol.GetString(start, ref start);
            string Cname = protocol.GetString(start, ref start);
            //处理
            player.data.name = Cname;
            protocol = new ProtocolBytes();
            protocol.AddString("EditName");
            if (DataMgr.instance.SavePlayer(player))
            {
                protocol.AddInt(0);
            }
            else
            {
                protocol.AddInt(-1);
            }
            player.Send(protocol);

        }

        //获取玩家列表
        public void MsgGetList(Player player,ProtocolBase protoBase)
        {
            Scene.instance.SendPlayerList(player);
        }

        public void MsgUpdateInfo(Player player,ProtocolBase protoBase)
        {
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protoBase;
            string protoName = protocol.GetString(start, ref start);
            float x = protocol.GetFloat(start, ref start);
            float y = protocol.GetFloat(start, ref start);
            float z = protocol.GetFloat(start, ref start);
            float xScale = protocol.GetFloat(start, ref start);
            int animInfo = protocol.GetInt(start, ref start);
            string name = player.data.name;
            Scene.instance.UpdateInfo(player.id, x, y, z, xScale, animInfo, name);
            //广播
            ProtocolBytes protocolRet = new ProtocolBytes();
            protocolRet.AddString("UpdateInfo");
            protocolRet.AddString(player.id);
            protocolRet.AddFloat(x);
            protocolRet.AddFloat(y);
            protocolRet.AddFloat(z);
            protocolRet.AddFloat(xScale);
            protocolRet.AddInt(animInfo);
            protocolRet.AddString(name);
            Serv.instance.Broadcast(protocolRet);
        }

        public void MsgSendChat(Player player,ProtocolBase protocolBase)
        {
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protocolBase;
            string protoName = protocol.GetString(start, ref start);
            string msg = protocol.GetString(start, ref start);
            Console.WriteLine(msg);
            ProtocolBytes protocolBytes = new ProtocolBytes();
            protocolBytes.AddString("SendChat");
            if (DataMgr.instance.SaveChat(player.id, msg))
            {
                protocolBytes.AddInt(0);
                player.Send(protocolBytes);
                ProtocolBytes protocolRet = new ProtocolBytes();
                protocolRet.AddString("PlayerChat");
                protocolRet.AddString(player.id);
                protocolRet.AddString(msg);
                Serv.instance.Broadcast(protocolRet);
            }
            else
            {
                protocolBytes.AddInt(-1);
            }
        }
    }
}
