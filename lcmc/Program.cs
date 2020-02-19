using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace lcmc
{
    class Program
    {
        static List<StateObject> stateObjects = new List<StateObject>();
        static void Main(string[] args)
        {

            Console.WriteLine("args, len: " + args.Length);
            for (int i=0; i<args.Length; i++)
            {
                Console.WriteLine("args[{0}]: {1}", i, args[i]);
            }

            IPEndPoint ipeAgents = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);
            IPEndPoint ipeEngineer = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11001);

            if (args.Length >= 1)
            {
                try
                {
                    string[] s = args[0].Split(':');
                    IPAddress iPAddress = IPAddress.Parse(s[0]);
                    int p = int.Parse(s[1]);

                    ipeAgents = new IPEndPoint(iPAddress, p);
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

                    ipeEngineer = new IPEndPoint(iPAddress, p);
                }
                catch (Exception e)
                {
                    Console.WriteLine("could not parse home socket, using defaults");
                }
            }


            listenforagents(ipeAgents);
            listenforagents(ipeEngineer);


            try
            {
                Console.WriteLine("press any key to exit...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.Write("e: " + e.ToString());
            }
        }

        private static void listenforagents(IPEndPoint ipe)
        {
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(ipe);

            listener.Listen(100);
            Console.WriteLine("listening on socket {0}:{1}",
                ipe.Address.ToString(),
                ipe.Port
                );


            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        }
        static void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            handler.NoDelay = true;

            // Signal the main thread to continue.  
            //allDone.Set();

            // Create the state object.  
            StateObject state = new StateObject();
            stateObjects.Add(state);
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);

            Console.WriteLine("client connected to port: {0}", ((IPEndPoint)handler.LocalEndPoint).Port);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            handler.NoDelay = true;


            int read = handler.EndReceive(ar);
            Console.Write("{0} {1} bytes",
                        ((IPEndPoint)state.workSocket.LocalEndPoint).Port == 11000 ? "-P->" : "<-A-",
                        read
                        );

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);


            foreach (StateObject s in stateObjects)
            {
                if (((IPEndPoint)s.workSocket.LocalEndPoint).Port + 1000 ==
                    ((IPEndPoint)state.workSocket.LocalEndPoint).Port
                    ||
                    ((IPEndPoint)s.workSocket.LocalEndPoint).Port - 1000 ==
                     ((IPEndPoint)state.workSocket.LocalEndPoint).Port
                    )
                {
                    // found corresponding stateobject
                    Console.WriteLine(".");

                    s.workSocket.NoDelay = true;
                    s.workSocket.Send(state.buffer, read, SocketFlags.None);
                    break;
                }
            }
        }
    }


}
