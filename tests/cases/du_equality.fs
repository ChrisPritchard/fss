type E = A of int | B of bool

let main () =
    (print_bool ((A 1) = (A 1)); print_newline ();
     print_bool ((A 1) = (A 2)); print_newline ();
     print_bool ((B true) = (B true)); print_newline ();
     print_bool ((A 1) = (B true)); print_newline ())
