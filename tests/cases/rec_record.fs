type Node = { X: int; Next: Node option }

let rec sumNodes n = match n.Next with | None -> n.X | Some m -> n.X + sumNodes m

let main () =
    let n = { X = 1; Next = Some { X = 2; Next = Some { X = 3; Next = None } } } in
    (print_int n.X; print_newline ();
     print_int (sumNodes n); print_newline ())
