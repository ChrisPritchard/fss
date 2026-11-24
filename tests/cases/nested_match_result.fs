type T = A of int | B of bool

let f t = match t with | A n -> n | B b -> (if b then 10 else 20)

let main () =
    (print_int (f (A 5)); print_newline ();
     print_int (f (B true)); print_newline ();
     print_int (f (B false)); print_newline ())
