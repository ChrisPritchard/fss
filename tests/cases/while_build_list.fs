let rec length xs = match xs with | [] -> 0 | h :: t -> 1 + length t

let main () =
    let mutable i = 0 in
    let mutable xs = [] in
    (while i < 100 do (xs <- i :: xs; i <- i + 1);
     print_int (length xs); print_newline ())
