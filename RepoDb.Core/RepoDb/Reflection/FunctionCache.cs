﻿using RepoDb.Extensions;
using RepoDb.Interfaces;
using RepoDb.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace RepoDb
{
    /// <summary>
    /// A class that is being used to cache the compiled functions.
    /// </summary>
    internal static class FunctionCache
    {
        #region Helpers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static long GetReaderFieldsHashCode(DbDataReader reader)
        {
            long hashCode = 0;

            if (reader is not null)
            {
                for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
                {
                    hashCode = HashCode.Combine(hashCode, reader.GetName(ordinal), ordinal, reader.GetFieldType(ordinal));
                }
            }

            return hashCode;
        }

        #endregion

        #region GetDataReaderToDataEntityCompiledFunction

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="reader"></param>
        /// <param name="dbFields">The list of the <see cref="DbField"/> objects to be used.</param>
        /// <param name="dbSetting">The instance of <see cref="IDbSetting"/> object to be used.</param>
        /// <returns></returns>
        internal static Func<DbDataReader, TResult> GetDataReaderToTypeCompiledFunction<TResult>(DbDataReader reader,
            IEnumerable<DbField> dbFields = null,
            IDbSetting dbSetting = null) =>
            DataReaderToTypeCache<TResult>.Get(reader, dbFields, dbSetting);

        #region DataReaderToTypeCache

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        private static class DataReaderToTypeCache<TResult>
        {
            private static ConcurrentDictionary<long, Func<DbDataReader, TResult>> cache = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="dbFields">The list of the <see cref="DbField"/> objects to be used.</param>
            /// <param name="dbSetting">The instance of <see cref="IDbSetting"/> object to be used.</param>
            /// <returns></returns>
            internal static Func<DbDataReader, TResult> Get(DbDataReader reader,
                IEnumerable<DbField> dbFields = null,
                IDbSetting dbSetting = null)
            {
                var key = GetKey(reader);
                if (cache.TryGetValue(key, out var result) == false)
                {
                    result = FunctionFactory.CompileDataReaderToType<TResult>(reader, dbFields, dbSetting);
                    cache.TryAdd(key, result);
                }
                return result;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="reader"></param>
            /// <returns></returns>
            private static long GetKey(DbDataReader reader) =>
                GetReaderFieldsHashCode(reader) + typeof(TResult).GetHashCode();
        }

        #endregion

        #endregion

        #region GetDataReaderToExpandoObjectCompileFunction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="dbFields"></param>
        /// <param name="dbSetting"></param>
        /// <returns></returns>
        internal static Func<DbDataReader, dynamic> GetDataReaderToExpandoObjectCompileFunction(DbDataReader reader,
            IEnumerable<DbField> dbFields = null,
            IDbSetting dbSetting = null) =>
            DataReaderToExpandoObjectCache.Get(reader, dbFields, dbSetting);

        #region DataReaderToExpandoObjectCache

        /// <summary>
        /// 
        /// </summary>
        private static class DataReaderToExpandoObjectCache
        {
            private static ConcurrentDictionary<long, Func<DbDataReader, dynamic>> cache = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="dbFields"></param>
            /// <param name="dbSetting"></param>
            /// <returns></returns>
            internal static Func<DbDataReader, dynamic> Get(DbDataReader reader,
                IEnumerable<DbField> dbFields = null,
                IDbSetting dbSetting = null)
            {
                var key = GetKey(reader);
                if (cache.TryGetValue(key, out var result) == false)
                {
                    result = FunctionFactory.CompileDataReaderToExpandoObject(reader, dbFields, dbSetting);
                    cache.TryAdd(key, result);
                }
                return result;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="reader"></param>
            /// <returns></returns>
            private static long GetKey(DbDataReader reader) =>
                GetReaderFieldsHashCode(reader);
        }

        #endregion

        #endregion

        #region GetDataEntityDbParameterSetterCompiledFunction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="cacheKey"></param>
        /// <param name="inputFields"></param>
        /// <param name="outputFields"></param>
        /// <param name="dbSetting"></param>
        /// <returns></returns>
        internal static Action<DbCommand, object> GetDataEntityDbParameterSetterCompiledFunction(Type entityType,
            string cacheKey,
            IEnumerable<DbField> inputFields,
            IEnumerable<DbField> outputFields,
            IDbSetting dbSetting = null) =>
            DataEntityDbParameterSetterCache.Get(entityType,
                cacheKey,
                inputFields,
                outputFields,
                dbSetting);

        #region DataEntityDbParameterSetterCache

        /// <summary>
        /// 
        /// </summary>
        private static class DataEntityDbParameterSetterCache
        {
            private static ConcurrentDictionary<long, Action<DbCommand, object>> cache = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="entityType"></param>
            /// <param name="cacheKey"></param>
            /// <param name="inputFields"></param>
            /// <param name="outputFields"></param>
            /// <param name="dbSetting"></param>
            /// <returns></returns>
            internal static Action<DbCommand, object> Get(Type entityType,
                string cacheKey,
                IEnumerable<DbField> inputFields,
                IEnumerable<DbField> outputFields,
                IDbSetting dbSetting = null)
            {
                var key = GetKey(entityType, cacheKey, inputFields, outputFields);
                if (cache.TryGetValue(key, out var func) == false)
                {
                    if (entityType.IsDictionaryStringObject())
                    {
                        func = FunctionFactory.CompileDictionaryStringObjectDbParameterSetter(entityType, inputFields, dbSetting);
                    }
                    else
                    {
                        func = FunctionFactory.CompileDataEntityDbParameterSetter(entityType, inputFields, outputFields, dbSetting);
                    }
                }
                return func;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="entityType"></param>
            /// <param name="cacheKey"></param>
            /// <param name="inputFields"></param>
            /// <param name="outputFields"></param>
            /// <returns></returns>
            private static long GetKey(Type entityType,
                string cacheKey,
                IEnumerable<DbField> inputFields,
                IEnumerable<DbField> outputFields)
            {
                var key = (long)entityType.GetHashCode() + cacheKey.GetHashCode();
                if (inputFields != null)
                {
                    foreach (var field in inputFields)
                    {
                        key += field.GetHashCode();
                    }
                }
                if (outputFields != null)
                {
                    foreach (var field in outputFields)
                    {
                        key += field.GetHashCode();
                    }
                }
                return key;
            }
        }

        #endregion

        #endregion

        #region GetDataEntityListDbParameterSetterCompiledFunction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="cacheKey"></param>
        /// <param name="inputFields"></param>
        /// <param name="outputFields"></param>
        /// <param name="batchSize"></param>
        /// <param name="dbSetting"></param>
        /// <returns></returns>
        internal static Action<DbCommand, IList<object>> GetDataEntityListDbParameterSetterCompiledFunction(Type entityType,
            string cacheKey,
            IEnumerable<DbField> inputFields,
            IEnumerable<DbField> outputFields,
            int batchSize,
            IDbSetting dbSetting = null) =>
            DataEntityListDbParameterSetterCache.Get(entityType, cacheKey, inputFields, outputFields, batchSize, dbSetting);

        #region DataEntityListDbParameterSetterCache

        /// <summary>
        /// 
        /// </summary>
        private static class DataEntityListDbParameterSetterCache
        {
            private static ConcurrentDictionary<long, Action<DbCommand, IList<object>>> cache = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="entityType"></param>
            /// <param name="cacheKey"></param>
            /// <param name="inputFields"></param>
            /// <param name="outputFields"></param>
            /// <param name="batchSize"></param>
            /// <param name="dbSetting"></param>
            /// <returns></returns>
            internal static Action<DbCommand, IList<object>> Get(Type entityType,
                string cacheKey,
                IEnumerable<DbField> inputFields,
                IEnumerable<DbField> outputFields,
                int batchSize,
                IDbSetting dbSetting = null)
            {
                var key = GetKey(entityType, cacheKey, inputFields, outputFields, batchSize);
                if (cache.TryGetValue(key, out var func) == false)
                {
                    if (entityType.IsDictionaryStringObject())
                    {
                        func = FunctionFactory.CompileDictionaryStringObjectListDbParameterSetter(entityType,
                            inputFields,
                            batchSize,
                            dbSetting);
                    }
                    else
                    {
                        func = FunctionFactory.CompileDataEntityListDbParameterSetter(entityType,
                            inputFields,
                            outputFields,
                            batchSize,
                            dbSetting);
                    }
                    cache.TryAdd(key, func);
                }
                return func;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="entityType"></param>
            /// <param name="cacheKey"></param>
            /// <param name="inputFields"></param>
            /// <param name="outputFields"></param>
            /// <param name="batchSize"></param>
            /// <returns></returns>
            private static long GetKey(Type entityType,
                string cacheKey,
                IEnumerable<DbField> inputFields,
                IEnumerable<DbField> outputFields,
                int batchSize)
            {
                var key = (long)entityType.GetHashCode() + batchSize.GetHashCode() +
                    (cacheKey?.GetHashCode()).GetValueOrDefault();
                if (inputFields?.Any() == true)
                {
                    foreach (var field in inputFields)
                    {
                        key += field.GetHashCode();
                    }
                }
                if (outputFields?.Any() == true)
                {
                    foreach (var field in outputFields)
                    {
                        key += field.GetHashCode();
                    }
                }
                return key;
            }
        }

        #endregion

        #endregion

        #region GetDbCommandToPropertyCompiledFunction

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <param name="parameterName"></param>
        /// <param name="index"></param>
        /// <param name="dbSetting"></param>
        /// <returns></returns>
        internal static Action<TEntity, DbCommand> GetDbCommandToPropertyCompiledFunction<TEntity>(Field field,
            string parameterName,
            int index,
            IDbSetting dbSetting = null)
            where TEntity : class =>
            DbCommandToPropertyCache<TEntity>.Get(field, parameterName, index, dbSetting);

        #region DbCommandToPropertyCache

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        private static class DbCommandToPropertyCache<TEntity>
            where TEntity : class
        {
            private static ConcurrentDictionary<long, Action<TEntity, DbCommand>> cache = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="field"></param>
            /// <param name="parameterName"></param>
            /// <param name="index"></param>
            /// <param name="dbSetting"></param>
            /// <returns></returns>
            internal static Action<TEntity, DbCommand> Get(Field field,
                string parameterName,
                int index,
                IDbSetting dbSetting = null)
            {
                var key = (long)typeof(TEntity).GetHashCode() + field.GetHashCode() +
                    parameterName.GetHashCode() + index.GetHashCode();
                if (cache.TryGetValue(key, out var func) == false)
                {
                    func = FunctionFactory.CompileDbCommandToProperty<TEntity>(field, parameterName, index, dbSetting);
                    cache.TryAdd(key, func);
                }
                return func;
            }
        }

        #endregion

        #endregion

        #region GetDataEntityPropertySetterCompiledFunction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        internal static Action<object, object> GetDataEntityPropertySetterCompiledFunction(Type entityType,
            Field field) =>
            DataEntityPropertySetterCache.Get(entityType, field);

        #region DataEntityPropertySetterCache

        /// <summary>
        /// 
        /// </summary>
        private static class DataEntityPropertySetterCache
        {
            private static ConcurrentDictionary<long, Action<object, object>> cache = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <param name="field"></param>
            /// <returns></returns>
            internal static Action<object, object> Get(Type type,
                Field field)
            {
                var key = (long)type.GetHashCode() + field.GetHashCode();
                if (cache.TryGetValue(key, out var func) == false)
                {
                    if (type.IsDictionaryStringObject())
                    {
                        func = FunctionFactory.CompileDictionaryStringObjectItemSetter(type, field);
                    }
                    else
                    {
                        func = FunctionFactory.CompileDataEntityPropertySetter(type, field);
                    }
                    cache.TryAdd(key, func);
                }
                return func;
            }
        }

        #endregion

        #endregion

        #region GetPlainTypeToDbParametersCompiledFunction

        /// <summary>
        /// 
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="entityType"></param>
        /// <param name="dbFields"></param>
        /// <returns></returns>
        internal static Action<DbCommand, object> GetPlainTypeToDbParametersCompiledFunction(Type paramType,
            Type entityType,
            IEnumerable<DbField> dbFields = null) =>
            PlainTypeToDbParametersCompiledFunctionCache.Get(paramType, entityType, dbFields);

        #region PlainTypeToDbParametersCompiledFunctionCache

        /// <summary>
        /// 
        /// </summary>
        private static class PlainTypeToDbParametersCompiledFunctionCache
        {
            private static ConcurrentDictionary<long, Action<DbCommand, object>> cache = new();

            /// <summary>
            /// 
            /// </summary>
            /// <param name="paramType"></param>
            /// <param name="entityType"></param>
            /// <param name="dbFields"></param>
            /// <returns></returns>
            internal static Action<DbCommand, object> Get(Type paramType,
                Type entityType,
                IEnumerable<DbField> dbFields = null)
            {
                if (paramType == null)
                {
                    return null;
                }
                var key = paramType.GetHashCode() + (entityType?.GetHashCode() ?? 0);
                if (cache.TryGetValue(key, out var func) == false)
                {
                    if (paramType.IsPlainType())
                    {
                        func = FunctionFactory.GetPlainTypeToDbParametersCompiledFunction(paramType, entityType, dbFields);
                    }
                    cache.TryAdd(key, func);
                }
                return func;
            }
        }

        #endregion

        #endregion
    }
}
