module SuaveBench

open Suave
open Suave.Filters
open Suave.Operators

let helloWorldWebPart =
  choose
    [ GET >=> path "/hello" >=> (Successful.OK "Hello World!")
      GET >=> pathScan "/hello/%s" (sprintf "Hello %s!" >> Successful.OK) ]

startWebServer
  { defaultConfig with
      bindings = [ HttpBinding.createSimple Protocol.HTTP "0.0.0.0" 8080 ] }
  helloWorldWebPart
