/*-----------------------Copyright 2017 www.wlniao.com---------------------------
    文件名称：Wlniao\Aliyun\TableStore\Requests.cs
    适用环境：NETCoreCLR 1.0+
    功能描述：TableStore 请求/响应对象

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
------------------------------------------------------------------------------*/
using System.Collections.Generic;

namespace Wlniao.Aliyun.TableStore
{
    /// <summary>
    /// 获取表列表
    /// </summary>
    public class ListTableRequest
    {
    }

    /// <summary>
    /// 获取表列表结果
    /// </summary>
    public class ListTableResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public IList<string> TableNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// 获取表结构
    /// </summary>
    public class DescribeTableRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        public DescribeTableRequest(string tableName)
        {
            TableName = tableName;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
    }

    /// <summary>
    /// 获取表结构结果
    /// </summary>
    public class DescribeTableResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public TableMeta TableMeta { get; set; }
    }

    /// <summary>
    /// 创建表
    /// </summary>
    public class CreateTableRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableMeta"></param>
        /// <param name="reservedThroughput"></param>
        public CreateTableRequest(TableMeta tableMeta, CapacityUnit reservedThroughput)
        {
            TableMeta = tableMeta;
            ReservedThroughput = reservedThroughput;
        }
        /// <summary>
        /// 
        /// </summary>
        public TableMeta TableMeta { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public CapacityUnit ReservedThroughput { get; set; }
    }

    /// <summary>
    /// 创建表结果
    /// </summary>
    public class CreateTableResponse
    {
    }

    /// <summary>
    /// 删除表
    /// </summary>
    public class DeleteTableRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        public DeleteTableRequest(string tableName)
        {
            TableName = tableName;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
    }

    /// <summary>
    /// 删除表结果
    /// </summary>
    public class DeleteTableResponse
    {
    }

    /// <summary>
    /// 创建多元索引
    /// </summary>
    public class CreateSearchIndexRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="indexName"></param>
        public CreateSearchIndexRequest(string tableName, string indexName)
        {
            TableName = tableName;
            IndexName = indexName;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string IndexName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IndexSchema IndexSchema { get; set; }
    }

    /// <summary>
    /// 创建多元索引结果
    /// </summary>
    public class CreateSearchIndexResponse
    {
    }

    /// <summary>
    /// 写入行
    /// </summary>
    public class PutRowRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="condition"></param>
        /// <param name="primaryKey"></param>
        /// <param name="attributeColumns"></param>
        public PutRowRequest(string tableName, Condition condition, PrimaryKey primaryKey, AttributeColumns attributeColumns)
        {
            TableName = tableName;
            Condition = condition;
            PrimaryKey = primaryKey;
            AttributeColumns = attributeColumns;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Condition Condition { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PrimaryKey PrimaryKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public AttributeColumns AttributeColumns { get; set; }
    }

    /// <summary>
    /// 写入行结果
    /// </summary>
    public class PutRowResponse
    {
    }

    /// <summary>
    /// 更新行
    /// </summary>
    public class UpdateRowRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="condition"></param>
        /// <param name="primaryKey"></param>
        /// <param name="updateOfAttribute"></param>
        public UpdateRowRequest(string tableName, Condition condition, PrimaryKey primaryKey, UpdateOfAttribute updateOfAttribute)
        {
            TableName = tableName;
            Condition = condition;
            PrimaryKey = primaryKey;
            UpdateOfAttribute = updateOfAttribute;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Condition Condition { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PrimaryKey PrimaryKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public UpdateOfAttribute UpdateOfAttribute { get; set; }
    }

    /// <summary>
    /// 更新行结果
    /// </summary>
    public class UpdateRowResponse
    {
    }

    /// <summary>
    /// 读取行
    /// </summary>
    public class GetRowRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKey"></param>
        public GetRowRequest(string tableName, PrimaryKey primaryKey)
        {
            TableName = tableName;
            PrimaryKey = primaryKey;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PrimaryKey PrimaryKey { get; set; }
    }

    /// <summary>
    /// 读取行结果
    /// </summary>
    public class GetRowResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public PrimaryKey PrimaryKey { get; set; } = new PrimaryKey();
        /// <summary>
        /// 
        /// </summary>
        public AttributeColumns Attribute { get; set; } = new AttributeColumns();
    }

    /// <summary>
    /// 搜索
    /// </summary>
    public class SearchRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="indexName"></param>
        /// <param name="searchQuery"></param>
        public SearchRequest(string tableName, string indexName, SearchQuery searchQuery)
        {
            TableName = tableName;
            IndexName = indexName;
            SearchQuery = searchQuery;
        }
        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string IndexName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SearchQuery SearchQuery { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ColumnsToGet ColumnsToGet { get; set; }
    }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public class SearchResponse
    {
        /// <summary>
        /// 匹配总数
        /// </summary>
        public long TotalCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<Row> Rows { get; set; } = new List<Row>();
        /// <summary>
        /// 
        /// </summary>
        public bool IsAllSuccess { get; set; }
        /// <summary>
        /// 继续检索的令牌
        /// </summary>
        public byte[] NextToken { get; set; }
    }
}
