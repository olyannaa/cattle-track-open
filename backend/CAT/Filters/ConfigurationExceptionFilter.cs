using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Filters
{
    public class ConfigurationExceptionFilterAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() == typeof(NotImplementedConfigurationException))
            {
                BadRequestDataException(context);
            }
        }

        private void BadRequestDataException(ExceptionContext context)
        {
            context.Result = new ContentResult
            {
                Content = "Ошибка конфигурационного файла.",
                StatusCode = 400
            };
            context.ExceptionHandled = true;
        }
    }

    public class NotImplementedConfigurationException : Exception
    { 

    }
}
