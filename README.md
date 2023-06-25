# rust-gene-calculator
A simple gene calculator for Rust game based on https://wgn.si/genetics/ and translated to C# for speed.

Compiled .exe file is in `\Rust Gene Calculator\Rust Gene Calculator\bin\Debug\net5.0`.

# Usage
Drop a text file with genes onto the `Rust Gene Calculator.exe` file - it will find the best crossbreed plant you can get depending on gene priority you set.

Text file should have a json string with gene priority as first row, then genes for each plant in seperate lines, e.g.
```
{"Y":1, "G":0.5, "H":0}
GYGXYY
YGXXGW
YGGWYH
GXXXHH
XYHYHH
WGGXYY
YXGYGY
XYGGWX
```
Above would set Y priority to 1.0, G priority to 0.5, H priority to 0.0, trying to find a crossbreed from listed genes with twice as many Y genes as G genes, i.e. a crossbreed with 4Y and 2G genes.
