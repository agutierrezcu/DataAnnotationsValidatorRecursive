using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DataAnnotationsValidator
{
    public static class ValidationContextExtensions
    {
        public static string GetMemberPrefix(this ValidationContext validationContext)
        {
            if (validationContext?.Items == null || !validationContext.Items.Any())
            {
                return string.Empty;
            }
            validationContext.Items.TryGetValue(MemberPrefix.Key, out var memberPrefix);
            return string.IsNullOrWhiteSpace(memberPrefix as string) ? string.Empty : $"{memberPrefix}{MemberPrefix.Delimiter}";
        }
    }
}