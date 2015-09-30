/*============================================================
Class:  TransferState

 * Назначение: Класс реализует подсчет статистики по данным
 ==========================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCompleteServer.Client
{
    /// <summary>
    /// Класс реализует подсчет статистики по данным.
    /// </summary>
     public class TransferState
        {
            /// <summary>
            /// Кол-во прочитанных байт.
            /// </summary>
            public Int64 totalreadbytes { get; private set; }
            
            /// <summary>
            /// Кол-во записанных байт.
            /// </summary>   
            public Int64 totalwritebytes { get; private set; }
                      
            object lockobj = new object();
            
            public TransferState()
            {
                Reset();
            }

            public TransferState(TransferState ts)
            {
                Reset();
                AddTS(ts);
            }
            /// <summary>
            /// Сброс данных.
            /// </summary>
            public void Reset()
            {
                lock (lockobj)
                {
                    totalreadbytes = 0;
                    totalwritebytes = 0;
                }
            }
            /// <summary>
            /// Добавить данные ts к текущему объекту.
            /// </summary>
            public void AddTS(TransferState ts)
            {
                lock(lockobj)
                {
                this.totalreadbytes += ts.totalreadbytes;
                this.totalwritebytes += ts.totalwritebytes;
                }
            }
            
            /// <summary>
            /// Добавить кол-во прочитанных байт.
            /// </summary>
            public void AddReadBytes(Int64 bytecount)
            {
                lock (lockobj)
                {
                    this.totalreadbytes += bytecount;
                }
            }
            /// <summary>
            /// Добавить кол-во записанных байт.
            /// </summary>
            public void AddWriteBytes(Int64 bytecount)
            {
                lock (lockobj)
                {
                    this.totalwritebytes += bytecount;
                }
            }
            /// <summary>
            /// Получить среднию скорость чтения.
            /// </summary>
            public double GetAvgReadSpeed(TimeSpan ts)
            {
                if (ts.TotalMilliseconds > 0)
                {
                    return ((totalreadbytes * 1000.0) / 1024.0) / ts.TotalMilliseconds;
                }
                return 0;
            }
            
            /// <summary>
            /// Получить среднию скорость записи.
            /// </summary>
            public double GetAvgWriteSpeed(TimeSpan ts)
            {
                if (ts.TotalMilliseconds > 0)
                {
                    return ((totalwritebytes * 1000.0) / 1024.0) / ts.TotalMilliseconds;
                }
                return 0;
            }

            /// <summary>
            /// Получить текстовое представление объекта.
            /// </summary>
            public string ToStringTS(TimeSpan time)
            {
                return String.Format("read [{0:D}b speed: {1:F2}kb/s], write [{2:D}b speed: {3:F2}kb/s]",
                    this.totalreadbytes,
                    this.GetAvgReadSpeed(time),
                    this.totalwritebytes,
                    this.GetAvgWriteSpeed(time)
                    );
            }
        }
}
