type Shape = Circle of int | Rect of int * int
let area s = match s with Circle r -> r * r | Rect (w, h) -> w * h
let main () =
    print_int (area (Circle 3));
    print_newline ();
    print_int (area (Rect (3, 4)));
    print_newline ()
