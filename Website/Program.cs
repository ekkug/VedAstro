using AspNetMonsters.Blazor.Geolocation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace Website
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            //the client made here is used via AppData everywhere
            builder.Services.AddScoped(sp => new HttpClient
            {
                //wait forever
                Timeout = new TimeSpan(0, 0, 0, 0, Timeout.Infinite),
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });

            //setup for global variables (to remember state)
            //TODO replace with AppData
            //var globalVariable = new GlobalVariableManager();
            //builder.Services.AddSingleton(globalVariable);

            //setup for getting location from browser
            //used by geolocation input
            builder.Services.AddSingleton<LocationService>();

            //service used to place data in browser's localstorage
            //builder.Services.AddSingleton<ILocalStorage, LocalStorageManager>();

            //TODO CORS Test
            //builder.Services.AddCors(options =>
            //{
            //    options.AddDefaultPolicy(
            //        policy =>
            //        {
            //            policy.AllowAnyOrigin();  //set the allowed origin  
            //        });
            //});

            //ERROR HANDLING

            //hide generic error messages only in RELEASE
            //when debugging enable default errors
            //apps published will not show any error while
            //same run locally will show messages
#if (DEBUG == false)
            //stop default error handling (prints in console log & shows default error message at page bottom)
            builder.Logging.ClearProviders();
#endif

            //add custom error handling (synchronous exceptions only)
            var unhandledExceptionSender = new UnhandledExceptionSender();
            var unhandledExceptionProvider = new UnhandledExceptionProvider(unhandledExceptionSender);
            builder.Logging.AddProvider(unhandledExceptionProvider);
            builder.Services.AddSingleton<IUnhandledExceptionSender>(unhandledExceptionSender);


            //The last thing that the Main method does is to take all of the configuration
            //specified with the WebAssemblyHostBuilder and call its Build method. This will
            //create an instance of a WebAssemblyHost which is the heart of the Blazor app.
            //It contains all of the application configuration and services needed to run your app.
            var webAssemblyHost = builder.Build();

            //CUSTOM CODE HERE
            Console.WriteLine("VedAstro");
            Console.WriteLine(RuntimeFeature.IsDynamicCodeSupported ? "INTERPRETED MODE" : "AOT MODE");

            //make the JS runtime globally accessible
            var jsRuntime = webAssemblyHost.Services.GetRequiredService<IJSRuntime>();
            AppData.JsRuntime = jsRuntime;

            //run like the wind, Bullseye!
            await webAssemblyHost.RunAsync();
        }
    }
}




//ARCHIVED CODE

//foreach (var builderService in builder.Services)
//{
//    Console.WriteLine(builderService.ToString());
//}
