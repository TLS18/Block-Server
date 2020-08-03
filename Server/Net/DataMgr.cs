using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

//数据库操作类，封装了所有需要对数据库进行操作的方法
namespace Server
{
    class DataMgr
    {
        MySqlConnection sqlConn;
        public static DataMgr instance;
        const string DBHost= "localhost";
        const string DBPort = "3306";
        const string user = "账号";
        const string pw = "密码";
        public DataMgr()
        {
            instance = this;
        }

        public bool Connect()
        {
            string connStr = "database=game;data source=" + DBHost + ";";
            connStr += "user=" + user + ";password=" + pw + ";port=" + DBPort + ";";
            sqlConn = new MySqlConnection(connStr);
            try
            {
                sqlConn.Open();
                Console.WriteLine("[数据库]连接成功");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[数据库]连接失败" + e.Message);
                return false;
            }
        }

        //防止sql注入
        public bool IsSafeStr(string str)
        {
            return !Regex.IsMatch(str, @"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']");
        }

        //判断用户是否存在
        private bool CanRegister(string id)
        {
            if (!IsSafeStr(id)) return false;
            string cmdStr = string.Format("select * from user where id = '{0}';", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            try
            {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return !hasRows;
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]查询用户失败" + e.Message);
                return false;
            }
        }

        public bool Register(string id,string pw)
        {
            if (!IsSafeStr(id) || !IsSafeStr(pw))
            {
                Console.WriteLine("[DataMgr]Register使用非法字符");
                return false;
            }
            if (!CanRegister(id))
            {
                Console.WriteLine("[DataMgr]用户已存在");
                return false;
            }
            string cmdStr = string.Format("insert into user(id, pw) values('{0}', '{1}'); ", id, pw);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]Register" + e.Message);
                return false;
            }
        }

        public bool CreatePlayer(string id,PlayerData playerData)
        {
            if (!IsSafeStr(id))
                return false;
            IFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            try
            {
                formatter.Serialize(stream, playerData);
            }
            catch (Exception e)
            {
                Console.WriteLine("[DataMgr]CreatePlayer序列化" + e.Message);
                return false;
            }
            byte[] bytrArr = stream.ToArray();
            //写入数据库
            string cmdStr = string.Format("insert into player(id, data) values('{0}', @data); ", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            cmd.Parameters.Add("@data", MySqlDbType.Blob);
            cmd.Parameters[0].Value = bytrArr;
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[DataMgr]CreatePlayer写入" + e.Message);
                return false;
            }
        }

        public bool CheckPassWord(string id,string pw)
        {
            if (!IsSafeStr(id) || !IsSafeStr(pw)) return false;
            string cmdStr = string.Format("select * from user where id = '{0}' and pw='{1}';", id, pw);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            try
            {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                bool hasRows = dataReader.HasRows;
                dataReader.Close();
                return hasRows;
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]检查密码" + e.Message);
                return false;
            }
        }

        public PlayerData GetPlayerData(string id)
        {
            PlayerData playerData = null;
            if (!IsSafeStr(id)) return playerData;
            string cmdStr = string.Format("select * from player where id = '{0}';", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            byte[] buffer = null;
            try
            {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                if(!dataReader.HasRows)
                {
                    dataReader.Close();
                    return playerData;
                }
                dataReader.Read();
                long len = dataReader.GetBytes(1, 0, null, 0, 0);
                buffer = new byte[len];
                len = dataReader.GetBytes(1, 0, buffer, 0, (int)len);
                dataReader.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]GetPlayerData查询" + e.Message);
                return playerData;
            }
            MemoryStream stream = new MemoryStream(buffer);
            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                playerData = (PlayerData)formatter.Deserialize(stream);
                return playerData;
            }
            catch (SerializationException e)
            {
                Console.WriteLine("[DataMgr]GetPlayData反序列化" + e.Message);
                return playerData;
            }

        }

        public bool SavePlayer(Player player)
        {
            PlayerData playerData = player.data;
            IFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            try
            {
                formatter.Serialize(stream, playerData);
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]SavePlayer序列化" + e.Message);
                return false;
            }
            byte[] byteArr = stream.ToArray();
            string formatStr = "update player set data =@data where id = '{0}';";
            string cmdStr = string.Format(formatStr, player.id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            cmd.Parameters.Add("@data", MySqlDbType.Blob);
            cmd.Parameters[0].Value = byteArr;
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[DataMgr]SavePlayer写入" + e.Message);
                return false;
            }

        }

        public bool SaveChat(string id,string msg)
        {
            if (!IsSafeStr(id) || !IsSafeStr(msg)) return false;
            string cmdStr = string.Format("insert into chat(id, msg) values('{0}', '{1}'); ", id, msg);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[DataMgr]SaveChat" + e.Message);
                return false;
            }
        }
    }
}
