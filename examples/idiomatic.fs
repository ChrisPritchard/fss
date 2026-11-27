// Idiomatic F# that fss accepts unchanged: pipes, list append, for loops,
// local recursive functions, records, unions and pattern matching.
// Compiles with both `fss build` and `fsc`, and prints the same output.

type Item =
    | Widget of int
    | Gadget of int * int

let rec total acc xs =
    match xs with
    | [] -> acc
    | h :: t -> total (acc + h) t

let cost item =
    match item with
    | Widget n -> n * 3
    | Gadget (n, m) -> n * m

[<EntryPoint>]
let main _ =
    let widgets = [1; 2; 3]
    let gadgets = [4; 5]
    let everything = widgets @ gadgets

    let doubled = everything |> List.map (fun x -> x * 2) |> total 0
    System.Console.WriteLine(doubled)

    let evens = everything |> List.filter (fun x -> x % 2 = 0)
    System.Console.WriteLine(List.fold (fun acc -> fun x -> acc + x) 0 evens)

    let items =
        List.map (fun n -> Widget n) widgets @ List.map (fun n -> Gadget (n, 10)) gadgets

    let mutable bill = 0
    for item in items do
        bill <- bill + cost item
    System.Console.WriteLine(bill)

    let mutable squares = 0
    for i = 1 to 5 do
        squares <- squares + i * i
    System.Console.WriteLine(squares)

    let rec sumTo n =
        if n <= 0 then 0 else n + sumTo (n - 1)
    System.Console.WriteLine(sumTo 10 |> fun x -> x * 2)
    0
