module FreyaBench.Api

open Freya.Core
open Freya.Core.Optics
open Freya.Types.Http
open Freya.Machines.Http
open Freya.Routers.Uri.Template

let name_ = Route.atom_ "name"

let sayHello = freya {
  let! uO = Freya.Optic.get name_
  return
    match uO with
    | Some u ->
      Represent.text (sprintf "Hello %s!" u)
    | None ->
      Represent.text "Hello World!"
}

let helloMachine = freyaMachine {
  handleOk sayHello
}

let router = freyaRouter {
  resource "/hello{/name}" helloMachine
}

let root = UriTemplateRouter.Freya router
