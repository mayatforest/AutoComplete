/*============================================================
Interface:  IClientWorker

 * Назначение: Интерфейс работы с классом обслуживания клиента
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using AutoCompleteLib;
using AutoCompleteLib.Builders;

namespace AutoCompleteServer.Client
{
    /// <summary>
    /// Интерфейс работы с классом обслуживания клиента.
    /// </summary>
    interface IClientWorker
    {

        /// <summary>
        /// Инициализация клиента с помощью TcpClient и IPrefixBuilder
        /// </summary>
        /// <param name="inclient">Подключенный клиент</param>
        /// <param name="pb">Построитель префиксного дерева с заполненными данными</param>
        bool Init(TcpClient inclient, IPrefixBuilder pb);
        
        /// <summary>
        /// Событие о завершении работы с клиентом
        /// </summary>
        event EventHandler OnLoopFinish;

        /// <summary>
        /// Выполнить закрытие клиента. Не блокирующий вызов. Для завершения работы с клиентом, необходимо вызвать WaitForDoneClient.
        /// </summary>
        bool CloseClient();

        /// <summary>
        /// Выполнить ожидание завершения работы с клиентом. Метод блокирующий.
        /// </summary>
        bool WaitForDoneClient();


        /// <summary>
        /// Запустить обработку данных от клиента. Метод не блокирующий
        /// </summary>
        bool StartLoop();
        
        /// <summary>
        /// Получить данные по обмену данными
        /// </summary>
        TransferState GetTS();
    
    }
}
