﻿/*============================================================
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
    /// <summary>
    /// Асинхронный многопоточный обработчик клиентов.
    /// </summary>
    class AsyncReadClientWorker : ClientWorker
    {
        Thread monitorthread = null;

        public AsyncReadClientWorker()
        {
            monitorthread = new Thread(new ThreadStart(DoLoopWorker));
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
            monitorthread.Start();
            return true;
        }
        /// <summary>
        /// Метод ожидает завершение потока обработки данных.
        /// </summary>
        public override bool WaitForDoneClient()
        {
            try
            {
                if (monitorthread != null)
                {
                    monitorthread.Join();
                }
                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error in WaitForDoneClient" + ex.ToString());
            }
            return false;
        }



        protected override bool DoLoopInner(NetworkStream ns, TransferState ts)
        {
            byte[] bufferread = new byte[4096];
            byte[] bufferwrite =null;
            int count;

            ManualResetEvent mreDone = new ManualResetEvent(false);

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
                    EOFPresent = HandleBuffer(bufferread, count, answer, out bufferwrite);
                    ns.Write(bufferwrite, 0, bufferwrite.Length);
                    
                    //echo test
                    //ns.Write(bufferread, 0, count);

                    if (bufferwrite != null)
                    {
                        ts.AddWriteBytes(bufferwrite.Length);
                    }

                    if (EOFPresent == false)
                    {
                        ns.BeginRead(bufferread, 0, bufferread.Length, readcallback, ts);
                    }
                    else
                    {
                        mreDone.Set();
                    }
                }
                catch (IOException ioe)
                {
                    if (ioe.InnerException != null)
                    {
                        SocketException se = ioe.InnerException as SocketException;
                        if (se != null)
                        {
                            if (se.SocketErrorCode == SocketError.ConnectionReset)
                            {
                                ConsoleLogger.LogMessage("Client closed connection!");
                                mreDone.Set();
                                return;
                            }
                        }
                        ObjectDisposedException ode = ioe.InnerException as ObjectDisposedException;
                        if (ode != null)
                        {
                            ConsoleLogger.LogMessage("Client closed connection.");
                            mreDone.Set();
                            return;
                        }
                    }
                    throw ioe;
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogMessage("Error in readcallback: " + ex.ToString());
                    mreDone.Set();
                }
            };

            //начинаем асинхронное чтение данных
            ns.BeginRead(bufferread, 0, bufferread.Length, readcallback, ts);
            
            //ожидаем завершения обработки
            mreDone.WaitOne();
            
            //обработка завершена
            ConsoleLogger.LogMessage("Thread shutdown!");
            return true;
        }
    }
}
