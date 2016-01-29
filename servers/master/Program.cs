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
using System.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace WaddleVillage_Master_Server
{
    class TcpSock
    {
        public string username;
        public bool isServer;
        public int serverLoad;
        public string serverName;
        public bool cleanUpRequired = false;

        #region Socket Stuff

        int tcpIndx = 0;
        int tcpByte = 0;
        byte[] tcpRecv = new byte[1024];

        public Socket tcpSock;

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
            return tcpSock.Send(Encoding.ASCII.GetBytes(tcpWrite));
        }

        public int SendLn(string tcpWrite)
        {
            return tcpSock.Send(Encoding.ASCII.GetBytes(tcpWrite + "\0"));
        }

        #endregion

        public int SendPolicyFile()
        {
            return tcpSock.Send(Encoding.ASCII.GetBytes("<?xml version=\"1.0\"?><!DOCTYPE cross-domain-policy SYSTEM \"http://www.macromedia.com/xml/dtds/cross-domain-policy.dtd\"><cross-domain-policy>" +
                "<allow-access-from domain=\"*\" to-ports=\"*\"/>" +
                "</cross-domain-policy>\0"));
        }
    }

    class Tcp
    {
        public ArrayList cc = new ArrayList();
        bool isClosing = false;

        [STAThread]
        static void Main()
        {
            Console.WriteLine("+===============================+");
            Console.WriteLine("+                               +");
            Console.WriteLine("+         Waddle Village        +");
            Console.WriteLine("+          MasterServer         +");
            Console.WriteLine("+                               +");
            Console.WriteLine("+===============================+");            

            IPEndPoint Ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2565); // MUST CONFIGURE IP!
            Socket Server = new Socket(Ipep.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            Tcp tcp = new Tcp();
            ArrayList Client = tcp.cc;

            string rln = null;

            Client.Capacity = 1000;
            Server.Blocking = false;
            Server.Bind(Ipep);
            Server.Listen(32);

            string[] cache1;
            string[] cache2;            
            string cache6;
            string cache7;            

            int[] serverLoads;

            string connString = "Server=waddlevillage.net; Database=db; Uid=user; Pwd=pass;"; // MUST CONFIGURE MYSQL CONN STRING (or change driver)
            MySqlConnection conn = new MySqlConnection(connString);

            Console.WriteLine("{0}: listening to port {1}", Dns.GetHostName(),
                Ipep.Port);

            while (!tcp.isClosing)
            {
                if (Server.Poll(0, SelectMode.SelectRead))
                {
                    int i = Client.Add(new TcpSock());
                    ((TcpSock)Client[i]).tcpSock = Server.Accept();

                    IPEndPoint remoteIpEndPoint = ((TcpSock)Client[i]).tcpSock.RemoteEndPoint as IPEndPoint;
                    IPEndPoint localIpEndPoint = ((TcpSock)Client[i]).tcpSock.LocalEndPoint as IPEndPoint;

                    if (remoteIpEndPoint != null)
                    {
                        // Using the RemoteEndPoint property.
                        //Console.WriteLine("I am connected to " + remoteIpEndPoint.Address + "on port number " + remoteIpEndPoint.Port);
                    }

                    if (localIpEndPoint != null)
                    {
                        // Using the LocalEndPoint property.
                        //Console.WriteLine("My local IpAddress is :" + localIpEndPoint.Address + "I am connected on port number " + localIpEndPoint.Port);
                    }

                    
                    Console.WriteLine("Client {0} connected.", i);
                }

                for (int i = 0; i < Client.Count; i++)
                {
                    if (((TcpSock)Client[i]).tcpSock.Poll(0, SelectMode.SelectRead))
                    {
                        try
                        {
                            if (((TcpSock)Client[i]).cleanUpRequired)
                            {
                                tcp.CleanUp(i);
                            }
                            else
                            {

                                if (((TcpSock)Client[i]).Recv(ref rln) > 0)
                                {
                                    Console.WriteLine("Received: {0}", rln);

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

                                        string sql = "SELECT nice_username FROM users WHERE nice_username='" + cache1[1].ToLower() +"' AND password='" + hash + "'";
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
                                            Console.WriteLine(ex);
                                        }

                                        comm.Dispose();

                                        if (sqlReturn != null)
                                        {
                                            bool multilogin = false;

                                            ((TcpSock)Client[i]).username = (string)sqlReturn;

                                            for (int y = 0; y < Client.Count; y++)
                                            {
                                                if (y != i)
                                                {
                                                    if (((TcpSock)Client[y]).username != null)
                                                    {
                                                        if (((TcpSock)Client[y]).username == ((TcpSock)Client[i]).username)
                                                        {
                                                            multilogin = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }

                                            if (multilogin)
                                            {
                                                ((TcpSock)Client[i]).SendLn("<loginCheck|-1|multi>"); // login OK
                                                ((TcpSock)Client[i]).cleanUpRequired = true;
                                            }
                                            else
                                            {
                                                ((TcpSock)Client[i]).SendLn("<loginCheck|-1|true>"); // login OK
                                            }
                                        }
                                        else
                                        {                               
                                            ((TcpSock)Client[i]).SendLn("<loginCheck|-1|false>"); // login NOT-OK
                                        }
                                    }
                                    else if (rln.Contains("<retrieveServerList"))
                                    {
                                        var feathersOnline = false;

                                        for (int e = 0; e < Client.Count; e++)
                                        {
                                            if (((TcpSock)Client[e]).isServer)
                                            {
                                                ((TcpSock)Client[i]).SendLn("<serverList|" + ((TcpSock)Client[e]).serverLoad + "|" + ((TcpSock)Client[e]).serverName + ">");

                                                if (((TcpSock)Client[e]).serverName == "Feathers")
                                                {
                                                    feathersOnline = true;
                                                }
                                            }
                                        }

                                        if (!feathersOnline)
                                        {
                                            ((TcpSock)Client[i]).SendLn("<serverList|-1|Feathers>");
                                        }
                                    }
                                    else if (rln.Contains("@serverLoad"))
                                    {
                                        cache6 = rln;
                                        cache6 = cache6.TrimEnd('>');
                                        cache6 = cache6.TrimStart('<');
                                        cache1 = cache6.Split('|');

                                        if (cache1[2] == "Feathers")
                                        {
                                            ((TcpSock)Client[i]).isServer = true;
                                            ((TcpSock)Client[i]).serverLoad = Convert.ToInt32(cache1[1]);
                                            ((TcpSock)Client[i]).serverName = "Feathers";
                                        }

                                        Console.WriteLine("Feathers server load is now {0}", ((TcpSock)Client[i]).serverLoad);
                                    }
                                }
                                else
                                {
                                    ((TcpSock)Client[i]).tcpSock.Shutdown(SocketShutdown.Both);
                                    ((TcpSock)Client[i]).tcpSock.Close();
                                    Client.RemoveAt(i);
                                    Console.WriteLine("Client {0} disconnected.", i);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Write("Exception: {0}", ex);
                        }
                    }
                }
            }
        }

        public void CleanUp(int userid)
        {
            Console.WriteLine("Cleaning user {0}", userid);
            ((TcpSock)cc[userid]).tcpSock.Shutdown(SocketShutdown.Both);
            ((TcpSock)cc[userid]).tcpSock.Close();
            cc.RemoveAt(userid);
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
    }
}
