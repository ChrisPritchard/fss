let main () =
    (write_file "build/cases/io_test.txt" "hello file";
     print_string (read_file "build/cases/io_test.txt"); print_newline ();
     write_file "build/cases/io_test.txt" "second write";
     print_string (read_file "build/cases/io_test.txt"); print_newline ())
