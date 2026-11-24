let pair a b = (a, b)
let main () =
    print_int (match pair 1 2 with (a, b) -> a + b);
    print_newline ();
    print_string (match pair "a" "b" with (a, b) -> a + b);
    print_newline ()
