using System.ComponentModel.DataAnnotations;
 
namespace CAT.Logic
{
    public class GreaterThanAttribute : ValidationAttribute
    {
        //массив для хранения допустимых имен
        int _value;
 
        public GreaterThanAttribute(int value)
        {
            _value = value;
        }
        public override bool IsValid(object? value)
        {
            var num = value as int?;
            return num != null && num > _value;
        }
    }
}