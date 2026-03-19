using System.Linq;
using System.Data.Common;
using System.Reflection;
using System;
using System.Threading.Tasks;
using DbDataReaderMapper.Exceptions;
using System.Collections.Generic;
using System.Threading;

namespace DbDataReaderMapper
{
    public static class DbDataReaderExtension
    {
        /// <summary>
        /// Maps the current row to the specified type
        /// </summary>
        /// <typeparam name="T">The type of the output object</typeparam>
        /// <param name="dataReader">The data source</param>
        /// <param name="customPropertyConverter">Use a custom converter for certain values</param>
        /// <returns>The object that contains the data in the current row of the reader</returns>
        public static T MapToObject<T>(this DbDataReader dataReader, CustomPropertyConverter customPropertyConverter = null) where T : class
        {
            T obj = Activator.CreateInstance<T>();
            PropertyInfo[] typeProperties = typeof(T).GetProperties();
            var customNameMappings = typeProperties
                .Where(tp => GetColumnAttribute(tp) != null)
                .ToDictionary(tp => GetColumnAttribute(tp), tp => tp);

            var nestedMappings = GetNestedMappings(typeProperties);
            var nestedInstances = CreateNestedInstances(nestedMappings);
            var nestedHasValue = InitializeNestedValueTracking(nestedMappings);

            for (int i = 0; i < dataReader.FieldCount; ++i)
            {
                string columnName = dataReader.GetName(i);

                var mappedProperty = typeProperties.Where(tp => tp.Name.Equals(columnName)).FirstOrDefault();
                var mappedPropertyCustomName = customNameMappings.ContainsKey(columnName) ? customNameMappings[columnName] : null;

                if (IsAttributePropertyNamingClash(customNameMappings, columnName, mappedProperty, mappedPropertyCustomName))
                {
                    /*
                     * If the attribute has the same name as another property in the model that doesn't have a custom name, it causes a clash
                     */
                    throw new DbColumnMappingException($"Attribute {columnName} has the same name as a property defined in the model");
                }

                // the attribute name takes precedence over the property name
                var resolvedMappedProperty = mappedPropertyCustomName ?? mappedProperty;

                if (resolvedMappedProperty != null && !IsNestedProperty(resolvedMappedProperty))
                {
                    var value = dataReader.GetValue(i);
                    if (value is DBNull)
                    {
                        value = null;
                    }

                    try
                    {
                        if (customPropertyConverter != null && customPropertyConverter[resolvedMappedProperty] != null)
                        {
                            resolvedMappedProperty.SetValue(obj, customPropertyConverter[resolvedMappedProperty].DynamicInvoke(value));
                        }
                        else
                        {
                            resolvedMappedProperty.SetValue(obj, value);
                        }
                    }
                    catch
                    {
                        throw new InvalidCastException($"Expected type {resolvedMappedProperty.PropertyType} but found {value.GetType()} for property {columnName}");
                    }
                }
                else if (TryResolveNestedProperty(nestedMappings, columnName, out var nestedMapping, out var nestedProp))
                {
                    var value = dataReader.GetValue(i);
                    if (value is DBNull)
                    {
                        value = null;
                    }

                    if (value != null)
                    {
                        nestedHasValue[nestedMapping] = true;
                    }

                    try
                    {
                        if (customPropertyConverter != null && customPropertyConverter[nestedProp] != null)
                        {
                            nestedProp.SetValue(nestedInstances[nestedMapping], customPropertyConverter[nestedProp].DynamicInvoke(value));
                        }
                        else
                        {
                            nestedProp.SetValue(nestedInstances[nestedMapping], value);
                        }
                    }
                    catch
                    {
                        throw new InvalidCastException($"Expected type {nestedProp.PropertyType} but found {value.GetType()} for property {columnName}");
                    }
                }
            }

            ApplyNestedInstances(obj, nestedMappings, nestedInstances, nestedHasValue);
            return obj;
        }

        /// <summary>
        /// Reads all rows asynchronously and maps each to the specified type.
        /// Column-to-property ordinals are resolved once before iteration.
        /// </summary>
        /// <typeparam name="T">The type of the output objects</typeparam>
        /// <param name="dataReader">The data source</param>
        /// <param name="customPropertyConverter">Use a custom converter for certain values</param>
        /// <returns>A list of objects mapped from every row in the reader</returns>
        public static async Task<IList<T>> MapToListAsync<T>(this DbDataReader dataReader, CustomPropertyConverter customPropertyConverter = null, CancellationToken cancellationToken = default) where T : class
        {
            PropertyInfo[] typeProperties = typeof(T).GetProperties();
            var customNameMappings = typeProperties
                .Where(tp => GetColumnAttribute(tp) != null)
                .ToDictionary(tp => GetColumnAttribute(tp), tp => tp);

            var nestedMappings = GetNestedMappings(typeProperties);

            var ordinalMap = new OrdinalEntry[dataReader.FieldCount];
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                string columnName = dataReader.GetName(i);

                var mappedProperty = typeProperties.Where(tp => tp.Name.Equals(columnName)).FirstOrDefault();
                var mappedPropertyCustomName = customNameMappings.ContainsKey(columnName) ? customNameMappings[columnName] : null;

                if (IsAttributePropertyNamingClash(customNameMappings, columnName, mappedProperty, mappedPropertyCustomName))
                {
                    throw new DbColumnMappingException($"Attribute {columnName} has the same name as a property defined in the model");
                }

                var resolvedMappedProperty = mappedPropertyCustomName ?? mappedProperty;

                if (resolvedMappedProperty != null && !IsNestedProperty(resolvedMappedProperty))
                {
                    ordinalMap[i] = new OrdinalEntry { Property = resolvedMappedProperty, NestedMapping = null };
                }
                else if (TryResolveNestedProperty(nestedMappings, columnName, out var nestedMapping, out var nestedProp))
                {
                    ordinalMap[i] = new OrdinalEntry { Property = nestedProp, NestedMapping = nestedMapping };
                }
            }

            var result = new List<T>();

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                T obj = Activator.CreateInstance<T>();
                var nestedInstances = CreateNestedInstances(nestedMappings);
                var nestedHasValue = InitializeNestedValueTracking(nestedMappings);

                for (int i = 0; i < ordinalMap.Length; i++)
                {
                    var entry = ordinalMap[i];
                    if (entry.Property == null)
                        continue;

                    var value = dataReader.GetValue(i);
                    if (value is DBNull)
                    {
                        value = null;
                    }

                    object target;
                    if (entry.NestedMapping != null)
                    {
                        target = nestedInstances[entry.NestedMapping];
                        if (value != null)
                        {
                            nestedHasValue[entry.NestedMapping] = true;
                        }
                    }
                    else
                    {
                        target = obj;
                    }

                    try
                    {
                        if (customPropertyConverter != null && customPropertyConverter[entry.Property] != null)
                        {
                            entry.Property.SetValue(target, customPropertyConverter[entry.Property].DynamicInvoke(value));
                        }
                        else
                        {
                            entry.Property.SetValue(target, value);
                        }
                    }
                    catch
                    {
                        throw new InvalidCastException($"Expected type {entry.Property.PropertyType} but found {value.GetType()} for property {dataReader.GetName(i)}");
                    }
                }

                ApplyNestedInstances(obj, nestedMappings, nestedInstances, nestedHasValue);
                result.Add(obj);
            }

            return result;
        }

        /// <summary>
        /// Determines whether the attribute custom name clashes with a property
        /// </summary>
        /// <remarks>
        /// A clash happens in a scenario similar to this
        /// `[DbColumn("Name")]`
        /// `public string Address { get; set; }`
        /// `public string Name { get; set; }`
        /// because column `Name` Can map either to the first property or to the second one
        /// </remarks>
        /// <param name="customNameMappings">The dictionary of custom attribute names -> model property mappings</param>
        /// <param name="columnName">The column name from the database</param>
        /// <param name="mappedProperty">The mapped property from the model definition</param>
        /// <param name="mappedPropertyCustomName">The mapped property from the attributes</param>
        /// <returns>True if there is a clash between the attribute and a property</returns>
        private static bool IsAttributePropertyNamingClash(Dictionary<string, PropertyInfo> customNameMappings,
            string columnName, PropertyInfo mappedProperty, PropertyInfo mappedPropertyCustomName) 
            => mappedProperty != null && mappedPropertyCustomName != null && !customNameMappings.Values.Any(tp => tp.Name.Equals(columnName));

        /// <summary>
        /// Gets the custom name attribute from the property
        /// </summary>
        /// <param name="property">The property in the model</param>
        /// <returns>The custom name if it's specified, null otherwise</returns>
        private static string GetColumnAttribute(PropertyInfo property)
        {
            var attributes = property.GetCustomAttributes(true);
            var customName = attributes
                .Select(attr => attr as DbColumnAttribute)
                .Where(attr => attr != null)
                .Select(attr => attr.Name)
                .FirstOrDefault();

            return customName;
        }

        #region Nested Object Mapping

        private class NestedObjectContext
        {
            public PropertyInfo ParentProperty { get; set; }
            public string ColumnPrefix { get; set; }
            public PropertyInfo[] TypeProperties { get; set; }
            /// <summary>Prefix-relative name mappings (Override = false): stripped column name → property</summary>
            public Dictionary<string, PropertyInfo> CustomNameMappings { get; set; }
            /// <summary>Full column name mappings (Override = true): exact column name → property</summary>
            public Dictionary<string, PropertyInfo> OverrideNameMappings { get; set; }
        }

        private struct OrdinalEntry
        {
            public PropertyInfo Property;
            public NestedObjectContext NestedMapping;
        }

        private static List<NestedObjectContext> GetNestedMappings(PropertyInfo[] typeProperties)
        {
            var result = new List<NestedObjectContext>();
            foreach (var prop in typeProperties)
            {
                var attr = prop.GetCustomAttributes(true)
                    .Select(a => a as DbNestedObjectAttribute)
                    .Where(a => a != null)
                    .FirstOrDefault();

                if (attr != null)
                {
                    var nestedType = prop.PropertyType;
                    var nestedProps = nestedType.GetProperties();
                    var nestedCustomNames = new Dictionary<string, PropertyInfo>();
                    var nestedOverrideNames = new Dictionary<string, PropertyInfo>();

                    foreach (var tp in nestedProps)
                    {
                        var colAttr = tp.GetCustomAttributes(true)
                            .OfType<DbColumnAttribute>()
                            .FirstOrDefault();

                        if (colAttr != null)
                        {
                            if (colAttr.Override)
                            {
                                nestedOverrideNames[colAttr.Name] = tp;
                            }
                            else
                            {
                                nestedCustomNames[colAttr.Name] = tp;
                            }
                        }
                    }

                    result.Add(new NestedObjectContext
                    {
                        ParentProperty = prop,
                        ColumnPrefix = attr.ColumnPrefix,
                        TypeProperties = nestedProps,
                        CustomNameMappings = nestedCustomNames,
                        OverrideNameMappings = nestedOverrideNames
                    });
                }
            }
            return result;
        }

        private static Dictionary<NestedObjectContext, object> CreateNestedInstances(List<NestedObjectContext> nestedMappings)
        {
            var instances = new Dictionary<NestedObjectContext, object>();
            foreach (var nm in nestedMappings)
            {
                instances[nm] = Activator.CreateInstance(nm.ParentProperty.PropertyType);
            }
            return instances;
        }

        private static Dictionary<NestedObjectContext, bool> InitializeNestedValueTracking(List<NestedObjectContext> nestedMappings)
        {
            var hasValue = new Dictionary<NestedObjectContext, bool>();
            foreach (var nm in nestedMappings)
            {
                hasValue[nm] = false;
            }
            return hasValue;
        }

        private static bool IsNestedProperty(PropertyInfo property)
        {
            return property.GetCustomAttributes(true)
                .Any(a => a is DbNestedObjectAttribute);
        }

        private static bool TryResolveNestedProperty(List<NestedObjectContext> nestedMappings, string columnName,
            out NestedObjectContext mapping, out PropertyInfo resolvedProperty)
        {
            foreach (var nm in nestedMappings)
            {
                if (nm.OverrideNameMappings.ContainsKey(columnName))
                {
                    mapping = nm;
                    resolvedProperty = nm.OverrideNameMappings[columnName];
                    return true;
                }

                if (columnName.StartsWith(nm.ColumnPrefix, StringComparison.Ordinal))
                {
                    string strippedName = columnName.Substring(nm.ColumnPrefix.Length);
                    var customProp = nm.CustomNameMappings.ContainsKey(strippedName)
                        ? nm.CustomNameMappings[strippedName]
                        : null;
                    var prop = nm.TypeProperties.FirstOrDefault(tp => tp.Name.Equals(strippedName));

                    resolvedProperty = customProp ?? prop;
                    if (resolvedProperty != null)
                    {
                        mapping = nm;
                        return true;
                    }
                }
            }

            mapping = null;
            resolvedProperty = null;
            return false;
        }

        private static void ApplyNestedInstances(object parent, List<NestedObjectContext> nestedMappings,
            Dictionary<NestedObjectContext, object> nestedInstances, Dictionary<NestedObjectContext, bool> nestedHasValue)
        {
            foreach (var nm in nestedMappings)
            {
                nm.ParentProperty.SetValue(parent, nestedHasValue[nm] ? nestedInstances[nm] : null);
            }
        }

        #endregion
    }
}
