/*============================================================
class:  ConsoleLogger

 * Назначение: Простой класс реализации логирования в Console.Error.
 *              
  ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace AutoCompleteLib.Util
{

    /// <summary>
    /// Простой класс реализации логирования в Console.Error.
    /// </summary>
    public class ConsoleLogger
    {

        /// <summary>
        /// Метод выводит сообщение msg в Console.Error
        /// </summary>        
        public static void LogMessage(string module, String msg)
        {
            LogMessage("[" + module + " " + msg);
        }

        /// <summary>
        /// Метод выводит сообщение msg в Console.Error
        /// </summary>        
        public static void LogMessage(String msg)
        {
//#if DEBUG
            if (msg == null) return;
            String outs = String.Format("[{0:dd.MM.yy HH:mm:ss.fff}] [{2:D}] {1:S}", DateTime.Now,msg,Thread.CurrentThread.ManagedThreadId);
            Console.Error.WriteLine(outs);
//#endif
        }
    }
}
