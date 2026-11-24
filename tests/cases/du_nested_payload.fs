type T = A of int option | B of int list | C of int * bool

let main () =
    (print_int (match (A (Some 5)) with | A (Some x) -> x | A None -> 0 | B xs -> 0 | C (x, b) -> 0); print_newline ();
     print_bool (match (C (7, true)) with | A _ -> false | B _ -> false | C (x, b) -> b); print_newline ())
