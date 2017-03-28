namespace Freya.Core

open Hopac

module Job =
  let toFreya xJ =
    fun e ->
      xJ |> Job.map (fun x -> (x, e))

module Async =
  let toFreya xA =
    fun e ->
      async.Bind(xA, fun x -> async.Return (x, e))

[<AutoOpen>]
module Extensions =
  type FreyaBuilder with
    member __.Bind (aF: Freya<'a>, a2bJ: 'a -> Job<'b>) : Freya<'b> =
      aF |> Freya.bind (a2bJ >> Job.toFreya)
    member __.Bind (aJ: Job<'a>, a2bF: 'a -> Freya<'b>) : Freya<'b> =
      Job.toFreya aJ |> Freya.bind a2bF
    member __.Bind (aJ: Job<'a>, a2bJ: 'a -> Job<'b>) : Freya<'b> =
      Job.toFreya aJ |> Freya.bind (a2bJ >> Job.toFreya)
    member __.Bind (aT: System.Threading.Tasks.Task<'a>, a2bF: 'a -> Freya<'b>) : Freya<'b> =
      Job.toFreya (Job.awaitTask aT) |> Freya.bind a2bF
    member __.Bind (uT: System.Threading.Tasks.Task, u2bF: unit -> Freya<'b>) : Freya<'b> =
      Job.toFreya (Job.awaitUnitTask uT) |> Freya.bind u2bF
    member __.Bind (aF: Freya<'a>, a2bT: 'a -> System.Threading.Tasks.Task<'b>) : Freya<'b> =
      aF |> Freya.bind (Job.liftTask a2bT >> Job.toFreya)
    member __.Bind (aF: Freya<'a>, a2uT: 'a -> System.Threading.Tasks.Task) : Freya<unit> =
      aF |> Freya.bind (Job.liftUnitTask a2uT >> Job.toFreya)
    member __.ReturnFrom (aJ: Job<'a>) : Freya<'a> =
      Job.toFreya aJ
    member __.Delay (u2aJ: unit -> Job<'a>) : Freya<'a> =
      Job.toFreya (Job.delay u2aJ)
    member __.Delay (u2aT: unit -> System.Threading.Tasks.Task<'a>) : Freya<'a> =
      Job.toFreya (Job.fromTask u2aT)
    member __.Delay (u2uT: unit -> System.Threading.Tasks.Task) : Freya<unit> =
      Job.toFreya (Job.fromUnitTask u2uT)
    member __.Combine (xF: Freya<_>, aJ: Job<'a>) : Freya<'a> =
      xF |> Freya.combine (Job.toFreya aJ)
    member __.Combine (xJ: Job<_>, aF: Freya<'a>) : Freya<'a> =
      Job.toFreya xJ |> Freya.combine aF
