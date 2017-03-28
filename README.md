# Freya, Hopac, and Kestrel Demo Repository

To build the demos, run the following from the repository root.

```
dotnet restore
dotnet build
```

Benchmarks are kept in a separate solution under the `benchmarks` directory. To build them, `cd` into the `benchmarks` directory and run:

```
dotnet restore
dotnet build -c Release
```

The `additions.txt` file in the `demo` project has several lines that can be incorporated into `Api.fs` to see how machines are composed, and the machine tracer can be used to see how the machine is optimized after configuration.