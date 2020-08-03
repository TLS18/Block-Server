using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Reflection.Metadata;
using Org.BouncyCastle.Crypto.Encodings;
using System.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualBasic;

//服务器管理类,用于管理所有的连接类
namespace Server {
    class Serv {
        public Socket listenfd;
        public Conn[] conns;
        public int maxConn = 50;
        public static Serv instance;
        System.Timers.Timer timer = new System.Timers.Timer(1000);
        //心跳时间
        public long heartBeatTime = 15;
        //协议
        public ProtocolBase proto;
        //消息分发
        public HandleConnMsg handleConnMsg = new HandleConnMsg();
        public HandlePlayerMsg handlePlayerMsg = new HandlePlayerMsg();
        public HandlePlayerEvent handlePlayerEvent = new HandlePlayerEvent();
        
        public Serv()
        {
            instance = this;
        }
        public int NewIndex() {
            if (conns == null) {
                return -1;
            }
            for (int i = 0; i < conns.Length; i++) {
                if (conns[i] == null)
                {
                    conns[i] = new Conn();
                    return i;
                }
                else if (conns[i].isUse == false)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Start(string host,int post)
        {
            conns = new Conn[maxConn];
            for(int i = 0; i < maxConn; i++)
            {
                conns[i] = new Conn();
            }
            //Socket部分
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Bind部分
            IPAddress ipAdr = IPAddress.Parse(host);
            IPEndPoint ipEp = new IPEndPoint(ipAdr, post);
            listenfd.Bind(ipEp);
            //Listen部分
            listenfd.Listen(maxConn); //最多可容纳的接收数
            //Accept部分
            listenfd.BeginAccept(AcceptCb, null);//非阻塞

            Console.WriteLine("[服务器]正在等待连接..");
            timer.Elapsed += new System.Timers.ElapsedEventHandler(HandleMainTimer); 
            timer.AutoReset = false; //只执行一次
            timer.Enabled = true;
        }

        public void HandleMainTimer(object sender,System.Timers.ElapsedEventArgs e)  //查询心跳的时间周期
        {
            //处理心跳
            HeartBeat();
            timer.Start();
        }

        //心跳协议
        public void HeartBeat()  
        {
            long timeNow = Sys.GetTimeStamp();
            for (int i = 0; i < conns.Length; i++)
            {
                Conn conn = conns[i];
                if (conn == null) continue;
                if (!conn.isUse) continue;
                if (conn.lastTickTime < timeNow - heartBeatTime)
                {
                    Console.WriteLine("[心跳机制触发，断开连接]" + conn.GetAddr());
                    lock (conn) conn.Close();
                }
            }
        }

        private void AcceptCb(IAsyncResult ar)
        {
            try
            {
                Socket socket = listenfd.EndAccept(ar);
                int index = NewIndex();

                if(index < 0)
                {
                    socket.Close();
                    Console.Write("[警告]连接已满");
                }
                else
                {
                    Conn conn = conns[index];
                    conn.Init(socket);
                    string adr = conn.GetAddr();
                    Console.WriteLine("客户端连接[" + adr + "]conn池ID：" + index);
                    conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
                    listenfd.BeginAccept(AcceptCb, null);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("AcceptCb失败：" + e.Message);
            }
        }
        private void ReceiveCb(IAsyncResult ar)
        {
            Conn conn = (Conn)ar.AsyncState;
            lock (conn)
            {
                try
                {
                    int count = conn.socket.EndReceive(ar);
                    if (count <= 0)
                    {
                        return;
                    }
                    conn.buffCount += count;
                    ProcessData(conn);
                    conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
                }
                catch { }
            }
        }

        public void HandleMsg(Conn conn, ProtocolBase protocolBase)
        {
            string name = protocolBase.GetName();
            if (name != "UpdateInfo")
            {
                Console.WriteLine("[收到协议] " + name);
            }
            string methodName = "Msg" + name;
            //连接协议分发
            if (conn.player == null || name == "HeartBeat" || name == "Logout" || name == "Login" || name == "Register")
            {
                MethodInfo mm = handleConnMsg.GetType().GetMethod(methodName);
                if (mm == null)
                {
                    string str = "[警告]HandleMsg没有处理连接方法";
                    Console.WriteLine(str + methodName);
                    return;
                }
                Object[] obj = new object[] { conn, protocolBase };
                Console.WriteLine("[处理连接信息] " + conn.GetAddr() + " : " + name);
                mm.Invoke(handleConnMsg, obj);
            }
            //角色协议分发
            else
            {
                MethodInfo mm = handlePlayerMsg.GetType().GetMethod(methodName);
                if(mm == null)
                {
                    string str = "[警告]HandleMsg没有处理玩家方法";
                    Console.WriteLine(str + methodName);
                    return;
                }
                Object[] obj = new object[] { conn.player, protocolBase };
                if (name != "UpdateInfo")
                {
                    Console.WriteLine("[处理玩家信息] " + conn.player.id + " : " + name);
                }  
                mm.Invoke(handlePlayerMsg, obj);
            }
        }

        //关闭服务端时保存玩家数据
        public void Close() 
        {
            for(int i = 0; i < conns.Length; i++)
            {
                Conn conn = conns[i];
                if (conn == null) continue;
                if (!conn.isUse) continue;
                lock (conn)
                {
                    conn.Close();
                }
            }
        }

        private void ProcessData(Conn conn)
        {
            if(conn.buffCount < sizeof(Int32))
            {
                return;
            }
            //获取消息长度
            Array.Copy(conn.readBuff, conn.lenBytes, sizeof(Int32));
            conn.msgLength = BitConverter.ToInt32(conn.lenBytes, 0);
            //Console.WriteLine("收到长度[" + conn.msgLength + "]");
            if (conn.buffCount < conn.msgLength + sizeof(Int32))
            {
                return;
            }
            ProtocolBase protocol = proto.Decode(conn.readBuff, sizeof(Int32), conn.msgLength);
            HandleMsg(conn, protocol);
            //清除掉已经处理的消息
            int count = conn.buffCount - conn.msgLength - sizeof(Int32);
            Array.Copy(conn.readBuff, sizeof(Int32) + conn.msgLength, conn.readBuff, 0, count);
            conn.buffCount = count;
            //清空缓存区
            if (conn.buffCount > 0)
            {
                ProcessData(conn);  
            }
        }

        public void Send(Conn conn,ProtocolBase protocol)
        {
            byte[] bytes = protocol.Encode();
            byte[] length = BitConverter.GetBytes(bytes.Length);
            byte[] sendbuff = length.Concat(bytes).ToArray();
            try
            {
                conn.socket.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, null, null);
            }
            catch(Exception e)
            {
                Console.WriteLine("[发送消息]" + conn.GetAddr() + " : " + e.Message);
            }
        }

        public void Broadcast(ProtocolBase protocol)
        {
            for(int i = 0; i < conns.Length; i++)
            {
                if (!conns[i].isUse) continue;
                if (conns[i].player == null) continue;
                Send(conns[i],protocol);
            }
        }

        public void Print()
        {
            Console.WriteLine("===服务器登录信息===");
            for(int i = 0;i< conns.Length; i++)
            {
                if (conns[i] == null) continue;
                if (!conns[i].isUse) continue;
                string str = "连接[" + conns[i].GetAddr() + "] ";
                if (conns[i].player != null) 
                { 
                    str += "玩家id: " + conns[i].player.id;
                }
                Console.WriteLine(str);
            }
        }
    }
}
