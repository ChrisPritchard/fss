type T = A of int

let main () = print_int (match (A true) with | A x -> x)
