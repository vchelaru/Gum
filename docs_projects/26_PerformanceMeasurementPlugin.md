# PerformanceMeasurementPlugin (Plugin de Rendimiento)

## Descripción

PerformanceMeasurementPlugin es un plugin simple que muestra métricas de rendimiento del editor Gum, específicamente el contador de draw calls. Actualmente está deshabilitado (la pestaña no se muestra) según los comentarios en el código.

## Diagrama de Relaciones

```mermaid
graph TB
    PerformanceMeasurementPlugin[PerformanceMeasurementPlugin]
    
    PerformanceMeasurementPlugin --> Gum[Gum Tool]
    PerformanceMeasurementPlugin --> System.ComponentModel.Composition
    
    subgraph PluginBase
        PluginBase[PluginBase]
    end
    
    PerformanceMeasurementPlugin -.->|Extends| PluginBase
    
    subgraph Classes
        MainPlugin[MainPlugin]
        PerformanceViewModel[PerformanceViewModel]
        PerformanceView[PerformanceView.xaml]
    end
    
    PerformanceMeasurementPlugin --> Classes
    
    subgraph Metrics
        DrawCallCount[Draw Call Count]
    end
    
    PerformanceViewModel --> Metrics
    
    note[NOTA: Plugin actualmente deshabilitado<br/>Tab no se muestra]
    
    PerformanceMeasurementPlugin -.-> note
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | WPF |
| **.NET** | net8.0-windows |
| **Dependencias** | Gum.csproj |

## Punto de Entrada

| Archivo | Clase | Ubicación |
|---------|-------|-----------|
| `MainPlugin.cs` | `MainPlugin` | `Gum/PerformanceMeasurementPlugin/` |

```csharp
[Export(typeof(PluginBase))]
public class MainPlugin : PluginBase
{
    private PerformanceView _view;
    
    public override void StartUp()
    {
        _view = new PerformanceView();
        _view.DataContext = new PerformanceViewModel();
        
        // NOTA: Actualmente deshabilitado
        // Descomentar para habilitar:
        // _guiCommands.AddControl(_view, "Performance");
    }
    
    public override bool ShutDown(PluginShutDownReason reason)
    {
        return false;
    }
}
```

## Métricas Disponibles

| Métrica | Descripción |
|---------|-------------|
| `DrawCallCount` | Número de draw calls en el último frame |

## Cómo Ampliar

### Habilitar el Plugin

```csharp
// En MainPlugin.cs, cambiar:
public override void StartUp()
{
    _view = new PerformanceView();
    _view.DataContext = new PerformanceViewModel();
    
    // Cambiar de:
    // (nada - tab no se muestra)
    
    // A:
    _guiCommands.AddControl(_view, "Performance");
}
```

### Añadir Nuevas Métricas

```csharp
public class PerformanceViewModel : ViewModel
{
    private int _drawCallCount;
    public int DrawCallCount
    {
        get => _drawCallCount;
        set => Set(ref _drawCallCount, value);
    }
    
    // Añadir nuevas métricas
    private int _fps;
    public int FPS
    {
        get => _fps;
        set => Set(ref _fps, value);
    }
    
    private long _memoryUsage;
    public long MemoryUsage
    {
        get => _memoryUsage;
        set => Set(ref _memoryUsage, value);
    }
    
    private int _elementCount;
    public int ElementCount
    {
        get => _elementCount;
        set => Set(ref _elementCount, value);
    }
    
    private int _instanceCount;
    public int InstanceCount
    {
        get => _instanceCount;
        set => Set(ref _instanceCount, value);
    }
}
```

### Actualizar Métricas en Tiempo Real

```csharp
public class PerformanceViewModel : ViewModel
{
    private DispatcherTimer _updateTimer;
    
    public PerformanceViewModel()
    {
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateTimer.Tick += UpdateMetrics;
        _updateTimer.Start();
    }
    
    private void UpdateMetrics(object sender, EventArgs e)
    {
        // Obtener métricas del wireframe
        var wireframeManager = Locator.GetRequiredService<IWireframeObjectManager>();
        DrawCallCount = wireframeManager.LastFrameDrawCalls;
        
        // Obtener memoria
        MemoryUsage = GC.GetTotalMemory(false);
        
        // Obtener contadores de elementos
        var projectState = Locator.GetRequiredService<IProjectState>();
        if (projectState.Project != null)
        {
            ElementCount = projectState.Project.Screens.Count + 
                          projectState.Project.Components.Count;
            InstanceCount = CountAllInstances(projectState.Project);
        }
        
        // FPS (calcular desde último update)
        FPS = CalculateFPS();
    }
}
```

### Ver Histórico de Métricas

```csharp
public class PerformanceHistory
{
    private readonly List<PerformanceSnapshot> _history = new();
    private readonly int _maxHistory = 300; // 5 segundos a 60 FPS
    
    public void AddSnapshot(PerformanceSnapshot snapshot)
    {
        _history.Add(snapshot);
        if (_history.Count > _maxHistory)
            _history.RemoveAt(0);
    }
    
    public IEnumerable<PerformanceSnapshot> GetLast(int count)
    {
        return _history.TakeLast(count);
    }
    
    public float GetAverageFPS(int lastN)
    {
        return GetLast(lastN).Average(s => s.FPS);
    }
    
    public int GetMaxDrawCalls(int lastN)
    {
        return GetLast(lastN).Max(s => s.DrawCalls);
    }
}
```

## Retos al Ampliar

### Overhead de Medición
- Medir performance añade overhead
- Puedefalsear los resultados
- **Recomendación**: Usar medición ligera o toggle

### Thread Safety
- Métricas pueden actualizarse desde UI thread
- Wireframe updates pueden ser enotro thread
- **Recomendación**: Usar Dispatcher para updates

### Precisión de Memoria
- GC.GetTotalMemory no es exacto
- No incluye memoria no manejada
- **Recomendación**: Usar diagnósticos de memoria para precisión

### FPS Variable
- FPS depende de vsync y otros factores
- No siempre es útil para editor
- **Recomendación**: Focus en draw calls y latency

## Estado Actual

El plugin está **deshabilitado**. Para habilitarlo:

1. Abrir `Gum/PerformanceMeasurementPlugin/MainPlugin.cs`
2. Descomentar la línea: `_guiCommands.AddControl(_view, "Performance");`
3. Recompilar Gum

Posibles mejoras para el plugin:
- FPS counter
- Memory profiling
- Element/instance counts
- Historical graphs
- Export a CSV para analysis