using Application.Usecases.Auth.Login;
using Application.Usecases.Auth.Logout;
using Application.Usecases.Auth.Register;
using Application.Usecases.Auth.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Register a new Spectator account. Account is activated immediately.
    /// </summary>
    [HttpPost("register/spectator")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> RegisterSpectator(
        [FromBody] RegisterSpectatorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RegisterCommand(
            request.Email, request.Password, request.FullName, request.PhoneNumber,
            RoleCode: "SPECTATOR", LicenseNumber: null, Weight: null, Bio: null),
            cancellationToken);
        return CreatedAtAction(nameof(RegisterSpectator), result);
    }

    /// <summary>
    /// Register a new Horse Owner account. Account requires admin approval before login is permitted.
    /// </summary>
    [HttpPost("register/horse-owner")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> RegisterHorseOwner(
        [FromBody] RegisterHorseOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RegisterCommand(
            request.Email, request.Password, request.FullName, request.PhoneNumber,
            RoleCode: "HORSE_OWNER", LicenseNumber: null, Weight: null, Bio: null),
            cancellationToken);
        return CreatedAtAction(nameof(RegisterHorseOwner), result);
    }

    /// <summary>
    /// Register a new Jockey account. Requires license number and weight.
    /// Account requires admin approval before login is permitted.
    /// </summary>
    [HttpPost("register/jockey")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> RegisterJockey(
        [FromBody] RegisterJockeyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RegisterCommand(
            request.Email, request.Password, request.FullName, request.PhoneNumber,
            RoleCode: "JOCKEY", LicenseNumber: request.LicenseNumber,
            Weight: request.Weight, Bio: request.Bio),
            cancellationToken);
        return CreatedAtAction(nameof(RegisterJockey), result);
    }

    /// <summary>
    /// Authenticate with email and password. Returns a JWT access token and a refresh token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Revoke the provided refresh token, effectively logging the user out.
    /// Requires a valid JWT access token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Exchange a valid refresh token for a new access token and refresh token (token rotation).
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return Ok(result);
    }
}

public sealed record LogoutRequest(string RefreshToken);
public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record RegisterSpectatorRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber);

public sealed record RegisterHorseOwnerRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber);

public sealed record RegisterJockeyRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    string LicenseNumber,
    decimal Weight,
    string? Bio);
