/*============================================================
Enum:  ClientWorkerTypes

 * Назначение: Типы доступных обработчиков клиентов.
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCompleteServer.Client
{
    /// <summary>
    /// Типы доступных обработчиков клиентов.
    /// </summary>
    public enum ClientWorkerTypes
    {
        /// <summary>
        /// Обработчик AsyncReadClientWorker
        /// </summary>
        AsyncRead = 1,
        
        /// <summary>
        /// Обработчик SimpleClientWorker
        /// </summary>
        Simple = 2
    }
}
