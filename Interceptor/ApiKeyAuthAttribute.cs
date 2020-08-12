using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAspNetIdentity.Interceptor
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
    {
        private const string _clientId = "ClientId";
        private const string _clientSecret = "super client secret";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if(!context.HttpContext.Request.Headers.TryGetValue(_clientId, out var ClientSecret))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            if (!_clientSecret.Equals(ClientSecret))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            await next();
        }
    }
}
