type Inner = { A: int }
type Outer = { I: Inner; B: bool }

let main () =
    let o = { I = { A = 1 }; B = true } in
    let o2 = { o with I = { A = 2 } } in
    (print_int o2.I.A; print_newline ();
     print_bool o2.B; print_newline ();
     print_int o.I.A; print_newline ())
