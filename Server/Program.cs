using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Server {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("启动服务器中");
            DataMgr dataMgr = new DataMgr();
            Scene scene = new Scene();
            Serv serv = new Serv();
            if (dataMgr.Connect())
            {
                serv.proto = new ProtocolBytes();
                serv.Start("127.0.0.1", 1234);
            }
            while (true)
            {
                string str = Console.ReadLine();
                switch (str)
                {
                    case "quit":
                        return;
                    case "print":
                        serv.Print();
                        break;
                }
            }
        }
    }
}
