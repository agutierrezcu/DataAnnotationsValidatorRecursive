using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataAnnotationsValidator
{
    internal sealed class TypePropertiesCache
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> TypeProperties = new ConcurrentDictionary<Type, List<PropertyInfo>>();

        public IEnumerable<PropertyInfo> GetOrAddPropertiesInfo(object obj)
        {
            var objectType = obj.GetType();
            if (!TypeProperties.TryGetValue(objectType, out var propertiesInfos))
            {
                propertiesInfos = objectType.GetProperties()
                    .Where(propertyInfo => propertyInfo.CanRead
                        && propertyInfo.GetCustomAttribute<SkipRecursiveValidationAttribute>(true) == null
                        && propertyInfo.GetIndexParameters().Length == 0
                        && propertyInfo.PropertyType != typeof(string)
                        && !propertyInfo.PropertyType.IsValueType)
                    .ToList();

                TypeProperties.TryAdd(objectType, propertiesInfos);
            }
            return propertiesInfos;
        }
    }
}