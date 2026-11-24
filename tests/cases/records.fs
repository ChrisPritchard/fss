type Point = { X: int; Y: int }

let main () =
    let p = { X = 3; Y = 4 } in
    (print_int p.X; print_newline ();
     print_int p.Y; print_newline ();
     let q = { p with Y = 10 } in
     (print_int q.X; print_newline ();
      print_int q.Y; print_newline ();
      print_bool (p = { X = 3; Y = 4 }); print_newline ()))
