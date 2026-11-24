type V = Int of int | Bool of bool

let toInt v = match v with | Int i -> i | Bool b -> (if b then 1 else 0)

let main () =
    (print_int (toInt (Int 42)); print_newline ();
     print_int (toInt (Bool true)); print_newline ();
     print_int (toInt (Bool false)); print_newline ())
