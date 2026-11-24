let optMap f o = match o with | None -> None | Some x -> Some (f x)

let main () =
    (print_bool (optMap (fun x -> x * 2) (Some 5) = Some 10); print_newline ();
     print_bool (optMap (fun x -> x * 2) None = None); print_newline ())
