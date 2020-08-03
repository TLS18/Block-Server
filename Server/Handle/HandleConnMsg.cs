using System;
using System.Collections.Generic;
using System.Text;

//连接消息处理类 
namespace Server
{
    class HandleConnMsg
    {
        //心跳
        public void MsgHeartBeat(Conn conn, ProtocolBase protocolBase)
        {
            conn.lastTickTime = Sys.GetTimeStamp();
            Console.WriteLine("[更新心跳时间]" + conn.GetAddr());
        }

        //注册
        public void MsgRegister(Conn conn, ProtocolBase protoBase)
        {
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protoBase;
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            string strFormat = "[收到注册协议] " + conn.GetAddr();
            Console.WriteLine(strFormat + " 用户名: " + id + " 密码: " + pw);
            //构建返回协议
            protocol = new ProtocolBytes();
            protocol.AddString("Register");
            //注册
            if (DataMgr.instance.Register(id, pw))
            {
                protocol.AddInt(0);
            }
            else
            {
                protocol.AddInt(-1);
            }
            //返回协议给客户端
            conn.Send(protocol);
        }

        //登录
        public void MsgLogin(Conn conn,ProtocolBase protocolBase)
        {
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protocolBase;
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            string strFormat = "[收到登录协议] " + conn.GetAddr();
            Console.WriteLine(strFormat + " 用户名: " + id + " 密码: " + pw);
            //构建返回协议
            protocol = new ProtocolBytes();
            protocol.AddString("Login");
            //验证
            if (!DataMgr.instance.CheckPassWord(id, pw))
            {
                protocol.AddInt(-1);
                conn.Send(protocol);
                return;
            }
            //是否已经登录
            ProtocolBytes protocolLogout = new ProtocolBytes();
            protocolLogout.AddString("Logout");
            if (!Player.KickOff(id, protocolLogout))
            {
                protocol.AddInt(-2);
                conn.Send(protocol);
                return;
            }
            //获取玩家数据
            PlayerData playerData = DataMgr.instance.GetPlayerData(id);
            conn.player = new Player(id, conn);
            conn.player.data = playerData;
            if (playerData == null)
            {
                protocol.AddInt(-3);
                conn.Send(protocol);
            }
            else
            {
                //事件触发
                Serv.instance.handlePlayerEvent.OnLogin(conn.player);
                protocol.AddInt(0);
                conn.Send(protocol);
            }
            //返回
            return;
        }

        
        //下线
        public void MsgLogout(Conn conn, ProtocolBase protoBase)
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("Logout");
            protocol.AddInt(0);
            if(conn.player == null)
            {
                conn.Send(protocol);
                conn.Close();
            }
            else
            {
                conn.Send(protocol);
                conn.player.Logout();
            }
        }

    }
}
