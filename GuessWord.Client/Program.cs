using GuessWord.Client;
using GuessWord.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseUri = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = baseUri
});

builder.Services.AddScoped(_ => new ApiSettings
{
    BaseUrl = baseUri.ToString()
});

builder.Services.AddScoped<ApiRequestService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddSingleton<UserStateService>();

await builder.Build().RunAsync();
