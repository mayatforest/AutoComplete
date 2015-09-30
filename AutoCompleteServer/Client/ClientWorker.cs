/*============================================================
Class:  ClientWorker

 * Назначение: Базовый класс обработчиков клиентов.
 *             Потомкам необходимо переопределить методы
 *              DoLoopInner
 *              WaitForDoneClient
 *              StartLoop            
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using AutoCompleteLib;
using AutoCompleteLib.Util;
using AutoCompleteLib.Builders;

namespace AutoCompleteServer.Client
{
    /// <summary>
    /// Базовый класс обработчиков клиентов.
    /// </summary>
    class ClientWorker:IClientWorker
    {
        int processdcmd = 0;
        private IPrefixBuilder _pb = null;
        protected TcpClient tcpclient = null;
        public bool isFinished { get; private set; }
        private TransferState ts = new TransferState();
        public TransferState TS
        {
            get
            {
                return GetTS();
            }
        }

        public TransferState GetTS()
        {
            return new TransferState(ts);
        }

        public bool Init(TcpClient inclient, IPrefixBuilder pb)
        {
            if (pb == null) throw new ArgumentException("Not valid params", "pb");
            if (inclient == null) throw new ArgumentException("Not valid params", "inclient");

            _pb = pb;
            tcpclient = inclient;


            return true;
        }

        protected ClientWorker()
        {

        }

        public event EventHandler OnLoopFinish = delegate { };

        /// <summary>
        /// Реализация метода интерфейса IClientWorker
        /// </summary>
        public bool CloseClient()
        {
            if (tcpclient != null)
            {
                if (tcpclient.Connected)
                {
                    if (EOFSended == false)
                    {
                        tcpclient.GetStream().Write(EOFbyte, 0, EOFbyte.Length);
                        tcpclient.GetStream().Flush();
                    }
                    tcpclient.Close();
                }
            }
            return true;
        }
        
        /// <summary>
        /// Реализация метода интерфейса IClientWorker
        /// </summary>
        public virtual bool WaitForDoneClient()
        {
            return true;
        }
        
        /// <summary>
        /// Реализация метода интерфейса IClientWorker
        /// </summary>
        public virtual bool StartLoop()
        {
            return true;
        }
        protected void NotifyOnFinishLoop()
        {
            this.OnLoopFinish(this, null);
        }
        StringBuilder request = new StringBuilder();
        protected StringBuilder answer = new StringBuilder();

        /// <summary>
        /// Метод обрабатывает входной буфер с помощью HandleRequest, очищает 
        /// </summary>
        /// <returns>
        /// Возвращает признак наличия в потоке EOF
        ///</returns>
        protected bool HandleBuffer(byte[] buffer, int count,StringBuilder sbAnswer, out byte[] answerbytes)
        {
            request.Append(Encoding.ASCII.GetString(buffer, 0, count));
            bool EOFPresent = HandleRequest(request, sbAnswer);
            answerbytes = Encoding.ASCII.GetBytes(sbAnswer.ToString());
            sbAnswer.Length = 0;

            return EOFPresent;
        }


        const char CR = '\r';
        const char LF = '\n';   
        const string sCR="\r";
        const string sLF="\n";


        const String EOL = "\r\n";
        byte[] EOLbyte = Encoding.ASCII.GetBytes(EOL);

        const char EOF = '\x1A';        
        byte[] EOFbyte = new byte[] { 0x1A };
        private bool EOFSended = false;
        private bool CommandSended = false;

        /// <summary>
        /// Обрабатывает входные данные из request и выводит данные в outsb. Входные данные могут содержать несколько команд, 
        /// в том числе не полных. Не обработанные данные остаются в request. Обработанные команды удаляются из request.
        /// </summary>
        /// <returns>
        /// Возвращает признак наличия в потоке EOF
        ///</returns>
        protected bool HandleRequest(StringBuilder request, StringBuilder outsb)
        {
            bool EOFpresent = false;
            try
            {


                String reqstr = request.ToString();

                if (reqstr.Contains(EOF))
                {
                    EOFpresent = true;
                    reqstr = reqstr.Replace(EOF, CR);
#if VERBOSE
                    ConsoleLogger.LogMessage("Recv EOF");
#endif
                }
                int idxred = 0;
                int totprocessed = 0;
                idxred = reqstr.IndexOf(LF);
                while (reqstr.Length >= 1 && idxred >= 0)
                {
                    String onecmd = "";
                    if (idxred >= 0 || EOFpresent)
                    {
                        if (idxred >= 0)
                        {
                            //вырезаем из буфера команду
                            onecmd = reqstr.Substring(0, idxred + 1);
                            reqstr = reqstr.Remove(0, idxred + 1);
                            totprocessed += idxred + 1;
                        }
                        if (CommandSended)
                        {
                            outsb.Append(EOL);
                        }
                        ProcessCommand(onecmd, outsb);
                        CommandSended = true;

                        idxred = reqstr.IndexOf(LF);
                    }
                }
                if (totprocessed > 0)
                {
                    request.Remove(0, totprocessed);
                }

                if (EOFpresent)
                {
                    outsb.Append(EOF);
                    EOFSended = true;
#if VERBOSE
                    ConsoleLogger.LogMessage("Send EOF");
#endif
                }
            }
            catch (WrongCommandException wce)
            {
                ConsoleLogger.LogMessage("WrongCommandException " + wce.Message);
                EOFpresent = true;
                EOFSended = true;
                outsb.Append(EOF);
            }
            return EOFpresent;
        }

        private String GetPrefixNoCRLF(String prefix)
        {
            prefix = prefix.Replace(sCR, "");
            prefix = prefix.Replace(sLF, "");
            return prefix;
        }
        /// <summary>
        /// Обрабатывает одну строку с командой вида get <строка>. Разделитель - один пробел. Если команда не соответствует 
        /// стандарту генерируется исключение.
        /// </summary>
        protected void ProcessCommand(String command, StringBuilder outsb)
        {
            processdcmd++;
            
            string[] arg=command.Split(' ');
            if (arg.Length != 2)
            {
                throw new WrongCommandException("Wrong line: " + command);
                //return;
            }
            if (arg[0] != "get")
            {
                throw new WrongCommandException("Wrong command: " + arg[0]);
            }
            String prefix= arg[1];
            
            
            //prefix = prefix.Replace(sCR, "");
            //prefix = prefix.Replace(sLF, "");
            prefix = GetPrefixNoCRLF(prefix);
            if (prefix == "")
            {
                return;
            }

            List<string> list = _pb.GetPrefixWords(prefix);
            if (list != null)
            {
                foreach (string s in list)
                {
                    outsb.Append(s);
                    outsb.Append(EOL);
                }
            }
        }

        /// <summary>
        /// Метод должен быть переопределен в потомках
        /// </summary>
        protected virtual bool DoLoopInner(NetworkStream ns, TransferState ts)
        {
            return false;
        }

        /// <summary>
        /// Метод замеряет время работы DoLoopInner. По завершении закрывает соединение, вызывает NotifyOnFinishLoop
        /// </summary>        
        protected void DoLoopWorker()
        {
            TimerUtil tu = new TimerUtil();
            ts.Reset();
            try
            {
                if (tcpclient == null) return;
                ConsoleLogger.LogMessage("Accepted client");

                NetworkStream ns = tcpclient.GetStream();

                DoLoopInner(ns, ts);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Exception: " + ex.Message);
            }
            finally
            {
                TimeSpan tuTS = tu.GetInterval();
                
                ConsoleLogger.LogMessage(String.Format("TC:{0:S} {1:S}",
                    tu.GetInterval(), ts.ToStringTS(tuTS)));

                CloseClient();

                isFinished = true;
                NotifyOnFinishLoop();
            }

        }

    }
    
    class WrongCommandException : Exception
    {
        public WrongCommandException(String msg)
            : base(msg)
        {

        }
    }
     
}
