﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsJson")>]
[<assembly: AssemblyProductAttribute("FsStorm")>]
[<assembly: AssemblyDescriptionAttribute("F# DSL and runtime for Storm topologies")>]
[<assembly: AssemblyVersionAttribute("0.0.6")>]
[<assembly: AssemblyFileVersionAttribute("0.0.6")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.6"
