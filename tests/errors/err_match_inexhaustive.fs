type T = A | B
let f t = match t with A -> 1
let main () = print_int (f A)
