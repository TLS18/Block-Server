using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using MySqlX.Protocol;

//连接类，用于和每一个客户端建立连接
namespace Server {
    public class Conn {
        //缓冲区
        public const int BUFFER_SIZE = 1024;
        public Socket socket;
        //该连接对象是否被使用
        public bool isUse = false;
        public byte[] readBuff;
        public int buffCount = 0;
        //粘包分包处理
        public byte[] lenBytes = new byte[sizeof(UInt32)];
        public Int32 msgLength = 0;
        //心跳时间
        public long lastTickTime = long.MinValue;
        //这个conn对应的player
        public Player player;

        public Conn() {
            readBuff = new byte[BUFFER_SIZE];
        }

        public void Init(Socket socket) {
            this.socket = socket;
            isUse = true;
            buffCount = 0;
            //心跳处理
            lastTickTime = Sys.GetTimeStamp ();
        }

        public int BuffRemain()
        {
            return BUFFER_SIZE - buffCount;
        }
        public string GetAddr() {
            if (!isUse) {
                return "无法获取地址";
            }
            return socket.RemoteEndPoint.ToString();
        }

        public void Close() {
            if (!isUse) return;
            if(player!= null)
            {
                player.Logout();
                return;
            }
            Console.WriteLine("[断开连接]" + GetAddr());
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            isUse = false;
        }

        public void Send(ProtocolBase protocol)
        {
            Serv.instance.Send(this, protocol);
        }
    }
}
