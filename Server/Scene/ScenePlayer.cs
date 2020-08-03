using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

//场景玩家类，记录场景中每个玩家的位置，姓名等
namespace Server
{
    public class ScenePlayer
    {
        public string id;
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public float xScale = 0;
        public int animInfo = 0;
        public string name = "";
    }
}
