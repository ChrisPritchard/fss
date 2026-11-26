module Fss.Driver

open System
open System.Diagnostics
open System.IO
open Fss.Typer

let private usage =
    "usage: fss build <file.fs> [-o <output>] [--keep-c] [-O<0|1|2|3|s|fast>] [--cc <name>] [--march-native]\n" +
    "       fss scan <file.fs>   # report F# features fss does not support"

let private compileC (cc: string) (opt: string) (marchNative: bool) (links: string list) (cPath: string) (outPath: string) : unit =
    // -fwrapv: F# `int` arithmetic is unchecked and wraps; C signed overflow
    // would otherwise be undefined behaviour and open to miscompilation.
    let march = if marchNative then "-march=native " else ""
    let psi = ProcessStartInfo()
    psi.FileName <- cc
    let linkFlags = links |> List.map (fun l -> " -l" + l) |> String.concat ""
    psi.Arguments <- opt + " -fwrapv -Wl,--stack,67108864 " + march + "-o \"" + outPath + "\" \"" + cPath + "\"" + linkFlags
    psi.UseShellExecute <- false
    psi.RedirectStandardError <- true
    psi.RedirectStandardOutput <- true
    let p = Process.Start(psi)
    let err = p.StandardError.ReadToEnd()
    let outp = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    if p.ExitCode <> 0 then
        eprintfn "C compiler failed (exit %s):" (string p.ExitCode)
        if outp <> "" then eprintfn "%s" outp
        if err <> "" then eprintfn "%s" err
        Environment.Exit 1

let private runBuild (srcPaths: string list) (out: string) (keepC: bool) (cc: string) (opt: string) (marchNative: bool) (library: bool) : int =
    let progs = srcPaths |> List.map (fun p -> File.ReadAllText p |> Fss.Parser.parse)
    let prog : Fss.Ast.Program =
        { Types = progs |> List.collect (fun p -> p.Types)
          Defs = progs |> List.collect (fun p -> p.Defs)
          Opens = progs |> List.collect (fun p -> p.Opens)
          Includes = progs |> List.collect (fun p -> p.Includes)
          Links = progs |> List.collect (fun p -> p.Links)
          Exports = progs |> List.collect (fun p -> p.Exports) }
    let tprog = Fss.Typer.check prog
    let mono = Fss.Mono.monomorphize tprog
    let lib = library || mono.Library
    let c = Fss.Codegen.emitWith lib mono
    let cPath = out + ".c"
    File.WriteAllText(cPath, c)
    if lib then
        File.WriteAllText(out + ".h", Fss.Codegen.emitHeader mono)
    let cflags = if library then " -shared -fPIC" else ""
    // Shared libraries get a conventional file extension.
    let libOut =
        if not lib then out
        elif out.EndsWith(".dll") || out.EndsWith(".so") || out.EndsWith(".dylib") then out
        elif System.OperatingSystem.IsWindows() then out + ".dll"
        else out + ".so"
    compileC cc (opt + cflags) marchNative prog.Links cPath libOut
    if not keepC then File.Delete cPath
    let exe =
        if File.Exists(libOut + ".exe") then libOut + ".exe"
        elif File.Exists(libOut) then libOut
        else out
    printfn "built %s" exe
    0

[<EntryPoint>]
let main (argv: string array) : int =
    if argv.Length < 2 then
        eprintfn "%s" usage
        1
    else
        match argv.[0] with
        | "build" ->
            let mutable srcPaths = []
            let mutable out = ""
            let mutable keepC = false
            let mutable cc = "cc"
            let mutable opt = "-O2"
            let mutable marchNative = false
            let mutable library = false
            let mutable i = 1
            while i < argv.Length do
                match argv.[i] with
                | "-o" ->
                    if i + 1 < argv.Length then
                        i <- i + 1
                        out <- argv.[i]
                | "--keep-c" -> keepC <- true
                | "--cc" ->
                    if i + 1 < argv.Length then
                        i <- i + 1
                        cc <- argv.[i]
                | "--march-native" -> marchNative <- true
                | "--lib" -> library <- true
                | a when a.StartsWith("-O") -> opt <- a
                | a ->
                    if srcPaths.IsEmpty then out <- Path.GetFileNameWithoutExtension a
                    srcPaths <- srcPaths @ [ a ]
                i <- i + 1

            try
                runBuild srcPaths out keepC cc opt marchNative library
            with
            | TypeError msg -> eprintfn "type error: %s" msg; 1
            | ex -> eprintfn "error: %s" ex.Message; 1
        | "scan" ->
            if argv.Length < 2 then
                eprintfn "%s" usage
                1
            else
                try
                    let src = File.ReadAllText argv.[1]
                    let findings = Fss.Scan.scan src
                    if List.isEmpty findings then
                        printfn "no unsupported F# features found in %s" argv.[1]
                        0
                    else
                        findings |> List.iter (printfn "%s")
                        0
                with ex ->
                    eprintfn "error: %s" ex.Message
                    1
        | _ ->
            eprintfn "%s" usage
            1
