type Color = Red | Green | Blue
let f c = match c with Red -> "r" | Green -> "g" | Blue -> "b"
let main () =
    print_string (f Red);
    print_newline ();
    print_string (f Green);
    print_newline ();
    print_string (f Blue);
    print_newline ()
