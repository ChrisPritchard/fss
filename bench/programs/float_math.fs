// Floating point math through System.Math (fss maps these to libm).
open System
let rec norm2 (xs: float list) acc =
    match xs with
    | [] -> acc
    | h :: t -> norm2 t (acc + h * h)

let rec build n acc = if n = 0 then acc else build (n - 1) (float n :: acc)

[<EntryPoint>]
let main _ =
    let xs = build 200 []
    let mutable total = 0.0
    let mutable i = 0
    while i < 20000 do
        total <- total + sqrt (norm2 xs 0.0) + Math.Sin 0.5 + Math.Log 2.0
        i <- i + 1
    System.Console.WriteLine(int total)
    0
