type P = { X: int; Y: int; Z: int }

let main () =
    let p = { X = 1; Y = 2; Z = 3 } in
    let q = { p with Y = 20; Z = 30 } in
    (print_int q.X; print_newline ();
     print_int q.Y; print_newline ();
     print_int q.Z; print_newline ();
     print_int p.Y; print_newline ())
