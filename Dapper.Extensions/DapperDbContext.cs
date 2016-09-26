﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using Dapper.Contrib.Extensions;
using Dapper.Linq;
using Dapper.Linq.Builder.Clauses;
using DbType = Dapper.Data.DbType;

namespace Dapper
{
    public class DapperDbContext :IDisposable
    {
        /// <summary>
        /// 数据库连接
        /// </summary>
        public IDbConnection Connection;

        /// <summary>
        /// 数据库事物对象
        /// </summary>
        public IDbTransaction Transaction { get; set; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DbType DbType { get; set; }

        /// <summary>
        /// 初始化连接字符
        /// </summary>
        /// <param name="connectionStringName">连接字符串名称</param>
        public DapperDbContext(string connectionStringName)
        {
            if (string.IsNullOrWhiteSpace(connectionStringName))
                throw new Exception("connectionStringName不能为空");

            var configurationManager = ConfigurationManager.ConnectionStrings[connectionStringName];

            SetDbType(configurationManager.ProviderName);

            CreateConnection(configurationManager.ConnectionString);
        }

        /// <summary>
        /// 建立DbConnection
        /// </summary>
        /// <param name="connectionString"></param>
        private void CreateConnection(string connectionString)
        {
            if (DbType == DbType.MSSQL)
            {
                Connection = new SqlConnection(connectionString);
            }

            if (Connection.State != ConnectionState.Open && Connection.State != ConnectionState.Connecting)
                Connection.Open();
        }

        /// <summary>
        /// 适配数据库类型
        /// </summary>
        /// <param name="providerName"></param>
        private void SetDbType(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
                throw new Exception("请设置connectionStringName的providerName");


            switch (providerName)
            {
                case "System.Data.SqlClient":
                    DbType = DbType.MSSQL;
                    break;
                case "MySqlClient":
                    DbType = DbType.MySQL;
                    break;
                default:
                    DbType = DbType.MSSQL;
                    break;
            }

        }

        /// <summary>
        /// 开始事务操作
        /// </summary>
        public void BeginTransaction()
        {
            Transaction = Connection.BeginTransaction();
        }
        
        /// <summary>
        /// 提交数据库事务
        /// </summary>
        public void Commit()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
            }
        }

        /// <summary>
        /// 数据库事务回滚
        /// </summary>
        public void Rollback()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
            }
        }

        /// <summary>
        /// 转换成可使用Linq的操作对象
        /// </summary>
        /// <typeparam name="T">当前T</typeparam>
        /// <returns></returns>
        public IQuery<T> Query<T>(Expression<Func<T,bool>> expression=null)
        {
            return new Query<T>(Connection,Transaction,expression);
        }

        /// <summary>
        /// 插入方法
        /// </summary>
        /// <param name="t">当前T</param>
        /// <returns></returns>
        public long Add<T>(T t) where T : class
        {
            return Connection.Insert(t,Transaction);
        }

        /// <summary>
        /// 单条删除方法
        /// </summary>
        /// <param name="t">当前T</param>
        /// <returns></returns>
        public bool Delete<T>(T t) where T : class
        {
            return Connection.Delete(t,Transaction);
        }

        /// <summary>
        /// 单表清空方法
        /// </summary>
        /// <typeparam name="T">当前T</typeparam>
        /// <returns></returns>
        public bool DeleteAll<T>() where T : class
        {
            return Connection.DeleteAll<T>(Transaction);
        }

        /// <summary>
        /// 更新方法
        /// </summary>
        /// <param name="t">当前T</param>
        /// <returns>返回更新的</returns>
        public bool Update<T>(T t) where T : class
        {
            return Connection.Update(t);
        }

        /// <summary>
        /// 批量删除 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int Delete<T>(Expression<Func<T,bool>> expression) where T : class
        {
            if (expression == null) return 0;
            var builder = new SqlBuilder<T>();
            var resolve =new  WhereExpressionVisitor<T>();
            resolve.Evaluate(expression,builder);
            string sql = $"DELETE [{builder.TableAliasName}] FROM {builder.Table} {builder.Where}";
            return Connection.Execute(sql, builder.Parameters, Transaction);
        }

        /// <summary>
        /// 批量修改
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereExpression"></param>
        /// <param name="updateExpression"></param>
        /// <returns></returns>
        public int Update<T>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T>> updateExpression)
            where T : class
        {
            if (whereExpression == null)
                return 0;
            var builder = new SqlBuilder<T>();
            var resolve = new WhereExpressionVisitor<T>();
            resolve.Evaluate(whereExpression,builder);

            string set = string.Empty;
            var expression = (MemberInitExpression) updateExpression.Body;
            int i = 0;
            int bindingCount = expression.Bindings.Count;
            foreach (var binding in expression.Bindings)
            {
                i++;
                var name = binding.Member.Name;
                object value;
                var memberExpression = ((MemberAssignment) binding).Expression;

                var constantExpression = memberExpression as ConstantExpression;
                if (constantExpression != null)
                {
                    value = constantExpression.Value;
                }
                else
                {
                    var lambda = Expression.Lambda(memberExpression, null);
                    value = lambda.Compile().DynamicInvoke();
                }
                set += $"[{builder.TableAliasName}].[{name}]=@{name}";
                if (i < bindingCount) set += ", ";
                builder.Parameters.Add(name, value);
            }

            string sql = $"UPDATE [{builder.TableAliasName}] SET {set}  FROM {builder.Table} {builder.Where}";

            return Connection.Execute(sql, builder.Parameters, Transaction);
        }

        /// <summary>
        /// 增删改SQL执行方法
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <param name="commandType">执行方式</param>
        /// <returns></returns>
        public int ExecuteSqlCommand(string sql,object param,CommandType? commandType = null)
        {
            return Connection.Execute(sql: sql, param: param, transaction: Transaction, commandTimeout: null,
                commandType: commandType);
        }

        /// <summary>
        /// 查询纯SQL方法
        /// </summary>
        /// <param name="sql">sql查询语句</param>
        /// <param name="param">sql参数</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public List<T> SqlQuery<T>(string sql, object param, CommandType? commandType = null)
        {
            return Connection.Query<T>(sql: sql, param: param, transaction: Transaction, buffered: true,
                commandTimeout: null, commandType: commandType).AsList();
        }

        /// <summary>
        /// 查询纯SQL方法
        /// </summary>
        /// <param name="sql">sql查询语句</param>
        /// <param name="param">sql参数</param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public List<T> SqlPageQuery<T>(string table,string where,string select,string order,object parametes,int pageIndex,int pageSize,out int total)
        {
            total = 0;
            if (pageIndex < 1)
                pageIndex = 1;

            var sql = new SqlAdapter().QueryStringPage(table,select,where,order,pageSize,pageIndex);

            var data = Connection.Query<T>(sql: sql,param: parametes,transaction: Transaction).AsList();

            if (data != null && data.Count > 0 && pageSize > 0)
            {
                total = Connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {table} {where}",parametes);
            }

            return data;
        }

        public void Dispose()
        {
            if (Connection != null && Connection.State != ConnectionState.Closed)
                Connection.Close();
            if (Transaction != null)
                Transaction.Dispose();
        }
    }
}
