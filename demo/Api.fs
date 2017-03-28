module Api

open Freya.Core
open Freya.Core.Optics
open Freya.Optics.Http
open Freya.Types.Http
open Freya.Types.Http.Cors
open Freya.Machines
open Freya.Machines.Http
open Freya.Machines.Http.Cors
open Freya.Machines.Http.Trace
open Freya.Routers.Uri.Template

let routeName_ = Route.atom_ "name"

let sayHello = freya {
  let! nameO = Freya.Optic.get routeName_
  let helloStr =
    match nameO with
    | Some name -> sprintf "Hello, %s!" name
    | None -> "Hello, World!"
  return Represent.text helloStr
}

let helloMachine = freyaMachine {
  methods [GET; HEAD; OPTIONS]

  handleOk sayHello
}

let trace machine = freyaMachine {
  availableMediaTypes [MediaType.Html; MediaType.Text]

  handleOk (traceMachine machine)
}

let router = freyaRouter {
  resource "/hello{/name}" helloMachine
  resource "/trace/hello" (trace helloMachine)
}

let root = UriTemplateRouter.Freya router
