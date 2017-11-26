using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAnnotationsValidator
{
    public interface IDataAnnotationsValidator
    {
        bool TryValidateObject(object obj, out IReadOnlyCollection<ValidationResult> validationResults, IDictionary<object, object> validationContextItems = null);

        bool TryValidateObjectRecursive<T>(T obj, out IReadOnlyCollection<ValidationResult> validationResults, IDictionary<object, object> validationContextItems = null)
            where T : class, new();
    }
}
