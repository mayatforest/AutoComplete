/*============================================================
Class:  SimpleClientWorker

 * Назначение: Простой многопоточный обработчик клиентов. На каждого клиента создается отдельный поток. 
 *              Чтение/запись осуществляется синхронно.
 *              
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using AutoCompleteLib.Util;
using AutoCompleteLib;
using System.Threading;

namespace AutoCompleteServer.Client
{
    /// <summary>
    /// Простой многопоточный обработчик клиентов.
    /// </summary>
    class SimpleClientWorker : ClientWorker
    {      
        Thread workerthread = null;

        public SimpleClientWorker()
        {
            workerthread = new Thread(new ThreadStart(DoLoopWorker));
        }
        
        public override string ToString()
        {            
            return String.Format("SimpleClientWorker: TH:[{0:D}] state: {1:S}", 
                workerthread!=null?workerthread.ManagedThreadId:0,
                base.State.ToString()
                );
        }
        /// <summary>
        /// Запуск потока обработки.
        /// </summary>
        public override bool StartLoop()
        {
            workerthread.Start();
            return true;
        }
        /// <summary>
        /// Ожидает завершение потока обработки данных
        /// </summary>
        public override bool WaitForDoneClient()
        {
            try
            {
                if (workerthread != null)
                {
                    workerthread.Join();
                }
                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error in WaitForDoneClient" + ex.ToString());
            }
            return false;
        }
        
        /// <summary>
        /// Обработчик данных. Чтение/Запись данных осуществляется синхронно.
        /// </summary>
        protected override EventWaitHandle DoLoopInner(NetworkStream ns, TransferState ts)
        {
            byte[] bufferread = new byte[4096];
            byte[] bufferwrite = null;
            int count;
            ts.Reset();

            while ((count = ns.Read(bufferread, 0, bufferread.Length)) > 0)
            {
                ts.AddReadBytes(count);
#if VERBOSE
                //if (totalreadbytes % 10000 == 0) ConsoleLogger.LogMessage("read: " + totalreadbytes);
#endif

                bool EOFPresent = HandleBuffer(bufferread, count,answer, out bufferwrite);
                ns.Write(bufferwrite, 0, bufferwrite.Length);

                ts.AddWriteBytes(bufferwrite.Length);
                if (EOFPresent) break;
            }
            return new ManualResetEvent(true);
        }




    }
}
