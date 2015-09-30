/*============================================================
Class:  SimplePrefixServer

 * Назначение: Класс работы с клиентами. Обрабатывает соединения клиентов и передает их новому обработчику.
 *              Обработка запускается в отдельном потоке.
 *              С консоли доступны команды
 *              <space><cr> получить информацию о текущем состоянии сервера
 *              <x><cr> завершить работу сервера
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoCompleteServer.Client;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using AutoCompleteLib;
using AutoCompleteLib.Util;
using System.Diagnostics;
using AutoCompleteLib.Builders;

namespace AutoCompleteServer.Server
{
    enum ServerState
    {
        Unknown,
        Inited,
        Working,
        Shutdown_in_Progress,
        Finished
    }


    /// <summary>
    /// Класс обрабатывает соединения клиентов и передает их новому обработчику клиентов.
    /// </summary>
    class SimplePrefixServer
    {
        TcpListener Listener;
        IPrefixBuilder _pb = null;
        TimerUtil TUAllClients = new TimerUtil();
        TransferState TSAllClients = new TransferState();
        TransferState TSAllTime = new TransferState();
        TimeSpan TSTimeAllTime = new TimeSpan();

        List<IClientWorker> clientlist = new List<IClientWorker>();
        readonly int _port;
        readonly ClientWorkerTypes _servertype = 0;
        
        Thread clientacceptor = null;
        bool isFinish = false;
        bool isAcceptorOk = false;
        public ServerState state { get; private set; }
        object lockobj = new object();
        ManualResetEvent mreClientAcceptorStarted = new ManualResetEvent(false);

        public SimplePrefixServer(int port, IPrefixBuilder pb, ClientWorkerTypes servertype)
        {
            state = ServerState.Inited;
            _port = port;
            _pb = pb;
            _servertype = servertype;
            state = ServerState.Inited;
            clientacceptor = new Thread(new ThreadStart(StartClientLoop));
        }

        /// <summary>
        /// Запуск цикла обработки команд
        /// </summary>
        public EnumError StartLoop()
        {
            lock (lockobj)
            {
                if (state != ServerState.Inited)
                {
                    ConsoleLogger.LogMessage("Server in not valid state: " + state);
                    return EnumError.Wrong_State;
                }
                state = ServerState.Working;
            }

            try
            {


                ShowServerInfo();
                clientacceptor.Start();
                mreClientAcceptorStarted.WaitOne();
                if (isAcceptorOk == false)
                {
                    StopClientLoop();
                    return EnumError.InvalidInputData;
                }
                while (true)
                {
                    int k = Console.Read();
                    if (k <= 0 || k == 'x')
                    {
                        StopClientLoop();
                        return EnumError.NoError;
                        //
                    }
                    if (k == ' ')
                    {
                        ShowServerInfo();
                    }
                    if (k == 'r')
                    {
                        ResetStatInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error in StartLoop: " + ex.Message);

            }
            finally
            {
                lock (lockobj)
                {
                    state = ServerState.Finished;
                }
                
            }
            return EnumError.NoError;
        }
        /// <summary>
        /// Метод сбрасывает общую статистику
        /// </summary>
        private void ResetStatInfo()
        {
            TSAllTime.Reset();
            TSTimeAllTime = new TimeSpan();
            ConsoleLogger.LogMessage("Stats reseted");
        }

        /// <summary>
        /// Метод показывает информацию о текущем состоянии сервера
        /// </summary>
        private void ShowServerInfo()
        {
            TransferState clientts = null;
            
            GetCurrentClientStatus(out clientts);
            
            ConsoleLogger.LogMessage("");
            ConsoleLogger.LogMessage("");
            ConsoleLogger.LogMessage("Press x<cr> to stop server, r<cr> to reset stats, space<cr> to show info");
            ConsoleLogger.LogMessage("");
            ConsoleLogger.LogMessage("Server info");
            ConsoleLogger.LogMessage("Server state: " + state.ToString());
            ConsoleLogger.LogMessage("Server type: " + _servertype.ToString());
            ConsoleLogger.LogMessage("Client count: " + clientlist.Count);
            ConsoleLogger.LogMessage("Threads count: " + Process.GetCurrentProcess().Threads.Count);
            ConsoleLogger.LogMessage("WorkingSet kb: " + (Process.GetCurrentProcess().WorkingSet64/1024.0).ToString("F2"));
            ConsoleLogger.LogMessage("Transfer state:");
            ConsoleLogger.LogMessage(String.Format("{0,10:S} {1:S}", "Current:", clientts.ToStringTS(TUAllClients.GetInterval())));
            ConsoleLogger.LogMessage(String.Format("{0,10:S} {2:S} {1:S} ", "All Time:", TSTimeAllTime.ToString(), TSAllTime.ToStringTS(TSTimeAllTime)));
            ConsoleLogger.LogMessage("");
            ConsoleLogger.LogMessage("");
        }
        
        
        private void GetCurrentClientStatus(out TransferState ts)
        {
            ts = new TransferState();
            lock (clientlist)
            {
                foreach (IClientWorker cw in clientlist)
                {
                    ts.AddTS(cw.GetTS());
                }
            }
        }


        /// <summary>
        /// Метод останавливает сервер
        /// </summary>
        private EnumError StopClientLoop()
        {
            ConsoleLogger.LogMessage("Server shutdown!");

            lock (lockobj)
            {
                if (state != ServerState.Working)
                {
                    ConsoleLogger.LogMessage("Server in not valid state: " + state);
                    return EnumError.Wrong_State;
                }
                state = ServerState.Shutdown_in_Progress;
            }
#if VERBOSE
            ConsoleLogger.LogMessage("StopClientLoop !");
#endif
            if (Listener != null)
            {
                Listener.Stop();
            }
            isFinish = true;
            clientacceptor.Join();
#if VERBOSE
            ConsoleLogger.LogMessage("StopClientLoop Done!");
#endif
            lock (lockobj)
            {
                state = ServerState.Finished;
            }

            return EnumError.NoError;
        }
        
        /// <summary>
        /// Метод обработки подключающихся клиентов.
        /// </summary>
        private void StartClientLoop()
        {
            try
            {
                Listener = new TcpListener(IPAddress.Any, _port);
                Listener.Start();
                isAcceptorOk = true;
                mreClientAcceptorStarted.Set();

                while (isFinish == false)
                {
                    TcpClient Client = Listener.AcceptTcpClient();
                    Client.NoDelay = true;

                    Client.ReceiveBufferSize = 1024 * 32;
                    Client.SendBufferSize = 1024 * 32;
                    AddClient(Client, _pb);
                };
            }
            catch (SocketException se)            
            {
                switch (se.SocketErrorCode)
                {
                    case SocketError.Interrupted:
                        {
                            ConsoleLogger.LogMessage("Normal listener shutdown!");
                            break;
                        }
                    case SocketError.AddressAlreadyInUse:
                        {
                            ConsoleLogger.LogMessage("Cant bind, Address/Port already in use!");
                            break;
                        }
                    default:
                        {
                            throw se;
                        }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error in StartLoop: " + ex.Message);
            }
            finally
            {
                mreClientAcceptorStarted.Set();
                CloseClients();
            }
        }

        /// <summary>
        /// Метод закрывает всех клиентов и ожидает их завершения. Режим блокирующий.
        /// </summary>        
        void CloseClients()
        {
            ConsoleLogger.LogMessage("CloseClients!");
                
                List<IClientWorker> clientlistcopy =new List<IClientWorker>();
                lock (this.clientlist)
                {
                    clientlistcopy.AddRange(this.clientlist);
                }

                foreach (IClientWorker cw in clientlistcopy)
                {
                    cw.CloseClient();
                }
                foreach (IClientWorker cw in clientlistcopy)
                {
                    cw.WaitForDoneClient();
                }
            ConsoleLogger.LogMessage("CloseClients Done!");
        }
        
        /// <summary>
        /// Метод добавляет нового клиента
        /// </summary>        
        void AddClient(TcpClient client, IPrefixBuilder pb)
        {
            IClientWorker scw = null;
            switch (_servertype)
            {
                case ClientWorkerTypes.Simple:
                    {
                        scw = new SimpleClientWorker();
                        break;
                    }
                case ClientWorkerTypes.AsyncRead:
                    {
                        scw = new AsyncReadClientWorker();
                        break;
                    }
                default:
                    {
                        scw = new SimpleClientWorker();
                        break;
                    }
            }

            if (scw == null)
            {
                ConsoleLogger.LogMessage("No clientworker used!");
                return;
            }
            scw.Init(client, pb);
            scw.OnLoopFinish += new EventHandler(worker_OnLoopFinish);
            
            lock (clientlist)
            {
                if (clientlist.Count == 0)
                {
                    TUAllClients.MarkInterval();
                    TSAllClients.Reset();
                }
                clientlist.Add(scw);
            }
            ConsoleLogger.LogMessage(String.Format("Added client: {0:S} {1:S}",
                scw.ToString(),client.Client.RemoteEndPoint.ToString()));
            scw.StartLoop();
        }
        void RemoveClient(IClientWorker scw)
        {
            lock (clientlist)
            {
                clientlist.Remove(scw);

#if VERBOSE
                ConsoleLogger.LogMessage("Exited client: " + scw.ToString()+"\r\n\r\n");
#endif
                
                if (clientlist.Count == 0)
                {
                    TUAllClients.MarkInterval();
                    TSTimeAllTime += TUAllClients.GetLastIntervalTime();
                    ConsoleLogger.LogMessage(String.Format("{0,18:S}{1:S} {2:S}",
                        "All Client TC:",
                        TUAllClients.GetLastIntervalTime().ToString(),
                        TSAllClients.ToStringTS(TUAllClients.GetLastIntervalTime())
                        ));
                    ConsoleLogger.LogMessage(String.Format("{0,18:S}{1:S} {2:S}",
                        "All Time: TC:",
                        TSTimeAllTime.ToString(), TSAllTime.ToStringTS(TSTimeAllTime)));

                }
            }
        }

        void worker_OnLoopFinish(object sender, EventArgs e)
        {
            IClientWorker icw = sender as IClientWorker;
            if (icw != null)
            {
                TSAllClients.AddTS(icw.GetTS());
                TSAllTime.AddTS(icw.GetTS());
                RemoveClient(icw);
            }
        }

    }
}
