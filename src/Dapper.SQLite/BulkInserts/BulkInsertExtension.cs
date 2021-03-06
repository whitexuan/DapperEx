﻿using System;
using System.Collections.Generic;
using System.Data;

namespace Dapper.SQLite
{
    public static class BulkInsertExtension
    {
        /// <summary>
        /// 批量插入
        /// </summary>
        /// <param name="context">当前DbContext</param>
        /// <param name="tableName">要插入的表名称</param>
        /// <param name="dataTable">DataTable</param>
        public static int BulkInsert(this SQLiteDbContext context,string tableName,DataTable dataTable)
        {
            var provider = new BulkInsertSQLiteProvider(context);
            var result = provider.BulkInsert(tableName,dataTable);
            return result;
        }

        /// <summary>
        /// 批量插入
        /// </summary>
        /// <param name="context">当前DbContext</param>
        /// <param name="tableName">要插入的表名称</param>
        /// <param name="dataReader">IDataReader</param>
        public static int BulkInsert(this SQLiteDbContext context, string tableName, IDataReader dataReader)
        {
            var provider = new BulkInsertSQLiteProvider(context);
            var result = provider.BulkInsert(tableName, dataReader);
            return result;
        }

        /// <summary>
        /// 批量插入,不建议使用List插入、性能会受一定影响
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">当前DbContext</param>
        /// <param name="tableName">要插入的表名称</param>
        /// <param name="list">List列明必须与数据表列一致,严格大小写区分</param>
        public static int BulkInsert<T>(this SQLiteDbContext context, string tableName, IList<T> list)
        {
            var provider = new BulkInsertSQLiteProvider(context);
            var result = provider.BulkInsert(tableName,list);
            return result;
        }

    }
}
