let rec build n acc = if n = 0 then acc else build (n - 1) (n :: acc)
let rec sum acc xs = match xs with [] -> acc | h :: t -> sum (acc + h) t
[<EntryPoint>]
let main _ =
    let mutable s = 0
    let mutable i = 0
    while i < 10 do
        s <- s + sum 0 (build 20000 [])
        i <- i + 1
    System.Console.WriteLine(s)
    0
