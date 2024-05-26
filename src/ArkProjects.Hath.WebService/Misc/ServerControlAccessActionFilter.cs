using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ArkProjects.Hath.WebService.Misc;

public class ServerControlAccessActionFilter : IAsyncActionFilter
{
    private readonly ServerControlApiOptions _options;

    public ServerControlAccessActionFilter(IOptions<ServerControlApiOptions> options)
    {
        _options = options.Value;
    }

    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_options.Ports.Count == 0 || _options.Ports.Contains(context.HttpContext.Connection.LocalPort)) 
            return next();
        
        context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        return Task.CompletedTask;
    }
}