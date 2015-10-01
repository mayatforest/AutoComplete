/*============================================================
Class:  PrefixBuilder

 * Назначение: Базовый класс реализации IPrefixBuilder.
 *              Содержит общие методы работы с потоками данных и замерами времени работы.
 *              Потомки должны переопределить методы 
 *              AddNewDictItem
 *              GetPrefixWords
 *              
 * ChangeList:
 *              v0.2 *Изменен PrefixBuilder на абстрактный класс
 *              v0.1 Первоначальная версия. 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AutoCompleteLib.Builders;
using AutoCompleteLib.Util;
using System.Threading;

namespace AutoCompleteLib
{
    /// <summary>
    /// PrefixBuilder Реализация базового класса построения префиксных данных 
    /// </summary>
    public abstract class PrefixBuilder : IPrefixBuilder
    {
        private int DicCnt = 0;
        
        //Максимальное число TopN слов
        protected int topNWords = 10;

        //по ТЗ максимально число входных слов.
        const int MaxInWords = 100000;

        protected PrefixBuilder()
        {

        }
        
        /// <summary>
        /// Реализация метода интерфейса IPrefixBuilder
        /// </summary>
        public EnumError Init(int topN)
        {
            topNWords = topN;
            return EnumError.NoError;
        }
                

        /// <summary>
        /// Реализация метода интерфейса IPrefixBuilder
        /// </summary>
        public EnumError UseDataConsole()
        {

            return DoBuildAndUse(
                new StreamReader(Console.OpenStandardInput()),
                new StreamWriter(Console.OpenStandardOutput())
            );
        }
        
        /// <summary>
        /// Реализация метода интерфейса IPrefixBuilder
        /// </summary>        
        public EnumError DoBuildAndUseFiles(String inFilePath, String outFilePath)
        {
            if (!File.Exists(inFilePath))
            {
                ConsoleLogger.LogMessage("Cant read input file: " + inFilePath);
                return EnumError.InvalidInputData;
            }
            System.IO.TextReader indata = new StreamReader(inFilePath);
            System.IO.TextWriter outdata = new StreamWriter(outFilePath);
            return DoBuildAndUse(indata, outdata);
            
        }
        
        /// <summary>
        /// Реализация метода интерфейса IPrefixBuilder
        /// </summary>
        public EnumError BuildData(string inFilePath)
        {
            if (!File.Exists(inFilePath))
            {
                return EnumError.InvalidInputData;
            }
            System.IO.TextReader indata = new StreamReader(inFilePath);
            return BuildData(indata);
        }

        private EnumError BuildData(System.IO.TextReader indata)
        {
            TimeSpan ts = new TimeSpan();
            return BuildData(indata, out ts);
        }

        private EnumError BuildData(System.IO.TextReader indata, out TimeSpan tstime)
        {
            TimerUtil tbuild = new TimerUtil();
            tstime = new TimeSpan();
            try
            {
                ConsoleLogger.LogMessage("BuildData() " + this.GetType().ToString());
                if (indata == null)
                {
                    throw new NullReferenceException("indata stream is null");
                }


                int cnt = 0;
                if (int.TryParse(indata.ReadLine(), out cnt) == false)
                {
                    ConsoleLogger.LogMessage("Cant parse dict cnt");
                    return EnumError.InvalidInputData;
                }

                //Проверка кол-во входных слов
                if (cnt < 0 || cnt > MaxInWords)
                {
                    ConsoleLogger.LogMessage("Wrong dict cnt " + cnt);
                    return EnumError.InvalidInputData;
                }

                ConsoleLogger.LogMessage("Uses DicCnt: " + cnt.ToString());

                this.DicCnt = cnt;

                for (int i = 1; i <= this.DicCnt; i++)
                {
#if VERBOSE
                        if (((i * 100.0) / this.DicCnt) % 10 == 0) ConsoleLogger.LogMessage("Readed: " + i.ToString() + " / " + this.DicCnt.ToString());
#endif
                    String wline = indata.ReadLine();
                    if (wline == null)
                    {
                        ConsoleLogger.LogMessage("Unexpected end of data");
                        return EnumError.InvalidInputData;
                    }
                    string[] items = wline.Split(' ');
                    if (items.Length == 2)
                    {
                        this.AddNewDictItem(items[0], int.Parse(items[1]));
                    }
                    else
                    {
                        ConsoleLogger.LogMessage("Wrong line: " + wline);
                        //throw new Exception("Wrong line: " + wline);
                        return EnumError.InvalidInputData;
                    }
                }
                AfterAllRead();
                return EnumError.NoError;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage(ex.ToString());
                return EnumError.Exception;
            }
            finally
            {
                tbuild.MarkInterval();
                tstime = tbuild.GetLastIntervalTime();

                ConsoleLogger.LogMessage(String.Format("TC: build: {0:S} compare: {1:D}",
                    tbuild.GetLastIntervalTime().ToString(),
                    WordItem.CompareCallCnt
                    ));
            }

            return EnumError.Unknown_Error;
        }

        private object lockobj = new object();
        StringBuilder outsb = new StringBuilder();
//        ManualResetEvent mreFinish = new ManualResetEvent(false);
        ManualResetEvent mrenewData = new ManualResetEvent(false);

        private EnumError WriteData(System.IO.TextReader indata, System.IO.TextWriter outdata,out TimeSpan tstime)
        {            
            TimerUtil tuse = new TimerUtil();
            tstime = new TimeSpan();
            try
            {
                    ConsoleLogger.LogMessage("WriteData() " + this.GetType().ToString());
                    tuse.MarkInterval();
                    int cnttest = 0;
                    if (int.TryParse(indata.ReadLine(), out cnttest) == false)
                    {
                        return EnumError.InvalidInputData;
                    }

                    if (cnttest > 0)
                    {
                        for (int i = 1; i <= cnttest; i++)
                        {
#if VERBOSE                            
                            if (((i * 100.0) / cnttest) % 10 == 0) ConsoleLogger.LogMessage("Writed: " + i.ToString() + " / " + cnttest.ToString());
#endif                            
                            if (i > 1)
                            {
                                outdata.WriteLine("");
                            }
                            //
                            string curprefix = indata.ReadLine();
                            List<String> autocompletewords = GetPrefixWords(curprefix);
                            if (autocompletewords != null)
                            {
                                foreach (string str in autocompletewords)
                                {
                                    outdata.WriteLine(str);
                                }
                            }
                            mrenewData.Set();
                        }
                    }                    
                    return EnumError.NoError;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage(ex.Message);
                return EnumError.Exception;
            }
            finally
            {
                tuse.MarkInterval();
                tstime = tuse.GetLastIntervalTime();

                ConsoleLogger.LogMessage(String.Format("TC: use: {0:S} compare: {1:D}",
                    tuse.GetLastIntervalTime().ToString(),
                    WordItem.CompareCallCnt
                    ));
            }

            return EnumError.Unknown_Error;
        }
        /// <summary>
        /// Метод читает данные из потока, строит словарь, читает проверочные данные и записывает их в выходной поток 
        /// </summary>
        protected EnumError DoBuildAndUse(System.IO.TextReader indata, System.IO.TextWriter outdata)
        {
            TimerUtil tall = new TimerUtil();
            TimeSpan tsbuild = new TimeSpan();
            TimeSpan tsuse = new TimeSpan();
            EnumError result = EnumError.Unknown_Error;
            if (indata == null) return EnumError.InvalidInputData;
            if (outdata == null) return EnumError.InvalidInputData;


            try
            {
                //Читаем из потока данные для построения префиксного словаря
                result=BuildData(indata, out tsbuild);
                
                //Если при построении префиксного словаря произошла ошибка выходим из функции.
                if (result != EnumError.NoError)
                {
                    return result;
                }
                
                //Продолжаем читать из потока данные для поиска префиксов и вывода TopN слов из словаря
                result=WriteData(indata,outdata,out tsuse);
                
                return result;
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage(ex.ToString());
                return EnumError.Exception;
            }
            finally
            {
                tall.MarkInterval();

                if (indata != null) { indata.Close(); };
                if (outdata != null) { outdata.Close(); };

                ConsoleLogger.LogMessage("All Done");
                ConsoleLogger.LogMessage(String.Format("TC: all {0:S} build: {1:S} use {2:S}, compare: {3:D}",
                    tall.GetLastIntervalTime().ToString(),
                    tsbuild.ToString(),
                    tsuse.ToString(),
                    WordItem.CompareCallCnt
                    ));

            }

            return EnumError.Unknown_Error;
        }
        /// <summary>
        /// Метод вызывается после окончания чтения словаря
        /// </summary>     
        protected virtual int AfterAllRead()
        {
            return 0;
        }

        /// <summary>
        /// Метод добавления элемента в словарь.
        /// </summary>
        /// <param name="pword">Слово для добавления в словарь.</param>
        /// <param name="pCnt">Частота употребления слова.</param>
        protected virtual EnumError AddNewDictItem(String pword, int pCnt)
        {
            return EnumError.Unknown_Error;
        }

        /// <summary>
        /// Реализация метода интерфейса IPrefixBuilder
        /// </summary>
        public virtual List<string> GetPrefixWords(String prefix)
        {
            return new List<string>();
        }
    }
}
