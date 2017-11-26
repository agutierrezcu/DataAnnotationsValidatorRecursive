using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAnnotationsValidator.Tests.Family
{
    public class GrandChild : IValidatableObject
    {
        [Required]
        [Range(0, 10, ErrorMessage = "GrandChild PropertyA not within range.")]
        public int? PropertyA { get; set; }

        [Required]
        [Range(0, 10, ErrorMessage = "GrandChild PropertyB not within range.")]
        public int? PropertyB { get; set; }

        public int? PropertyC { get; set; }

        [SaveValidationContext]
        public bool HasNoRealValidation { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var memberPrefix = validationContext.GetMemberPrefix();

            if (PropertyA.HasValue && PropertyB.HasValue && PropertyA + PropertyB > 10)
                yield return new ValidationResult("GrandChild PropertyA and PropertyB cannot add up to more than 10.");

            if (PropertyA.HasValue && PropertyC.HasValue && PropertyA + PropertyC > 20)
                yield return new ValidationResult("GrandChild PropertyA and PropertyC cannot add up to more than 20.",
                    new List<string>
                    {
                        $"{memberPrefix}{nameof(PropertyA)}",
                        $"{memberPrefix}{nameof(PropertyC)}"
                    });
        }
    }
}
