using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace DataAnnotationsValidator
{
    public class DataAnnotationsValidator : IDataAnnotationsValidator
    {
        private readonly TypePropertiesCache _typePropertiesCache = new TypePropertiesCache();

        public bool TryValidateObject(object obj, out IReadOnlyCollection<ValidationResult> validationResults, IDictionary<object, object> validationContextItems = null)
        {
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(obj, new ValidationContext(obj, null, validationContextItems), results, true);

            validationResults = new List<ValidationResult>(results);
            return isValid;
        }

        public bool TryValidateObjectRecursive<T>(T obj, out IReadOnlyCollection<ValidationResult> validationResults, IDictionary<object, object> validationContextItems = null)
            where T : class, new()
        {
            var results = new List<ValidationResult>();
            var isValid = TryValidateObjectRecursive(obj, results, new HashSet<object>(), new Stack<string>(), validationContextItems);

            validationResults = new List<ValidationResult>(results);
            return isValid;
        }

        private bool TryValidateObjectRecursive<T>(T toValidate, ICollection<ValidationResult> validationResults, ISet<object> validatedObjects,
            Stack<string> parentPrefixes, IDictionary<object, object> validationContextItems = null)
            where T : class, new()
        {
            //short-circuit to avoid infinit loops on cyclical object graphs
            if (validatedObjects.Contains(toValidate))
            {
                return true;
            }
            validatedObjects.Add(toValidate);

            var toValidateType = toValidate.GetType();

            // check stop condition
            if (toValidate is string || toValidateType.IsPrimitive)
            {
                return true;
            }

            validationContextItems = validationContextItems ?? new Dictionary<object, object>();

            // if toValidate is assignable to IEnumerable
            if (toValidate is IEnumerable enumeration)
            {
                return TryValidateEnumeration(enumeration, validationResults, validatedObjects, parentPrefixes, validationContextItems);
            }

            // if toValidate is a not enumerable instance
            var propertyInfos = _typePropertiesCache.GetOrAddPropertiesInfo(toValidate);

            var isValid = TryValidateNestedObjects(propertyInfos, toValidate, validationResults, validatedObjects, parentPrefixes, validationContextItems);

            // validate object and enhance member name info
            var memberPrefix = BuildMemberPrefix(parentPrefixes);
            AddPrefixToContextItems(validationContextItems, memberPrefix);

            isValid &= TryValidateObject(toValidate, out var results, validationContextItems);

            UpdateValidationResults(validationResults, results, memberPrefix);

            return isValid;
        }

        private bool TryValidateEnumeration(IEnumerable enumeration, ICollection<ValidationResult> validationResults,
            ISet<object> validatedObjects, Stack<string> parentPrefixes, IDictionary<object, object> validationContextItems)
        {
            var isValid = true;

            var index = 0;
            foreach (var currentElement in enumeration)
            {
                var prefix = $"[{index}]";
                var nextToValidate = currentElement;

                var currentElementType = currentElement.GetType();
                if (currentElementType.IsGenericType &&
                        currentElementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var key = currentElementType.GetProperty("Key").GetValue(currentElement, null);
                    prefix = $@"[Index={index}, Key=""{key}""]";

                    nextToValidate = currentElementType.GetProperty("Value").GetValue(currentElement, null);
                }

                try
                {
                    parentPrefixes.Push(prefix);

                    isValid &= TryValidateObjectRecursive(nextToValidate, validationResults,
                        validatedObjects, parentPrefixes, validationContextItems);
                }
                finally
                {
                    parentPrefixes.Pop();
                }

                ++index;
            }

            return isValid;
        }

        private bool TryValidateNestedObjects(IEnumerable<PropertyInfo> propertyInfos, object toValidate, ICollection<ValidationResult> validationResults,
            ISet<object> validatedObjects, Stack<string> parentPrefixes, IDictionary<object, object> validationContextItems)
        {
            var isValid = true;
            foreach (var propertyInfo in propertyInfos)
            {
                var propertyValue = propertyInfo.GetValue(toValidate, null);
                if (propertyValue == null)
                {
                    continue;
                }

                try
                {
                    parentPrefixes.Push(propertyInfo.Name);

                    isValid &= TryValidateObjectRecursive(propertyValue, validationResults,
                        validatedObjects, parentPrefixes, validationContextItems);
                }
                finally
                {
                    parentPrefixes.Pop();
                }
            }
            return isValid;
        }

        private static string BuildMemberPrefix(Stack<string> parentPrefixes)
        {
            return string.Join(MemberPrefix.Delimiter, parentPrefixes.Reverse()).Replace(".[", "[");
        }

        private static void AddPrefixToContextItems(IDictionary<object, object> validationContextItems, string memberPrefix)
        {
            if (validationContextItems.ContainsKey(MemberPrefix.Key))
            {
                validationContextItems[MemberPrefix.Key] = memberPrefix;
            }
            else
            {
                validationContextItems.Add(MemberPrefix.Key, memberPrefix);
            }
        }

        private static void UpdateValidationResults(ICollection<ValidationResult> validationResults, IEnumerable<ValidationResult> results, string memberPrefix)
        {
            foreach (var result in results)
            {
                var memberNames = result.MemberNames
                    .Select(member => member.StartsWith(memberPrefix)
                        ? member
                        : $"{memberPrefix}{MemberPrefix.Delimiter}{member}").ToList();

                if (!memberNames.Any())
                {
                    memberNames = new List<string> { memberPrefix };
                }

                validationResults.Add(new ValidationResult(result.ErrorMessage, memberNames));
            }
        }
    }
}
