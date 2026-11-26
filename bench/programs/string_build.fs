// String allocation: build text from ints with int_to_string + concat.
let rec numbers n acc =
    if n = 0 then acc else numbers (n - 1) (string n + "," + acc)

let foldText times =
    let mutable s = ""
    let mutable i = 0
    while i < times do
        s <- s + numbers 20 ""
        i <- i + 1
    s

[<EntryPoint>]
let main _ =
    let s = foldText 400
    System.Console.WriteLine(s)
    0
