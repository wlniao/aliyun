/*==============================================================================
    文件名称：SwithCaseExtend.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：switch/case组扩展
================================================================================

示例：
    string typeName = string.Empty;
        typeId.Switch((string s) => typeName = s)
            .Case(1, "男")
            .Case(2, "女")
            .Default("未知");

================================================================================
 
    Copyright 2014 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/

using System;
using Aliyun.TableStore.DataModel;
namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ObjectExtend
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public static void SetProperty(this Object input, String key, ColumnValue val)
        {
            if (val.Type == ColumnValueType.Integer)
            {
                Wlniao.Runtime.Reflection.SetPropertyValue(input, key, val.IntegerValue);
            }
            else if (val.Type == ColumnValueType.String)
            {
                if (string.IsNullOrEmpty(val.StringValue))
                {
                    throw new EmptyPrimaryKeyException();
                }
                Wlniao.Runtime.Reflection.SetPropertyValue(input, key, val.StringValue);
            }
            else if (val.Type == ColumnValueType.Double)
            {
                Wlniao.Runtime.Reflection.SetPropertyValue(input, key, val.DoubleValue);
            }
            else if (val.Type == ColumnValueType.Boolean)
            {
                Wlniao.Runtime.Reflection.SetPropertyValue(input, key, val.BooleanValue);
            }
            else if (val.Type == ColumnValueType.Binary)
            {
                Wlniao.Runtime.Reflection.SetPropertyValue(input, key, val.BinaryValue);
            }
        }
    }
}