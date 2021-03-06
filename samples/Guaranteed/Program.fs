﻿module Guaranteed.Program

open FsShelter
open Guaranteed.Topology
open Common

let exePath = System.Reflection.Assembly.GetEntryAssembly().Location

// management CLI and task execution entry point
[<EntryPoint>]
let main argv = 
    let topology = 
        sampleTopology
        |> withConf [ Conf.TOPOLOGY_MULTILANG_SERIALIZER, box "com.prolucid.protoshell.ProtoSerializer"
                      Conf.TOPOLOGY_MAX_SPOUT_PENDING, box 123
                      Conf.TOPOLOGY_DEBUG, box false]

    match argv |> List.ofArray with
    | "submit"::address::[port] -> 
        Startup.submit topology exePath (Startup.mkWindowsArgs []) address (int port) 
    | "submit-mono"::address::[port] -> 
        Startup.submit topology exePath (Startup.mkMonoArgs []) address (int port)
    | ["submit-local"] -> 
        let mkArgs = if isNull (System.Type.GetType "Mono.Runtime") then Startup.mkWindowsArgs
                     else Startup.mkMonoArgs
        Startup.submit topology exePath (mkArgs []) "localhost" Nimbus.DefaultPort
    | "kill"::address::[port] ->
        Nimbus.withClient address (int port) 
            (fun client -> Nimbus.kill client sampleTopology.Name)
    | ["graph"] ->
        topology
        |> DotGraph.writeToConsole
    | ["self-host"] ->
        let stop = 
            topology
//            |> Host.runWith (sprintf "self-%d-%d" (System.Diagnostics.Process.GetCurrentProcess().Id) >> Logging.callbackLog)
            |> Host.run
        printf "Running the topology, press ENTER to stop..."
        let sw = System.Diagnostics.Stopwatch.StartNew()
        System.Threading.Thread.Sleep (-1) |> ignore
        stop()
        sw.Stop()
        let (count,_) = Topology.source.PostAndReply Get
        printf "Count: %s, %d/s\n" count (1000L*(int64 count)/sw.ElapsedMilliseconds)
    | _ -> 
        topology
        |> Task.ofTopology
        |> Task.run ProtoIO.start
//        |> Task.runWith (string >> Logging.callbackLog)  ProtoIO.start // start using a traffic logger 
    0

