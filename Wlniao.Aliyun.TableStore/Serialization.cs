﻿/*==============================================================================
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
using Aliyun.TableStore.Response;
using Aliyun.TableStore.DataModel;
using System.Collections.Generic;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// 序列化/反序列化
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="row"></param>
        /// <returns></returns>
        public static T ConvertTo<T>(Row row)
        {
            if (row == null || row.PrimaryKey.Count == 0)
            {
                return default(T);
            }
            try
            {
                var obj = System.Activator.CreateInstance<T>();
                foreach (KeyValuePair<string, ColumnValue> entry in row.PrimaryKey)
                {
                    obj.SetProperty(entry.Key, entry.Value);
                }
                foreach (KeyValuePair<string, ColumnValue> entry in row.AttributeColumns)
                {
                    obj.SetProperty(entry.Key, entry.Value);
                }
                return obj;
            }
            catch (EmptyPrimaryKeyException ex)
            {
                return default(T);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="res"></param>
        /// <returns></returns>
        public static T ConvertTo<T>(GetRowResponse res)
        {
            if (res == null || res.PrimaryKey.Count == 0)
            {
                return default(T);
            }
            try
            {
                var obj = System.Activator.CreateInstance<T>();
                foreach (KeyValuePair<string, ColumnValue> entry in res.PrimaryKey)
                {
                    obj.SetProperty(entry.Key, entry.Value);
                }
                foreach (KeyValuePair<string, ColumnValue> entry in res.Attribute)
                {
                    obj.SetProperty(entry.Key, entry.Value);
                }
                return obj;
            }
            catch (EmptyPrimaryKeyException ex)
            {
                return default(T);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}