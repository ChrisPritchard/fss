type Inner = { A: int }
type Outer = { I: Inner }
let main () =
    print_bool ({ I = { A = 1 } } = { I = { A = 1 } });
    print_newline ()
