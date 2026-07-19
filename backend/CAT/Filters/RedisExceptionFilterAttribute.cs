using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace CAT.Filters
{
    public class RedisExceptionFilterAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() == typeof(RedisConnectionException))
            {
                NoConnectionException(context);
            }

            if (context.Exception.GetType() == typeof(ArgumentNullException))
            {
                ConfigurationException(context);
            }
        }

        private void NoConnectionException(ExceptionContext context)
        {
            context.Result = new ContentResult
            {
                Content = "Соединение с сервером кэширования не установлено.",
                StatusCode = 503
            };
            context.ExceptionHandled = true;
        }

        private void ConfigurationException(ExceptionContext context)
        {
            context.Result = new ContentResult
            {
                Content = "Настройки подключения к серверу кэширования не указаны в конфигурационном файле",
                StatusCode = 500
            };
            context.ExceptionHandled = true;
        }
    }
}