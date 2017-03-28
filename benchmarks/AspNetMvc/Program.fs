namespace MvcBench

open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Mvc

open MvcBench.Controllers

type Startup () =
  member x.ConfigureServices (svc: IServiceCollection) =
    svc.AddMvc() |> ignore
    svc.AddSingleton(HelloHandler.impl) |> ignore

  member x.Configure (app: IApplicationBuilder) =
    app.UseMvc() |> ignore

module Program =
  [<EntryPoint>]
  let main argv =
    let webhost =
      WebHostBuilder()
        .UseUrls([|"http://*:8080"|])
        .UseKestrel()
        .UseStartup<Startup>()
        .Build()
    webhost.Run()
    0 // return an integer exit code
