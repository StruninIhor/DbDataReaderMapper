using System;

namespace DbDataReaderMapper
{
    /// <summary>
    /// Marks a property as a nested object whose properties are mapped from
    /// columns that share a common prefix in the result set.
    /// </summary>
    /// <remarks>
    /// When a query returns flattened columns from a JOIN, use this attribute
    /// to map prefixed columns into a nested object. For example, columns
    /// "Dept_Id" and "Dept_Name" with prefix "Dept_" map to the nested
    /// object's Id and Name properties respectively.
    /// If all prefixed columns are NULL (e.g. from a LEFT JOIN with no match),
    /// the nested property is set to null rather than an empty instance.
    /// 
    /// Properties on the nested type can use <see cref="DbColumnAttribute"/> to
    /// map from a custom column name. By default, the attribute value is treated as
    /// a prefix-relative name (the prefix is prepended). For example, with prefix
    /// "Dept_", <c>[DbColumn("DepartmentId")]</c> matches column "Dept_DepartmentId".
    /// 
    /// Set <see cref="DbColumnAttribute.Override"/> to <c>true</c> to treat the
    /// attribute value as the full column name, ignoring the prefix entirely.
    /// For example, <c>[DbColumn("DepartmentId", Override = true)]</c> matches
    /// column "DepartmentId" directly. Override mappings take priority over
    /// prefix-based matching.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class DbNestedObjectAttribute : Attribute
    {
        public string ColumnPrefix { get; private set; }

        /// <summary>
        /// Maps prefixed columns in the result set to a nested object property
        /// </summary>
        /// <param name="columnPrefix">The column name prefix that identifies columns belonging to this nested object</param>
        public DbNestedObjectAttribute(string columnPrefix)
        {
            ColumnPrefix = columnPrefix;
        }
    }
}
