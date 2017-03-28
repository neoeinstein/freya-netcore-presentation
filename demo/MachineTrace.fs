module Freya.Machines.Http.Trace

open Aether
open Freya.Core
open Freya.Machines
open Freya.Machines.Http
open Freya.Machines.Http.Machine.Configuration
open Freya.Machines.Http.Machine.Models
open Freya.Types.Http
open Hephaestus

module Represent =
  let html (data : string) =
    { Data = System.Text.Encoding.UTF8.GetBytes data
      Description =
        { Encodings = None
          Charset = Some Charset.Utf8
          MediaType = Some MediaType.Html
          Languages = None }}

module LogProcessing =
  module Key =
    let toString (Key k) = String.concat "." k

  module Log =
    let extractPasses (Log ps) = ps

  module GoogleCharts =
    let formatTerminal parentKeyString childKeyString =
      sprintf "['%s','%s']" childKeyString parentKeyString

    let rec formatDecision parentKeyString nodeKeyString left right =
      sprintf "%s,%s,%s" (formatTerminal parentKeyString nodeKeyString) (formatMachineWithParent nodeKeyString left) (formatMachineWithParent nodeKeyString right)
    and formatMachineWithParent parentKeyString machine =
      match machine with
      | Terminal (k,_) ->
        let nodeKeyString = Key.toString k
        formatTerminal parentKeyString nodeKeyString
      | Decision (k,_,(l,r)) ->
        let nodeKeyString = Key.toString k
        formatDecision parentKeyString nodeKeyString l r

    let formatMachine machine =
      formatMachineWithParent "" machine

    let includeDataRows ident graph =
      sprintf """
        var %s_data = new google.visualization.DataTable();
        %s_data.addColumn('string', 'Name');
        %s_data.addColumn('string', 'Manager');

        %s_data.addRows([%s]);

        var %s_chart = new google.visualization.OrgChart(document.getElementById('%s_chart_div'));
        %s_chart.draw(%s_data, {allowHtml:true});
        """ ident ident ident ident graph ident ident ident ident

    let includeChartDiv ident =
      sprintf @"<div id=""%s_chart_div""></div>" ident

  module Html =
    let formatKeyByDecision key = function
      | Some Left -> sprintf @"<div style=""color: red;font-weight: bold"">%s</div>" key
      | Some Right -> sprintf @"<div style=""color: green;font-weight: bold"">%s</div>" key
      | None -> sprintf @"<div style=""color: blue;font-weight: bold"">%s</div>" key

    let describeNodeByMetadata = function
    | "function", _ -> "<div><em>&lt;&lt;function&gt;&gt;</em></div>"
    | "terminal", _ -> @"<div style=""color:gray;font-variant:small-caps;font-weight:bold"">terminal</div>"
    | "literal", meta -> sprintf @"<div style=""color:blue""><em>always %s</em></div>" (Map.find "result" meta)
    | "decision", _ -> "<div><em>&laquo;decision&raquo;</em></div>"
    | t, _ -> sprintf @"<div style=""color:red"">%s</div>" t

    let printNode parentKey ((p,Descriptor (t,meta),s):Hekate.MContext<Key,Descriptor,DecisionResult option>) =
      let key = Key.toString parentKey
      let ds v = (formatKeyByDecision key v) + (describeNodeByMetadata (t,meta))
      let edges =
        if not <| Map.isEmpty p then
          Map.toList p |> List.map (fun (k,v) -> sprintf "[{v:'%s',f:'%s'},'%s']" key (ds v) (Key.toString k))
        else
          []
      match edges with
      | [] -> None
      | _ -> Some <| String.concat "," edges

    let formatOperation (Operation (o,t)) =
      let v =
        match Map.tryFind "value" t with
        | Some v -> " <em>= " + v + "</em>"
        | None -> ""
      let k = Map.find "key" t
      if k = "" then
        ""
      else
        sprintf "<li><b>%s:</b> %s%s</li>" o k v

    let formatPass ident desc opers =
      sprintf """<h3>Pass "%s"</h3><h4>Operations</h4><ul>%s</ul><h4>Result</h4>%s""" desc opers (GoogleCharts.includeChartDiv ident)

    let preprocessPasses passes =
      let preprocessPass i (Pass (desc,graph,opers)) =
        let graphRep =
          graph
          |> Map.toList
          |> List.choose (fun (k,g) -> printNode k g)
          |> String.concat ","
        let opersRep =
          opers
          |> List.map formatOperation
          |> String.concat "\n"
        (sprintf "pass_%i" i, desc, graphRep, opersRep)

      passes
      |> List.rev
      |> List.mapi preprocessPass

    let format machine log =
      let passes = Log.extractPasses log
      let preprocessedPasses = preprocessPasses passes
      let headerData =
        preprocessedPasses
        |> List.map (fun (ident, _, graph, _) -> GoogleCharts.includeDataRows ident graph)
        |> String.concat "\n"
      let body =
        preprocessedPasses
        |> List.map (fun (ident, desc, _, opers) -> formatPass ident desc opers)
        |> String.concat "\n"
      sprintf
        """<html><head>
        <script type="text/javascript" src="https://www.gstatic.com/charts/loader.js"></script>
        <script type="text/javascript">
          google.charts.load('current', {packages:["orgchart"]});
          google.charts.setOnLoadCallback(drawChart);

          function drawChart() {
            var data = new google.visualization.DataTable();
            data.addColumn('string', 'Name');
            data.addColumn('string', 'Manager');

            data.addRows([%s]);

            var chart = new google.visualization.OrgChart(document.getElementById('chart_div'));
            chart.draw(data, {allowHtml:true});
            %s
          }
        </script></head><body><h1>Freya Machine construction trace log</h1><div id="chart_div"></div><h2>Log</h2>%s</body></html>"""
        (GoogleCharts.formatMachine machine) headerData body

    let represent machine log =
      Represent.html (format machine log)

  module Text =
    let format machine log =
      sprintf "Machine representation:\n%A\n\nLog Representation:%A" machine log

    let represent machine log =
      Represent.text (format machine log)

let traceMachine machine =
  let (HttpMachine m) = machine
  let config = snd (m Configuration.empty)
  let (Configuration c) = config
  let exts = Optic.get Extensions.Components.components_ config
  let model = Http.model exts
  let prototype = Prototype.create model
  let machine, log = Machine.createLogged prototype config

  let machineTextRepresent =
    lazy (LogProcessing.Text.represent machine log)
  let machineHtmlRepresent =
    lazy (LogProcessing.Html.represent machine log)

  fun (acceptable : Acceptable) ->
    Freya.init <|
      match acceptable.MediaTypes with
      | Acceptance.Acceptable (best::_) when best = MediaType.Html ->
          machineHtmlRepresent.Force()
      | _ ->
          machineTextRepresent.Force()
