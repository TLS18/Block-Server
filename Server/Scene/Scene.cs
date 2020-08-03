using System;
using System.Collections.Generic;
using System.Text;

//场景类，用于管理服务器的场景信息
namespace Server
{
    public class Scene
    {
        public static Scene instance;   //这里只涉及一个场景，所以弄成单例
        public Scene()
        {
            instance = this;
        }

        List<ScenePlayer> list = new List<ScenePlayer>();

        private ScenePlayer GetScenePlayer(string id)
        {
            for(int i = 0; i < list.Count; i++)
            {
                if (list[i].id == id)
                    return list[i];
            }
            return null;
        }

        public void AddPlayer(string id)
        {
            lock (list)
            {
                ScenePlayer p = new ScenePlayer();
                p.id = id;
                list.Add(p);
                Console.WriteLine("[场景管理]添加玩家");
            }
        }

        public void DelPlayer(string id)
        {
            lock (list)
            {
                ScenePlayer p = GetScenePlayer(id);
                if(p!=null)
                {
                    list.Remove(p);
                    Console.WriteLine("[场景管理]删除玩家");
                }
            }
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("PlayerLeave");
            protocol.AddString(id);
            Serv.instance.Broadcast(protocol);
        }

        public void SendPlayerList(Player player)
        {
            Console.WriteLine("发送玩家信息");
            int count = list.Count;
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("GetList");
            protocol.AddInt(count);
            for(int i = 0; i < count; i++)
            {
                ScenePlayer p = list[i];
                protocol.AddString(p.id);
                protocol.AddFloat(p.x);
                protocol.AddFloat(p.y);
                protocol.AddFloat(p.z);
                protocol.AddFloat(p.xScale);
                protocol.AddInt(p.animInfo);
                protocol.AddString(p.name);
            }
            player.Send(protocol);
        }

        public void UpdateInfo(string id,float x,float y,float z,float xScale,int animInfo,string name)
        {
            int count = list.Count;
            ProtocolBytes bytes = new ProtocolBytes();
            ScenePlayer p = GetScenePlayer(id);
            if (p == null)
            {
                return;
            }
            p.x = x;
            p.y = y;
            p.z = z;
            p.xScale = xScale;
            p.animInfo = animInfo;
            p.name = name;
        }
    }
}
