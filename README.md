# Yak
Yak is console wrapper around SymmetricDifferenceFinder library (fast symmetric difference finder for k-mer sets, also developed by us ). Currently, it is in heavy development. Please see Program.cs for possible commands. The program is mainly intented for experiments. Apis may and will change. This is not stable implementation.
See SymmetricDifferenceFinder for more stable API. 
## Instalation - Linux
Net 7.0 is required

Run build.sh command, then add Yak to your path, by:
```
PATH=$PATH:/workspaces/codespaces-blank/Yak/Yak/bin/Release/net8.0
```
## Basic info
There are two stages, encoding and decoding(recovery).
First one needs to describe its encoding template.
Simplest looks like this
```
{"SketchConfigurations":[{"HashFunctionSchemesFileNames":["mul1","mul2","mul3"],"TableSize":10000,"Filter":null}]}
```
This sketch requires three hash functions. They need to be created by calling command:
```
Yak create-hash [hash function name] [hash function family] [size] [offset]
```
Possible hash families may be seen by this command:
```
Yak get-all-hash-functions
```

To encode to sketch
```
Yak encode-to-sketch [template-location]  [source-location] [sketch-filename] [n-threads]
```

To take symmetric difference of two sketches
```
Yak symmetric-difference [first-sketch-location] [second-sketch-location] [resulting-sketch-location]
```
To decode 
```
Yak recover-sketch [sketch-location] [result-file]
```
To more advanced features use interactive mode or file execution
More can be found in Commands.cs

