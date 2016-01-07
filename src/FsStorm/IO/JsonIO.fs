﻿module Storm.JsonIO

open Multilang
open System
open System.Text
open System.IO
open Newtonsoft.Json
open TupleSchema
open Newtonsoft.Json.Linq

let private isMono() = not <| isNull (System.Type.GetType("Mono.Runtime"))

let private write (text:string) (_,textWriter:TextWriter) =
    textWriter.WriteLine(text.Replace("\n","\\n"))
    textWriter.WriteLine("end")
    textWriter.Flush()

let toLog (msg:string) (lvl:int) =
    use sw = new StringWriter()
    use w = new JsonTextWriter(sw)
    w.WriteStartObject()
    w.WritePropertyName("command")
    w.WriteValue("log")
    w.WritePropertyName("msg")
    w.WriteValue(msg)
    w.WritePropertyName("level")
    w.WriteValue(lvl)
    w.WriteEndObject()
    w.Close()
    sw.ToString()

let private toEmit streamRW t (tid:string option) (anchors:string list) (stream:string) (needTaskIds:bool option) =
    let writeAnchors (w:JsonTextWriter) = 
        w.WritePropertyName("anchors")
        w.WriteStartArray()
        anchors |> List.iter w.WriteValue
        w.WriteEndArray()

    let writeId (w:JsonTextWriter) =
        match tid with
        | Some tid -> 
            w.WritePropertyName("id")
            w.WriteValue(tid)
        | _ -> ()

    let writeTaskIdsNeed (w:JsonTextWriter)= 
        w.WritePropertyName("need_task_ids")
        w.WriteValue(needTaskIds.IsSome && needTaskIds.Value)

    let writeTuple (deconstr:FieldWriter->obj->unit) (w:JsonTextWriter) =
        w.WritePropertyName("tuple")
        w.WriteStartArray()
        deconstr (JsonConvert.SerializeObject >> w.WriteRawValue) t
        w.WriteEndArray()

    let (_,d) = streamRW |> Map.find stream
    use sw = new StringWriter()
    use w = new JsonTextWriter(sw)
    w.WriteStartObject()
    w.WritePropertyName("command")
    w.WriteValue("emit")
    writeId w
    writeTuple d w
    writeAnchors w
    w.WritePropertyName("stream")
    w.WriteValue(stream)
    writeTaskIdsNeed w
    w.WriteEndObject()
    w.Close()
    sw.ToString()

let private (|Init|_|) (o:JObject) =
    let readConf (conf:JToken) = 
        let r = conf.CreateReader()
        r.Read() |> ignore
        seq {
            while r.Read() && JsonToken.EndObject <> r.TokenType do
                let name = (string r.Value)
                r.Read() |> ignore
                yield name,r.Value
        } |> Map.ofSeq
        
    let readTaskMap (ctx:JToken) =
        let r = ctx.["task->component"].CreateReader()
        r.Read() |> ignore
        seq {
            while r.Read() && JsonToken.EndObject <> r.TokenType do
                let taskId= r.Value |> (string >> Int64.Parse)
                yield taskId,r.ReadAsString()
        } |> Map.ofSeq

    let readContext (ctx:JToken) = 
        let taskMap = readTaskMap ctx
        let taskId = ctx.["taskid"].ToObject()
        {
            ComponentId= taskMap |> Map.find taskId
            TaskId=taskId
            Components=taskMap
        }
        
    match o.HasValues,o.Property("pidDir") |> Option.ofObj with
    | true, Some p ->
        let pidDir = p.ToObject()
        let ctx = readContext (o.["context"])
        let conf = readConf(o.["conf"])
        Some (Handshake(conf, pidDir, ctx))
    | _ -> None

let private (|Control|_|) (o:JObject) =
    match o.HasValues,o.Property("command") |> Option.ofObj with
    | true, Some p ->
        match p.ToObject() with
        | "next" -> Some (Next)
        | "ack" -> Some (Ack (o.["id"].ToObject()))
        | "fail" -> Some (Nack (o.["id"].ToObject()))
        | _ -> None
    | _ -> None

let private (|Stream|_|) findConstructor (o:JObject) =
    match o.HasValues,o.Property("stream") |> Option.ofObj with
    | true, Some p ->
        match p.ToObject() with
        | "__heartbeat" -> Some (Heartbeat)
        | streamId -> 
             let xs = o.["tuple"].Children().GetEnumerator()
             let constr = findConstructor streamId <| fun t -> xs.MoveNext() |> ignore; xs.Current.ToObject(t)
             Some (InCommand.Tuple(constr(), o.["id"].ToObject(), o.["comp"].ToObject(), streamId, o.["task"].ToObject()))
    | _ -> None

let private toCommand (findConstructor:string->FieldReader->unit->'t) str : InCommand<'t> =
    let jobj = JObject.Parse str
    match jobj with
    | Init cmd -> cmd
    | Control cmd -> cmd
    | Stream findConstructor cmd -> cmd
    | _ -> failwithf "Unable to parse: %s" str

let start tag :Topology.IO<'t> =
    if isMono() then
        () //on osx/linux under mono, set env LANG=en_US.UTF-8
    else
        if not Console.IsInputRedirected then
            Console.InputEncoding <- Encoding.UTF8
        if not Console.IsOutputRedirected then
            Console.OutputEncoding <- Encoding.UTF8

    let streamRW = TupleSchema.mapSchema<'t>() |> Map.ofArray
   
    let out' cmd = 
        match cmd with
        | Sync -> """{"command":"sync"}"""
        | Pid pid -> sprintf """{"pid":%d}""" pid
        | Fail tid -> sprintf """{"command":"fail","id":"%s"}""" tid
        | Ok tid -> sprintf """{"command":"ack","id":"%s"}""" tid
        | Log (msg,lvl) -> toLog msg (int lvl)
        | Emit (t,tid,anchors,stream,needTaskIds) -> toEmit streamRW t tid anchors stream needTaskIds
        |> write |> IO.Common.sync_out

    let in' ():Async<InCommand<'t>> =
        let findConstructor stream = streamRW |> Map.find stream |> fst
        async {
            let! msg  = Console.In.ReadLineAsync() |> Async.AwaitTask
            let! term = Console.In.ReadLineAsync() |> Async.AwaitTask
            return match msg,term with
                   | msg,"end" when not <| String.IsNullOrEmpty msg -> toCommand findConstructor msg
                   | _ -> failwithf "Unexpected input msg/term: %s/%s" msg term
         }

    (in',out')
