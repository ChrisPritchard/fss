type Inner = { A: int }
type Outer = { I: Inner; B: string }
let o = { I = { A = 5 }; B = "x" }
let main () =
    print_int o.I.A;
    print_newline ();
    print_string o.B;
    print_newline ()
