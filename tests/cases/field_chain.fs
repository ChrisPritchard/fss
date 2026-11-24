type A = { X: int }
type B = { A: A; Y: int }

let main () =
    let b = { A = { X = 9 }; Y = 8 } in
    print_int b.A.X; print_newline ()
