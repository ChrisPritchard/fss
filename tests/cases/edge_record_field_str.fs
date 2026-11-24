type R = { Name: string; N: int }
let r = { Name = "foo"; N = 5 }
let main () =
    print_string r.Name;
    print_newline ();
    print_int r.N;
    print_newline ()
