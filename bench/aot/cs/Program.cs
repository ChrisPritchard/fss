using System;

static class Bench {
    static int Fib(int n) => n < 2 ? n : Fib(n - 1) + Fib(n - 2);

    static int Main(string[] args) {
        switch (args[0]) {
            case "fib30": Console.WriteLine(Fib(35)); break;
            case "loop_count": {
                int x = 0; for (int i = 0; i < 100000000; i++) x++;
                Console.WriteLine(x); break;
            }
            case "collatz": {
                int total = 0;
                for (int i = 1; i <= 100000; i++) {
                    int steps = 0; long m = i;
                    while (m != 1) { m = (m % 2 == 0) ? m / 2 : 3 * m + 1; steps++; }
                    total += steps;
                }
                Console.WriteLine(total); break;
            }
            case "str_concat": {
                string s = "";
                for (int i = 0; i < 20000; i++) s += "x";
                Console.WriteLine(s); break;
            }
            case "prime_count": {
                int c = 0;
                for (int i = 2; i < 100000; i++) {
                    bool p = true;
                    for (int d = 2; d * d <= i; d++) if (i % d == 0) { p = false; break; }
                    if (p) c++;
                }
                Console.WriteLine(c); break;
            }
            default: Console.WriteLine("?"); break;
        }
        return 0;
    }
}
