// Pipes (|>, <|), list append (@) and for loops. This file is also valid F#.
let rec total acc xs =
    match xs with
    | [] -> acc
    | h :: t -> total (acc + h) t

let rec mapF f xs =
    match xs with
    | [] -> []
    | h :: t -> f h :: mapF f t

let double x = x * 2
let inc x = x + 1

[<EntryPoint>]
let main _ =
    let xs = [1; 2; 3]
    let ys = xs @ [4; 5]
    System.Console.WriteLine(total 0 ys)
    System.Console.WriteLine(5 |> double |> inc)
    System.Console.WriteLine(double <| inc 5)
    let mutable sum = 0
    for i = 1 to 10 do
        sum <- sum + i
    System.Console.WriteLine(sum)
    for i = 10 downto 1 do
        sum <- sum - i
    System.Console.WriteLine(sum)
    for x in ys do
        sum <- sum + x
    System.Console.WriteLine(sum)
    let squares = mapF (fun x -> x * x) ys |> total 0
    System.Console.WriteLine(squares)
    0
