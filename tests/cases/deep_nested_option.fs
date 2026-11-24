let main () =
    let v = Some (Some (Some 3)) in
    (print_int (match v with
                | Some (Some (Some x)) -> x
                | Some (Some None) -> 0
                | Some None -> 0
                | None -> 0); print_newline ())
