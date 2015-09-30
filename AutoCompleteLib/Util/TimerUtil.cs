/*============================================================
class:  TimerUtil

 * Назначение: Класс замера времени выполнения с использованием System.Diagnostics.Stopwatch
 *              
  ==========================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace AutoCompleteLib
{

    /// <summary>
    /// Класс замера времени выполнения с использованием System.Diagnostics.Stopwatch.
    /// </summary>
    public class TimerUtil
    {
        System.Diagnostics.Stopwatch sw = null;

        TimeSpan lastts = new TimeSpan();
        public TimerUtil()
        {
            sw = new Stopwatch();
            MarkInterval();

        }
        
        /// <summary>
        /// Пометить интервал и сбросить таймер. Интервал может быть получен с помощью функции GetLastIntervalTime.
        /// </summary>
        public void MarkInterval()
        {
            sw.Stop();
            lastts = sw.Elapsed;
            sw.Reset();
            sw.Start();
        }
        
        /// <summary>
        /// Получить последний отмеченный интервал.
        /// </summary>
        public TimeSpan GetLastIntervalTime()
        {
            return lastts;
        }
        
        /// <summary>
        /// Получить текущее время с начала интервала.
        /// </summary>
        public TimeSpan GetInterval()
        {

            return sw.Elapsed;
        }
        /// <summary>
        /// Остановить таймер
        /// </summary>
        public void Stop()
        {
            sw.Stop();
        }
        /// <summary>
        /// Сбросить таймер
        /// </summary>
        public void Reset()
        {
            sw.Reset();
        }
    }
}
