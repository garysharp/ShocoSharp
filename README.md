
**ShocoSharp**: a fast compressor for short strings
--------------------------------------------

_based on [**shoco**](https://github.com/Ed-von-Schleck/shoco) - a C implementation by Christian Schramm_

**ShocoSharp** is a .NET library to compress and decompress short strings. The default compression model is optimized for English words, but you can [generate your own compression model](#generating-compression-models) based on *your specific* input data.

**ShocoSharp** is free software, distributed under the [MIT license](https://github.com/garysharp/ShocoSharp/blob/master/LICENSE).

## Quick Start

> #### [on NuGet](https://nuget.org/packages/ShocoSharp/)
> ```PowerShell
> Install-Package ShocoSharp
> ```

### API

For very basic usage the following global helper methods are offered:

```C#
byte[] Shoco.Compress(string Data);
string Shoco.DecompressString(byte[] Data);
```

A range of overloads provide great flexibility and the opportunity to optimize use of the library.

The global helper methods operate on the `Shoco.DefaultModel` compression model. By default, the `ShocoCompatibleEnglishWordsModel` is used (to provide compatibilty with other implementations). You can substitute the global default model or use any other model directly.

The library ships with several [pre-generated compression models](https://github.com/garysharp/ShocoSharp/tree/master/src/ShocoSharp/Models), or you can [generate your own model](#generating-compression-models) from your specific sample data.

## How It Works

This library is ported from its original C implementation. See Christian Schramm's excellent explanation at the [shoco](https://github.com/Ed-von-Schleck/shoco#how-it-works) repository.

## Generating Compression Models

Maybe your typical input isn’t English words. Maybe it’s German or French – or whole sentences. Or file system paths. Or URLs. While the standard compression model of **ShocoSharp** should work for all of these, it might be worthwhile to train **ShocoSharp** for this specific type of input data.

Fortunately, that’s really easy: **ShocoSharp** includes a compression model generator which will train a model on your sample data. This model can be used directly after training, or exported to a C# class or C header file for use in your project.

```C#
// generate model
var myModel = ShocoModelGenerator.GenerateModelFromFile(@"C:\MyTrainingData\MyData.txt");

// use the model directly
var compressedString = myModel.Compress("sample string");
var originalString = myModel.Decompress(compressedString);

// write out the model
myModel.WriteAsCSharpClass(Filename: @"C:\MyTrainingData\MyModel.cs", Name: "MyShocoModel")
```

As with the compression/decompression methods there are many overloads that provide great flexibility when generating models. All features provided by the original [python model generator](https://github.com/Ed-von-Schleck/shoco#generating-compression-models) are available.

If you use the [python model generator](https://github.com/Ed-von-Schleck/shoco#generating-compression-models) to create your compression model, you can load and convert this into **ShocoSharp** with `ShocoModel.ReadFromCHeader(string Filename)`.

## Credits

**ShocoSharp** is based on [**shoco**](https://github.com/Ed-von-Schleck/shoco), written by Christian Schramm which was released under the MIT license.