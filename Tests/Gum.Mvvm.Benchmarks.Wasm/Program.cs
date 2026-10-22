using Gum.Mvvm.Benchmarks.Wasm;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
WebAssemblyHost host = builder.Build();
IJSRuntime js = host.Services.GetRequiredService<IJSRuntime>();

// Starts on load, no clicks. The page and window.__gumBenchResults / __gumBenchDone are filled by WasmBench.
await WasmBench.RunAndPublishAsync(js);

await host.RunAsync();
