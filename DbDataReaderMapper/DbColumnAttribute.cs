using System;

namespace DbDataReaderMapper
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DbColumnAttribute : Attribute
    {
        public string Name { get; private set; }

        /// <summary>
        /// When true on a property inside a nested object type, the <see cref="Name"/>
        /// is treated as the full column name in the result set, ignoring the prefix.
        /// When false (the default), the prefix is prepended to <see cref="Name"/>.
        /// Has no effect on properties of the root/parent type.
        /// </summary>
        public bool Override { get; set; }

        /// <summary>
        /// Maps to a column in the result set with the given name
        /// </summary>
        /// <param name="name">The name of the column to map to</param>
        public DbColumnAttribute(string name)
        {
            Name = name;
        }
    }
}
