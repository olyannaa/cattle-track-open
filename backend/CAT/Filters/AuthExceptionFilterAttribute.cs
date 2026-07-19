using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CAT.Filters
{
    public class AuthExceptionFilterAttribute : Attribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() == typeof(NullReferenceException))
            {
                BadRequestDataException(context);
            }
        }

        private void BadRequestDataException(ExceptionContext context)
        {
            context.Result = new ContentResult
            {
                Content = "Логин или пароль введены неверно.",
                StatusCode = 400
            };
            context.ExceptionHandled = true;
        }
    }
}
