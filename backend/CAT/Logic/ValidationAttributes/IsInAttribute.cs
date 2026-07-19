using System.ComponentModel.DataAnnotations;
 
namespace CAT.Logic
{
    public class IsInAttribute : ValidationAttribute
    {
        //массив для хранения допустимых имен
        private readonly string[] _values;
 
        public IsInAttribute(params string[] values)
        {
            _values = values;
            ErrorMessage = $"Входящее значение должно быть одним из: {String.Join(", ", _values)}";
        }
        public override bool IsValid(object? value)
        {
            var str = value as string;
            return (value is null || str != null) && _values.Contains(value);
        }
    }
}