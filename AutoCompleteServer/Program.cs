/*============================================================
Program:  AutoCompleteServer

 * Назначение: Консольное приложение tcp/ip сервер построителя префиксного дерева.
 *             Признаком конца переданных данных является EOF (\x1A), Ctrl-Z в консоли.
 *             Принимает от клиентов команды вида get <строка>, выводит TopN слов клиенту.
 *              Использование
 *              
 *              1) 
 *              >AutoCompleteServer.exe <in_file> <port>
 *              Сервер загружает словарь in_file.
 *              Первая строка файла указывает кол-во строк словаря.
 *              Ожидает подключение на порту <port>.
 *              При получении данных от клиента, передает обратно список TopN слов.
 *              При ошибке ввода данных, соединение разрывается.
 *                           
 *              Вывод информационных сообщений из Console.Error может быть перенаправлен с помощью
 *              >AutoCompleteServer.exe <in_file> <port> 2>info.txt
 * 
 *              2) 
 *              >AutoCompleteServer.exe <in_file> <port> [server_type:1|2]
 *               ,где server_type - тип сервера
 *                      1-AsyncReadClientWorker
 *                      2-SimpleClientWorker               
 *               
 *               Пример:
 *              >AutoCompleteServer.exe in_file 5000 1 2>info.txt
 
 *              С консоли запущенного сервера доступны команды:
 *              <space><cr> получить информацию о текущем состоянии сервера
 *              <x><cr> завершить работу сервера 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using AutoCompleteLib.Util;
using AutoCompleteLib;
using AutoCompleteLib.Builders;
using System.IO;
using AutoCompleteServer.Server;
using AutoCompleteServer.Client;

namespace AutoCompleteServer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ConsoleLogger.LogMessage("AutoCompleteServer");
#if DEBUG                
                ConsoleLogger.LogMessage("DEBUG");
#endif
#if VERBOSE
                ConsoleLogger.LogMessage("VERBOSE");
#endif

                if (args.Length < 2)
                {
                    ConsoleLogger.LogMessage("Usage AutoCompleteServer.exe <in_file> <port> [server_type:1|2]");
                    return;
                }
                IPrefixBuilder wdc = new TriePrefixBuilder();
                string filename = args[0];
                int port = int.Parse(args[1]);
                int servertype = 1;                

                if (args.Length >= 3)
                {
                    servertype = int.Parse(args[2]);
                }
            
                //
                EnumError result=wdc.BuildData(args[0]);
                if (result != EnumError.NoError)
                {
                    ConsoleLogger.LogMessage("Cant build prefix tree: " + result.ToString());
                    return;
                }
                SimplePrefixServer srv = new SimplePrefixServer(port, wdc, (ClientWorkerTypes)servertype);
                
                result= srv.StartLoop();
                if (result != EnumError.NoError)
                {
                    ConsoleLogger.LogMessage("Server error: " + result.ToString());
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogMessage("Error: " + ex.ToString());
            }
        }
    }

    

  
}
