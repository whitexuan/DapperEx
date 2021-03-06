﻿using System;
using System.Configuration;
using System.Data;
using System.Data.SQLite;

namespace Dapper.SQLite
{
    public class SQLiteDbContext : DbContext
    {
        /// <summary>
        /// 初始化连接字符
        /// </summary>
        /// <param name="connectionStringName">连接字符串名称</param>
        public SQLiteDbContext(string connectionStringName)
        {
            if (string.IsNullOrWhiteSpace(connectionStringName))
                throw new Exception("connectionStringName不能为空");

            var configurationManager = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (string.IsNullOrEmpty(configurationManager.ProviderName))
                throw new Exception("请设置connectionStringName的providerName");

            SetAdapter(EnmDbType.SQLite);

            CreateConnection(configurationManager.ConnectionString);
        }

        /// <summary>
        /// 建立DbConnection
        /// </summary>
        /// <param name="connectionString"></param>
        private void CreateConnection(string connectionString)
        {
            Connection = new SQLiteConnection(connectionString);

            if (Connection.State != ConnectionState.Open && Connection.State != ConnectionState.Connecting)
                Connection.Open();
        }

        /// <summary>
        /// 返回DataTable
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public DataTable SqlQueryDataTable(string sql,object param = null)
        {
            var dataAdapter = new SQLiteDataAdapter(sql,(SQLiteConnection)Connection);
            if (param != null)
                dataAdapter.SelectCommand.Parameters.Add(param);

            DataTable dt = new DataTable();

            dataAdapter.Fill(dt);
            dataAdapter.SelectCommand.Parameters.Clear();

            return dt;
        }
    }
}
