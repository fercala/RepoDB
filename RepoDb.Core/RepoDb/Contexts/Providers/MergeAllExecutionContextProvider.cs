﻿using RepoDb.Contexts.Cachers;
using RepoDb.Contexts.Execution;
using RepoDb.Extensions;
using RepoDb.Interfaces;
using RepoDb.Requests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RepoDb.Contexts.Providers
{
    /// <summary>
    /// 
    /// </summary>
    internal static class MergeAllExecutionContextProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="tableName"></param>
        /// <param name="qualifiers"></param>
        /// <param name="fields"></param>
        /// <param name="batchSize"></param>
        /// <param name="hints"></param>
        /// <returns></returns>
        private static string GetKey(Type entityType,
            string tableName,
            IEnumerable<Field> qualifiers,
            IEnumerable<Field> fields,
            int batchSize,
            string hints)
        {
            return string.Concat(entityType.FullName,
                ";",
                tableName,
                ";",
                qualifiers?.Select(f => f.Name).Join(","),
                ";",
                fields?.Select(f => f.Name).Join(","),
                ";",
                batchSize.ToString(),
                ";",
                hints);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="connection"></param>
        /// <param name="entities"></param>
        /// <param name="tableName"></param>
        /// <param name="qualifiers"></param>
        /// <param name="batchSize"></param>
        /// <param name="fields"></param>
        /// <param name="hints"></param>
        /// <param name="transaction"></param>
        /// <param name="statementBuilder"></param>
        /// <returns></returns>
        public static MergeAllExecutionContext Create(Type entityType,
            IDbConnection connection,
            IEnumerable<object> entities,
            string tableName,
            IEnumerable<Field> qualifiers,
            int batchSize,
            IEnumerable<Field> fields,
            string hints = null,
            IDbTransaction transaction = null,
            IStatementBuilder statementBuilder = null)
        {
            var key = GetKey(entityType, tableName, qualifiers, fields, batchSize, hints);

            // Get from cache
            var context = MergeAllExecutionContextCache.Get(key);
            if (context != null)
            {
                return context;
            }

            // Create
            var dbFields = DbFieldCache.Get(connection, tableName, transaction);
            var commandText = (string)null;

            // Create a different kind of requests
            if (batchSize > 1)
            {
                var request = new MergeAllRequest(tableName,
                    connection,
                    transaction,
                    fields,
                    qualifiers,
                    batchSize,
                    hints,
                    statementBuilder);
                commandText = CommandTextCache.GetMergeAllText(request);
            }
            else
            {
                var request = new MergeRequest(tableName,
                    connection,
                    transaction,
                    fields,
                    qualifiers,
                    hints,
                    statementBuilder);
                commandText = CommandTextCache.GetMergeText(request);
            }

            // Call
            context = CreateInternal(entityType,
                connection,
                entities,
                dbFields,
                tableName,
                qualifiers,
                batchSize,
                fields,
                commandText);

            // Add to cache
            MergeAllExecutionContextCache.Add(key, context);

            // Return
            return context;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="connection"></param>
        /// <param name="entities"></param>
        /// <param name="tableName"></param>
        /// <param name="qualifiers"></param>
        /// <param name="batchSize"></param>
        /// <param name="fields"></param>
        /// <param name="hints"></param>
        /// <param name="transaction"></param>
        /// <param name="statementBuilder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<MergeAllExecutionContext> CreateAsync(Type entityType,
            IDbConnection connection,
            IEnumerable<object> entities,
            string tableName,
            IEnumerable<Field> qualifiers,
            int batchSize,
            IEnumerable<Field> fields,
            string hints = null,
            IDbTransaction transaction = null,
            IStatementBuilder statementBuilder = null,
            CancellationToken cancellationToken = default)
        {
            var key = GetKey(entityType, tableName, qualifiers, fields, batchSize, hints);

            // Get from cache
            var context = MergeAllExecutionContextCache.Get(key);
            if (context != null)
            {
                return context;
            }

            // Create
            var dbFields = await DbFieldCache.GetAsync(connection, tableName, transaction, cancellationToken);
            var commandText = (string)null;

            // Create a different kind of requests
            if (batchSize > 1)
            {
                var request = new MergeAllRequest(tableName,
                    connection,
                    transaction,
                    fields,
                    qualifiers,
                    batchSize,
                    hints,
                    statementBuilder);
                commandText = await CommandTextCache.GetMergeAllTextAsync(request, cancellationToken);
            }
            else
            {
                var request = new MergeRequest(tableName,
                    connection,
                    transaction,
                    fields,
                    qualifiers,
                    hints,
                    statementBuilder);
                commandText = await CommandTextCache.GetMergeTextAsync(request, cancellationToken);
            }

            // Call
            context = CreateInternal(entityType,
                connection,
                entities,
                dbFields,
                tableName,
                qualifiers,
                batchSize,
                fields,
                commandText);

            // Add to cache
            MergeAllExecutionContextCache.Add(key, context);

            // Return
            return context;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="connection"></param>
        /// <param name="entities"></param>
        /// <param name="dbFields"></param>
        /// <param name="tableName"></param>
        /// <param name="qualifiers"></param>
        /// <param name="batchSize"></param>
        /// <param name="fields"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        private static MergeAllExecutionContext CreateInternal(Type entityType,
            IDbConnection connection,
            IEnumerable<object> entities,
            IEnumerable<DbField> dbFields,
            string tableName,
            IEnumerable<Field> qualifiers,
            int batchSize,
            IEnumerable<Field> fields,
            string commandText)
        {
            var dbSetting = connection.GetDbSetting();
            var identity = (Field)null;
            var inputFields = (IEnumerable<DbField>)null;
            var identityDbField = dbFields?.FirstOrDefault(f => f.IsIdentity);

            // Check the fields
            if (fields?.Any() != true)
            {
                fields = dbFields?.AsFields();
            }

            // Check the qualifiers
            if (qualifiers?.Any() != true)
            {
                var primary = dbFields?.FirstOrDefault(dbField => dbField.IsPrimary == true);
                qualifiers = primary?.AsField().AsEnumerable();
            }

            // Set the identity field
            identity = IdentityCache.Get(entityType)?.AsField() ??
                FieldCache
                    .Get(entityType)?
                    .FirstOrDefault(field =>
                        string.Equals(field.Name.AsUnquoted(true, dbSetting), identityDbField?.Name.AsUnquoted(true, dbSetting), StringComparison.OrdinalIgnoreCase)) ??
                identityDbField?.AsField();

            // Filter the actual properties for input fields
            inputFields = dbFields?
                .Where(dbField =>
                    fields.FirstOrDefault(field => string.Equals(field.Name.AsUnquoted(true, dbSetting), dbField.Name.AsUnquoted(true, dbSetting), StringComparison.OrdinalIgnoreCase)) != null)
                .AsList();

            // Exclude the fields not on the actual entity
            if (entityType.IsClassType() == false)
            {
                var entityFields = Field.Parse(entities?.FirstOrDefault());
                inputFields = inputFields?
                    .Where(field =>
                        entityFields.FirstOrDefault(f => string.Equals(f.Name.AsUnquoted(true, dbSetting), field.Name.AsUnquoted(true, dbSetting), StringComparison.OrdinalIgnoreCase)) != null)
                    .AsList();
            }

            // Variables for the context
            var multipleEntitiesFunc = (Action<DbCommand, IList<object>>)null;
            var singleEntityFunc = (Action<DbCommand, object>)null;
            var identitySetterFunc = (Action<object, object>)null;

            // Get if we have not skipped it
            if (identity != null)
            {
                identitySetterFunc = FunctionCache.GetDataEntityPropertySetterCompiledFunction(entityType, identity);
            }

            // Identity which objects to set
            if (batchSize <= 1)
            {
                singleEntityFunc = FunctionCache.GetDataEntityDbParameterSetterCompiledFunction(entityType,
                    string.Concat(entityType.FullName, StringConstant.Period, tableName, ".MergeAll"),
                    inputFields,
                    null,
                    dbSetting);
            }
            else
            {
                multipleEntitiesFunc = FunctionCache.GetDataEntityListDbParameterSetterCompiledFunction(entityType,
                    string.Concat(entityType.FullName, StringConstant.Period, tableName, ".MergeAll"),
                    inputFields,
                    null,
                    batchSize,
                    dbSetting);
            }

            // Return the value
            return new MergeAllExecutionContext
            {
                CommandText = commandText,
                InputFields = inputFields,
                BatchSize = batchSize,
                SingleDataEntityParametersSetterFunc = singleEntityFunc,
                MultipleDataEntitiesParametersSetterFunc = multipleEntitiesFunc,
                IdentityPropertySetterFunc = identitySetterFunc
            };
        }
    }
}
