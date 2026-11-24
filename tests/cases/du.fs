type Shape = Circle of int | Rect of int * int | Empty

let area s = match s with | Circle r -> r * r | Rect (w, h) -> w * h | Empty -> 0

let main () =
    print_int (area (Circle 5)); print_newline ();
    print_int (area (Rect (3, 4))); print_newline ();
    print_int (area Empty); print_newline ()
