﻿module Storm.TranslationTests

open NUnit.Framework
open Swensen.Unquote
open Storm.TestTopology
open System

[<Test>]
let ``translates to Thrift``() = 
    let tt = t1 |> Storm.ThriftModel.ofTopology [] "zzz" |> snd

    test <@ tt.Spouts |> Seq.forall2 (fun sid2 sd1 -> sd1.Key = sid2) ["s1"] @>
    test <@ tt.Bolts |> Seq.toList |> List.forall2 (fun sid2 sd1 -> sd1.Key = sid2) ["b1";"b2"] @>
    test <@ tt.Spouts.["s1"].Common.Streams |> Seq.map (fun x -> x.Key) |> Seq.toList = ["Original"] @>
    test <@ tt.Bolts.["b1"].Common.Streams |> Seq.map (fun x -> x.Key) |> Seq.toList = ["Even"; "Odd"] @>
    test <@ tt.Bolts.["b2"].Common.Streams |> Seq.toList |> List.isEmpty @>
    test <@ tt.Spouts.["s1"].Common.Inputs |> Seq.toList |> List.isEmpty @>
    test <@ tt.Bolts.["b1"].Common.Inputs |> Seq.map (fun x -> x.Key.ComponentId,x.Key.StreamId) |> Seq.toList = ["s1","Original"] @>
    test <@ tt.Bolts.["b2"].Common.Inputs |> Seq.map (fun x -> x.Key.ComponentId,x.Key.StreamId) |> Seq.toList = ["b1","Even"; "b1","Odd"] @>

    printf "%A" tt.Bolts.["b1"].Bolt_object.Shell

open Storm.DotGraph
[<Test>]
let ``translates to Dot``() = 
    let w = new System.IO.StringWriter()
    t1 |> exportToDot (writeHeader,writeFooter,writeSpout,writeBolt,writeStream) w
    test <@ not <| String.IsNullOrEmpty(w.ToString()) @>
