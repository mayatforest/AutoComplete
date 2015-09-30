/*============================================================
Enum:  EnumPrefixBuilder

 * Назначение: Перечень доступных построителей префиксного словаря
 
 ==========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoCompleteLib.Builders
{
    public enum EnumPrefixBuilder
    {
        Simple=1,
        Trie=2
    }
}
