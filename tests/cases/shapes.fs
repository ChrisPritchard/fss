type Shape = Circle of int | Rect of int * int

let area s = match s with | Circle r -> 3 * r * r | Rect (w, h) -> w * h
let perim s = match s with | Circle r -> 6 * r | Rect (w, h) -> 2 * (w + h)

let main () =
    (print_int (area (Circle 3)); print_newline ();
     print_int (perim (Rect (4, 5))); print_newline ())
