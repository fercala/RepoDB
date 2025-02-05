﻿using RepoDb.Extensions;
using RepoDb.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;

namespace RepoDb.Requests
{
    /// <summary>
    /// A class that holds the value of the 'Merge' operation arguments.
    /// </summary>
    internal class MergeRequest : BaseRequest
    {
        private int? hashCode = null;

        /// <summary>
        /// Creates a new instance of <see cref="MergeRequest"/> object.
        /// </summary>
        /// <param name="type">The target type.</param>
        /// <param name="connection">The connection object.</param>
        /// <param name="transaction">The transaction object.</param>
        /// <param name="fields">The list of the target fields.</param>
        /// <param name="qualifiers">The list of qualifier <see cref="Field"/> objects.</param>
        /// <param name="hints">The hints for the table.</param>
        /// <param name="statementBuilder">The statement builder.</param>
        public MergeRequest(Type type,
            IDbConnection connection,
            IDbTransaction transaction,
            IEnumerable<Field> fields = null,
            IEnumerable<Field> qualifiers = null,
            string hints = null,
            IStatementBuilder statementBuilder = null)
            : this(ClassMappedNameCache.Get(type),
                connection,
                transaction,
                fields,
                qualifiers,
                hints,
                statementBuilder)
        {
            Type = type;
        }

        /// <summary>
        /// Creates a new instance of <see cref="MergeRequest"/> object.
        /// </summary>
        /// <param name="name">The name of the request.</param>
        /// <param name="connection">The connection object.</param>
        /// <param name="transaction">The transaction object.</param>
        /// <param name="fields">The list of the target fields.</param>
        /// <param name="qualifiers">The list of qualifier <see cref="Field"/> objects.</param>
        /// <param name="hints">The hints for the table.</param>
        /// <param name="statementBuilder">The statement builder.</param>
        public MergeRequest(string name,
            IDbConnection connection,
            IDbTransaction transaction,
            IEnumerable<Field> fields = null,
            IEnumerable<Field> qualifiers = null,
            string hints = null,
            IStatementBuilder statementBuilder = null)
            : base(name,
                connection,
                transaction,
                statementBuilder)
        {
            Fields = fields?.AsList();
            Qualifiers = qualifiers?.AsList();
            Hints = hints;
        }

        /// <summary>
        /// Gets the list of the target fields.
        /// </summary>
        public IEnumerable<Field> Fields { get; set; }

        /// <summary>
        /// Gets the qualifier <see cref="Field"/> objects.
        /// </summary>
        public IEnumerable<Field> Qualifiers { get; set; }

        /// <summary>
        /// Gets the hints for the table.
        /// </summary>
        public string Hints { get; }

        #region Equality and comparers

        /// <summary>
        /// Returns the hashcode for this <see cref="MergeRequest"/>.
        /// </summary>
        /// <returns>The hashcode value.</returns>
        public override int GetHashCode()
        {
            // Make sure to return if it is already provided
            if (this.hashCode != null)
            {
                return this.hashCode.Value;
            }

            // Get first the entity hash code
            var hashCode = HashCode.Combine(base.GetHashCode(), Name, ".Merge");

            // Get the qualifier <see cref="Field"/> objects
            if (Fields != null)
            {
                foreach (var field in Fields)
                {
                    hashCode = HashCode.Combine(hashCode, field);
                }
            }

            // Get the qualifier <see cref="Field"/> objects
            if (Qualifiers != null) // Much faster than Qualifiers?.<Methods|Properties>
            {
                foreach (var field in Qualifiers)
                {
                    hashCode = HashCode.Combine(hashCode, field);
                }
            }

            // Add the hints
            if (!string.IsNullOrWhiteSpace(Hints))
            {
                hashCode = HashCode.Combine(hashCode, Hints);
            }

            // Set and return the hashcode
            return (this.hashCode = hashCode).Value;
        }

        #endregion
    }
}
