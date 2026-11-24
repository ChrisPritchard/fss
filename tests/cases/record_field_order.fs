type P = { X: int; Y: int }

let main () =
    let p = { Y = 4; X = 3 } in
    print_int (p.X + p.Y); print_newline ()
