let mutable n = 0
let counter d = (n <- n + 1; n)
let main () =
    print_int (counter 0);
    print_newline ();
    print_int (counter 0);
    print_newline ()
