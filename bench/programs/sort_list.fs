let rec insert x xs =
    match xs with
    | [] -> [x]
    | h :: t -> if x <= h then x :: h :: t else h :: insert x t

let rec sort xs =
    match xs with
    | [] -> []
    | h :: t -> insert h (sort t)

let rec build n = if n = 0 then [] else n :: build (n - 1)

[<EntryPoint>]
let main _ =
    let sorted = sort (build 1500)
    match sorted with
    | a :: b :: _ -> printf "%d\n" (a * 1000 + b)
    | _ -> printf "0\n"
    0
