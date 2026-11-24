type T = A of int list | B of T
let rec size t = match t with A xs -> (match xs with [] -> 0 | _ :: r -> 1 + (match r with [] -> 0 | _ -> 1)) | B t2 -> 1 + size t2
let main () =
    print_int (size (B (A [1; 2])));
    print_newline ()
