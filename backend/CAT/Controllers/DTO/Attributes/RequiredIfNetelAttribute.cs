using System.ComponentModel.DataAnnotations;

namespace CAT.Controllers.DTO.Attributes
{
    public class RequiredIfNetelAttribute : ValidationAttribute
    {
        private readonly string _typePropertyName;

        public RequiredIfNetelAttribute(string typePropertyName)
        {
            _typePropertyName = typePropertyName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var typeProperty = validationContext.ObjectType.GetProperty(_typePropertyName);
            if (typeProperty == null)
            {
                return new ValidationResult($"Не найдено свойство {_typePropertyName}");
            }

            var typeValue = typeProperty.GetValue(validationContext.ObjectInstance) as string;

            if (string.Equals(typeValue, "Нетель", StringComparison.OrdinalIgnoreCase) && value == null)
            {
                return new ValidationResult(ErrorMessage ?? "Это поле обязательно для типа 'Нетель'");
            }

            return ValidationResult.Success;
        }
    }
}
