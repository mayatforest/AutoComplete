/*============================================================
Program:  AutoCompleteClient

 * Назначение: Консольное приложение tcp/ip клиент для работы с сервером построителя префиксного дерева.
 *             Признаком конца переданных данных является EOF (\x1A), Ctrl-Z в консоли.
 *              Использование
 *              
 *              1) вариант
 *              >AutoCompleteClient.exe <server> <port>
 *              Подключается к серверу <server>:<port>. Из стандартного ввода читает строку 
 *              вида <get> <слово> и возвращает список TopN слов в стандартный вывод.
 *              
 *              Ввод/вывод может быть перенаправлен с помощью стандартных методов.
 *              >AutoCompleteClient.exe <server> <port> <in_file.txt >out_file.txt
 *              
 *              Вывод информационных сообщений из Console.Error может быть перенаправлен с помощью
 *              >AutoCompleteClient.exe <server> <port> <in_file.txt >out_file.txt 2>info.txt
 * 
 *               2) вариант
 *               AutoCompleteClient.exe <server> <port> [loop_cnt loop_string]
 *               Подключается к серверу <server>:<port>. В цикле loop_cnt отправляет строку loop_string серверу.
 *               Можно использовать для тестирования пропускной способности. 
 *               
 *               Пример:
 *               AutoCompleteClient.exe localhost 5000 100000 "get abc" >nul 2>info.txt
 *               
 * 
 * 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using AutoCompleteLib.Util;
using AutoCompleteLib;
using AutoCompleteClient.Client;

namespace AutoCompleteClient
{
    class Program
    {

        static void Main(string[] args)
        {
            ConsoleLogger.LogMessage("AutoCompleteClient");
#if DEBUG
            ConsoleLogger.LogMessage("DEBUG");
#endif
#if VERBOSE
                ConsoleLogger.LogMessage("VERBOSE");
#endif
            
            try
            {
                if (args.Length < 2)
                {
                    ConsoleLogger.LogMessage("Usage AutoCompleteClient.exe <server> <port> [loop_cnt loop_string]");
                    return;
                }
                ClientStream client = new ClientStream(args[0], int.Parse(args[1]));
                if (args.Length >= 4)
                {
                    client.loopenable=true;
                    client.loopmaxcnt = int.Parse(args[2]);
                    client.loopstring = (args[3]);
                }
                client.DoLoop();
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error in main " + ex.Message);
            }
        }
            
        

    }
}
