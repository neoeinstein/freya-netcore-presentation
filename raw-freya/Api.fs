module Api

open Freya.Core
open Freya.Core.Optics
open Freya.Optics.Http
open Freya.Types.Http

let hwBytes = "Hello, World!"B
let contentLen = ContentLength hwBytes.Length

let root = freya {
  do! Freya.Optic.set Response.statusCode_ (Some 200)
  do! Freya.Optic.set Response.reasonPhrase_ (Some "OK")

  do! Freya.Optic.set Response.Headers.contentLength_ (Some contentLen)
  let! bodyStream = Freya.Optic.get Response.body_
  do! bodyStream.WriteAsync (hwBytes, 0, hwBytes.Length)

  return PipelineChoice.Halt
}
