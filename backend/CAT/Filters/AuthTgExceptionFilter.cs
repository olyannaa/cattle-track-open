using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Filters
{
    public class AuthTgExceptionFilterAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() == typeof(NullReferenceException))
            {
                BadRequestDataException(context);
            }

            if (context.Exception.GetType() == typeof(AccessViolationException))
            {
                BadRequestAccessViolationException(context);
            }
        }

        private void BadRequestDataException(ExceptionContext context)
        {
            context.Result = new ContentResult
            {
                Content = "Пользователь не зарегистрирован.",
                StatusCode = 400
            };
            context.ExceptionHandled = true;
        }

        private void BadRequestAccessViolationException(ExceptionContext context)
        {
            context.Result = new ContentResult
            {
                Content = "Попытка входа через телеграм предотвращена.",
                StatusCode = 400
            };
            context.ExceptionHandled = true;
        }
    }
}
