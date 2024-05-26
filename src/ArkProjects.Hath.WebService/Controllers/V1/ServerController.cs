using ArkProjects.Hath.WebService.Services;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;

namespace ArkProjects.Hath.WebService.Controllers.V1;

[ApiController]
[Authorize(AuthenticationSchemes = BasicAuthenticationDefaults.AuthenticationScheme)]
[Route("/api/v1/[controller]")]
public class ServerController : ControllerBase
{
    private readonly IServer _server;
    private readonly IHathServerLifetimeService _hathServerLifetime;

    public ServerController(IServer server, IHathServerLifetimeService hathServerLifetime)
    {
        _server = server;
        _hathServerLifetime = hathServerLifetime;
    }

    [HttpGet("[action]")]
    public IServerAddressesFeature GetUrls()
    {
        return _server.Features.Get<IServerAddressesFeature>()!;
    }

    [HttpGet("[action]")]
    public async Task<object> NotifySuspend()
    {
        await _hathServerLifetime.NotifySuspendAsync();
        return _hathServerLifetime.ServerStatus;
    }

    [HttpGet("[action]")]
    public async Task<object> NotifyResume()
    {
        await _hathServerLifetime.NotifyResumeAsync();
        return _hathServerLifetime.ServerStatus;
    }

    [HttpGet("[action]")]
    public async Task<object> NotifyStart()
    {
        await _hathServerLifetime.NotifyStartAsync();
        return _hathServerLifetime.ServerStatus;
    }

    [HttpGet("[action]")]
    public async Task<object> NotifyStop()
    {
        await _hathServerLifetime.NotifyStopAsync();
        return _hathServerLifetime.ServerStatus;
    }
}