let main () =
    (print_int (try 1 + 2 with | "boom" -> 0 | _ -> 0); print_newline ();
     print_int (try (failwith "boom") with | "boom" -> 42 | _ -> 0); print_newline ();
     print_int (try (failwith "other") with | "boom" -> 0 | _ -> 99); print_newline ();
     print_string (try (failwith "msg") with | x -> x); print_newline ())
