using System;
using System.Collections.Generic;
using System.Text;

//玩家活动处理类
namespace Server
{
    class HandlePlayerEvent
    {
        public void OnLogin(Player player)
        {
            Scene.instance.AddPlayer(player.id);
        }
        
        public void OnLogout(Player player)
        {
            Scene.instance.DelPlayer(player.id);
        }
    }
}
