/*============================================================
Program:  AutoCompleteConsole

 * Назначение: Консольное приложение для работы с построителем префиксного дерева.
 *              Использование
 *              
 *              1) 
 *              >AutoCompleteConsole.exe
 *                  Читает данные из Console.In и выводит в Console.Out.
 *              
 *              Ввод/вывод может быть перенаправлен с помощью стандартных методов.
 *              >AutoCompleteConsole.exe <in_file.txt >out_file.txt
 *              
 *              Вывод информационных сообщений из Console.Error может быть перенаправлен с помощью
 *              >AutoCompleteConsole.exe <in_file.txt >out_file.txt 2>info.txt
 *              
 *              2) 
 *              >AutoCompleteConsole.exe [prefixbuilder:1|2] <in_file> <out_file>
 *               ,где prefixbuilder - тип построителя 
 *                      1-SimplePrefixBuilder
 *                      2-TriePrefixBuilder
 *              <in_file> - входной файл
 *              <out_file> - выходной файл
 *               
 *              Вывод информационных сообщения из Console.Error может быть перенаправлен с помощью
 *              >AutoCompleteConsole.exe [prefixbuilder:1|2] <in_file> <out_file> 2>info.txt
 * 
 * Описание формата входных данных:
 * В первой строке находится единственное число N (1 ≤ N ≤ 10^5) — количество слов в найденных текстах. 
 * Каждая из следующих N строк содержит слово wi (непустая последовательность строчных латинских 
 * букв длиной не более 15) и целое число ni (1 ≤ ni ≤ 10^6) — число раз, которое встречается это слово в текстах.
 * Слово и число разделены единственным пробелом. Ни одно слово не повторяется более одного раза.
 * В (N + 2)-й строке находится число M (1 ≤ M ≤ 15000).
 * В следующих M строках содержатся слова ui (непустая последовательность
 * строчных латинских букв длиной не более 15) — начала слов, введенных пользователем. 
 * 
 ==========================================================*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoCompleteLib;
using AutoCompleteLib.Builders;
using AutoCompleteLib.Util;
using System.IO;

namespace AutoCompleteConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            EnumError result = EnumError.NoError;
            try
            {
                IPrefixBuilder wdc = null;

                if (args.Length > 0)
                {
                    if (args.Length != 3)
                    {
                        ConsoleLogger.LogMessage("\r\nUsage AutoCompleteConsole.exe\r\n\r\nor\r\n\r\nUsage AutoCompleteConsole.exe [prefixbuilder:1|2] <in_file> <out_file>");
                        return;
                    }
                    switch (int.Parse(args[0]))
                    {
                        case (int)EnumPrefixBuilder.Simple:
                            {
                                wdc = new SimplePrefixBuilder();
                                break;
                            }
                        case (int)EnumPrefixBuilder.Trie:
                            {
                                wdc = new TriePrefixBuilder();
                                break;
                            }
                        default:
                            {
                                wdc = new TriePrefixBuilder();
                                break;
                            }

                    }
                    wdc.Init(10);

                    result = wdc.DoBuildAndUseFiles(args[1], args[2]);
#if DEBUG
                    ConsoleLogger.LogMessage("Press Enter to continue");
                    Console.Read();
#endif

                }
                else
                {
                    wdc = new TriePrefixBuilder();
                    result = wdc.UseDataConsole();
                }
                if (result != EnumError.NoError)
                {
                    ConsoleLogger.LogMessage("Code return with error: result: " + result.ToString());
                }
            }
            catch (IOException ioe)
            {
                ConsoleLogger.LogMessage("Ошибка ввода/вывода " + ioe.Message);
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error: "+ex.Message);
            }
        }
    }
}
