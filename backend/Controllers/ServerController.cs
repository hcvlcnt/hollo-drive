using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("api/server")]
public class ServerController : ControllerBase
{
    private readonly HolloServerOptions _options;

    public ServerController(IOptions<HolloServerOptions> options)
    {
        _options = options.Value;
    }

    [AllowAnonymous]
    [HttpGet("pairing")]
    public ActionResult<ServerPairingResponse> GetPairing()
    {
        if (!Uri.TryCreate(_options.PublicUrl, UriKind.Absolute, out var publicUrl) ||
            publicUrl.Scheme is not ("http" or "https"))
        {
            return Problem("HolloServer:PublicUrl is not configured correctly.", statusCode: 503);
        }

        var normalizedUrl = publicUrl.ToString().TrimEnd('/') + "/";
        return Ok(new ServerPairingResponse(
            Version: 1,
            ServerId: _options.Id.Trim(),
            Name: _options.Name.Trim(),
            ApiUrl: normalizedUrl));
    }
}

public record ServerPairingResponse(int Version, string ServerId, string Name, string ApiUrl);
