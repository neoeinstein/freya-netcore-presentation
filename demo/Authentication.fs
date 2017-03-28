module Authentication

open Freya.Core
open Freya.Core.Optics
open Freya.Optics.Http
open Freya.Types.Http
open Freya.Machines
open Freya.Machines.Http
open Freya.Routers.Uri.Template

type Username = Username of string
type Password = Password of string

let schemeAndData =
  let splitSchemeAndData = (do ()); fun (s:string) ->
    match s.Split([|' '|], 2) with
    | [|s;d|] -> Some (s.ToLowerInvariant(), d)
    | _ -> None
  Freya.memo <| freya {
    let! ahO = Freya.Optic.get Request.Headers.authorization_
    return Option.bind splitSchemeAndData ahO
  }

let scheme = schemeAndData |> Freya.map (Option.map (fun (s,_) -> s.ToLower()))

let data = schemeAndData |> Freya.map (Option.map snd)

module BasicAuth =
  let [<Literal>] Scheme = "basic"

  let userPassword = Freya.memo <| freya {
    let! dO = data
    return
      dO
      |> Option.bind (fun d ->
        try
          System.Convert.FromBase64String d
          |> System.Text.Encoding.UTF8.GetString
          |> Some
        with e -> None)
      |> Option.bind (fun up ->
        match up.Split([|':'|], 2) with
        | [|u;p|] -> Some (Username u,Password p)
        | _ -> None)
  }

  let user = userPassword |> Freya.map (Option.map fst)

  let password = userPassword |> Freya.map (Option.map snd)

  let isAllowed checker = freya {
    let! upO = userPassword
    return!
      match upO with
      | Some (u,p) -> checker u p
      | _ -> Freya.init false
  }

  let isAuthorized checker = freya {
    let! schemeO = scheme
    if Option.contains Scheme schemeO then
      let! upO = userPassword
      match upO with
      | Some (u,p) ->
          return! checker u p
      | _ ->
          return false
    else
      return false
  }

  let handleUnauthorized realm =
    let wwwAuthHeader = Some (sprintf "Basic realm='%s'" realm)
    freya {
      do! Operations.unauthorized
      do! Freya.Optic.set Response.Headers.wwwAuthenticate_ wwwAuthHeader
      return Represent.text "Unauthorized"
    }

let user = Freya.memo <| freya {
  let! sO = scheme
  return!
    match sO with
    | Some BasicAuth.Scheme -> BasicAuth.user
    | _ -> Freya.init None
}

let basicAuth realm = freyaMachine {
  handleUnauthorized (BasicAuth.handleUnauthorized realm)
}
