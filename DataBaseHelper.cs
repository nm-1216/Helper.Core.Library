﻿/*
 * 作用：数据库增/删/改/查操作，SqlServer 支持批量导入以及分页查询。
 * 联系：QQ 100101392
 * 来源：https://github.com/snipen/Helper.Core.Library
 * */
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Data.Common;
using System.Linq.Expressions;
using Helper.Core.Library.Translator;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace Helper.Core.Library
{
    #region 逻辑处理辅助枚举
    /// <summary>
    /// 数据库类型
    /// </summary>
    public class DataBaseTypeEnum
    {
        /// <summary>
        /// Sql
        /// </summary>
        public const string Sql = "sql";
        /// <summary>
        /// MySql
        /// </summary>
        public const string MySql = "mysql";
    }
    /// <summary>
    /// 数据操作类型（用在 Transaction 开头的事务函数中）
    /// </summary>
    public class DataBaseExecuteTypeEnum
    {
        public const string ExecuteNonQuery = "ExecuteNonQuery";
        public const string ExecuteScalar = "ExecuteScalar";
        public const string ToEntityList = "ToEntityList";
        public const string ToEntity = "ToEntity";
    }
    /// <summary>
    /// 数据库分页处理所需字段
    /// </summary>
    public class DataBaseParameterEnum
    {
        /// <summary>
        /// 查询语句，表连接查询时，主表用 T 代替
        /// </summary>
        public const string FieldSql = "FieldSql";
        /// <summary>
        /// 查询语句，值为空时，与 FieldSql 字段相同
        /// </summary>
        public const string Field = "Field";
        /// <summary>
        /// 表名称
        /// </summary>
        public const string TableName = "TableName";
        /// <summary>
        /// 主键名称
        /// </summary>
        public const string PrimaryKey = "PrimaryKey";
        /// <summary>
        /// 页索引，从 1 开始
        /// </summary>
        public const string PageIndex = "PageIndex";
        /// <summary>
        /// 页大小
        /// </summary>
        public const string PageSize = "PageSize";
        /// <summary>
        /// Where 语句
        /// </summary>
        public const string WhereSql = "WhereSql";
        /// <summary>
        /// Order 语句
        /// </summary>
        public const string OrderSql = "OrderSql";
        /// <summary>
        /// 表连接语句 例： inner join B on A.XX=B.XX
        /// </summary>
        public const string JoinSql = "JoinSql";
    }
    #endregion

    public class DataBaseHelper
    {
        #region 私有属性常量

        private const string INSERT_FIELD_SQL = ",{0}{1}";
        private const string INSERT_FIELD_PARAMETER_SQL = ",@{0}{1}";
        private const string INSERT_SQL = "insert into {0}({1})values({2})";

        private const string UPDATE_FIELD_PARAMETER_SQL = ",{0}=@{1}{2}";
        private const string UPDATE_SQL = "update {0} set {1} where {2}";

        private const string DELETE_SQL = "delete from {0} where {1}";

        private static readonly object lockItem = new object();
        private static readonly Dictionary<string, Dictionary<PropertyInfo, string>> PropertyAttributeDict = new Dictionary<string, Dictionary<PropertyInfo, string>>();

        #region 异常消息
        private const string PaginationNotSupportException = "此函数目前不支持 Oracle 数据库！";
        private const string BatchImportException = "此函数目前只支持 Sql Server 数据库！";
        #endregion

        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        private static string ConnectionString = "";
        /// <summary>
        /// 数据库类型
        /// </summary>
        private static string DataBaseType = null;
        #endregion

        #region 对外公开方法

        #region 初始化数据库连接
        /// <summary>
        /// 初始化数据库连接字符串和数据库类型
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="dataBaseEnum">数据库类型</param>
        public static void InitConnectionString(string connectionString, string dataBaseType = DataBaseTypeEnum.Sql)
        {
            ConnectionString = connectionString;
            DataBaseType = dataBaseType;
        }
        #endregion

        #region Insert 插入指定字段数据
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="data">数据</param>
        /// <param name="ignorePropertyList">忽略字段列表</param>
        /// <returns></returns>
        public static bool Insert(string tableName, object data, string[] ignorePropertyList = null)
        {
            return Insert(null, null, tableName, data, ignorePropertyList);
        }
        /// <summary>
        /// 插入数据（异步）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="data">数据</param>
        /// <param name="ignorePropertyList">忽略字段列表</param>
        /// <returns></returns>
        public static async Task<bool> InsertAsync(string tableName, object data, string[] ignorePropertyList = null)
        {
            return await InsertAsync(null, null, tableName, data, ignorePropertyList);
        }
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="tableName">表名</param>
        /// <param name="data">数据</param>
        /// <param name="ignorePropertyList">忽略字段列表</param>
        /// <returns></returns>
        public static bool Insert(string connectionString, string dataBaseType, string tableName, object data, string[] ignorePropertyList = null)
        {
            return ExecuteInsert(connectionString, dataBaseType, tableName, data, ignorePropertyList, null, null);
        }
        /// <summary>
        /// 插入数据（异步）
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="tableName">表名</param>
        /// <param name="data">数据</param>
        /// <param name="ignorePropertyList">忽略字段列表</param>
        /// <returns></returns>
        public static async Task<bool> InsertAsync(string connectionString, string dataBaseType, string tableName, object data, string[] ignorePropertyList = null)
        {
            return await ExecuteInsertAsync(connectionString, dataBaseType, tableName, data, ignorePropertyList, null, null);
        }
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool Insert<T>(object data, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return Insert<T>(null, null, data, ignoreLambda, tableName);
        }
        /// <summary>
        /// 插入数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> InsertAsync<T>(object data, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return await InsertAsync<T>(null, null, data, ignoreLambda, tableName);
        }
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool Insert<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            tableName = GetDataBaseTableName<T>(tableName);
            return Insert(connectionString, dataBaseType, tableName, data, ignoreLambda != null ? CommonHelper.GetExpressionList(ignoreLambda).ToArray() : null);
        }
        /// <summary>
        /// 插入数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> InsertAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            tableName = GetDataBaseTableName<T>(tableName);
            return await InsertAsync(connectionString, dataBaseType, tableName, data, ignoreLambda != null ? CommonHelper.GetExpressionList(ignoreLambda).ToArray() : null);
        }
        /// <summary>
        /// 插入数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool TransactionInsert<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            tableName = GetDataBaseTableName<T>(tableName);
            return ExecuteInsert(null, null, tableName, data, ignoreLambda != null ? CommonHelper.GetExpressionList(ignoreLambda).ToArray() : null, con, transaction);
        }
        /// <summary>
        /// 插入数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> TransactionInsertAsync<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            tableName = GetDataBaseTableName<T>(tableName);
            return await ExecuteInsertAsync(null, null, tableName, data, ignoreLambda != null ? CommonHelper.GetExpressionList(ignoreLambda).ToArray() : null, con, transaction);
        }
        #endregion

        #region Update 根据条件更新指定字段数据，必须指定 Where 条件
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool Update<T>(object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return Update<T>(null, null, data, whereLambda, ignoreLambda, tableName);
        }
        /// <summary>
        /// 更新数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> UpdateAsync<T>(object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return await UpdateAsync<T>(null, null, data, whereLambda, ignoreLambda, tableName);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool Update<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return ExecuteUpdate<T>(connectionString, dataBaseType, data, whereLambda, ignoreLambda, tableName, null, null);
        }
        /// <summary>
        /// 更新数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> UpdateAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return await ExecuteUpdateAsync<T>(connectionString, dataBaseType, data, whereLambda, ignoreLambda, tableName, null, null);
        }
        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool TransactionUpdate<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return ExecuteUpdate<T>(null, null, data, whereLambda, ignoreLambda, tableName, con, transaction);
        }
        /// <summary>
        /// 更新数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="ignoreLambda">忽略字段表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> TransactionUpdateAsync<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null) where T : class
        {
            return await ExecuteUpdateAsync<T>(null, null, data, whereLambda, ignoreLambda, tableName, con, transaction);
        }
        #endregion

        #region Delete 根据条件删除数据，必须指定 Where 条件
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool Delete<T>(object data, Expression<Func<T, bool>> whereLambda, string tableName = null) where T : class
        {
            return Delete<T>(null, null, data, whereLambda, tableName);
        }
        /// <summary>
        /// 删除数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync<T>(object data, Expression<Func<T, bool>> whereLambda, string tableName = null) where T : class
        {
            return await DeleteAsync<T>(null, null, data, whereLambda, tableName);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool Delete<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, string tableName = null) where T : class
        {
            return ExecuteDelete<T>(connectionString, dataBaseType, data, whereLambda, tableName, null, null);
        }
        /// <summary>
        /// 删除数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, string tableName = null) where T : class
        {
            return await ExecuteDeleteAsync<T>(connectionString, dataBaseType, data, whereLambda, tableName, null, null);
        }
        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static bool TransactionDelete<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, bool>> whereLambda, string tableName = null) where T : class
        {
            return ExecuteDelete<T>(null, null, data, whereLambda, tableName, con, transaction);
        }
        /// <summary>
        /// 删除数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <returns></returns>
        public static async Task<bool> TransactionDeleteAsync<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, bool>> whereLambda, string tableName = null) where T : class
        {
            return await ExecuteDeleteAsync<T>(null, null, data, whereLambda, tableName, con, transaction);
        }
        #endregion

        #region First 根据条件返回查询字段中的首行数据
        /// <summary>
        /// 返回单个字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="K">简单类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static K First<T, K>(object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, bool withNoLock = true, string tableName = null) where T : class
        {
            return First<T, K>(null, null, data, queryLambda, whereLambda, tableName, withNoLock);
        }
        /// <summary>
        /// 返回单个字段（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="K">简单类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<K> FirstAsync<T, K>(object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, bool withNoLock = true, string tableName = null) where T : class
        {
            return await FirstAsync<T, K>(null, null, data, queryLambda, whereLambda, tableName, withNoLock);
        }
        /// <summary>
        /// 返回单个字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="K">简单类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static K First<T, K>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, string tableName = null, bool withNoLock = true) where T : class
        {
            return ExecuteFirst<T, K>(connectionString, dataBaseType, data, queryLambda, whereLambda, withNoLock, tableName, null, null);
        }
        /// <summary>
        /// 返回单个字段（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="K">简单类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<K> FirstAsync<T, K>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, string tableName = null, bool withNoLock = true) where T : class
        {
            return await ExecuteFirstAsync<T, K>(connectionString, dataBaseType, data, queryLambda, whereLambda, withNoLock, tableName, null, null);
        }
        /// <summary>
        /// 返回单个字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="K">简单类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static K TransactionFirst<T, K>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, string tableName = null, bool withNoLock = true) where T : class
        {
            return ExecuteFirst<T, K>(null, null, data, queryLambda, whereLambda, withNoLock, tableName, con, transaction);
        }
        /// <summary>
        /// 返回单个字段（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="K">简单类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<K> TransactionFirstAsync<T, K>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, string tableName = null, bool withNoLock = true) where T : class
        {
            return await ExecuteFirstAsync<T, K>(null, null, data, queryLambda, whereLambda, withNoLock, tableName, con, transaction);
        }
        #endregion

        #region Exists 根据条件判断某个字段数据是否存在
        /// <summary>
        /// 是否存在
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="identityID">唯一标识，自增编号</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static bool Exists<T>(object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true) where T : class
        {
            return Exists<T>(null, null, data, queryLambda, whereLambda, identityID, tableName, withNoLock);
        }
        /// <summary>
        /// 是否存在（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="identityID">唯一标识，自增编号</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<bool> ExistsAsync<T>(object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true) where T : class
        {
            return await ExistsAsync<T>(null, null, data, queryLambda, whereLambda, identityID, tableName, withNoLock);
        }
        /// <summary>
        /// 是否存在
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="identityID">唯一标识，自增编号</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static bool Exists<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true) where T : class
        {
            return ExecuteExists<T>(connectionString, dataBaseType, data, queryLambda, whereLambda, identityID, tableName, withNoLock, null, null);
        }
        /// <summary>
        /// 是否存在（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="identityID">唯一标识，自增编号</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<bool> ExistsAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true) where T : class
        {
            return await ExecuteExistsAsync<T>(connectionString, dataBaseType, data, queryLambda, whereLambda, identityID, tableName, withNoLock, null, null);
        }
        /// <summary>
        /// 是否存在
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="identityID">唯一标识，自增编号</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static bool TransactionExists<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true) where T : class
        {
            return ExecuteExists<T>(null, null, data, queryLambda, whereLambda, identityID, tableName, withNoLock, con, transaction);
        }
        /// <summary>
        /// 是否存在（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="identityID">唯一标识，自增编号</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<bool> TransactionExistsAsync<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true) where T : class
        {
            return await ExecuteExistsAsync<T>(null, null, data, queryLambda, whereLambda, identityID, tableName, withNoLock, con, transaction);
        }
        #endregion

        #region Count 根据条件返回结果总数
        /// <summary>
        /// 查询总数
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static int Count<T>(object data, Expression<Func<T, bool>> whereLambda = null, string tableName = null, bool withNoLock = true) where T : class
        {
            return Count<T>(null, null, data, whereLambda, tableName, withNoLock);
        }
        /// <summary>
        /// 查询总数（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<T>(object data, Expression<Func<T, bool>> whereLambda = null, string tableName = null, bool withNoLock = true) where T : class
        {
            return await CountAsync<T>(null, null, data, whereLambda, tableName, withNoLock);
        }
        /// <summary>
        /// 查询总数
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static int Count<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda = null, string tableName = null, bool withNoLock = true) where T : class
        {
            return ExecuteCount<T>(connectionString, dataBaseType, data, whereLambda, withNoLock, tableName, null, null);
        }
        /// <summary>
        /// 查询总数（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static async Task<int> CountAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda = null, string tableName = null, bool withNoLock = true) where T : class
        {
            return await ExecuteCountAsync<T>(connectionString, dataBaseType, data, whereLambda, withNoLock, tableName, null, null);
        }
        /// <summary>
        /// 查询总数
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <returns></returns>
        public static int TransactionCount<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, bool>> whereLambda = null, string tableName = null, bool withNoLock = true) where T : class
        {
            return ExecuteCount<T>(null, null, data, whereLambda, withNoLock, tableName, con, transaction);
        }
        /// <summary>
        /// 查询总数（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        public static async Task<int> TransactionCountAsync<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, bool>> whereLambda = null, string tableName = null, bool withNoLock = true) where T : class
        {
            return await ExecuteCountAsync<T>(null, null, data, whereLambda, withNoLock, tableName, con, transaction);
        }
        #endregion

        #region Single 根据条件查询单条数据
        /// <summary>
        /// 返回单条查询语句
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T Single<T>(object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return Single<T>(null, null, data, queryLambda, whereLambda, tableName, propertyMatchList, withNoLock, reflectionType);
        }
        /// <summary>
        /// 返回单条查询语句（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<T> SingleAsync<T>(object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await SingleAsync<T>(null, null, data, queryLambda, whereLambda, tableName, propertyMatchList, withNoLock, reflectionType);
        }
        /// <summary>
        /// 返回单条查询语句
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T Single<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ExecuteSingle<T>(connectionString, dataBaseType, data, queryLambda, whereLambda, withNoLock, tableName, propertyMatchList, reflectionType, null, null);
        }
        /// <summary>
        /// 返回单条查询语句（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        public static async Task<T> SingleAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await ExecuteSingleAsync<T>(connectionString, dataBaseType, data, queryLambda, whereLambda, withNoLock, tableName, propertyMatchList, reflectionType, null, null);
        }
        /// <summary>
        /// 返回单条查询语句
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T TransactionSingle<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ExecuteSingle<T>(null, null, data, queryLambda, whereLambda, withNoLock, tableName, propertyMatchList, reflectionType, con, transaction);
        }
        /// <summary>
        /// 返回单条查询语句（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<T> TransactionSingleAsync<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await ExecuteSingleAsync<T>(null, null, data, queryLambda, whereLambda, withNoLock, tableName, propertyMatchList, reflectionType, con, transaction);
        }
        #endregion

        #region More 根据条件查询多条数据
        /// <summary>
        /// 查询多条数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">字段表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="orderLambda">排序表达式</param>
        /// <param name="orderDesc">是否倒序</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> More<T>(object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return More<T>(null, null, data, queryLambda, whereLambda, orderLambda, orderDesc, tableName, propertyMatchList, withNoLock, reflectionType);
        }
        /// <summary>
        /// 查询多条数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">字段表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="orderLambda">排序表达式</param>
        /// <param name="orderDesc">是否倒序</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<List<T>> MoreAsync<T>(object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await MoreAsync<T>(null, null, data, queryLambda, whereLambda, orderLambda, orderDesc, tableName, propertyMatchList, withNoLock, reflectionType);
        }
        /// <summary>
        /// 查询多条数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="orderLambda">排序表达式</param>
        /// <param name="orderDesc">是否倒序</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> More<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ExecuteMore<T>(connectionString, dataBaseType, data, queryLambda, whereLambda, orderLambda, orderDesc, tableName, propertyMatchList, withNoLock, reflectionType, null, null);
        }
        /// <summary>
        /// 查询多条数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="orderLambda">排序表达式</param>
        /// <param name="orderDesc">是否倒序</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<List<T>> MoreAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await ExecuteMoreAsync<T>(connectionString, dataBaseType, data, queryLambda, whereLambda, orderLambda, orderDesc, tableName, propertyMatchList, withNoLock, reflectionType, null, null);
        }
        /// <summary>
        /// 查询多条数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="orderLambda">排序表达式</param>
        /// <param name="orderDesc">是否倒序</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> TransactionMore<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ExecuteMore<T>(null, null, data, queryLambda, whereLambda, orderLambda, orderDesc, tableName, propertyMatchList, withNoLock, reflectionType, con, transaction);
        }
        /// <summary>
        /// 查询多条数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="data">数据</param>
        /// <param name="queryLambda">查询表达式</param>
        /// <param name="whereLambda">条件表达式</param>
        /// <param name="orderLambda">排序表达式</param>
        /// <param name="orderDesc">是否倒序</param>
        /// <param name="tableName">表名，当 T 与表名不同时指定</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="withNoLock">是否 with(nolock)</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<List<T>> TransactionMoreAsync<T>(DbConnection con, DbTransaction transaction, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await ExecuteMoreAsync<T>(null, null, data, queryLambda, whereLambda, orderLambda, orderDesc, tableName, propertyMatchList, withNoLock, reflectionType, con, transaction);
        }
        #endregion

        #region ExecuteNonQuery 返回影响操作的数据行数
        /// <summary>
        /// ExecuteNonQuery
        /// </summary>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            return ExecuteNonQuery(null, null, commandText, parameterList, commandType);
        }
        /// <summary>
        /// ExecuteNonQuery（异步）
        /// </summary>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static async Task<int> ExecuteNonQueryAsync(string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            return await ExecuteNonQueryAsync(null, null, commandText, parameterList, commandType);
        }
        /// <summary>
        /// ExecuteNonQuery
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string connectionString, string dataBaseType, string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            int result = 0;
            ExecuteCommand(connectionString, dataBaseType, commandText, parameterDict, null, commandType, (DbCommand command) =>
            {
                if (command.Connection.State != ConnectionState.Open) command.Connection.Open();
                result = command.ExecuteNonQuery();
            });
            return result;
        }
        /// <summary>
        /// ExecuteNonQuery（异步）
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static async Task<int> ExecuteNonQueryAsync(string connectionString, string dataBaseType, string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            int result = 0;

            await ExecuteCommandAsync(connectionString, dataBaseType, commandText, parameterDict, null, commandType, async (DbCommand command) =>
            {
                result = await command.ExecuteNonQueryAsync();
            });

            return result;
        }
        /// <summary>
        /// ExecuteNonQuery
        /// </summary>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <returns></returns>
        public static int TransactionNonQuery(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            int result = 0;
            ExecuteCommand(null, null, commandText, parameterDict, null, CommandType.Text, (DbCommand command) =>
            {
                result = command.ExecuteNonQuery();
            }, con, transaction);
            return result;
        }
        /// <summary>
        /// ExecuteNonQuery（异步）
        /// </summary>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <returns></returns>
        public static async Task<int> TransactionNonQueryAsync(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            int result = 0;
            await ExecuteCommandAsync(null, null, commandText, parameterDict, null, CommandType.Text, async (DbCommand command) =>
            {
                result = await command.ExecuteNonQueryAsync();
            }, con, transaction);
            return result;
        }
        /// <summary>
        /// ExecuteNonQuery（事务）
        /// </summary>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <returns></returns>
        public static int TransactionNonQuery(List<DataBaseTransactionItem> transactionCommandList)
        {
            return TransactionNonQuery(null, null, transactionCommandList);
        }
        /// <summary>
        /// ExecuteNonQuery（事务）
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <returns></returns>
        public static int TransactionNonQuery(string connectionString, string dataBaseType, List<DataBaseTransactionItem> transactionCommandList)
        {
            return (int)ExecuteTransaction<DataBaseTransactionItem>(connectionString, dataBaseType, transactionCommandList, ReflectionTypeEnum.Expression);
        }
        #endregion

        #region ExecuteScalar<T> 返回查询结果中的首行首列数据
        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static dynamic ExecuteScalar<T>(string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            return ExecuteScalar<T>(null, null, commandText, parameterList, commandType);
        }
        /// <summary>
        /// ExecuteScalar（异步）
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static async Task<dynamic> ExecuteScalarAsync<T>(string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            return await ExecuteScalarAsync<T>(null, null, commandText, parameterList, commandType);
        }
        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static dynamic ExecuteScalar<T>(string connectionString, string dataBaseType, string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            object result = null;
            ExecuteCommand(connectionString, dataBaseType, commandText, parameterDict, null, commandType, (DbCommand command) =>
            {
                result = command.ExecuteScalar();
            });
            if (result == null) return default(T);
            return (T)Convert.ChangeType(result, typeof(T));
        }
        /// <summary>
        /// ExecuteScalar（异步）
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static async Task<dynamic> ExecuteScalarAsync<T>(string connectionString, string dataBaseType, string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            object result = null;
            await ExecuteCommandAsync(connectionString, dataBaseType, commandText, parameterDict, null, commandType, async (DbCommand command) =>
            {
                result = await command.ExecuteScalarAsync();
            });
            if (result == null) return default(T);
            return (T)Convert.ChangeType(result, typeof(T));
        }
        /// <summary>
        /// ExecuteScalar
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <returns></returns>
        public static dynamic TransactionScalar<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            object result = null;
            ExecuteCommand(null, null, commandText, parameterDict, null, CommandType.Text, (DbCommand command) =>
            {
                result = command.ExecuteScalar();
            }, con, transaction);
            if (result == null) return default(T);
            return (T)Convert.ChangeType(result, typeof(T));
        }
        /// <summary>
        /// ExecuteScalar（异步）
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <returns></returns>
        public static async Task<dynamic> TransactionScalarAsync<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            object result = null;
            await ExecuteCommandAsync(null, null, commandText, parameterDict, null, CommandType.Text, async (DbCommand command) =>
            {
                result = await command.ExecuteScalarAsync();
            }, con, transaction);
            if (result == null) return default(T);
            return (T)Convert.ChangeType(result, typeof(T));
        }
        /// <summary>
        /// ExecuteScalar（事务）
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <returns></returns>
        public static dynamic TransactionScalar<T>(List<DataBaseTransactionItem> transactionCommandList)
        {
            return TransactionScalar<T>(null, null, transactionCommandList);
        }
        /// <summary>
        /// ExecuteScalar（事务）
        /// </summary>
        /// <typeparam name="T">基类类型，例：int</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <returns></returns>
        public static dynamic TransactionScalar<T>(string connectionString, string dataBaseType, List<DataBaseTransactionItem> transactionCommandList)
        {
            object result = ExecuteTransaction<DataBaseTransactionItem>(connectionString, dataBaseType, transactionCommandList);
            return (T)Convert.ChangeType(result, typeof(T));
        }
        #endregion

        #region ExecuteDataReader 返回查询结果中的每条 DbDataReader 对象
        /// <summary>
        /// ExecuteDataReader
        /// </summary>
        /// <param name="callback">DbDataReader 处理函数</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        public static void ExecuteDataReader(Action<DbDataReader> callback, string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            ExecuteDataReader(null, null, callback, commandText, parameterList, commandType);
        }
        /// <summary>
        /// ExecuteDataReader
        /// </summary>
        /// <param name="callback">DbDataReader 处理函数</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        public static void ExecuteDataReader(Action<DbDataReader> callback, string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            ExecuteDataReader(null, null, callback, commandText, parameterList, ref pageCount, ref totalCount, parameterPageCountName, parameterTotalCountName, commandType);
        }
        /// <summary>
        /// ExecuteDataReader
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="callback">DbDataReader 处理函数</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        public static void ExecuteDataReader(string connectionString, string dataBaseType, Action<DbDataReader> callback, string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            ExecuteCommand(connectionString, dataBaseType, commandText, parameterDict, null, commandType, (DbCommand command) =>
            {
                ExecuteDataReader(callback, command);
            });
        }
        /// <summary>
        /// ExecuteDataReader
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="callback">DbDataReader 处理函数</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        public static void ExecuteDataReader(string connectionString, string dataBaseType, Action<DbDataReader> callback, string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);

            Dictionary<string, object> outParameterList = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(parameterPageCountName)) outParameterList.Add("@" + parameterPageCountName, 0);
            if (!string.IsNullOrEmpty(parameterTotalCountName)) outParameterList.Add("@" + parameterTotalCountName, 0);

            if (string.IsNullOrEmpty(commandText) && dataBaseType == DataBaseTypeEnum.Sql)
            {
                commandText = SqlDataBaseItem.PaginationSql;
            }

            int commandPageCount = 0;
            int commandTotalCount = 0;

            ExecuteCommand(connectionString, dataBaseType, commandText, parameterDict, outParameterList, commandType, (DbCommand command) =>
            {
                ExecuteDataReader(callback, command);

                if (!string.IsNullOrEmpty(parameterPageCountName)) commandPageCount = (int)command.Parameters["@" + parameterPageCountName].Value;
                if (!string.IsNullOrEmpty(parameterTotalCountName)) commandTotalCount = (int)command.Parameters["@" + parameterTotalCountName].Value;
            });

            pageCount = commandPageCount;
            totalCount = commandTotalCount;
        }
        #endregion

        #region ToEntityList<T> 返回多条查询结果
        /// <summary>
        /// 返回实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> ToEntityList<T>(string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ToEntityList<T>(null, null, commandText, parameterList, propertyMatchList, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<List<T>> ToEntityListAsync<T>(string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await ToEntityListAsync<T>(null, null, commandText, parameterList, propertyMatchList, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="propertyMatchList">属性匹配，，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> ToEntityList<T>(string commandText, object parameterList, ref int pageCount, ref int totalCount, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ToEntityList<T>(null, null, commandText, parameterList, ref pageCount, ref totalCount, propertyMatchList, parameterPageCountName, parameterTotalCountName, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<DataBasePaginationDataItem<T>> ToEntityListAsync<T>(string commandText, object parameterList, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await ToEntityListAsync<T>(null, null, commandText, parameterList, propertyMatchList, parameterPageCountName, parameterTotalCountName, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表（事务）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> TransactionEntityList<T>(List<DataBaseTransactionItem> transactionCommandList, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return TransactionEntityList<T>(null, null, transactionCommandList, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> ToEntityList<T>(string connectionString, string dataBaseType, string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnEntityList<T>(connectionString, dataBaseType, commandText, parameterDict, propertyMatchList, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<List<T>> ToEntityListAsync<T>(string connectionString, string dataBaseType, string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return await ReturnEntityListAsync<T>(connectionString, dataBaseType, commandText, parameterDict, propertyMatchList, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表（事务）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> TransactionEntityList<T>(string connectionString, string dataBaseType, List<DataBaseTransactionItem> transactionCommandList, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return (List<T>)ExecuteTransaction<T>(connectionString, dataBaseType, transactionCommandList, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> ToEntityList<T>(string connectionString, string dataBaseType, string commandText, object parameterList, ref int pageCount, ref int totalCount, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnEntityList<T>(connectionString, dataBaseType, commandText, parameterDict, ref pageCount, ref totalCount, propertyMatchList, parameterPageCountName, parameterTotalCountName, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<DataBasePaginationDataItem<T>> ToEntityListAsync<T>(string connectionString, string dataBaseType, string commandText, object parameterList, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return await ReturnEntityListAsync<T>(connectionString, dataBaseType, commandText, parameterDict, propertyMatchList, parameterPageCountName, parameterTotalCountName, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> TransactionEntityList<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnEntityList<T>(null, null, commandText, parameterDict, propertyMatchList, CommandType.Text, reflectionType, con, transaction);
        }
        /// <summary>
        /// 返回实体数据列表（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<List<T>> TransactionEntityListAsync<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return await ReturnEntityListAsync<T>(null, null, commandText, parameterDict, propertyMatchList, CommandType.Text, reflectionType, con, transaction);
        }
        /// <summary>
        /// 返回实体数据列表
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static List<T> TransactionEntityList<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList, ref int pageCount, ref int totalCount, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnEntityList<T>(null, null, commandText, parameterDict, ref pageCount, ref totalCount, propertyMatchList, parameterPageCountName, parameterTotalCountName, CommandType.Text, reflectionType, con, transaction);
        }
        /// <summary>
        /// 返回实体数据列表（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<DataBasePaginationDataItem<T>> TransactionEntityListAsync<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return await ReturnEntityListAsync<T>(null, null, commandText, parameterDict, propertyMatchList, parameterPageCountName, parameterTotalCountName, CommandType.Text, reflectionType, con, transaction);
        }
        #endregion

        #region ToEntity<T> 返回单条查询结果
        /// <summary>
        /// 返回实体数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T ToEntity<T>(string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ToEntity<T>(null, null, commandText, parameterList, propertyMatchList, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据（实体）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<T> ToEntityAsync<T>(string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return await ToEntityAsync<T>(null, null, commandText, parameterList, propertyMatchList, commandType, reflectionType);
        }
        /// <summary>
        /// 返回实体数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T ToEntity<T>(string connectionString, string dataBaseType, string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            List<T> dataList = ToEntityList<T>(connectionString, dataBaseType, commandText, parameterList, propertyMatchList, commandType, reflectionType);
            if (dataList.Count > 0) return dataList[0];
            return null;
        }
        /// <summary>
        /// 返回实体数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<T> ToEntityAsync<T>(string connectionString, string dataBaseType, string commandText, object parameterList = null, object propertyMatchList = null, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            List<T> dataList = await ToEntityListAsync<T>(connectionString, dataBaseType, commandText, parameterList, propertyMatchList, commandType, reflectionType);
            if (dataList.Count > 0) return dataList[0];
            return null;
        }
        /// <summary>
        /// 返回实体数据
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T TransactionEntity<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            List<T> dataList = TransactionEntityList<T>(con, transaction, commandText, parameterList, propertyMatchList, reflectionType);
            if (dataList.Count > 0) return dataList[0];
            return null;
        }
        /// <summary>
        /// 返回实体数据（异步）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static async Task<T> TransactionEntityAsync<T>(DbConnection con, DbTransaction transaction, string commandText, object parameterList = null, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            List<T> dataList = await TransactionEntityListAsync<T>(con, transaction, commandText, parameterList, propertyMatchList, reflectionType);
            if (dataList.Count > 0) return dataList[0];
            return null;
        }
        /// <summary>
        /// 返回实体数据（事务）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T TransactionEntity<T>(List<DataBaseTransactionItem> transactionCommandList, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return TransactionEntity<T>(null, null, transactionCommandList, reflectionType);
        }
        /// <summary>
        /// 返回实体数据（事务）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="transactionCommandList">DataBaseTransactionItem 列表</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static T TransactionEntity<T>(string connectionString, string dataBaseType, List<DataBaseTransactionItem> transactionCommandList, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            List<T> dataList = TransactionEntityList<T>(connectionString, dataBaseType, transactionCommandList, reflectionType);
            if (dataList.Count > 0) return dataList[0];
            return null;
        }
        #endregion

        #region ToDataSet 返回 DataSet
        /// <summary>
        /// 返回 DataSet
        /// </summary>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataSet ToDataSet(string commandText, object parameterList, CommandType commandType = CommandType.Text)
        {
            return ToDataSet(null, null, commandText, parameterList, commandType);
        }
        /// <summary>
        /// 返回 DataSet
        /// </summary>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataSet ToDataSet(string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            return ToDataSet(null, null, commandText, parameterList, ref pageCount, ref totalCount, parameterPageCountName, parameterTotalCountName, commandType);
        }
        /// <summary>
        /// 返回 DataSet
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataSet ToDataSet(string connectionString, string dataBaseType, string commandText, object parameterList=null, CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnDataSet(connectionString, dataBaseType, commandText, parameterDict, commandType);
        }
        /// <summary>
        /// 返回 DataSet
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataSet ToDataSet(string connectionString, string dataBaseType, string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnDataSet(connectionString, dataBaseType, commandText, parameterDict, ref pageCount, ref totalCount, parameterPageCountName, parameterTotalCountName, commandType);
        }
        /// <summary>
        /// 返回 DataSet
        /// </summary>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataSet TransactionDataSet(DbConnection con, DbTransaction transaction, string commandText, object parameterList, CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnDataSet(null, null, commandText, parameterDict, commandType, con, transaction);
        }
        /// <summary>
        /// 返回 DataSet
        /// </summary>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataSet TransactionDataSet(DbConnection con, DbTransaction transaction, string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(parameterList);
            return ReturnDataSet(null, null, commandText, parameterDict, ref pageCount, ref totalCount, parameterPageCountName, parameterTotalCountName, commandType, con, transaction);
        }
        #endregion

        #region ToDataTable 返回 DataTable
        /// <summary>
        /// 返回 DataTable
        /// </summary>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataTable ToDataTable(string commandText, object parameterList, CommandType commandType = CommandType.Text)
        {
            DataSet dataSet = ToDataSet(commandText, parameterList, commandType);
            if (dataSet != null && dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }
            return null;
        }
        /// <summary>
        /// 返回 DataTable
        /// </summary>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataTable ToDataTable(string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            DataSet dataSet = ToDataSet(commandText, parameterList, ref pageCount, ref totalCount, parameterPageCountName, parameterTotalCountName, commandType);
            if (dataSet != null && dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }
            return null;
        }
        /// <summary>
        /// 返回 DataTable
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataTable ToDataTable(string connectionString, string dataBaseType, string commandText, object parameterList = null, CommandType commandType = CommandType.Text)
        {
            DataSet dataSet = ToDataSet(connectionString, dataBaseType, commandText, parameterList, commandType);
            if (dataSet != null && dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }
            return null;
        }
        /// <summary>
        /// 返回 DataTable
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataTable ToDataTable(string connectionString, string dataBaseType, string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            DataSet dataSet = ToDataSet(connectionString, dataBaseType, commandText, parameterList, ref pageCount, ref totalCount, parameterPageCountName, parameterTotalCountName, commandType);
            if (dataSet != null && dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }
            return null;
        }
        /// <summary>
        /// 返回 DataTable
        /// </summary>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataTable TransactionDataTable(DbConnection con, DbTransaction transaction, string commandText, object parameterList, CommandType commandType = CommandType.Text)
        {
            DataSet dataSet = TransactionDataSet(con, transaction, commandText, parameterList, commandType);
            if (dataSet != null && dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }
            return null;
        }
        /// <summary>
        /// 返回 DataTable
        /// </summary>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="commandText">Sql 语句或者存储过程名称</param>
        /// <param name="parameterList">参数列表，new {} 或 Dictionary&lt;string, object&gt;</param>
        /// <param name="pageCount">页总数，输出</param>
        /// <param name="totalCount">数据总数，输出</param>
        /// <param name="parameterPageCountName">页总数参数名称，例如：PageCount</param>
        /// <param name="parameterTotalCountName">数据总数参数名称，例如：TotalCount</param>
        /// <param name="commandType">CommandType 枚举类型</param>
        /// <returns></returns>
        public static DataTable TransactionDataTable(DbConnection con, DbTransaction transaction, string commandText, object parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text)
        {
            DataSet dataSet = TransactionDataSet(con, transaction, commandText, parameterList, ref pageCount, ref totalCount, parameterPageCountName, parameterTotalCountName, commandType);
            if(dataSet != null && dataSet.Tables.Count > 0)
            {
                return dataSet.Tables[0];
            }
            return null;
        }
        #endregion

        #region 批量导入数据，只支持 SqlServer 数据库
        /// <summary>
        /// 实体数据列表批量导入
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="tableName">数据库表名称</param>
        /// <param name="dataList">实体类型数据列表</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="propertyList">属性列表，如果指定，则按指定属性列表生成 DataTable 数据</param>
        /// <param name="propertyContain">是否包含，true 属性包含，false 属性排除</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static bool EntityListBatchImport<T>(string tableName, List<T> dataList, object propertyMatchList = null, string[] propertyList = null, bool propertyContain = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class
        {
            return EntityListBatchImport<T>(null, tableName, dataList, propertyMatchList, propertyList, propertyContain, reflectionType);
        }
        /// <summary>
        /// DataTable 数据批量导入
        /// </summary>
        /// <param name="tableName">数据库表名称</param>
        /// <param name="dataTable">DataTable 数据</param>
        /// <returns></returns>
        public static bool DataTableBatchImport(string tableName, DataTable dataTable)
        {
            return DataTableBatchImport(null, tableName, dataTable);
        }
        /// <summary>
        /// 实体数据列表批量导入
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="tableName">数据库表名称</param>
        /// <param name="dataList">实体类型数据列表</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="propertyList">属性列表，如果指定，则按指定属性列表生成 DataTable 数据</param>
        /// <param name="propertyContain">是否包含，true 属性包含，flase 属性排除</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static bool EntityListBatchImport<T>(string connectionString, string tableName, List<T> dataList, object propertyMatchList = null, string[] propertyList = null, bool propertyContain = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class
        {
            DataTable dataTable = DataTableHelper.ToDataTable<T>(dataList, propertyMatchList, propertyList, propertyContain, reflectionType);
            if (dataTable != null) return DataTableBatchImport(connectionString, tableName, dataTable);
            return false;
        }
        /// <summary>
        /// 实体数据列表批量导入
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="tableName">数据库表名称</param>
        /// <param name="dataList">实体类型数据列表</param>
        /// <param name="propertyMatchList">属性匹配，Dictionary&lt;string, object&gt; 或 new {}</param>
        /// <param name="propertyList">属性列表，如果指定，则按指定属性列表生成 DataTable 数据</param>
        /// <param name="propertyContain">是否包含，true 属性包含，flase 属性排除</param>
        /// <param name="reflectionType">反射类型</param>
        public static void TransactionEntityListBatchImport<T>(DbConnection con, DbTransaction transaction, string tableName, List<T> dataList, object propertyMatchList = null, string[] propertyList = null, bool propertyContain = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class
        {
            DataTable dataTable = DataTableHelper.ToDataTable<T>(dataList, propertyMatchList, propertyList, propertyContain, reflectionType);
            if (dataTable != null)
            {
                TransactionDataTableBatchImport(con, transaction, tableName, dataTable);
            }
        }
        /// <summary>
        /// DataTable 数据批量导入
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="tableName">数据库表名称</param>
        /// <param name="dataTable">DataTable 数据</param>
        /// <returns></returns>
        public static bool DataTableBatchImport(string connectionString, string tableName, DataTable dataTable)
        {
            if (string.IsNullOrEmpty(connectionString)) connectionString = ConnectionString;

            if (DataBaseType != DataBaseTypeEnum.Sql) throw new Exception(BatchImportException);
            DbConnection con = null;
            try
            {
                using (con = CreateDbConnection(connectionString))
                {
                    ExecuteBatchDataTable(new SqlBulkCopy(connectionString, SqlBulkCopyOptions.UseInternalTransaction), tableName, dataTable);
                    return true;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (con != null) con.Close();
            }
        }
        /// <summary>
        /// DataTable 数据批量导入
        /// </summary>
        /// <param name="con">DbConnection</param>
        /// <param name="transaction">DbTransaction</param>
        /// <param name="tableName">数据库表名称</param>
        /// <param name="dataTable">DataTable 数据</param>
        public static void TransactionDataTableBatchImport(DbConnection con, DbTransaction transaction, string tableName, DataTable dataTable)
        {
            if (DataBaseType != DataBaseTypeEnum.Sql) throw new Exception(BatchImportException);
            ExecuteBatchDataTable(new SqlBulkCopy((SqlConnection)con, SqlBulkCopyOptions.Default, (SqlTransaction)transaction), tableName, dataTable);
        }
        #endregion

        #region 事务处理，Func 函数可以处理所有 Transaction 开头的函数
        /// <summary>
        /// 事务处理
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="func">回调函数</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static object Transaction(Func<DbConnection, DbTransaction, object> func)
        {
            return Transaction(null, null, func);
        }
        /// <summary>
        /// 事务处理
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="dataBaseType">数据库类型</param>
        /// <param name="func">回调函数</param>
        /// <param name="reflectionType">反射类型</param>
        /// <returns></returns>
        public static object Transaction(string connectionString, string dataBaseType, Func<DbConnection, DbTransaction, object> func)
        {
            return ExecuteTransaction(connectionString, dataBaseType, func);
        }
        #endregion

        #endregion

        #region 逻辑处理私有方法

        #region 创建 CURD 相关
        private static DbConnection CreateDbConnection(string connectionString = null, string dataBaseType = null)
        {
            if (string.IsNullOrEmpty(connectionString)) connectionString = ConnectionString;
            if (string.IsNullOrEmpty(dataBaseType)) dataBaseType = DataBaseType;

            DbConnection connection = null;
            switch (dataBaseType)
            {
                case DataBaseTypeEnum.MySql: connection = new MySqlConnection(connectionString); break;
                default: connection = new SqlConnection(connectionString); break;
            }
            return connection;
        }
        private static DbDataAdapter CreateDbDataAdapter(string dataBaseType = null)
        {
            if (string.IsNullOrEmpty(dataBaseType)) dataBaseType = DataBaseType;
            DbDataAdapter dataAdapter = null;
            switch(dataBaseType)
            {
                case DataBaseTypeEnum.MySql: dataAdapter = new MySqlDataAdapter(); break;
                default: dataAdapter = new SqlDataAdapter(); break;
            }
            return dataAdapter;
        }
        private static DbCommand CreateDbCommand(DbConnection connection, DbCommand command, string commandText, Dictionary<string, object> parameterList, Dictionary<string, object> outParameterList, CommandType commandType = CommandType.Text)
        {
            if (command == null)
            {
                command = connection.CreateCommand();
                command.Connection = connection;
            }
            command.CommandText = commandText;
            command.CommandType = commandType;

            // 清空参数
            command.Parameters.Clear();

            if (parameterList != null && parameterList.Count > 0)
            {
                foreach (var dictItem in parameterList)
                {
                    CreateDbParameter(command, dictItem.Key, dictItem.Value, ParameterDirection.Input);
                }
            }
            if (outParameterList != null && outParameterList.Count > 0)
            {
                foreach (var dictItem in outParameterList)
                {
                    CreateDbParameter(command, dictItem.Key, dictItem.Value, ParameterDirection.Output);
                }
            }
            return command;
        }
        private static void CreateDbParameter(DbCommand command, string parameterName, object value, ParameterDirection direction)
        {
            parameterName = parameterName.Trim();
            if (!parameterName.StartsWith("@")) parameterName = "@" + parameterName;

            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value == null ? "" : value;
            parameter.Direction = direction;
            command.Parameters.Add(parameter);
        }
        #endregion

        private static List<T> ReturnEntityList<T>(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, object propertyMatchList, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            List<T> dataList = null;
            ExecuteCommand(connectionString, dataBaseType, commandText, parameterList, null, commandType, (DbCommand command) =>
            {
                dataList = DataReaderToEntityList<T>(command, propertyMatchList, reflectionType, con, transaction);
            }, con, transaction);
            return dataList;
        }
        private static async Task<List<T>> ReturnEntityListAsync<T>(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, object propertyMatchList, CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            List<T> dataList = null;
            await ExecuteCommandAsync(connectionString, dataBaseType, commandText, parameterList, null, commandType, async (DbCommand command) =>
            {
                dataList = await DataReaderToEntityListAsync<T>(command, propertyMatchList, reflectionType, con, transaction);
            }, con, transaction);
            return dataList;
        }
        private static List<T> ReturnEntityList<T>(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, ref int pageCount, ref int totalCount, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            List<T> dataList = null;

            Dictionary<string, object> outParameterList = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(parameterPageCountName)) outParameterList.Add("@" + parameterPageCountName, 0);
            if (!string.IsNullOrEmpty(parameterTotalCountName)) outParameterList.Add("@" + parameterTotalCountName, 0);

            if (string.IsNullOrEmpty(commandText) && dataBaseType == DataBaseTypeEnum.Sql)
            {
                commandText = SqlDataBaseItem.PaginationSql;
            }

            int commandPageCount = 0;
            int commandTotalCount = 0;

            ExecuteCommand(connectionString, dataBaseType, commandText, parameterList, outParameterList, commandType, (DbCommand command) =>
            {
                dataList = DataReaderToEntityList<T>(command, propertyMatchList, reflectionType, con, transaction);

                if (!string.IsNullOrEmpty(parameterPageCountName)) commandPageCount = (int)command.Parameters["@" + parameterPageCountName].Value;
                if (!string.IsNullOrEmpty(parameterTotalCountName)) commandTotalCount = (int)command.Parameters["@" + parameterTotalCountName].Value;
            }, con, transaction);

            pageCount = commandPageCount;
            totalCount = commandTotalCount;

            return dataList;
        }
        private static async Task<DataBasePaginationDataItem<T>> ReturnEntityListAsync<T>(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, object propertyMatchList, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            DataBasePaginationDataItem<T> paginationDataItem = new DataBasePaginationDataItem<T>();

            Dictionary<string, object> outParameterList = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(parameterPageCountName)) outParameterList.Add("@" + parameterPageCountName, 0);
            if (!string.IsNullOrEmpty(parameterTotalCountName)) outParameterList.Add("@" + parameterTotalCountName, 0);

            if (string.IsNullOrEmpty(commandText) && dataBaseType == DataBaseTypeEnum.Sql)
            {
                commandText = SqlDataBaseItem.PaginationSql;
            }

            int commandPageCount = 0;
            int commandTotalCount = 0;

            await ExecuteCommandAsync(connectionString, dataBaseType, commandText, parameterList, outParameterList, commandType, async (DbCommand command) =>
            {
                paginationDataItem.DataList = await DataReaderToEntityListAsync<T>(command, propertyMatchList, reflectionType, con, transaction);

                if (!string.IsNullOrEmpty(parameterPageCountName)) commandPageCount = (int)command.Parameters["@" + parameterPageCountName].Value;
                if (!string.IsNullOrEmpty(parameterTotalCountName)) commandTotalCount = (int)command.Parameters["@" + parameterTotalCountName].Value;
            }, con, transaction);

            paginationDataItem.PageCount = commandPageCount;
            paginationDataItem.TotalCount = commandTotalCount;

            return paginationDataItem;
        }
        private static DataSet ReturnDataSet(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, CommandType commandType = CommandType.Text, DbConnection con = null, DbTransaction transaction = null)
        {
            DataSet dataSet = null;
            ExecuteCommand(connectionString, dataBaseType, commandText, parameterList, null, commandType, (DbCommand command) =>
            {
                if (dataSet == null) dataSet = new DataSet();

                DbDataAdapter dataAdapter = CreateDbDataAdapter(dataBaseType);
                dataAdapter.SelectCommand = command;
                dataAdapter.Fill(dataSet);

            }, con, transaction);
            return dataSet;
        }
        private static DataSet ReturnDataSet(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, ref int pageCount, ref int totalCount, string parameterPageCountName = "PageCount", string parameterTotalCountName = "TotalCount", CommandType commandType = CommandType.Text, DbConnection con = null, DbTransaction transaction = null)
        {
            DataSet dataSet = null;
            Dictionary<string, object> outParameterList = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(parameterPageCountName)) outParameterList.Add("@" + parameterPageCountName, 0);
            if (!string.IsNullOrEmpty(parameterTotalCountName)) outParameterList.Add("@" + parameterTotalCountName, 0);

            if(string.IsNullOrEmpty(commandText) && dataBaseType == DataBaseTypeEnum.Sql)
            {
                commandText = SqlDataBaseItem.PaginationSql;
            }

            int commandPageCount = 0;
            int commandTotalCount = 0;

            ExecuteCommand(connectionString, dataBaseType, commandText, parameterList, outParameterList, commandType, (DbCommand command) =>
            {
                if (dataSet == null) dataSet = new DataSet();

                DbDataAdapter dataAdapter = CreateDbDataAdapter(dataBaseType);
                dataAdapter.SelectCommand = command;
                dataAdapter.Fill(dataSet);

                if (!string.IsNullOrEmpty(parameterPageCountName)) commandPageCount = (int)command.Parameters["@" + parameterPageCountName].Value;
                if (!string.IsNullOrEmpty(parameterTotalCountName)) commandTotalCount = (int)command.Parameters["@" + parameterTotalCountName].Value;
            }, con, transaction);

            pageCount = commandPageCount;
            totalCount = commandTotalCount;

            return dataSet;
        }
        private static bool ExecuteInsert(string connectionString, string dataBaseType, string tableName, object data, string[] ignorePropertyList = null, DbConnection con = null, DbTransaction transaction = null)
        {
            Dictionary<string, object> mapperDict = InitEntityToPropertyMapper(data, ignorePropertyList);

            string commandText = GetInsertCommandText(mapperDict, tableName);
            if (con == null && transaction == null)
            {
                return ExecuteNonQuery(connectionString, dataBaseType, commandText, mapperDict, CommandType.Text) > 0;
            }
            else
            {
                return TransactionNonQuery(con, transaction, commandText, mapperDict) > 0;
            }
        }
        private static async Task<bool> ExecuteInsertAsync(string connectionString, string dataBaseType, string tableName, object data, string[] ignorePropertyList = null, DbConnection con = null, DbTransaction transaction = null)
        {
            Dictionary<string, object> mapperDict = InitEntityToPropertyMapper(data, ignorePropertyList);

            string commandText = GetInsertCommandText(mapperDict, tableName);
            if (con == null && transaction == null)
            {
                return await ExecuteNonQueryAsync(connectionString, dataBaseType, commandText, mapperDict, CommandType.Text) > 0;
            }
            else
            {
                return await TransactionNonQueryAsync(con, transaction, commandText, mapperDict) > 0;
            }
        }
        private static string GetInsertCommandText(Dictionary<string, object> mapperDict, string tableName)
        {
            string fieldDataList = INSERT_FIELD_SQL;
            string fieldParameterList = INSERT_FIELD_PARAMETER_SQL;
            foreach (KeyValuePair<string, object> keyValueItem in mapperDict)
            {
                fieldDataList = string.Format(fieldDataList, keyValueItem.Key, INSERT_FIELD_SQL);
                fieldParameterList = string.Format(fieldParameterList, keyValueItem.Key, INSERT_FIELD_PARAMETER_SQL);
            }
            fieldDataList = StringHelper.TrimChar(string.Format(fieldDataList, "", ""), ",");
            fieldParameterList = StringHelper.TrimChar(StringHelper.TrimChar(string.Format(fieldParameterList, "", ""), "@"), ",");

            return string.Format(INSERT_SQL, tableName, fieldDataList, fieldParameterList);
        }
        private static bool ExecuteUpdate<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            Dictionary<string, object> mapperDict = InitEntityToPropertyMapper(data, ignoreLambda != null ? CommonHelper.GetExpressionList<T>(ignoreLambda).ToArray() : null);

            string whereSql = "";

            string commandText = GetUpdateCommandText<T>(mapperDict, whereLambda, ref whereSql, tableName);
            if (con == null && transaction == null)
            {
                return ExecuteNonQuery(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(mapperDict, data, whereSql), CommandType.Text) > 0;
            }
            else
            {
                return TransactionNonQuery(con, transaction, commandText, RevisePropertyMapperDict<T>(mapperDict, data, whereSql)) > 0;
            }
        }
        private static async Task<bool> ExecuteUpdateAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> ignoreLambda, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            Dictionary<string, object> mapperDict = InitEntityToPropertyMapper(data, ignoreLambda != null ? CommonHelper.GetExpressionList<T>(ignoreLambda).ToArray() : null);

            string whereSql = "";

            string commandText = GetUpdateCommandText<T>(mapperDict, whereLambda, ref whereSql, tableName);
            if (con == null && transaction == null)
            {
                return await ExecuteNonQueryAsync(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(mapperDict, data, whereSql), CommandType.Text) > 0;
            }
            else
            {
                return await TransactionNonQueryAsync(con, transaction, commandText, RevisePropertyMapperDict<T>(mapperDict, data, whereSql)) > 0;
            }
        }
        private static string GetUpdateCommandText<T>(Dictionary<string, object> mapperDict, Expression<Func<T, bool>> whereLambda, ref string whereSql, string tableName) where T : class
        {
            tableName = GetDataBaseTableName<T>(tableName);

            whereSql = new WhereTranslator().Translate(whereLambda);
            whereSql = whereSql.Replace(string.Format("[{0}].", typeof(T).Name), "");

            string fieldParameterList = UPDATE_FIELD_PARAMETER_SQL;
            foreach (KeyValuePair<string, object> keyValueItem in mapperDict)
            {
                fieldParameterList = string.Format(fieldParameterList, keyValueItem.Key, keyValueItem.Key, UPDATE_FIELD_PARAMETER_SQL);
            }
            fieldParameterList = StringHelper.TrimChar(fieldParameterList.Substring(0, fieldParameterList.LastIndexOf(",")), ",");

            return string.Format(UPDATE_SQL, tableName, fieldParameterList, whereSql);
        }
        private static bool ExecuteDelete<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            string whereSql = "";
            string commandText = GetDeleteCommandText<T>(tableName, whereLambda, ref whereSql);
            if (con == null && transaction == null)
            {
                return ExecuteNonQuery(commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), CommandType.Text) > 0;
            }
            else
            {
                return TransactionNonQuery(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql)) > 0;
            }
        }
        private static async Task<bool> ExecuteDeleteAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            string whereSql = "";
            string commandText = GetDeleteCommandText<T>(tableName, whereLambda, ref whereSql);
            if (con == null && transaction == null)
            {
                return await ExecuteNonQueryAsync(commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), CommandType.Text) > 0;
            }
            else
            {
                return await TransactionNonQueryAsync(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql)) > 0;
            }
        }
        private static string GetDeleteCommandText<T>(string tableName, Expression<Func<T, bool>> whereLambda, ref string whereSql) where T : class
        {
            tableName = GetDataBaseTableName<T>(tableName);

            whereSql = new WhereTranslator().Translate(whereLambda);
            whereSql = whereSql.Replace(string.Format("[{0}].", typeof(T).Name), "");

            return string.Format(DELETE_SQL, tableName, whereSql);
        }
        private static K ExecuteFirst<T, K>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, bool withNoLock = true, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            string whereSql = "";

            string commandText = GetFirstCommandText<T, K>(queryLambda, whereLambda, ref whereSql, withNoLock, tableName);
            if (con == null && transaction == null)
            {
                return ExecuteScalar<K>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), CommandType.Text);
            }
            else
            {
                return TransactionScalar<K>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql));
            }
        }
        private static async Task<K> ExecuteFirstAsync<T, K>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, bool withNoLock = true, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            string whereSql = "";

            string commandText = GetFirstCommandText<T, K>(queryLambda, whereLambda, ref whereSql, withNoLock, tableName);
            if (con == null && transaction == null)
            {
                return await ExecuteScalarAsync<K>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), CommandType.Text);
            }
            else
            {
                return await TransactionScalarAsync<K>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql));
            }
        }
        private static string GetFirstCommandText<T, K>(Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, ref string whereSql, bool withNoLock = true, string tableName = null) where T : class
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            stringBuilder.Append(GetQueryFieldSql<T>(queryLambda));
            stringBuilder.Append(" from ");
            stringBuilder.Append(GetDataBaseTableName<T>(tableName));
            stringBuilder.Append(" ");
            stringBuilder.Append(GetWithNoLockSql(withNoLock));
            stringBuilder.Append(" ");
            if (whereLambda != null)
            {
                stringBuilder.Append(" where ");
                whereSql = GetWhereConditionSql<T>(whereLambda);
                stringBuilder.Append(whereSql);
            }

            return stringBuilder.ToString();
        }
        private static bool ExecuteExists<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            int result = 0;
            if (con == null && transaction == null)
            {
                result = First<T, int>(connectionString, dataBaseType, data, queryLambda, whereLambda, tableName, withNoLock);
            }
            else
            {
                result = TransactionFirst<T, int>(con, transaction, data, queryLambda, whereLambda, tableName, withNoLock);
            }
            if (identityID == 0) return result > 0;
            return result == 0 ? false : (result != identityID);
        }
        private static async Task<bool> ExecuteExistsAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, int identityID, string tableName = null, bool withNoLock = true, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            int result = 0;
            if (con == null && transaction == null)
            {
                result = await FirstAsync<T, int>(connectionString, dataBaseType, data, queryLambda, whereLambda, tableName, withNoLock);
            }
            else
            {
                result = await TransactionFirstAsync<T, int>(con, transaction, data, queryLambda, whereLambda, tableName, withNoLock);
            }
            if (identityID == 0) return result > 0;
            return result == 0 ? false : (result != identityID);
        }
        private static int ExecuteCount<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda = null, bool withNoLock = true, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            string whereSql = "";

            string commandText = GetCountCommandText<T>(whereLambda, ref whereSql, withNoLock, tableName);
            if (con == null && transaction == null)
            {
                return ExecuteScalar<int>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), CommandType.Text);
            }
            else
            {
                return TransactionScalar<int>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql));
            }
        }
        private static async Task<int> ExecuteCountAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, bool>> whereLambda = null, bool withNoLock = true, string tableName = null, DbConnection con = null, DbTransaction transaction = null) where T : class
        {
            string whereSql = "";

            string commandText = GetCountCommandText<T>(whereLambda, ref whereSql, withNoLock, tableName);
            if (con == null && transaction == null)
            {
                return await ExecuteScalarAsync<int>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), CommandType.Text);
            }
            else
            {
                return await TransactionScalarAsync<int>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql));
            }
        }
        private static string GetCountCommandText<T>(Expression<Func<T, bool>> whereLambda, ref string whereSql, bool withNoLock = true, string tableName = null) where T : class
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select count(0) from ");
            stringBuilder.Append(GetDataBaseTableName<T>(tableName));
            stringBuilder.Append(" ");
            stringBuilder.Append(GetWithNoLockSql(withNoLock));
            stringBuilder.Append(" ");
            if (whereLambda != null)
            {
                stringBuilder.Append(" where ");
                whereSql = GetWhereConditionSql<T>(whereLambda);
                stringBuilder.Append(whereSql);
            }

            return stringBuilder.ToString();
        }
        private static T ExecuteSingle<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, bool withNoLock = true, string tableName = null, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            string whereSql = "";

            string commandText = GetSingleCommandText<T>(queryLambda, whereLambda, ref whereSql, withNoLock, tableName);
            if (con == null && transaction == null)
            {
                return ToEntity<T>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, CommandType.Text, reflectionType);
            }
            else
            {
                return TransactionEntity<T>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, reflectionType);
            }
        }
        private static async Task<T> ExecuteSingleAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, bool withNoLock = true, string tableName = null, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            string whereSql = "";

            string commandText = GetSingleCommandText<T>(queryLambda, whereLambda, ref whereSql, withNoLock, tableName);
            if (con == null && transaction == null)
            {
                return await ToEntityAsync<T>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, CommandType.Text, reflectionType);
            }
            else
            {
                return await TransactionEntityAsync<T>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, reflectionType);
            }
        }
        private static string GetSingleCommandText<T>(Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, ref string whereSql, bool withNoLock = true, string tableName = null) where T : class, new()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            if (queryLambda == null)
            {
                stringBuilder.Append(" * ");
            }
            else
            {
                stringBuilder.Append(GetQueryFieldSql<T>(queryLambda));
            }
            stringBuilder.Append(" from ");
            stringBuilder.Append(GetDataBaseTableName<T>(tableName));
            stringBuilder.Append(" ");
            stringBuilder.Append(GetWithNoLockSql(withNoLock));
            stringBuilder.Append(" ");
            if (whereLambda != null)
            {
                stringBuilder.Append(" where ");
                whereSql = GetWhereConditionSql<T>(whereLambda);
                stringBuilder.Append(whereSql);
            }

            return stringBuilder.ToString();
        }
        private static List<T> ExecuteMore<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            string whereSql = "";

            string commandText = GetMoreCommandText<T>(queryLambda, whereLambda, orderLambda, ref whereSql, orderDesc, tableName, withNoLock);
            if (con == null && transaction == null)
            {
                return ToEntityList<T>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, CommandType.Text, reflectionType);
            }
            else
            {
                return TransactionEntityList<T>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, reflectionType);
            }
        }
        private static async Task<List<T>> ExecuteMoreAsync<T>(string connectionString, string dataBaseType, object data, Expression<Func<T, object>> queryLambda = null, Expression<Func<T, bool>> whereLambda = null, Expression<Func<T, object>> orderLambda = null, bool orderDesc = true, string tableName = null, object propertyMatchList = null, bool withNoLock = true, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            string whereSql = "";

            string commandText = GetMoreCommandText<T>(queryLambda, whereLambda, orderLambda, ref whereSql, orderDesc, tableName, withNoLock);
            if (con == null && transaction == null)
            {
                return await ToEntityListAsync<T>(connectionString, dataBaseType, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, CommandType.Text, reflectionType);
            }
            else
            {
                return await TransactionEntityListAsync<T>(con, transaction, commandText, RevisePropertyMapperDict<T>(new Dictionary<string, object>(), data, whereSql), propertyMatchList, reflectionType);
            }
        }
        private static string GetMoreCommandText<T>(Expression<Func<T, object>> queryLambda, Expression<Func<T, bool>> whereLambda, Expression<Func<T, object>> orderLambda, ref string whereSql, bool orderDesc = true, string tableName = null, bool withNoLock = true) where T : class, new()
        {
            string descSql = "asc";
            if (orderDesc) descSql = "desc";

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select ");
            if (queryLambda == null)
            {
                stringBuilder.Append(" * ");
            }
            else
            {
                stringBuilder.Append(GetQueryFieldSql<T>(queryLambda));
            }
            stringBuilder.Append(" from ");
            stringBuilder.Append(GetDataBaseTableName<T>(tableName));
            stringBuilder.Append(" ");
            stringBuilder.Append(GetWithNoLockSql(withNoLock));
            stringBuilder.Append(" ");
            if (whereLambda != null)
            {
                stringBuilder.Append(" where ");
                whereSql = GetWhereConditionSql<T>(whereLambda);
                stringBuilder.Append(whereSql);
            }
            stringBuilder.Append(" ");
            if (orderLambda != null)
            {
                stringBuilder.Append(" order by ");
                stringBuilder.Append(GetOrderConditionSql<T>(orderLambda));
                stringBuilder.Append(" ");
                stringBuilder.Append(descSql);
            }

            return stringBuilder.ToString();
        }
        
        private static void ExecuteCommand(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, Dictionary<string, object> outParameterList, CommandType commandType = CommandType.Text, Action<DbCommand> callback = null, DbConnection con = null, DbTransaction transaction = null)
        {
            DbCommand command = null;
            try
            {
                if (con == null)
                {
                    using (con = CreateDbConnection(connectionString, dataBaseType))
                    {
                        command = CreateDbCommand(con, null, commandText, parameterList, outParameterList, commandType);
                        if (transaction != null) command.Transaction = transaction;
                        if (callback != null) callback(command);
                    }
                }
                else
                {
                    command = CreateDbCommand(con, null, commandText, parameterList, outParameterList, commandType);
                    if (transaction != null) command.Transaction = transaction;
                    if (callback != null) callback(command);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (command != null) command.Dispose();
            }
        }
        private static async Task ExecuteCommandAsync(string connectionString, string dataBaseType, string commandText, Dictionary<string, object> parameterList, Dictionary<string, object> outParameterList, CommandType commandType = CommandType.Text, Func<DbCommand, Task> callback = null, DbConnection con = null, DbTransaction transaction = null)
        {
            DbCommand command = null;
            try
            {
                if (con == null)
                {
                    using (con = CreateDbConnection(connectionString, dataBaseType))
                    {
                        if (con.State != ConnectionState.Open) await con.OpenAsync();

                        command = CreateDbCommand(con, null, commandText, parameterList, outParameterList, commandType);
                        if (transaction != null) command.Transaction = transaction;
                        if (callback != null) await callback(command);
                    }
                }
                else
                {
                    command = CreateDbCommand(con, null, commandText, parameterList, outParameterList, commandType);
                    if (transaction != null) command.Transaction = transaction;
                    if (callback != null) await callback(command);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (command != null) command.Dispose();
            }
        }
        private static object ExecuteTransaction<T>(string connectionString, string dataBaseType, List<DataBaseTransactionItem> transactionItemList, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            return ExecuteTransaction(connectionString, dataBaseType, (DbConnection con, DbTransaction transaction) =>
            {
                object result = null;
                DbCommand command = null;
                // 记录输出参数信息
                Dictionary<string, object> outputParamDict = new Dictionary<string, object>();
                foreach (DataBaseTransactionItem commandItem in transactionItemList)
                {
                    Dictionary<string, object> parameterDict = CommonHelper.GetParameterDict(commandItem.ParameterList);
                    // 如果需要使用之前的输出数据
                    if (commandItem.InputList != null && commandItem.InputList.Length > 0)
                    {
                        foreach (string inputParamItem in commandItem.InputList)
                        {
                            if (outputParamDict.ContainsKey(inputParamItem) && parameterDict.ContainsKey(inputParamItem))
                            {
                                parameterDict[inputParamItem] = outputParamDict[inputParamItem];
                            }
                        }
                    }
                    command = CreateDbCommand(con, command, commandItem.CommandText, parameterDict, null, CommandType.Text);
                    command.Transaction = transaction;

                    if (commandItem.ExecuteType == DataBaseExecuteTypeEnum.ExecuteNonQuery)
                    {
                        result = command.ExecuteNonQuery();
                    }
                    else if (commandItem.ExecuteType == DataBaseExecuteTypeEnum.ExecuteScalar)
                    {
                        result = command.ExecuteScalar();
                    }
                    else if (commandItem.ExecuteType == DataBaseExecuteTypeEnum.ToEntityList)
                    {
                        result = DataReaderToEntityList<T>(command, commandItem.PropertyMatchList, reflectionType, con, transaction);
                    }
                    else if (commandItem.ExecuteType == DataBaseExecuteTypeEnum.ToEntity)
                    {
                        List<T> dataList = DataReaderToEntityList<T>(command, commandItem.PropertyMatchList, reflectionType, con, transaction);
                        if (dataList.Count > 0) result = dataList[0];
                    }
                    // 如果输出参数不为空，则记录输出值
                    if (!string.IsNullOrEmpty(commandItem.OutputName))
                    {
                        outputParamDict.Add(commandItem.OutputName, result);
                    }
                }

                return result;

            });
        }
        private static object ExecuteTransaction(string connectionString, string dataBaseType, Func<DbConnection, DbTransaction, object> func)
        {
            DbConnection con = null;
            DbTransaction transaction = null;
            try
            {
                object result = null;
                using (con = CreateDbConnection(connectionString, dataBaseType))
                {
                    try
                    {
                        transaction = con.BeginTransaction();
                        if (func != null)
                        {
                            result = func(con, transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                return result;
            }
            catch
            {
                throw;
            }
        }
        private static void ExecuteDataReader(Action<DbDataReader> callback, DbCommand command)
        {
            if (callback == null) return;

            using (DbDataReader dataReader = command.ExecuteReader(CommandBehavior.CloseConnection))
            {
                while (dataReader.Read())
                {
                    callback(dataReader);
                }
            }
        }
        private static async Task ExecuteDataReaderAsync(Action<DbDataReader> callback, DbCommand command)
        {
            if (callback == null) return;

            using (DbDataReader dataReader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
            {
                while (dataReader.Read())
                {
                    callback(dataReader);
                }
            }
        }
        private static void ExecuteBatchDataTable(SqlBulkCopy sqlBulkCopy, string tableName, DataTable dataTable)
        {
            using (sqlBulkCopy)
            {
                sqlBulkCopy.DestinationTableName = tableName;

                int columnCount = dataTable.Columns.Count;
                DataColumn dataColumn = null;

                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    dataColumn = dataTable.Columns[columnIndex];
                    sqlBulkCopy.ColumnMappings.Add(dataColumn.ColumnName, dataColumn.ColumnName);
                }

                sqlBulkCopy.WriteToServer(dataTable);
            }
        }
        private static List<T> DataReaderToEntityList<T>(DbCommand command, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            List<T> dataList = new List<T>();

            using (DbDataReader dataReader = (con == null && transaction == null) ? command.ExecuteReader(CommandBehavior.CloseConnection) : command.ExecuteReader())
            {
                dataList = ReadDataFromDataReader<T>(dataReader, propertyMatchList, reflectionType);
            }
            return dataList;
        }
        private static async Task<List<T>> DataReaderToEntityListAsync<T>(DbCommand command, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression, DbConnection con = null, DbTransaction transaction = null) where T : class, new()
        {
            List<T> dataList = new List<T>();

            using (DbDataReader dataReader = (con == null && transaction == null) ? await command.ExecuteReaderAsync(CommandBehavior.CloseConnection) : await command.ExecuteReaderAsync())
            {
                dataList = ReadDataFromDataReader<T>(dataReader, propertyMatchList, reflectionType);
            }
            return dataList;
        }
        private static List<T> ReadDataFromDataReader<T>(DbDataReader dataReader, object propertyMatchList = null, ReflectionTypeEnum reflectionType = ReflectionTypeEnum.Expression) where T : class, new()
        {
            if (dataReader == null) return null;

            List<T> dataList = new List<T>();

            bool initStatus = false;
            List<string> columnNameList = null;
            Dictionary<PropertyInfo, string> columnNameDict = null;

            dynamic propertySetDict = null;
            if (reflectionType != ReflectionTypeEnum.Original) propertySetDict = ReflectionExtendHelper.PropertySetCallDict<T>(reflectionType);

            if (dataReader.Read())
            {
                if (!initStatus)
                {
                    columnNameList = new List<string>();
                    for (int index = 0; index < dataReader.FieldCount; index++) columnNameList.Add(dataReader.GetName(index));
                    columnNameDict = InitDbToEntityMapper<T>(dataReader, propertyMatchList);
                    initStatus = true;
                }
                dataList.Add(DataReaderToEntity<T>(dataReader, propertySetDict, columnNameList, columnNameDict));
            }
            while (dataReader.Read())
            {
                dataList.Add(DataReaderToEntity<T>(dataReader, propertySetDict, columnNameList, columnNameDict));
            }

            return dataList;
        }
        private static T DataReaderToEntity<T>(DbDataReader reader, dynamic propertySetDict, List<string> columnNameList, Dictionary<PropertyInfo, string> columnNameDict) where T : class, new()
        {
            T t = ReflectionGenericHelper.New<T>();
            foreach (var keyValueItem in columnNameDict)
            {
                if (columnNameList.IndexOf(keyValueItem.Value) >= 0)
                {
                    if (propertySetDict != null && propertySetDict.ContainsKey(keyValueItem.Key.Name))
                    {
                        ReflectionGenericHelper.SetPropertyValue(propertySetDict[keyValueItem.Key.Name], t, reader[keyValueItem.Value].ToString(), keyValueItem.Key);
                    }
                    else
                    {
                        ReflectionHelper.SetPropertyValue(t, reader[keyValueItem.Value].ToString(), keyValueItem.Key);
                    }
                }
            }
            return t;
        }
        private static Dictionary<PropertyInfo, string> InitDbToEntityMapper<T>(DbDataReader reader, object propertyMatchList = null) where T : class
        {
            Dictionary<PropertyInfo, string> resultDict = new Dictionary<PropertyInfo, string>();
            if (propertyMatchList != null)
            {
                Dictionary<string, object> propertyMatchDict = CommonHelper.GetParameterDict(propertyMatchList);
                ReflectionGenericHelper.Foreach<T>((PropertyInfo propertyInfo) =>
                {
                    string columnName = propertyInfo.Name;
                    object propertyValue = propertyMatchDict[propertyInfo.Name];
                    if (propertyValue != null) columnName = propertyValue.ToString();
                    resultDict.Add(propertyInfo, columnName);
                });
                return resultDict;
            }

            string key = typeof(T).FullName;
            if (PropertyAttributeDict.ContainsKey(key)) return PropertyAttributeDict[key];

            lock (lockItem)
            {
                ReflectionGenericHelper.Foreach<T>((PropertyInfo propertyInfo) =>
                {
                    string columnName = ReflectionExtendHelper.GetAttributeValue<DataBaseTAttribute>(typeof(T), propertyInfo, p => p.Name);
                    if (string.IsNullOrEmpty(columnName)) columnName = propertyInfo.Name;
                    resultDict.Add(propertyInfo, columnName);
                });
                if (!PropertyAttributeDict.ContainsKey(key))
                {
                    PropertyAttributeDict.Add(key, resultDict);
                }
                return resultDict;
            }
        }
        private static Dictionary<string, object> InitEntityToPropertyMapper(object parameterList, params string[] ignorePropertyList)
        {
            List<string> filterPropertyList = ignorePropertyList != null ? ignorePropertyList.ToList<string>() : null;
            Type type = parameterList.GetType();
            Dictionary<string, object> resultDict = new Dictionary<string, object>();
            ReflectionHelper.Foreach((PropertyInfo propertyInfo) =>
            {
                string fieldName = ReflectionExtendHelper.GetAttributeValue<DataBaseTAttribute>(type, propertyInfo, p => { return p.Name; });
                if (string.IsNullOrEmpty(fieldName)) fieldName = propertyInfo.Name;

                if (ignorePropertyList == null || !ignorePropertyList.Contains(fieldName))
                {
                    resultDict.Add(fieldName, ReflectionHelper.GetPropertyValue(parameterList, propertyInfo));
                }
            }, type);
            return resultDict;
        }
        private static Dictionary<string, object> RevisePropertyMapperDict<T>(Dictionary<string, object> mapperDict, object data, string whereSql) where T : class
        {
            if (!string.IsNullOrEmpty(whereSql) && whereSql.IndexOf("@") > 0)
            {
                Regex regex = new Regex(@"@([a-z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                MatchCollection matchCollection = regex.Matches(whereSql);
                if (matchCollection != null && matchCollection.Count > 0)
                {
                    foreach (Match match in matchCollection)
                    {
                        GroupCollection groupCollection = match.Groups;
                        if (groupCollection != null && groupCollection.Count >= 2)
                        {
                            string propertyName = groupCollection[1].Value;
                            if (!mapperDict.ContainsKey(propertyName))
                            {
                                mapperDict.Add(propertyName, ReflectionHelper.GetPropertyValue(data, propertyName));
                            }
                        }
                    }
                }
            }
            return mapperDict;
        }
        private static string GetDataBaseTableName<T>(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                tableName = ReflectionExtendHelper.GetAttributeValue<DataBaseTAttribute>(typeof(T), (p) => { return p.Name; });
            }
            if (string.IsNullOrEmpty(tableName)) tableName = typeof(T).Name;
            return tableName;
        }
        private static string GetQueryFieldSql<T>(Expression<Func<T, object>> lambda) where T : class
        {
            string sql = "*";
            if (lambda != null)
            {
                sql = new QueryTranslator().Translate(lambda);
                sql = sql.Replace(string.Format("[{0}].", typeof(T).Name), "");
            }
            return sql;
        }
        private static string GetWhereConditionSql<T>(Expression<Func<T, bool>> lambda)
        {
            string sql = new WhereTranslator().Translate(lambda);
            sql = sql.Replace(string.Format("[{0}].", typeof(T).Name), "");
            return sql;
        }
        private static string GetOrderConditionSql<T>(Expression<Func<T, object>> lambda) where T : class
        {
            string sql = "*";
            if (lambda != null)
            {
                sql = new OrderTranslator().Translate(lambda);
                sql = sql.Replace(string.Format("[{0}].", typeof(T).Name), "");
            }
            return sql;
        }
        private static string GetWithNoLockSql(bool withNoLock)
        {
            string sql = "";
            if (withNoLock) sql = "with(nolock)";
            return sql;
        }
        #endregion
    }

    #region 逻辑处理辅助特性
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DataBaseTAttribute : Attribute
    {
        private string name;

        /// <summary>
        /// 实体属性映射数据库列名
        /// </summary>
        /// <param name="name"></param>
        public DataBaseTAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// 实体属性所对应的数据库列名
        /// </summary>
        public string Name { get { return this.name; } }
    }
    #endregion

    #region 逻辑处理辅助类
    public class DataBaseTransactionItem
    {
        /// <summary>
        /// DataBaseExecuteTypeEnum
        /// </summary>
        public string ExecuteType { get; set; }
        /// <summary>
        /// SQL 语句
        /// </summary>
        public string CommandText { get; set; }
        /// <summary>
        /// 参数列表
        /// </summary>
        public object ParameterList { get; set; }
        /// <summary>
        /// 数据匹配
        /// </summary>
        public object PropertyMatchList { get; set; }
        /// <summary>
        /// 使用前一步的导出参数数据
        /// </summary>
        public string[] InputList { get; set; }
        /// <summary>
        /// 导出数据信息
        /// </summary>
        public string OutputName { get; set; }
    }
    public class DataBaseParameterItem
    {
        private string _fieldSql;
        private string _field;
        private string _tableName;
        private string _primaryKey;
        private int _pageIndex;
        private int _pageSize;
        private string _whereSql;
        private string _orderSql;
        private string _joinSql;

        public DataBaseParameterItem() { }
        public DataBaseParameterItem(string tableName, string primaryKey, int pageIndex, int pageSize, string whereSql = "", string orderSql = "", string joinSql = "")
        {
            this._tableName = tableName;
            this._primaryKey = primaryKey;
            this._pageIndex = pageIndex;
            this._pageSize = pageSize;
            this._whereSql = whereSql;
            this._orderSql = orderSql;
            this._joinSql = joinSql;
        }
        /// <summary>
        /// 查询语句，表连接查询时，主表用 T 代替
        /// </summary>
        public string FieldSql
        {
            get { return this._fieldSql; }
            set { this._fieldSql = value; }
        }
        /// <summary>
        /// 查询语句，值为空时，与 FieldSql 字段相同
        /// </summary>
        public string Field
        {
            get { return this._field; }
            set { this._field = value; }
        }
        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName
        {
            get { return this._tableName; }
            set { this._tableName = value; }
        }
        /// <summary>
        /// 主键名称
        /// </summary>
        public string PrimaryKey
        {
            get { return this._primaryKey; }
            set { this._primaryKey = value; }
        }
        /// <summary>
        /// 页索引，从 1 开始
        /// </summary>
        public int PageIndex
        {
            get { return this._pageIndex; }
            set { this._pageIndex = value; }
        }
        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize
        {
            get { return this._pageSize; }
            set { this._pageSize = value; }
        }
        /// <summary>
        /// Where 语句
        /// </summary>
        public string WhereSql
        {
            get { return this._whereSql; }
            set { this._whereSql = value; }
        }
        /// <summary>
        /// Order 语句
        /// </summary>
        public string OrderSql
        {
            get { return this._orderSql; }
            set { this._orderSql = value; }
        }
        /// <summary>
        /// 表连接语句 例： inner join B on A.XX=B.XX
        /// </summary>
        public string JoinSql
        {
            get { return this._joinSql; }
            set { this._joinSql = value; }
        }
    }
    public class DataBasePaginationDataItem<T> where T : class, new()
    {
        public List<T> DataList { get; set; }
        public int PageCount { get; set; }
        public int TotalCount { get; set; }
    }

    #region SqlServer 分页查询语句
    internal class SqlDataBaseItem
    {
        public static string PaginationSql
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("declare @Sql varchar(max)\r\n");
                stringBuilder.Append("declare @Sql1 nvarchar(max)\r\n");


                stringBuilder.Append("if @Field = ''\r\n");
                stringBuilder.Append("begin\r\n");
                stringBuilder.Append("set @Field = @FieldSql\r\n");
                stringBuilder.Append("end\r\n");

                stringBuilder.Append("if @OrderSql = ''\r\n");
                stringBuilder.Append("begin\r\n");
                stringBuilder.Append("set @OrderSql = @PrimaryKey + ' desc '\r\n");
                stringBuilder.Append("end\r\n");

                stringBuilder.Append("if (@WhereSql<>'')\r\n");
                stringBuilder.Append("begin\r\n");
                stringBuilder.Append("set @Sql = 'select ' + @FieldSql + ' from (select ROW_NUMBER() over(order by ' + @OrderSql + ') AS RowNums,' + @Field + ' from ' + @TableName + ' with(nolock) ' + @JoinSql + ' where ' + @WhereSql + ') AS T ' + ' where RowNums between ' + Str((@PageIndex-1) * @PageSize + 1) + ' and ' + Str(@PageIndex * @PageSize) + ' order by ' + @OrderSql\r\n");
                stringBuilder.Append("set @Sql1 = N'Select @Count=Count(0) from ['+@TableName+'] with(nolock) ' + @JoinSql + ' where '+@WhereSql\r\n");
                stringBuilder.Append("end\r\n");
                stringBuilder.Append("else\r\n");
                stringBuilder.Append("begin\r\n");
                stringBuilder.Append("set @Sql = 'select ' + @FieldSql + ' from (select ROW_NUMBER() over(order by ' + @OrderSql + ') AS RowNums,' + @Field + ' from ' + @TableName + ' with(nolock) ' + @JoinSql + ' ) AS T ' + ' where RowNums between ' + Str((@PageIndex-1) * @PageSize + 1) + ' and ' + Str(@PageIndex * @PageSize) + ' order by ' + @OrderSql\r\n");
                stringBuilder.Append("set @Sql1 = N'Select @Count=Count(0) from ['+@TableName+'] with(nolock) ' + @JoinSql + ''\r\n");
                stringBuilder.Append("end\r\n");
                stringBuilder.Append("execute sp_executesql @Sql1, N'@Count int output',@Count=@TotalCount output\r\n");
                stringBuilder.Append("set @PageCount = ceiling(convert(float,@TotalCount)/@PageSize)\r\n");
                stringBuilder.Append("print(@Sql)");
                stringBuilder.Append("exec (@Sql)\r\n");

                return stringBuilder.ToString();
            }
        }
    }
    #endregion

    #endregion
}
