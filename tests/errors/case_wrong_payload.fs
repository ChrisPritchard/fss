type T = A of int

let main () = print_int (match (A 1) with | A true -> 0)
