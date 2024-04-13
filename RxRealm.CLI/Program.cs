// See https://aka.ms/new-console-template for more information

using RxRealm.CLI;
using RxRealm.CLI.Services;
using RxRealm.Core.Services;

Console.WriteLine("Hello, World!");

var productsService = new ProductsService(new FileSystemService());
const int count = 1_000_000;
using var consoleProgress = new ConsoleProgress(count, "Generating products...");
await productsService.CreateFakeProducts(count, consoleProgress);