using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ToDoList.BlazorUI;
using ToDoList.BlazorUI.Auth;
using ToDoList.BlazorUI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base address (from wwwroot/appsettings.json).  
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://2.28.62.244:5000/";

// Local storage for tokens.  
builder.Services.AddBlazoredLocalStorage();

// Authentication / authorization.  
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ITokenStore, TokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
   sp.GetRequiredService<JwtAuthenticationStateProvider>());

// Message handler that attaches the bearer token and refreshes it on 401.  
builder.Services.AddScoped<AuthHeaderHandler>();

// Typed API client with the auth handler wired in.  
builder.Services
   .AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
   .AddHttpMessageHandler<AuthHeaderHandler>();

// UI helpers and feature services.  
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AuthClientService>();
builder.Services.AddScoped<ToDoClientService>();

await builder.Build().RunAsync();
