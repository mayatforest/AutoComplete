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
            /// Получить среднюю скорость чтения b/s.
            /// </summary>
            public double GetAvgReadSpeed(TimeSpan ts)
            {
                if (ts.TotalMilliseconds > 0)
                {
                    return ((totalreadbytes * 1000.0) ) / ts.TotalMilliseconds;
                }
                return 0;
            }
            
            /// <summary>
            /// Получить среднюю скорость записи b/s.
            /// </summary>
            public double GetAvgWriteSpeed(TimeSpan ts)
            {
                if (ts.TotalMilliseconds > 0)
                {
                    return ((totalwritebytes * 1000.0) ) / ts.TotalMilliseconds;
                }
                return 0;
            }

            private String FormatFloatKb(double v)
            {
                double vconv = 0;
                string vname = "";
                if (v < 1024)
                {
                    vconv = v;
                    vname = "b";
                }else if (v < 1024*1024)
                {
                    vconv = v/1024.0;
                    vname = "kb";
                }
                else if (v < 1024 * 1024 * 1024)
                {
                    vconv = v / (1024.0*1024.0);
                    vname = "mb";
                }
                else
                {
                    vconv = v / (1024.0*1024.0*1024.0);
                    vname = "Gb";
                }
                
                    
                    return String.Format("{0:N2}{1:S}",vconv,vname);
            }

            /// <summary>
            /// Получить текстовое представление объекта.
            /// </summary>
            public string ToStringTS(TimeSpan time)
            {                                
                return String.Format("read [{0:s} speed: {1:S}/s], write [{2:s} speed: {3:s}/s]",
                    FormatFloatKb(this.totalreadbytes),
                    FormatFloatKb(this.GetAvgReadSpeed(time)),
                    FormatFloatKb(this.totalwritebytes),
                    FormatFloatKb(this.GetAvgWriteSpeed(time))
                    );
            }
        }
}
