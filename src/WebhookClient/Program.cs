﻿using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using WebhookClient;
using WebhookClient.Components;
using WebhookClient.Services;
using eShop.ServiceDefaults;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient<WebhooksClient>(o => o.BaseAddress = new("http://webhooks-api")).AddAuthToken();
builder.Services.AddOptions<WebhookClientOptions>().BindConfiguration(nameof(WebhookClientOptions));
builder.AddAuthenticationServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/logout", async (HttpContext httpContext, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(httpContext);
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
});

bool.TryParse(builder.Configuration["ValidateToken"], out var validateToken);
var tokenToValidate = builder.Configuration["Token"];

app.MapMethods("/check", [HttpMethods.Options], Results<Ok, BadRequest<string>> ([FromHeader(Name = "X-eshop-whtoken")] string value, HttpResponse response) =>
{
    if (!validateToken || value == tokenToValidate)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            response.Headers.Append("X-eshop-whtoken", value);
        }

        return TypedResults.Ok();
    }

    return TypedResults.BadRequest("Invalid token");
});

app.Run();
