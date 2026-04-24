using System.Text.Json;
using cli_intelligence.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace cli_intelligence.Server;

/// <summary>
/// Request/response DTO for POST /echo.
/// </summary>
sealed record EchoResponse(JsonElement Echo, DateTime ReceivedAt);

static class ServerHost
{
    /// <summary>
    /// Builds and runs the Kestrel HTTP server. Blocks until <paramref name="ct"/> is cancelled
    /// (Ctrl+C) or the host shuts down. Designed to be extended — <paramref name="session"/> is
    /// available for AI endpoints and tool access later.
    /// </summary>
    public static async Task RunAsync(AppSession session, CancellationToken ct = default)
    {
        var cfg = session.Config.Server;

        // ── Builder ───────────────────────────────────────────────────────────
        // CreateSlimBuilder: lightest footprint — no MVC, no auth, no CORS boilerplate.
        // Still includes routing, Kestrel, Minimal API, and the diagnostics middleware.
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production
        });

        // Silence the framework's own Microsoft.* loggers — we handle request logging
        // ourselves using Serilog so the file log stays consistent with the rest of the app.
        builder.Logging.ClearProviders();

        // ── App ───────────────────────────────────────────────────────────────
        var app = builder.Build();

        // Bind to our configured URL only; ignore env vars / launchSettings.json.
        app.Urls.Clear();
        app.Urls.Add(cfg.Url);

        // Centralised exception handler — must be registered before other middleware.
        // Returns a consistent JSON error envelope for any unhandled exception in a route.
        app.UseExceptionHandler(errPipeline => errPipeline.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";

            var feature = ctx.Features.Get<IExceptionHandlerFeature>();
            Log.Error(feature?.Error, "[HTTP] Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

            await ctx.Response.WriteAsJsonAsync(new
            {
                error = feature?.Error.Message ?? "An unexpected error occurred.",
                status = 500
            }, cancellationToken: ctx.RequestAborted);
        }));

        // Lightweight request/response logging via static Serilog — no DI plumbing needed.
        app.Use(async (ctx, next) =>
        {
            Log.Information("[HTTP] → {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await next(ctx);
            Log.Information("[HTTP] ← {Method} {Path} {Status}",
                ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode);
        });

        // ── Endpoints ─────────────────────────────────────────────────────────

        // GET / — service identity card
        app.MapGet("/", () => Results.Ok(new
        {
            service = cfg.ServiceName,
            version = cfg.Version,
            utc = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName
        }));

        // GET /health — liveness probe; 200 = process is up
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        // GET /ping — round-trip latency check
        app.MapGet("/ping", () => Results.Ok(new { pong = true, utc = DateTime.UtcNow }));

        // POST /echo — reflects the JSON body back; validates it is non-null JSON
        app.MapPost("/echo", async (HttpContext ctx, CancellationToken token) =>
        {
            JsonElement body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<JsonElement>(
                    ctx.Request.Body, cancellationToken: token);
            }
            catch (JsonException)
            {
                return Results.BadRequest(new { error = "Request body must be valid JSON." });
            }

            if (body.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return Results.BadRequest(new { error = "Body must not be null." });
            }

            return Results.Ok(new EchoResponse(body, DateTime.UtcNow));
        });

        // GET /headers — returns a safe subset of the request headers
        app.MapGet("/headers", (HttpRequest req) =>
        {
            var inspect = new[]
            {
                "User-Agent", "Accept", "Accept-Language", "Accept-Encoding",
                "Host", "Content-Type", "X-Forwarded-For"
                // Note: Authorization is intentionally omitted to avoid leaking credentials in logs
            };

            var headers = inspect
                .Where(k => req.Headers.ContainsKey(k))
                .ToDictionary(k => k, k => req.Headers[k].ToString());

            return Results.Ok(headers);
        });

        // GET /ip — caller's remote IP address
        app.MapGet("/ip", (HttpContext ctx) =>
            Results.Ok(new { ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown" }));

        // GET /error-demo — intentionally throws to exercise the global error handler
        app.MapGet("/error-demo", () =>
        {
            throw new InvalidOperationException(
                "This is a controlled error-demo exception — the global handler caught it.");
        });

        // ── Start ─────────────────────────────────────────────────────────────
        Log.Information("HTTP server starting on {Url}", cfg.Url);
        Console.WriteLine();
        Console.WriteLine($"  HTTP server running → {cfg.Url}");
        Console.WriteLine("  Press Ctrl+C to stop.");
        Console.WriteLine();

        await app.RunAsync(ct);

        Log.Information("HTTP server stopped");
    }
}
