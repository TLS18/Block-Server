using System;
using System.Collections.Generic;
using System.Text;

//用于获取时间戳
namespace Server
{
    class Sys 
    {
        public static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
    }
}
