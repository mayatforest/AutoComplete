/*============================================================
Class:  AsyncReadClientWorker

 * Назначение: Асинхронный многопоточный обработчик клиентов. На каждого клиента создается отдельный поток. 
 *              Поток запускает чтение данных с помощью BeginRead и останавливается в ожидании события завершения обработки. 
 *              Чтение данных осуществляется асинхронно с помощью BeginRead. Запись осуществляется синхронно в read_callback.
 *              
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoCompleteLib.Util;
using AutoCompleteLib;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace AutoCompleteServer.Client
{
    class AsyncReadClientWorkerMonitor
    {
        static AsyncReadClientWorkerMonitor obj=null;
        static object lockObj = new object();
        public static AsyncReadClientWorkerMonitor Obj
        {
            get
            {
                lock (lockObj)
                {
                    if (obj == null)
                    {
                        obj = new AsyncReadClientWorkerMonitor();
                    }
                    return obj;
                }
            }
        }

        Thread monitorthread = null;//new Thread(new ThreadStart(LoopMonitor));
        
        object lockListWait = new object();
        AutoResetEvent mreWakeup = new AutoResetEvent(true);
        ManualResetEvent mreLoopMonitorStarted = new ManualResetEvent(false);
        bool NeedFinish = false;
        List<AsyncReadClientWorker> listCli = new List<AsyncReadClientWorker>();
        Queue<AsyncReadClientWorker> queueCli = new Queue<AsyncReadClientWorker>();


        
        ManualResetEvent mrePulse = new ManualResetEvent(false);
        public void Finish()
        {
            lock (lockObj)
            {
                ConsoleLogger.LogMessage("Finish");
                if (listCli.Count == 0)
                {
                    ConsoleLogger.LogMessage("stop monitor thread");
                    if (monitorthread.ThreadState == ThreadState.Running)
                    {
                        mreLoopMonitorStarted.WaitOne();

                        NeedFinish = true;
                        mreWakeup.Set();
                        ConsoleLogger.LogMessage("stop monitor thread join");
                        monitorthread.Join();
                        ConsoleLogger.LogMessage("stop monitor thread join done");
                    }
                }

            }
        }

        public void Start()
        {
            ConsoleLogger.LogMessage("Start()");
            lock (lockObj)
            {
                if (listCli.Count == 0)
                {
                    monitorthread = new Thread(new ThreadStart(LoopMonitor));
                    NeedFinish = false;
                    ConsoleLogger.LogMessage("Recreates thread");
                    {
                        ConsoleLogger.LogMessage("Recreates thread start...");
                        monitorthread.Start();
                        mreLoopMonitorStarted.WaitOne();
                        ConsoleLogger.LogMessage("Recreates thread start...Done");
                    }
                }
            }
        }
        public void Pulse(AsyncReadClientWorker cli)
        {
            lock (lockListWait)
            {
                queueCli.Enqueue(cli);
                mreWakeup.Set();
            }
        }

        public void AddClient(AsyncReadClientWorker cli)
        {
            Start();

            lock (lockListWait)
            {
                listCli.Add(cli);
                //mreWakeup.Set();
            }

            Pulse(cli);
        }
        public void RemoveClient(AsyncReadClientWorker cli)
        {
            lock (lockListWait)
            {
                listCli.Remove(cli);
                //mreWakeup.Set();
            }
            Finish();
        }

        void LoopMonitor()
        {
            lock (lockListWait)
            {
                mreLoopMonitorStarted.Set();
            }

            while (true)
            {
                mreWakeup.WaitOne();
                ConsoleLogger.LogMessage("mreWakeup.WaitOne(); wakeuped");

                while (queueCli.Count > 0)
                {
                    AsyncReadClientWorker cli = null;
                    lock (lockListWait)
                    {
                        cli = queueCli.Dequeue();
                    }

                    if (cli != null)
                    {
                        cli.NextStep();
                    }
                }

                if (NeedFinish)
                {
                    ConsoleLogger.LogMessage("Exit from LoopMonitor!");
                    break;
                }
            }
        }

    }

    /// <summary>
    /// Асинхронный многопоточный обработчик клиентов.
    /// </summary>
    class AsyncReadClientWorker : ClientWorker
    {

        ManualResetEvent mreDone = new ManualResetEvent(false);
        
        public AsyncReadClientWorker()
        {
            //if (moni
            //monitorthread = new Thread(new ThreadStart(LoopMonitor));
        }
        
        public override string ToString()
        {
            return String.Format("AsyncReadClientWorker: finished: {0:S}",                
                isFinished
                );
        }
        /// <summary>
        /// Метод запускает поток мониторинга.
        /// </summary>
        public override bool StartLoop()
        {
            //Start();

            mreDone.Reset();
            // AddClient(this);
            base.StartLoop();
            AsyncReadClientWorkerMonitor.Obj.AddClient(this);
            
            return true;
        }


        public void NextStep()
        {
            ConsoleLogger.LogMessage("=======NextStep() state:"+State.ToString());
            switch (base.State)
            {
                case ClientState.Inited:
                    {
                        base.DoLoopWorker_Prepare();
                        AsyncReadClientWorkerMonitor.Obj.Pulse(this);
                        //NextStep();
                        break;
                    }
                case ClientState.Prepared:
                   {
                       base.DoLoopWorker_Work();
                       break;
                   }
                case ClientState.Working:
                   {
                       base.DoLoopWorker_Finish();
                       break;
                   }
            }
            ConsoleLogger.LogMessage("=======NextStep() state:" + State.ToString()+" Done");
        }

        /// <summary>
        /// Метод ожидает завершение потока обработки данных.
        /// </summary>
        public override bool WaitForDoneClient()
        {
            try
            {
                /*
                if (monitorthread != null)
                {
                    monitorthread.Join();
                }
                 */
                mreDone.WaitOne();
                //Finish();
                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error in WaitForDoneClient" + ex.ToString());
            }
            return false;
        }



        protected override EventWaitHandle DoLoopInner(NetworkStream ns, TransferState ts)
        {
            byte[] bufferread = new byte[1024*1];
            byte[] bufferwrite =null;
            int count;
            bool isDoneRead = false;

            //ManualResetEvent mreDone = new ManualResetEvent(false);

            AsyncCallback readcallback=null;            

            readcallback = ar_c =>
            {
                try
                {
                    count = ns.EndRead(ar_c);
                    ts.AddReadBytes(count);
#if VERBOSE                    
                    //if (ts.totalreadbytes % 10000 == 0) ConsoleLogger.LogMessage("read: " + ts.totalreadbytes);
#endif
                    bool EOFPresent = false;
                    if (count > 0)
                    {

                        EOFPresent = HandleBuffer(bufferread, count, answer, out bufferwrite);
                        ns.Write(bufferwrite, 0, bufferwrite.Length);

                        if (bufferwrite != null)
                        {
                            ts.AddWriteBytes(bufferwrite.Length);
                        }
                    }

                    if (EOFPresent == false && count > 0)
                    {
                        ns.BeginRead(bufferread, 0, bufferread.Length, readcallback, ts);
                    }
                    else
                    {
                        isDoneRead = true;
                    }
                }
                catch (IOException ioe)
                {
                    isDoneRead = true;
                    if (ioe.InnerException != null)
                    {
                        SocketException se = ioe.InnerException as SocketException;
                        if (se != null)
                        {
                            {
                                ConsoleLogger.LogMessage("Client closed connection! Socket error: " + se.SocketErrorCode);
                                
                                return;
                            }
                        }
                        ObjectDisposedException ode = ioe.InnerException as ObjectDisposedException;
                        if (ode != null)
                        {
                            ConsoleLogger.LogMessage("Client closed connection.");
                            
                            return;
                        }
                    }
                    throw ioe;
                }
                catch (Exception ex)
                {
                    isDoneRead = true;
                    ConsoleLogger.LogMessage("Error in readcallback: " + ex.Message);
                    
                }
                finally
                {
                    if (isDoneRead)
                    {
                        ConsoleLogger.LogMessage("isDoneRead...");
                        mreDone.Set();
                        AsyncReadClientWorkerMonitor.Obj.Pulse(this);
                        AsyncReadClientWorkerMonitor.Obj.RemoveClient(this);
                        ConsoleLogger.LogMessage("isDoneRead...Done");
                    }
                }

            };

            //начинаем асинхронное чтение данных
            ns.BeginRead(bufferread, 0, bufferread.Length, readcallback, ts);
            
            //ожидаем завершения обработки
            //mreDone.WaitOne();
            
            //обработка завершена
            //ConsoleLogger.LogMessage("Thread shutdown!");
            return mreDone;
        }
    }
}
