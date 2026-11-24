type P = { X: int; Y: int }
type T = Point of P | None2

let main () =
    let p = Point { X = 3; Y = 4 } in
    (print_int (match p with | Point r -> r.X + r.Y | None2 -> 0); print_newline ();
     print_bool (p = Point { X = 3; Y = 4 }); print_newline ())
