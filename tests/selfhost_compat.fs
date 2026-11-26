open System

let greet (name: System.String) = "hi " + name

[<EntryPoint>]
let main () =
    Console.WriteLine (greet "world")
