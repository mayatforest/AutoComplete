/*============================================================
Class:  ClientStream

 * Назначение: Реализация tcp/ip клиента, данные читаются из Console.In, 
 *             отправляются серверу, данные от сервера выводятся в Console.Out.
 *             Обмен данными завершается по EOF (Ctrl-Z)
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoCompleteLib;
using System.Net.Sockets;
using System.Threading;
using AutoCompleteLib.Util;
using System.IO;

namespace AutoCompleteClient.Client
{

    /// <summary>
    /// Реализация tcp/ip клиента
    /// </summary>
    class ClientStream
    {
        NetworkStream stream;
        bool thrNeedExit = false;

        ManualResetEvent mrethrNeedExit = new ManualResetEvent(false);

        readonly string hostname="";
        readonly int port = 0;

        /// <summary>
        /// Включение режима циклической отправки предустановленной строки
        /// </summary>
        public bool loopenable = false;

        /// <summary>
        /// Строка для отправки в циклическом режиме
        /// </summary>
        public string loopstring = "";

        /// <summary>
        /// Кол-во повторов для отправки в циклическом режиме
        /// </summary>
        public int loopmaxcnt =0;
        
        public ClientStream(string hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
        }

        const char EOF = '\x1A';
        byte[] EOFbyte = new byte[] { 0x1A };

        const String EOL = "\r\n";
        byte[] EOLbyte = Encoding.ASCII.GetBytes(EOL);

        ManualResetEvent mreServerEndData = new ManualResetEvent(false);

        public void DoLoop()
        {
            TimerUtil tu = new TimerUtil();
            TimerUtil tcReadWrite = new TimerUtil();
            tcReadWrite.Stop();
            tcReadWrite.Reset();

            try
            {

                Thread thrReadStream_WriteConsole = new Thread(new ThreadStart(ReadStream_WriteConsole));
                Thread thrReadConsole_WriteStream = new Thread(new ThreadStart(ReadConsole_WriteStream));
                
                TcpClient client = new TcpClient(hostname, port);
                client.NoDelay = true;
                
                stream = client.GetStream();
                
                tcReadWrite.MarkInterval();

                //запускаем потоки
                thrReadStream_WriteConsole.Start();
                thrReadConsole_WriteStream.Start();

                WaitHandle[] wh = new WaitHandle[]
                    {
                        mreServerEndData
                    };
                WaitHandle.WaitAll(wh);
                //server ends data abort thrReadConsoleWriteStream and wait for thrReadStreamWriteConsole
                //thrReadConsole_WriteStream.Abort(); this not work, see http://stackoverflow.com/questions/9479573/interrupt-console-readline


                thrReadStream_WriteConsole.Join();
                thrReadConsole_WriteStream.Join();
#if VERBOSE

                ConsoleLogger.LogMessage("WaitHandle.WaitAll(wh); exit");
#endif
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error in DoLoop: "+ex.Message);
            }finally
            {
                ConsoleLogger.LogMessage("TC:" + tu.GetInterval() + " TCReadWrite: " + tcReadWrite.GetInterval());
            }
        }

        public void ReadConsole_WriteStream()
        {
            try
            {
                bool ConsoleEOFPresent = false;                             
                
                //бесконечный цикл чтения данных из Console.In
                //прерывание происходит по концу данных EOF, Ctrl-Z в консоли
                //или по достижению loopcnt
                int loopcnt = 0;
                while (true)
                {
                    String readeds = null;
                    if (ServerEOFpresent == false && ConsoleEOFPresent==false)
                    {
                        #region readfromconsole
                        if (loopenable)
                        {
                            loopcnt++;
                            if (loopcnt > loopmaxcnt)
                            {
                                readeds = null;
                            }
                            else
                            {
                                readeds = loopstring;
                            }
                        }
                        else
                        {
                            readeds = Console.ReadLine();
                        }
                        #endregion

                        if (readeds != null)
                        {
                            ConsoleEOFPresent = readeds.IndexOf(EOF)>=0;
                        }
                        else
                        {
                            ConsoleEOFPresent = true;
                            readeds = ""+EOF;
                        }

                        if (readeds != null)
                        {

                            Byte[] data = null;

                            if (ConsoleEOFPresent == false)
                            {
                                data = System.Text.Encoding.ASCII.GetBytes(readeds+EOL);
                            }
                            else
                            {
                                data = System.Text.Encoding.ASCII.GetBytes(readeds);
                            }

                            if (data.Length > 0)
                            {
                                if (ServerEOFpresent==false)
                                {
                                    stream.Write(data, 0, data.Length);
                                }
                            }
                        }

                        if (ConsoleEOFPresent)
                        {
                            break;
                        }

                    }

                    if (ServerEOFpresent)
                    {
                        break;
                    }

                
                }
                ConsoleLogger.LogMessage("Shutdown!");
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage(ex.Message);
            }
            finally
            {
//                ConsoleLogger.LogMessage("thrReadConsole_WriteStream exit:");
            }
        }
        bool ServerEOFpresent = false;
        int totalwrited = 0;

        /// <summary>
        /// Метод чтения данных из networkstream и отправка в Console.Out
        /// </summary>
        private void ReadStream_WriteConsole()
        {
            byte[] buffer = new byte[1024*16];
            try
            {

            ManualResetEvent mreFinish = new ManualResetEvent(false);      
            AsyncCallback readcallback=null;

            WaitHandle[] wh = new WaitHandle[]
                    {
                        mreFinish
                    };

            readcallback = ar_c =>
            {
                try
                {
                    int len = stream.EndRead(ar_c);
                    if (len > 0)
                        {
                            totalwrited += len;
                            String sr = System.Text.Encoding.ASCII.GetString(buffer, 0, len);
                            if (sr.IndexOf(EOF)>=0)
                            {
                                ServerEOFpresent = true;
                                sr = sr.Replace(EOF.ToString(), "");
                            }
                            if (sr != String.Empty)
                            {
                                Console.Out.Write(sr);
                                Console.Out.Flush();
                            }
                        }
                    if (len == 0)
                    {
                        mreFinish.Set();
                        return;
                    }
                    //if (!thrNeedExit)
                    {
                        stream.BeginRead(buffer,0,buffer.Length,readcallback,null);
                    }
                }
                catch (Exception ex)
                {
                    ConsoleLogger.LogMessage(ex.Message);
                    mreFinish.Set();
                }
            };
             
                stream.BeginRead(buffer,0,buffer.Length,readcallback,null);
                int res=WaitHandle.WaitAny(wh);
                ServerEOFpresent=true;
                mreServerEndData.Set();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("ReadData: " + ex.Message);
            }
            finally
            {
                stream.Close();
#if VERBOSE
                ConsoleLogger.LogMessage("main: stream.close()");

                ConsoleLogger.LogMessage("ReadData: exit!");
#endif           
            }
        }
    }
}
