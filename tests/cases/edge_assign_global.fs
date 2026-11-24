let mutable x = 0
let inc d = (x <- x + 1; x)
let main () =
    print_int (inc 0);
    print_newline ();
    print_int (inc 0);
    print_newline ();
    print_int x;
    print_newline ()
