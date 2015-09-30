/*============================================================
Enum:  EnumError

 * Назначение: Перечень ошибок, возвращаемых функциями библиотеки.
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCompleteLib.Builders
{
    /// <summary>
    /// Перечень ошибок, возвращаемых функциями библиотеки
    /// </summary>
    public enum EnumError
    {
        /// <summary>
        /// Нет ошибки
        /// </summary>
        NoError=0,
        
        /// <summary>
        /// Входные данные не корректны.
        /// </summary>
        InvalidInputData=1,
        
        /// <summary>
        /// В ходе выполнения данных возникло исключение.
        /// </summary>
        Exception = 2,

        /// <summary>
        /// Ошибка не определена.
        /// </summary>
        Unknown_Error = 3,
        /// <summary>
        /// Объект не в нужном статусе
        /// </summary>
        Wrong_State = 4
        
    }
}
