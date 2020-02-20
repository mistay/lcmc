using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lcmc
{
    class Program
    {
        static IPEndPoint ipeAgent = null;
        private static IPEndPoint ipeConsumer = null;

        public static void agentHandler(object data)
        {

            if (!(data is Socket))
                return;

            Socket handler = (Socket)data;


            byte[] buffer = new Byte[1024];

            try
            {
                int bytesRec = 0;
                while ((bytesRec = handler.Receive(buffer)) > 0)
                {
                    foreach (Socket s in consumerSockets)
                    {
                        if (
                            ((IPEndPoint)s.LocalEndPoint).Port + 1000 == ((IPEndPoint)handler.LocalEndPoint).Port ||
                            ((IPEndPoint)s.LocalEndPoint).Port - 1000 == ((IPEndPoint)handler.LocalEndPoint).Port
                            )
                        {
                            Console.WriteLine("-C-> {0} bytes", bytesRec);
                            s.Send(buffer, bytesRec, SocketFlags.None);
                            break;
                        }
                    }
                }
            } catch (Exception e)
            {
                // ignore
            }

            try
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                // ignore
            }
        }

        static List<Socket> agentSockets = new List<Socket>();
        static List<Socket> consumerSockets = new List<Socket>();

        public static void listenForAgents()
        {
            byte[] bytes = new Byte[1024];

            Socket listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

            while(true)
            {
                try
                {
                    listener.Bind(ipeAgent);
                    listener.Listen(100);

                    Console.WriteLine("listen4agents: " + ((IPEndPoint)listener.LocalEndPoint).ToString());

                    while (true)
                    {
                        Socket handler = listener.Accept();
                        agentSockets.Add(handler);
                        Console.WriteLine("agent connected: " + ((IPEndPoint)handler.RemoteEndPoint).ToString());

                        Thread t = new Thread(agentHandler);
                        t.Start(handler);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

        }

        public static void consumerHandler(object data)
        {
            if (!(data is Socket))
                return;
            Socket handler = (Socket)data;

            byte[] buffer = new Byte[1024];

            try
            {
                int bytesRec = 0;
                while ((bytesRec = handler.Receive(buffer)) > 0)
                {
                    foreach (Socket s in agentSockets)
                    {
                        if (
                        ((IPEndPoint)s.LocalEndPoint).Port + 1000 == ((IPEndPoint)handler.LocalEndPoint).Port ||
                        ((IPEndPoint)s.LocalEndPoint).Port - 1000 == ((IPEndPoint)handler.LocalEndPoint).Port 
                        )
                        {
                            // found corresponding socket
                            Console.WriteLine("<A-- {0} bytes", bytesRec);
                            s.Send(buffer, bytesRec, SocketFlags.None);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // ignore
            }

            try
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                // ignore
            }
        }



        public static void listenForConsumers()
        {
            byte[] bytes = new Byte[1024];

            Socket listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(ipeConsumer);
                listener.Listen(100);

                while (true)
                {
                    Socket handler = listener.Accept();
                    consumerSockets.Add(handler);
                    Console.WriteLine("consumer connected: " + ((IPEndPoint)handler.RemoteEndPoint).ToString());

                    Thread t = new Thread(consumerHandler);
                    t.Start(handler);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                try
                {
                    string[] s = args[0].Split(':');
                    IPAddress iPAddress = IPAddress.Parse(s[0]);
                    int p = int.Parse(s[1]);

                    ipeAgent = new IPEndPoint(iPAddress, p);
                }
                catch (Exception e)
                {
                    Console.WriteLine("could not parse local socket, using defaults");
                }
            }

            if (args.Length >= 2)
            {
                try
                {
                    string[] s = args[1].Split(':');
                    IPAddress iPAddress = IPAddress.Parse(s[0]);
                    int p = int.Parse(s[1]);

                    ipeConsumer = new IPEndPoint(iPAddress, p);
                }
                catch (Exception e)
                {
                    Console.WriteLine("could not parse home socket, using defaults");
                }
            }

            Thread t = new Thread(new ThreadStart(listenForAgents));
            t.Start();

            Thread t2 = new Thread(new ThreadStart(listenForConsumers));
            t2.Start();

            Console.WriteLine("\nPress ENTER to quit...");
            Console.ReadLine();
        }
    }
}
