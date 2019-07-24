﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Nm.Lib.Data.Abstractions;
using Nm.Lib.Data.Abstractions.Entities;
using Nm.Lib.Data.Abstractions.SqlQueryable;
using Nm.Lib.Data.Core.Internal;
using Nm.Lib.Data.Core.SqlQueryable;
using CommonExtensions = Nm.Lib.Data.Core.Internal.CommonExtensions;

namespace Nm.Lib.Data.Core
{
    public class DbSet<TEntity> : IDbSet<TEntity> where TEntity : IEntity, new()
    {
        #region ==属性==

        public IDbContext DbContext { get; }

        public IEntityDescriptor EntityDescriptor { get; }

        #endregion

        #region ==字段==

        private readonly ISqlAdapter _sqlAdapter;

        private readonly EntitySql _sql;

        private readonly ILogger _logger;

        #endregion

        #region ==构造函数==

        public DbSet(IDbContext context)
        {
            DbContext = context;
            EntityDescriptor = EntityDescriptorCollection.Get<TEntity>();
            _sqlAdapter = context.Options.SqlAdapter;
            _sql = EntityDescriptor.Sql;

            _logger = context.Options.LoggerFactory?.CreateLogger("DbSet-" + EntityDescriptor.TableName);
        }

        #endregion

        #region ==Insert==

        public bool Insert(TEntity entity, string tableName = null)
        {
            Check.NotNull(entity, nameof(entity));

            SetCreatedBy(entity);

            var sql = _sql.Insert(tableName);

            if (EntityDescriptor.PrimaryKey.IsInt())
            {
                sql += _sqlAdapter.IdentitySql;
                var id = ExecuteScalar<int>(sql, entity);
                if (id > 0)
                {
                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, id);

                    _logger?.LogDebug("Insert:({0}),NewID({1})", sql, id);

                    return true;
                }

                return false;
            }
            if (EntityDescriptor.PrimaryKey.IsLong())
            {
                sql += _sqlAdapter.IdentitySql;
                var id = ExecuteScalar<long>(sql, entity);
                if (id > 0)
                {
                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, id);

                    _logger?.LogDebug("Insert:({0}),NewID({1})", sql, id);

                    return true;
                }
                return false;
            }

            if (EntityDescriptor.PrimaryKey.IsGuid())
            {
                var id = (Guid)EntityDescriptor.PrimaryKey.PropertyInfo.GetValue(entity);
                if (id == Guid.Empty)
                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, _sqlAdapter.GenerateSequentialGuid());

                _logger?.LogDebug("Insert:({0}),NewID({1})", sql, id);

                return Execute(sql, entity) > 0;
            }
            return Execute(sql, entity) > 0;

        }

        public async Task<bool> InsertAsync(TEntity entity, string tableName = null)
        {
            Check.NotNull(entity, nameof(entity));
            SetCreatedBy(entity);

            var sql = _sql.Insert(tableName);

            if (EntityDescriptor.PrimaryKey.IsInt())
            {
                sql += _sqlAdapter.IdentitySql;

                var id = await ExecuteScalarAsync<int>(sql, entity);
                if (id > 0)
                {
                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, id);

                    _logger?.LogDebug("Insert:({0}),NewID({1})", sql, id);

                    return true;
                }

                return false;
            }
            if (EntityDescriptor.PrimaryKey.IsLong())
            {
                sql += _sqlAdapter.IdentitySql;
                var id = await ExecuteScalarAsync<long>(sql, entity);
                if (id > 0)
                {
                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, id);

                    _logger?.LogDebug("Insert:({0}),NewID({1})", sql, id);

                    return true;
                }
                return false;
            }
            if (EntityDescriptor.PrimaryKey.IsGuid())
            {
                var id = (Guid)EntityDescriptor.PrimaryKey.PropertyInfo.GetValue(entity);
                if (id == Guid.Empty)
                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, _sqlAdapter.GenerateSequentialGuid());

                _logger?.LogDebug("Insert:({0}),NewID({1})", sql, id);

                return await ExecuteAsync(sql, entity) > 0;
            }

            _logger?.LogDebug("Insert:({0})", sql);

            return await ExecuteAsync(sql, entity) > 0;

        }

        #endregion

        #region ==BatchInsert==

        public bool BatchInsert(List<TEntity> entityList, int flushSize = 10000, string tableName = null)
        {
            if (entityList == null || !entityList.Any())
                return false;

            var uow = new UnitOfWork(DbContext);

            //判断有没有事务
            var hasTran = DbContext.Transaction != null;
            try
            {
                if (!hasTran)
                    uow.BeginTransaction();

                if (_sqlAdapter.SqlDialect == Abstractions.Enums.SqlDialect.SQLite)
                {
                    #region ==SQLite使用Dapper的官方方法==

                    if (EntityDescriptor.PrimaryKey.IsGuid())
                    {
                        entityList.ForEach(entity =>
                        {
                            SetCreatedBy(entity);

                            var value = EntityDescriptor.PrimaryKey.PropertyInfo.GetValue(entity);
                            if ((Guid)value == Guid.Empty)
                            {
                                value = _sqlAdapter.GenerateSequentialGuid();
                                EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, value);
                            }
                        });
                    }

                    Execute(_sql.Insert(tableName), entityList);

                    #endregion
                }
                else
                {
                    #region ==自定义==

                    var sqlBuilder = new StringBuilder();

                    for (var t = 0; t < entityList.Count; t++)
                    {
                        var mod = (t + 1) % flushSize;
                        if (mod == 1)
                        {
                            sqlBuilder.Clear();
                            sqlBuilder.Append(_sql.BatchInsert(tableName));
                        }

                        var entity = entityList[t];
                        SetCreatedBy(entity);

                        sqlBuilder.Append("(");
                        for (var i = 0; i < _sql.BatchInsertColumnList.Count; i++)
                        {
                            var col = _sql.BatchInsertColumnList[i];
                            var value = col.PropertyInfo.GetValue(entity);
                            var type = col.PropertyInfo.PropertyType;

                            if (col.IsPrimaryKey && EntityDescriptor.PrimaryKey.IsGuid())
                            {
                                if ((Guid)value == Guid.Empty)
                                {
                                    value = _sqlAdapter.GenerateSequentialGuid();
                                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, value);
                                }
                            }

                            AppendValue(sqlBuilder, type, value);

                            if (i < _sql.BatchInsertColumnList.Count - 1)
                                sqlBuilder.Append(",");
                        }

                        sqlBuilder.Append(")");

                        if (mod > 0 && t < entityList.Count - 1)
                            sqlBuilder.Append(",");
                        else if (mod == 0 || t == entityList.Count - 1)
                        {
                            sqlBuilder.Append(";");
                            Execute(sqlBuilder.ToString());
                        }
                    }

                    #endregion
                }

                if (!hasTran)
                    uow.Commit();

                return true;
            }
            catch
            {
                if (!hasTran)
                    uow.Commit();

                uow.Dispose();

                throw;
            }
        }

        public async Task<bool> BatchInsertAsync(List<TEntity> entityList, int flushSize = 10000, string tableName = null)
        {
            if (entityList == null || !entityList.Any())
                return false;

            var uow = new UnitOfWork(DbContext);

            //判断有没有事务
            var hasTran = DbContext.Transaction != null;
            try
            {
                if (!hasTran)
                    uow.BeginTransaction();

                if (_sqlAdapter.SqlDialect == Abstractions.Enums.SqlDialect.SQLite)
                {
                    #region ==SQLite使用Dapper的官方方法==

                    if (EntityDescriptor.PrimaryKey.IsGuid())
                    {
                        entityList.ForEach(entity =>
                        {
                            SetCreatedBy(entity);

                            var value = EntityDescriptor.PrimaryKey.PropertyInfo.GetValue(entity);
                            if ((Guid)value == Guid.Empty)
                            {
                                value = _sqlAdapter.GenerateSequentialGuid();
                                EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, value);
                            }
                        });
                    }

                    await ExecuteAsync(_sql.Insert(tableName), entityList);

                    #endregion
                }
                else
                {
                    #region ==自定义方法==

                    var sqlBuilder = new StringBuilder();

                    for (var t = 0; t < entityList.Count; t++)
                    {
                        var mod = (t + 1) % flushSize;
                        if (mod == 1)
                        {
                            sqlBuilder.Clear();
                            sqlBuilder.Append(_sql.BatchInsert(tableName));
                        }

                        var entity = entityList[t];

                        SetCreatedBy(entity);

                        sqlBuilder.Append("(");
                        for (var i = 0; i < _sql.BatchInsertColumnList.Count; i++)
                        {
                            var col = _sql.BatchInsertColumnList[i];
                            var value = col.PropertyInfo.GetValue(entity);
                            var type = col.PropertyInfo.PropertyType;

                            //是否GUID主键
                            if (col.IsPrimaryKey && EntityDescriptor.PrimaryKey.IsGuid())
                            {
                                if ((Guid)value == Guid.Empty)
                                {
                                    value = _sqlAdapter.GenerateSequentialGuid();
                                    EntityDescriptor.PrimaryKey.PropertyInfo.SetValue(entity, value);
                                }
                            }

                            AppendValue(sqlBuilder, type, value);

                            if (i < _sql.BatchInsertColumnList.Count - 1)
                                sqlBuilder.Append(",");
                        }

                        sqlBuilder.Append(")");

                        if (mod > 0 && t < entityList.Count - 1)
                            sqlBuilder.Append(",");
                        else if (mod == 0 || t == entityList.Count - 1)
                        {
                            sqlBuilder.Append(";");
                            await ExecuteAsync(sqlBuilder.ToString());
                        }
                    }

                    #endregion
                }

                if (!hasTran)
                    uow.Commit();

                return true;
            }
            catch
            {
                if (!hasTran)
                    uow.Rollback();

                uow.Dispose();

                throw;
            }
        }

        #endregion

        #region ==Delete==

        private DynamicParameters GetDeleteParameters(dynamic id)
        {
            PrimaryKeyValidate(id);

            var dynParams = new DynamicParameters();
            dynParams.Add(_sqlAdapter.AppendParameter("Id"), id);
            return dynParams;
        }

        public bool Delete(dynamic id, string tableName = null)
        {
            var dynParams = GetDeleteParameters(id);
            return Execute(_sql.DeleteSingle(tableName), dynParams) > 0;
        }

        public async Task<bool> DeleteAsync(dynamic id, string tableName = null)
        {
            var dynParams = GetDeleteParameters(id);
            return await ExecuteAsync(_sql.DeleteSingle(tableName), dynParams) > 0;
        }

        #endregion

        #region ==SoftDelete==

        private DynamicParameters GetSoftDeleteParameters(dynamic id)
        {
            PrimaryKeyValidate(id);
            var dynParams = new DynamicParameters();
            dynParams.Add(_sqlAdapter.AppendParameter("Id"), id);
            dynParams.Add(_sqlAdapter.AppendParameter("DeletedTime"), DateTime.Now);

            var deleteBy = Guid.Empty;
            if (DbContext.LoginInfo != null)
            {
                deleteBy = DbContext.LoginInfo.AccountId;
            }
            dynParams.Add(_sqlAdapter.AppendParameter("DeletedBy"), deleteBy);


            return dynParams;
        }

        public bool SoftDelete(dynamic id, string tableName = null)
        {
            if (!EntityDescriptor.SoftDelete)
                throw new Exception("该实体未继承软删除实体，无法使用软删除功能~");

            var dynParams = GetSoftDeleteParameters(id);

            return Execute(_sql.SoftDeleteSingle(tableName), dynParams) > 0;
        }

        public async Task<bool> SoftDeleteAsync(dynamic id, string tableName = null)
        {
            if (!EntityDescriptor.SoftDelete)
                throw new Exception("该实体未继承软删除实体，无法使用软删除功能~");

            var dynParams = GetSoftDeleteParameters(id);
            return await ExecuteAsync(_sql.SoftDeleteSingle(tableName), dynParams) > 0;
        }

        #endregion

        #region ==Update==

        private void UpdateCheck(TEntity entity)
        {
            Check.NotNull(entity, nameof(entity));

            if (EntityDescriptor.PrimaryKey.IsNo())
                throw new ArgumentException("没有主键的实体对象无法使用该方法", nameof(entity));

            SetModifiedBy(entity);
        }

        public bool Update(TEntity entity, string tableName = null)
        {
            UpdateCheck(entity);
            return Execute(_sql.UpdateSingle(tableName), entity) > 0;
        }

        public async Task<bool> UpdateAsync(TEntity entity, string tableName = null)
        {
            UpdateCheck(entity);
            return await ExecuteAsync(_sql.UpdateSingle(tableName), entity) > 0;
        }

        #endregion

        #region ==Get==

        private DynamicParameters GetParameters(dynamic id)
        {
            PrimaryKeyValidate(id);

            var dynParams = new DynamicParameters();
            dynParams.Add(_sqlAdapter.AppendParameter("Id"), id);
            return dynParams;
        }
        public TEntity Get(dynamic id, string tableName = null)
        {
            var dynParams = GetParameters(id);
            return QuerySingleOrDefault<TEntity>(_sql.Get(tableName), dynParams);
        }

        public Task<TEntity> GetAsync(dynamic id, string tableName = null)
        {
            var dynParams = GetParameters(id);
            return QuerySingleOrDefaultAsync<TEntity>(_sql.Get(tableName), dynParams);
        }

        #endregion

        #region ==Exists==

        public bool Exists(dynamic id, string tableName = null)
        {
            //没有主键的表无法使用Exists方法
            if (EntityDescriptor.PrimaryKey.IsNo())
                throw new ArgumentException("该实体没有主键，无法使用Exists方法~");

            var dynParams = GetParameters(id);
            return QuerySingleOrDefault<int>(_sql.Exists(tableName), dynParams) > 0;
        }

        public async Task<bool> ExistsAsync(dynamic id, string tableName = null)
        {
            //没有主键的表无法使用Exists方法
            if (EntityDescriptor.PrimaryKey.IsNo())
                throw new ArgumentException("该实体没有主键，无法使用Exists方法~");

            var dynParams = GetParameters(id);
            return (await QuerySingleOrDefaultAsync<int>(_sql.Exists(tableName), dynParams)) > 0;
        }

        #endregion

        #region ==Execute==

        public int Execute(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().Execute(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<int> ExecuteAsync(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().ExecuteAsync(sql, param, DbContext.Transaction, commandType: commandType);
        }

        #endregion

        #region ==ExecuteScalar==

        public T ExecuteScalar<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().ExecuteScalar<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<T> ExecuteScalarAsync<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().ExecuteScalarAsync<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }

        #endregion

        #region ==QueryFirstOrDefault==

        public dynamic QueryFirstOrDefault(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QueryFirstOrDefault(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public T QueryFirstOrDefault<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QueryFirstOrDefault<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<dynamic> QueryFirstOrDefaultAsync(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QueryFirstOrDefaultAsync(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QueryFirstOrDefaultAsync<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }


        #endregion

        #region ==QuerySingleOrDefault==

        public dynamic QuerySingleOrDefault(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QuerySingleOrDefault(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public T QuerySingleOrDefault<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QuerySingleOrDefault<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<dynamic> QuerySingleOrDefaultAsync(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QuerySingleOrDefaultAsync(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<T> QuerySingleOrDefaultAsync<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QuerySingleOrDefaultAsync<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }

        #endregion

        #region ==Query==

        public IEnumerable<dynamic> Query(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().Query(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public IEnumerable<T> Query<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().Query<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<IEnumerable<dynamic>> QueryAsync(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QueryAsync(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, CommandType? commandType = null)
        {
            return DbContext.Open().QueryAsync<T>(sql, param, DbContext.Transaction, commandType: commandType);
        }

        public INetSqlQueryable<TEntity> Find(Expression<Func<TEntity, bool>> expression = null, string tableName = null)
        {
            return new NetSqlQueryable<TEntity>(this, expression, tableName);
        }

        #endregion

        #region ==私有方法==

        /// <summary>
        /// 主键验证
        /// </summary>
        /// <param name="id"></param>
        private void PrimaryKeyValidate(dynamic id)
        {
            //没有主键的表无法删除单条记录
            if (EntityDescriptor.PrimaryKey.IsNo())
                throw new ArgumentException("该实体没有主键，无法使用该方法~");

            //验证id有效性
            if (EntityDescriptor.PrimaryKey.IsInt() || EntityDescriptor.PrimaryKey.IsLong())
            {
                if (id < 1)
                    throw new ArgumentException("主键不能小于1~");
            }
            else
            {
                if (id == null)
                    throw new ArgumentException("主键不能为空~");
            }
        }

        /// <summary>
        /// 附加值
        /// </summary>
        /// <param name="sqlBuilder"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        private void AppendValue(StringBuilder sqlBuilder, Type type, object value)
        {
            if (type.IsEnum || type == typeof(bool))
                sqlBuilder.AppendFormat("{0}", CommonExtensions.ToInt(value));
            else if (type == typeof(string) || type == typeof(char) || type == typeof(Guid))
                sqlBuilder.AppendFormat("'{0}'", value);
            else if (type == typeof(DateTime))
                sqlBuilder.AppendFormat("'{0:yyyy-MM-dd HH:mm:ss}'", CommonExtensions.ToDateTime(value));
            else
                sqlBuilder.AppendFormat("{0}", value);
        }

        /// <summary>
        /// 设置添加人以及修改人
        /// </summary>
        /// <param name="entity"></param>
        private void SetCreatedBy(TEntity entity)
        {
            if (EntityDescriptor.IsEntityBase && DbContext.LoginInfo != null)
            {
                int i = 0;
                foreach (var column in EntityDescriptor.Columns)
                {
                    if (column.Name.Equals("CreatedBy") || column.Name.Equals("ModifiedBy"))
                    {
                        var createdBy = (Guid)column.PropertyInfo.GetValue(entity);
                        if (createdBy == Guid.Empty)
                        {
                            createdBy = DbContext.LoginInfo.AccountId;
                            column.PropertyInfo.SetValue(entity, createdBy);
                            i++;
                        }
                    }

                    if (i > 1)
                        break;
                }
            }
        }

        /// <summary>
        /// 设置修改人
        /// </summary>
        /// <param name="entity"></param>
        private void SetModifiedBy(TEntity entity)
        {
            if (EntityDescriptor.IsEntityBase && DbContext.LoginInfo != null)
            {
                int i = 0;
                foreach (var column in EntityDescriptor.Columns)
                {
                    if (column.Name.Equals("ModifiedBy"))
                    {
                        var modifiedBy = (Guid)column.PropertyInfo.GetValue(entity);
                        var accountId = DbContext.LoginInfo.AccountId;
                        if (modifiedBy == Guid.Empty || modifiedBy != accountId)
                        {
                            column.PropertyInfo.SetValue(entity, accountId);
                            i++;
                        }
                    }

                    if (column.Name.Equals("ModifiedTime"))
                    {
                        column.PropertyInfo.SetValue(entity, DateTime.Now);
                        i++;
                    }

                    if (i > 1)
                        break;
                }
            }
        }

        #endregion
    }
}
