type Inner = { A: int }
type Outer = { B: Inner; C: bool }

let main () =
    let o = { B = { A = 7 }; C = true } in
    (print_int o.B.A; print_newline ();
     print_bool o.C; print_newline ();
     print_bool (o = { B = { A = 7 }; C = true }); print_newline ())
