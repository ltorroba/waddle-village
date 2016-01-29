/***

This file is part of Waddle Village Game System.

Waddle Village Game System is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Waddle Village Game System is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Waddle Village Game System.  If not, see <http://www.gnu.org/licenses/>.

***/

using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.IO;
using MySql.Data;
using MySql.Data.MySqlClient;
using WaddleVillage_Server;
using System.Threading;


namespace TcpSock
{
    class TcpSock
    {
        #region User vars

        // STATIC VARS
        public int id { get; set; } 
        public string username { get; set; }
        public bool isMuted { get; set; }
        public bool isBanned { get; set; }
        public int colour { get; set; }
        public int head { get; set; }
        public int torso { get; set; }
        public int legs { get; set; }
        public int feet { get; set; }
        public int access1 { get; set; }
        public int access2 { get; set; }
        public int access3 { get; set; }
        public DateTime dateRegistered { get; set; }
        public int coins { get; set; }
        public int privilege { get; set; }

        public int[][] inventory { get; set; }

        // TEMP VARS
        public int x { get; set; }
        public int y { get; set; }
        public int currRoom { get; set; }
        public int pos { get; set; }

        #endregion

        #region Socket Methods

        int tcpIndx = 0;

        public Socket tcpSock;
        byte[] tcpRecv = new byte[1024];
        int tcpByte = 0;

        public int Recv(ref string tcpRead)
        {
            tcpByte = tcpSock.Available;
            if (tcpByte > tcpRecv.Length)
            {
                //check that if a message is too long, it is explained so can be fixed
                Console.WriteLine("Wanted to read {0} bytes (limit {1}). Will corrupt", tcpByte, tcpRecv.Length);
                tcpByte = tcpRecv.Length;
            }

            try
            {
                tcpByte = tcpSock.Receive(tcpRecv, tcpByte, SocketFlags.None);
                tcpRead = Encoding.ASCII.GetString(tcpRecv, 0, tcpByte);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception at: {0}", ex);
            }

            return tcpRead.Length;
        }

        public int RecvLn(ref string tcpRead)
        {
            tcpRead = Encoding.ASCII.GetString
                (tcpRecv, 0, tcpIndx);
            tcpIndx = 0;
            return tcpRead.Length;
        }

        public int Send(string tcpWrite)
        {
            try
            {
                Console.WriteLine("[TCP-SOCK]: Sending ({0})", tcpWrite);
                return tcpSock.Send(Encoding.ASCII.GetBytes(tcpWrite));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return 0;
        }

        public int SendLn(string tcpWrite)
        {
            try
            {
                return tcpSock.Send(Encoding.ASCII.GetBytes(tcpWrite + "\0"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return 0;
        }

        public int SendPolicyFile()
        {
            try
            {
                return tcpSock.Send(Encoding.ASCII.GetBytes("<cross-domain-policy>" +
                    "<allow-access-from domain=\"*\" to-ports=\"*\"/>" +
                    "</cross-domain-policy>\0"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return 0;
        }

        #endregion        
    }

    class Tcp
    {
        public ArrayList cc = new ArrayList();

        [STAThread]
        static void Main()
        {
            Console.WriteLine("+===============================+");
            Console.WriteLine("+                               +");
            Console.WriteLine("+         Waddle Village        +");
            Console.WriteLine("+             Server            +");
            Console.WriteLine("+                               +");
            Console.WriteLine("+===============================+");

            Console.WriteLine("[STARTUP]: Creating listener socket...");
            IPEndPoint Ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2566); // PLEASE CONFIGURE IP
            Socket Server = new Socket(Ipep.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("[STARTUP]: Listening to in {0}:{1}", Dns.GetHostName(), Ipep.Port);

            Console.WriteLine("[STARTUP]: Creating Master Server socket...");
            IPEndPoint masterEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2565); // PLEASE CONFIGURE MASTER SERVER IP
            Socket masterSocket = new Socket(masterEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            while (true)
            {
                try
                {
                    masterSocket.Connect(masterEndPoint);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10061)
                    {
                        Console.WriteLine("[STARTUP]: Failed to connect to master server. Retrying in 5 seconds");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERRORHANDLER]: Unexpected exception encoutered: {0}", ex);
                }

                if (masterSocket.Connected)
                {
                    Console.WriteLine("[STARTUP]: Connected to {0}:{1}...", masterEndPoint.Address.AddressFamily, masterEndPoint.Port);
                    Console.WriteLine("[LOADMANAGER]: Attempting to send new server load...");
                    masterSocket.Send(Encoding.ASCII.GetBytes("<@serverLoad|0|Feathers>"));
                    Console.WriteLine("[LOADMANAGER]: New server load successfully sent.");
                    break;
                }
                else
                {                    
                    Thread.Sleep(5000);
                }
            }


            Console.WriteLine("[STARTUP]: Creating and setting up client holder arrays, caches, and sockets.");

            Tcp tcp = new Tcp();
            ArrayList Client = tcp.cc;
            string rln = null;

            Client.Capacity = 100;
            Server.Blocking = false;
            Server.Bind(Ipep);
            Server.Listen(32);

            string[] cache1;
            string[] cache2;
            string cache6;
            string cache7;

            bool onShutdown = false;

            int initialRoom = 0; // room id 0    

            Console.WriteLine("[STARTUP]: Components successfully set up.");

            Console.WriteLine("[STARTUP]: Attempting to create database connector...");

            string connString = "Server=waddlevillage.net; Database=db; Uid=user; Pwd=pass;";
            MySqlConnection conn = new MySqlConnection(connString);

            Console.WriteLine("[STARTUP]: Connected successfully.");

            ItemManager items = new ItemManager();

            int masterServerReconnect = 0;
                        
            while (true)
            {
                if (!onShutdown)
                {
                    if (!masterSocket.Connected)
                    {
                        Console.WriteLine("[MS-HEARTBEAT]: Lost connection to Master Server. Attempting to reconnect...");

                        try
                        {
                            masterSocket.Connect(masterEndPoint);
                        }
                        catch (SocketException ex)
                        {
                            if (ex.ErrorCode == 10061)
                            {
                                Console.WriteLine("[STARTUP]: Failed to reconnect to master server. Retrying in 5 seconds");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[ERRORHANDLER]: Unexpected exception encoutered: {0}", ex);
                        }

                        if (masterSocket.Connected)
                        {
                            Console.WriteLine("[STARTUP]: Reconnected to {0}:{1}...", masterEndPoint.Address.AddressFamily, masterEndPoint.Port);
                            Console.WriteLine("[LOADMANAGER]: Attempting to resend server load...");
                            masterSocket.Send(Encoding.ASCII.GetBytes("<@serverLoad|" + Client.Count + "|Feathers>"));
                            Console.WriteLine("[LOADMANAGER]: New server load successfully resent.");
                        }
                        else
                        {
                            masterServerReconnect++;
                            if (masterServerReconnect < 5)
                            {
                                Thread.Sleep(5000);
                            }
                            else
                            {
                                Console.WriteLine("[MS-HEARBEAT]: Maximum number of reconnect attempts has been reached. Server shutting down in 10 seconds.");
                                Thread.Sleep(10000);
                                onShutdown = true;
                            }
                        }
                    }
                    else
                    {
                        if (Server.Poll(0, SelectMode.SelectRead))
                        {
                            int i = Client.Add(new TcpSock());
                            ((TcpSock)Client[i]).tcpSock = Server.Accept();
                            Console.WriteLine("[LOOP]: Client {0} connected.", i);
                            masterSocket.Send(Encoding.ASCII.GetBytes("<@serverLoad|" + (i + 1) + "|Feathers>"));
                        }

                        for (int i = 0; i < Client.Count; i++)
                        {
                            if (((TcpSock)Client[i]).tcpSock.Poll(0, SelectMode.SelectRead))
                            {
                                if (((TcpSock)Client[i]).Recv(ref rln) > 0)
                                {
                                    Console.WriteLine(rln);

                                    if (rln.Contains("<") && rln.Contains(">"))
                                    {
                                        if (rln.Contains("<policy-file-request/>\0"))
                                        {
                                            ((TcpSock)Client[i]).SendPolicyFile();
                                        }
                                        else if (rln.Contains("<loginCheck"))
                                        {
                                            if (conn.State != ConnectionState.Open)
                                            {
                                                conn.Open();
                                            }

                                            cache6 = rln;
                                            cache6 = cache6.TrimEnd('\0');
                                            cache6 = cache6.TrimEnd('>');
                                            cache6 = cache6.TrimStart('<');
                                            cache1 = cache6.Split('|');

                                            string hash = PHPHash(cache1[2].ToString());

                                            string sql = "SELECT * FROM users WHERE nice_username='" + cache1[1].ToLower() + "' AND password='" + hash + "'";
                                            MySqlCommand comm = new MySqlCommand(sql, conn);

                                            //bool repeat = false;
                                            object sqlReturn = null;

                                            /*do
                                            {
                                                try
                                                {
                                                    repeat = false;
                                                    sqlReturn = comm.ExecuteScalar();
                                                }
                                                catch (Exception ex)
                                                {
                                                    if (ex.GetType().ToString().ToUpper().Contains("TIMEOUT"))
                                                    {
                                                        repeat = true;
                                                    }
                                                }

                                                if (!repeat)
                                                {
                                                    break;
                                                }
                                            } while (repeat);*/

                                            try
                                            {
                                                sqlReturn = comm.ExecuteScalar();
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("Caugh exception: {0}", ex);
                                            }

                                            if (sqlReturn != null)
                                            {
                                                ((TcpSock)Client[i]).SendLn("<loginCheck|-1|true>"); // login OK
                                                MySqlDataReader data = comm.ExecuteReader();

                                                while (data.Read())
                                                {
                                                    ((TcpSock)Client[i]).username = (string)data["username"];
                                                    ((TcpSock)Client[i]).isMuted = (bool)data["isMuted"];
                                                    ((TcpSock)Client[i]).isBanned = (bool)data["isBanned"];
                                                    ((TcpSock)Client[i]).head = (int)data["head"];
                                                    ((TcpSock)Client[i]).torso = (int)data["torso"];
                                                    ((TcpSock)Client[i]).legs = (int)data["legs"];
                                                    ((TcpSock)Client[i]).feet = (int)data["feet"];
                                                    ((TcpSock)Client[i]).access1 = (int)data["access1"];
                                                    ((TcpSock)Client[i]).access2 = (int)data["access2"];
                                                    ((TcpSock)Client[i]).access3 = (int)data["access3"];
                                                    ((TcpSock)Client[i]).dateRegistered = (DateTime)data["dateRegistered"];
                                                    ((TcpSock)Client[i]).coins = (int)data["coins"];
                                                    ((TcpSock)Client[i]).privilege = (int)data["privilege"];
                                                }

                                                data.Close();
                                                data.Dispose();

                                                //((TcpSock)Client[i]).inventory = [[0, 0]];

                                                /*
                                                string sql2 = "SELECT * FROM users WHERE nice_username='" + cache1[1] + "'";
                                                //MySqlCommand comm2 = 

                                                repeat = false;
                                                sqlReturn = null;

                                                do
                                                {
                                                    try
                                                    {
                                                        repeat = false;
                                                        sqlReturn = comm.ExecuteScalar();
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        if (ex.GetType().ToString().ToUpper().Contains("TIMEOUT"))
                                                        {
                                                            repeat = true;
                                                        }
                                                    }

                                                    if (!repeat)
                                                    {
                                                        break;
                                                    }
                                                } while (repeat);
                                                 * 
                                                 */

                                            }
                                            else
                                            {
                                                ((TcpSock)Client[i]).SendLn("<loginCheck|-1|false>"); // login NOT-OK
                                            }
                                        }
                                        else if (rln.Contains("<startPlay"))
                                        {
                                            try
                                            {
                                                ((TcpSock)Client[i]).SendLn("<changeRoom|" + initialRoom + ">");
                                                //((TcpSock)Client[i]).SendLn("<addMyPlayer|" + ((TcpSock)Client[i]).username + "|190|521|" + ((TcpSock)Client[i]).privilege + ">");

                                                ((TcpSock)Client[i]).currRoom = initialRoom;
                                                ((TcpSock)Client[i]).x = 190;
                                                ((TcpSock)Client[i]).y = 521;

                                                ((TcpSock)Client[i]).SendLn("<addMyPlayer|" + ((TcpSock)Client[i]).username + "|" + ((TcpSock)Client[i]).x + "|" + ((TcpSock)Client[i]).y + "|" + ((TcpSock)Client[i]).privilege + ">");

                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[i]).SendLn("<addPlayer|" + ((TcpSock)Client[y]).username + "|" + ((TcpSock)Client[y]).x + "|" + ((TcpSock)Client[y]).y + "|" + ((TcpSock)Client[y]).privilege + ">");
                                                            ((TcpSock)Client[y]).SendLn("<addPlayer|" + ((TcpSock)Client[i]).username + "|" + ((TcpSock)Client[i]).x + "|" + ((TcpSock)Client[i]).y + "|" + ((TcpSock)Client[i]).privilege + ">");
                                                            ((TcpSock)Client[i]).SendLn("<changeColour|" + ((TcpSock)Client[y]).colour + "|" + ((TcpSock)Client[y]).username + ">");
                                                            ((TcpSock)Client[y]).SendLn("<changeColour|" + ((TcpSock)Client[i]).colour + "|" + ((TcpSock)Client[i]).username + ">");
                                                            ((TcpSock)Client[y]).SendLn("<changeShirt|" + ((TcpSock)Client[i]).torso + "|" + ((TcpSock)Client[i]).username + ">");
                                                            ((TcpSock)Client[i]).SendLn("<changeShirt|" + ((TcpSock)Client[y]).torso + "|" + ((TcpSock)Client[y]).username + ">");
                                                        }
                                                    }
                                                }

                                                ((TcpSock)Client[i]).SendLn("<endStartup|-1>");
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<move"))
                                        {
                                            try
                                            {
                                                cache7 = rln;
                                                cache7 = cache7.TrimEnd('\0');
                                                cache7 = cache7.TrimEnd('>');
                                                cache7 = cache7.TrimStart('<');

                                                cache2 = cache7.Split('|');

                                                ((TcpSock)Client[i]).x = Convert.ToInt32(cache2[1]);
                                                ((TcpSock)Client[i]).y = Convert.ToInt32(cache2[2]);
                                                ((TcpSock)Client[i]).pos = Convert.ToInt32(cache2[3]);

                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<movePlayer|" + ((TcpSock)Client[i]).username + "|" + ((TcpSock)Client[i]).x + "|" + ((TcpSock)Client[i]).y + "|" + ((TcpSock)Client[i]).pos + ">");
                                                        }
                                                        else
                                                        {
                                                            ((TcpSock)Client[i]).SendLn("<moveMyPlayer|" + ((TcpSock)Client[i]).x + "|" + ((TcpSock)Client[i]).y + ">");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<publicChat"))
                                        {
                                            try
                                            {
                                                cache7 = rln;
                                                cache7 = cache7.TrimEnd('\0');
                                                cache7 = cache7.TrimEnd('>');
                                                cache7 = cache7.TrimStart('<');

                                                cache2 = cache7.Split('|');

                                                //Console.WriteLine(cache2[1]);

                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<publicChat|" + ((TcpSock)Client[i]).username + "|" + cache2[1] + "|" + ((TcpSock)Client[i]).privilege + ">");
                                                        }
                                                        else
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<publicChat|0|" + cache2[1] + "|" + ((TcpSock)Client[i]).privilege + ">");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<changeRoom"))
                                        {
                                            try
                                            {
                                                cache7 = rln;
                                                cache7 = cache7.TrimEnd('\0');
                                                cache7 = cache7.TrimEnd('>');
                                                cache7 = cache7.TrimStart('<');

                                                cache2 = cache7.Split('|');

                                                ((TcpSock)Client[i]).SendLn("<removeMyPlayer|-1>");

                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<removePlayer|" + ((TcpSock)Client[i]).username + ">");
                                                            ((TcpSock)Client[i]).SendLn("<removePlayer|" + ((TcpSock)Client[y]).username + ">");
                                                        }
                                                    }
                                                }

                                                ((TcpSock)Client[i]).currRoom = Convert.ToInt32(cache2[1]);

                                                switch (((TcpSock)Client[i]).currRoom)
                                                {
                                                    case 0:
                                                        ((TcpSock)Client[i]).x = 190;
                                                        ((TcpSock)Client[i]).y = 521;
                                                        break;
                                                    case 1:
                                                        ((TcpSock)Client[i]).x = 500;
                                                        ((TcpSock)Client[i]).y = 458;
                                                        break;
                                                }

                                                ((TcpSock)Client[i]).SendLn("<addMyPlayer|" + ((TcpSock)Client[i]).username + "|" + ((TcpSock)Client[i]).x + "|" + ((TcpSock)Client[i]).y + "|" + ((TcpSock)Client[i]).privilege + ">");

                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[i]).SendLn("<addPlayer|" + ((TcpSock)Client[y]).username + "|" + ((TcpSock)Client[y]).x + "|" + ((TcpSock)Client[y]).y + "|" + ((TcpSock)Client[y]).privilege + ">");
                                                            ((TcpSock)Client[y]).SendLn("<addPlayer|" + ((TcpSock)Client[i]).username + "|" + ((TcpSock)Client[i]).x + "|" + ((TcpSock)Client[i]).y + "|" + ((TcpSock)Client[i]).privilege + ">");
                                                            ((TcpSock)Client[y]).SendLn("<changeColour|" + ((TcpSock)Client[i]).colour + "|" + ((TcpSock)Client[i]).username + ">");
                                                            ((TcpSock)Client[i]).SendLn("<changeColour|" + ((TcpSock)Client[y]).colour + "|" + ((TcpSock)Client[y]).username + ">");
                                                            ((TcpSock)Client[y]).SendLn("<changeShirt|" + ((TcpSock)Client[i]).torso + "|" + ((TcpSock)Client[i]).username + ">");
                                                            ((TcpSock)Client[i]).SendLn("<changeShirt|" + ((TcpSock)Client[y]).torso + "|" + ((TcpSock)Client[y]).username + ">");
                                                        }
                                                    }
                                                }

                                                ((TcpSock)Client[i]).SendLn("<changeMyColour|" + ((TcpSock)Client[i]).colour + ">");
                                                ((TcpSock)Client[i]).SendLn("<changeMyShirt|" + ((TcpSock)Client[i]).torso + ">");
                                                ((TcpSock)Client[i]).SendLn("<endRoomChange|-1>");
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<logout"))
                                        {
                                            try
                                            {
                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<removePlayer|" + ((TcpSock)Client[i]).username + ">");
                                                        }
                                                    }
                                                }

                                                ((TcpSock)Client[i]).SendLn("<closeSession|-1>");
                                                ((TcpSock)Client[i]).tcpSock.Shutdown(SocketShutdown.Both);
                                                ((TcpSock)Client[i]).tcpSock.Close();
                                                Client.RemoveAt(i);
                                                Console.WriteLine("Client {0} disconnected.", i);
                                                masterSocket.Send(Encoding.ASCII.GetBytes("<@serverLoad|" + i + "|Feathers>"));
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<@ms_shutdown>"))
                                        {
                                            onShutdown = true;
                                        }
                                        else if (rln.Contains("<getInventory"))
                                        {
                                            cache7 = rln;
                                            cache7 = cache7.TrimEnd('\0');
                                            cache7 = cache7.TrimEnd('>');
                                            cache7 = cache7.TrimStart('<');

                                            cache2 = cache7.Split('|');

                                            switch (Convert.ToInt32(cache2[1]))
                                            {
                                                case -1:

                                                    break;
                                                case 0:

                                                    break;
                                                case 1:

                                                    break;
                                            }
                                        }
                                        else if (rln.Contains("<changeColour|"))
                                        {
                                            try
                                            {
                                                cache7 = rln;
                                                cache7 = cache7.TrimEnd('\0');
                                                cache7 = cache7.TrimEnd('>');
                                                cache7 = cache7.TrimStart('<');

                                                cache2 = cache7.Split('|');

                                                ((TcpSock)Client[i]).colour = Convert.ToInt32(cache2[1]);

                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<changeColour|" + ((TcpSock)Client[i]).colour + "|" + ((TcpSock)Client[i]).username + ">");
                                                        }
                                                        else
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<changeMyColour|" + ((TcpSock)Client[i]).colour + ">");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<changeShirt|"))
                                        {
                                            try
                                            {
                                                cache7 = rln;
                                                cache7 = cache7.TrimEnd('\0');
                                                cache7 = cache7.TrimEnd('>');
                                                cache7 = cache7.TrimStart('<');

                                                cache2 = cache7.Split('|');

                                                ((TcpSock)Client[i]).torso = Convert.ToInt32(cache2[1]);

                                                for (int y = 0; y < Client.Count; y++)
                                                {
                                                    if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                                    {
                                                        if (y != i)
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<changeShirt|" + ((TcpSock)Client[i]).torso + "|" + ((TcpSock)Client[i]).username + ">");
                                                        }
                                                        else
                                                        {
                                                            ((TcpSock)Client[y]).SendLn("<changeMyShirt|" + ((TcpSock)Client[i]).torso + ">");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<kick|"))
                                        {
                                            try
                                            {
                                                if (((TcpSock)Client[i]).privilege >= 1)
                                                {
                                                    cache7 = rln;
                                                    cache7 = cache7.TrimEnd('\0');
                                                    cache7 = cache7.TrimEnd('>');
                                                    cache7 = cache7.TrimStart('<');

                                                    cache2 = cache7.Split('|');

                                                    for (int y = 0; i < Client.Count; i++)
                                                    {
                                                        if (((TcpSock)Client[y]).username.ToUpper() == cache2[1].ToUpper())
                                                        {
                                                            for (int x = 0; x < Client.Count; x++)
                                                            {
                                                                if (x != y)
                                                                {
                                                                    ((TcpSock)Client[x]).SendLn("<removePlayer|" + ((TcpSock)Client[y]).username + ">");
                                                                }
                                                            }

                                                            ((TcpSock)Client[y]).tcpSock.Shutdown(SocketShutdown.Both);
                                                            ((TcpSock)Client[y]).tcpSock.Close();
                                                            Client.RemoveAt(y);
                                                            Console.WriteLine("Client {0} kicked.", y);
                                                            masterSocket.Send(Encoding.ASCII.GetBytes("<@serverLoad|" + Client.Count + "|Feathers>"));
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                            }
                                        }
                                        else if (rln.Contains("<serv_print|"))
                                        {
                                            cache7 = rln;
                                            cache7 = cache7.TrimEnd('\0');
                                            cache7 = cache7.TrimEnd('>');
                                            cache7 = cache7.TrimStart('<');

                                            cache2 = cache7.Split('|');

                                            switch(cache2[1].ToUpper())
                                            {
                                                case "USERS_CC":
                                                    foreach (TcpSock user in tcp.cc)
                                                    {
                                                        Console.WriteLine("[DEVTOOL]: Printing user #{0} in 'tcp.cc' -- {1}", user.id, user.username);
                                                    }
                                                    break;
                                                case "USERS_CLIENT":
                                                    foreach (TcpSock user in Client)
                                                    {
                                                        Console.WriteLine("[DEVTOOL]: Printing user #{0} in 'Client' -- {1}", user.id, user.username);
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Client #{0} ({1}) sent a corrupt message!", i, ((TcpSock)Client[i]).username);
                                    }

                                }
                                else
                                {
                                    for (int y = 0; y < Client.Count; y++)
                                    {
                                        if (((TcpSock)Client[y]).currRoom == ((TcpSock)Client[i]).currRoom)
                                        {
                                            if (y != i)
                                            {
                                                ((TcpSock)Client[y]).SendLn("<removePlayer|" + ((TcpSock)Client[i]).username + ">");
                                            }
                                        }
                                    }

                                    ((TcpSock)Client[i]).tcpSock.Shutdown(SocketShutdown.Both);
                                    ((TcpSock)Client[i]).tcpSock.Close();
                                    Client.RemoveAt(i);
                                    Console.WriteLine("Client {0} disconnected.", i);
                                    masterSocket.Send(Encoding.ASCII.GetBytes("<@serverLoad|" + Client.Count + "|Feathers>"));
                                }
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine("Server has shutdown. Press any key to continue...");
            Console.ReadLine();
        }

        public static string PHPHash(string text)
        {
            MD5 md5 = MD5CryptoServiceProvider.Create();
            byte[] dataMd5 = md5.ComputeHash(Encoding.Default.GetBytes(text));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < dataMd5.Length; i++)
                sb.AppendFormat("{0:x2}", dataMd5[i]);
            return sb.ToString();
        }

        public void CleanUp(int userid)
        {
            Console.WriteLine("Cleaning user {0}", userid);
            ((TcpSock)cc[userid]).tcpSock.Shutdown(SocketShutdown.Both);
            ((TcpSock)cc[userid]).tcpSock.Close();
            cc.RemoveAt(userid);
        }
    }
}