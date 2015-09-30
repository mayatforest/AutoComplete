/*============================================================
Class:  ClientWorker

 * Назначение: Базовый класс обработчиков клиентов.
 *             Потомкам необходимо переопределить методы
 *              DoLoopInner
 *              WaitForDoneClient
 *              StartLoop        
 *              
 * ChangeList:
 *              v0.2 *Общий прирост скорости (Client+Server) после оптимизации ~50%-300% (до 20мб/сек на Xeon) 
 *                    в зависимости от кол-ва параллельных клиентов.
 *                   +Добавлена поддержка статуса обработчика ClientState, вызов функции с неправильным статусом 
 *                    генерирует исключение.
 *                   *Оптимизированы размеры буферов приема/передачи
 *                   +Оптимизированы функции работы с буфером.
 *                   +Добавлен метод NextStep() для возможности реализации обработчиков по паттерну async/await.
 *                   +Добавлены функции тестирования производительности TestHandleBuffer и ProcessCommandMockUp.
 *              
 *              v0.1 Первоначальная версия.
 * 
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using AutoCompleteLib;
using AutoCompleteLib.Util;
using AutoCompleteLib.Builders;
using System.Threading;

namespace AutoCompleteServer.Client
{
    /// <summary>
    ///  Статус обработчика
    /// </summary>
    enum ClientState
    {
        /// <summary>
        ///  Статус не известен
        /// </summary>
        Unknown,
        
        /// <summary>
        /// Инициализирован
        /// </summary>
        Inited,
        
        /// <summary>
        ///  Подготовка к запуску обработки
        /// </summary>
        Prepare_Work,
        
        /// <summary>
        ///  Готов к запуску обработки
        /// </summary>
        Prepared,
        
        /// <summary>
        ///  Идет обработка
        /// </summary>
        Working,
        
        /// <summary>
        ///  Подготовка к завершению
        /// </summary>
        Shutdown_in_Progress,
        
        /// <summary>
        ///  Обработка закончена
        /// </summary>
        Finished
    }

    /// <summary>
    /// Базовый класс обработчиков клиентов.
    /// </summary>
    class ClientWorker:IClientWorker
    {
        #region public
        public bool isFinished
        {
            get
            {
                return State == ClientState.Finished;
            }
        }
        public ClientState State { get; private set; }

        
        public TransferState TS
        {
            get
            {
                return GetTS();
            }
        }
        #endregion

        #region private
        private IPrefixBuilder _pb = null;
        private TransferState ts = new TransferState();
        private Int64 processdcmd = 0;

        protected TcpClient tcpclient = null;

        public TransferState GetTS()
        {
            return new TransferState(ts);
        }
        #endregion


        public bool Init(TcpClient inclient, IPrefixBuilder pb)
        {
            if (pb == null) throw new ArgumentException("Not valid params", "pb");
            if (inclient == null) throw new ArgumentException("Not valid params", "inclient");

            _pb = pb;
            tcpclient = inclient;
            State = ClientState.Inited;

            return true;
        }

        protected ClientWorker()
        {
            State = ClientState.Unknown;
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
            //TestHandleBuffer(500000/64);
            return true;
        }
        

        protected void NotifyOnFinishLoop()
        {
            this.OnLoopFinish(this, null);
        }
        StringBuilder request = new StringBuilder();
        protected StringBuilder answer = new StringBuilder();

        void QuickAndDirtyAsciiEncode(string chars, byte[] buffer)
        {
            int length = chars.Length;
            for (int i = 0; i < length; i++)
            {
                buffer[i] = (byte)(chars[i] & 0x7f);
            }
        }

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
        const String EOFS = "\x1A";
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

                if (reqstr.IndexOf(EOF)>=0)
                {
                    EOFpresent = true;
                    reqstr = reqstr.Replace(EOFS, "");
#if VERBOSE
                    ConsoleLogger.LogMessage("Recv EOF");
#endif
                }
                int idxred = 0;
                int totprocessed = 0;
                idxred = reqstr.IndexOf(LF);
                while ((reqstr.Length >= 1 && EOFpresent) || idxred >= 0)
                {
                    String onecmd = "";
                    if (EOFpresent && idxred==-1)
                    {
                        idxred = reqstr.Length - 1;
                    }
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
        
        private bool SplitCommandFast(String command, out string prefix)
        {
            prefix = String.Empty;

            int idx_space = command.IndexOf(' ');
            if (idx_space >= 0)
            {
                if (idx_space != 3)
                {
                    throw new WrongCommandException("Wrong command: " + command);
                }
                string cmd = command.Substring(0, idx_space);
                if (cmd != "get")
                {
                    throw new WrongCommandException("Wrong command: " + command);
                }

                prefix = command.Substring(idx_space + 1, command.Length - idx_space - cmd.Length + 2);
                return true;
            }
            return false;
        }

        private bool SplitCommand(String command,out string prefix)
        {
            prefix = String.Empty;

            string[] arg = command.Split(' ');
            if (arg.Length != 2)
            {
                throw new WrongCommandException("Wrong line: " + command);
                //return;
            }
            if (arg[0] != "get")
            {
                throw new WrongCommandException("Wrong command: " + arg[0]);
            }
            prefix = arg[1];
            return true;
        }
        #region ProcessCommandMockUp
        StringBuilder tmpsb = null;
        protected void ProcessCommandMockUp(String command, StringBuilder outsb)
        {
            processdcmd++;
            String prefix = String.Empty;

            command = GetPrefixNoCRLF(command);

            if (command == String.Empty) return;

            SplitCommandFast(command, out prefix);


            if (prefix == String.Empty)
            {
                throw new WrongCommandException("Wrong command: " + command);
            }

            //string prefix = "bbbbcaacba";
            {
                //////
                if (tmpsb == null)
                {
                    tmpsb = new StringBuilder();
                    tmpsb.Append("bbbbcaacbaab");
                    tmpsb.Append(EOL);
                    tmpsb.Append("bbbbcaacba");
                    tmpsb.Append(EOL);
                    tmpsb.Append("bbbbcaacbaabcb");
                    tmpsb.Append(EOL);
                    tmpsb.Append("bbbbcaacbaa");
                    tmpsb.Append(EOL);
                    tmpsb.Append("bbbbcaacbaabc");
                    tmpsb.Append(EOL);
                }
                else
                {
                    outsb.Append(tmpsb.ToString());
                }
                return;
                //////
            }
        }
        #endregion

        /// <summary>
        /// Обрабатывает одну строку с командой вида get строка. Разделитель - один пробел. Если команда не соответствует 
        /// стандарту генерируется исключение.
        /// </summary>
        protected void ProcessCommand(String command, StringBuilder outsb)
        {
            processdcmd++;
            String prefix = String.Empty;

            command = GetPrefixNoCRLF(command);

            if (command == String.Empty) return;

            SplitCommandFast(command, out prefix);
            
            
            if (prefix == String.Empty)
            {
                throw new WrongCommandException("Wrong command: " + command);
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
        ///  Метод выполняет следующий по статусу метод обработчика 
        ///  DoLoopWorker_Prepare->DoLoopWorker_Work->DoLoopWorker_Finish
        ///  Необходим для реализации паттерна async/await.
        ///  Позволит реализовать модификацию AsyncReadClientWorker без создания дополнительных потоков.
        /// </summary>
        public void NextStep()
        {
#if VERSBOSE            
            ConsoleLogger.LogMessage("=======NextStep() state:" + State.ToString());
#endif
            switch (State)
            {
                case ClientState.Inited:
                    {
                        DoLoopWorker_Prepare();
                        break;
                    }
                case ClientState.Prepared:
                    {
                        DoLoopWorker_Work();
                        break;
                    }
                case ClientState.Working:
                    {
                        DoLoopWorker_Finish();
                        break;
                    }
            }
#if VERSBOSE            
            ConsoleLogger.LogMessage("=======NextStep() state:" + State.ToString() + " Done");
#endif
        }

        /// <summary>
        /// Метод должен быть переопределен в потомках
        /// </summary>
        protected virtual EventWaitHandle DoLoopInner(NetworkStream ns, TransferState ts)
        {
            return null;
        }

        TimerUtil tuLoopWorker = new TimerUtil();
        NetworkStream nsLoopWorker = null;

        protected void DoLoopWorker_Prepare()
        {
            if (State != ClientState.Inited)
            {
                throw new Exception("DoLoopWorker_Prepare Not valid state!" + State);
            }
            if (tcpclient == null) return;
            ConsoleLogger.LogMessage("Accepted client");

            nsLoopWorker = tcpclient.GetStream();

            tuLoopWorker.Reset();
            tuLoopWorker.Start();
            State = ClientState.Prepared;
        }
        
        protected EventWaitHandle DoLoopWorker_Work()
        {
            if (State != ClientState.Prepared)
            {
                throw new Exception("DoLoopWorker_Work Not valid state!" + State);
            }
            try
            {
                State = ClientState.Working;
                tuLoopWorker.MarkInterval();
                EventWaitHandle ewh= DoLoopInner(nsLoopWorker, ts);
                return ewh;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error: " + ex.Message);
            }            
            return null;
        }

        protected void DoLoopWorker_Finish()
        {
            if (State != ClientState.Working)
            {
                throw new Exception("DoLoopWorker_Finish Not valid state!" + State);
            }
            try
            {
                State = ClientState.Shutdown_in_Progress;

                TimeSpan tuTS = tuLoopWorker.GetInterval();

                ConsoleLogger.LogMessage(String.Format("{0,18:S}{1:S} {2:S}",
                    "Client Exit TC:",
                    tuTS, ts.ToStringTS(tuTS)));

                CloseClient();

                State = ClientState.Finished;                
                NotifyOnFinishLoop();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error: " + ex.Message);
            }
            finally
            {
                State = ClientState.Finished;
            }
        }


        /// <summary>
        /// Метод замеряет время работы DoLoopInner.
        /// По завершении закрывает соединение, вызывает NotifyOnFinishLoop в DoLoopWorker_Finish()
        /// </summary>        
        protected void DoLoopWorker()
        {
            try
            {
                DoLoopWorker_Prepare();

                DoLoopWorker_Work();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Exception: " + ex.Message);
            }
            finally
            {
                DoLoopWorker_Finish();
            }
        }


        #region TestHandleBufferFunc
        private void TestHandleBuffer(int rptcnt)
        {
            StringBuilder sb = new StringBuilder();
            string bufcmd = "get bbbbcaacba";
            int cmdcnt = 1024 / (bufcmd.Length + 2);
            for (int i = 0; i < cmdcnt; i++)
            {
                sb.Append(bufcmd + EOL);
            }

            byte[] buffer = Encoding.ASCII.GetBytes(sb.ToString());
            byte[] answer_bytes = null;
            StringBuilder answer = new StringBuilder();
            TransferState testts = new TransferState();
            TimerUtil tu = new TimerUtil();

            for (int hbi = 1; hbi < rptcnt; hbi++)
            {
                HandleBuffer(buffer, buffer.Length, answer, out answer_bytes);
                ts.AddReadBytes(buffer.Length);
                ts.AddWriteBytes(answer_bytes.Length);
            }
            ConsoleLogger.LogMessage(String.Format("TestHandleBuffer Cnt: {0:D} {1:S} {2:S}", rptcnt, tu.GetInterval(), ts.ToStringTS(tu.GetInterval())));
            //throw new Exception("1");
        }
        #endregion
    }
    
    class WrongCommandException : Exception
    {
        public WrongCommandException(String msg)
            : base(msg)
        {

        }
    }
     
}
