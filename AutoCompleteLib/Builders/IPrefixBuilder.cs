/*============================================================
Interface:  IPrefixBuilder

 * Назначение: Интерфейс работы с построителем префиксного словаря.
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCompleteLib.Builders
{
    /// <summary>
    /// Интерфейс работы с построителем префиксного словаря.
    /// </summary>
    public interface IPrefixBuilder
    {
        /// <summary>
        /// Инициализация с указанием числа TOP слов для составления. Если метод не вызван используется - 10.
        /// </summary>
        /// <param name="topN">Число самых популярных слов</param>
        EnumError Init(int topN);

        /// <summary>
        /// Чтение и запись данных в консоль, формат данных указан в ТЗ.
        /// </summary>
        EnumError UseDataConsole();

        /// <summary>
        /// Чтение и запись данных из/в отдельные файлы, формат данных указан в ТЗ.
        /// </summary>
        /// <param name="inFilePath">Путь к входному файлу со словарем и проверочными строками</param>
        /// <param name="outFilePath">Путь к выходному файлу с TopN строками</param>
        EnumError DoBuildAndUseFiles(String inFilePath, String outFilePath);

        /// <summary>
        /// Построение данных на основе файла словаря. Первой строкой в входном файле должно быть указано число строк в нем.
        /// </summary>        
        /// <param name="inFilePath">Путь к входному файлу со словарем</param>
        EnumError BuildData(string inFilePath);


        /// <summary>
        /// Получить список TopN слов для указанного префикса.
        /// </summary>        
        /// <param name="prefix">Строка префикса</param>
        /// <returns>
        /// Список TopN слов для указанного префикса.
        /// </returns> 
        List<string> GetPrefixWords(String prefix);
    }
}
