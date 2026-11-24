type T = A of int
let main () = print_int (match A 1 with A (x, y) -> x | _ -> 0)
