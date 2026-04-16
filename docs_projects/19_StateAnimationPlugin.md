# StateAnimationPlugin (Plugin de Animaciones)

## Descripción

StateAnimationPlugin proporciona un sistema de animaciones basado en estados y keyframes para elementos Gum. Permite crear animaciones donde cada keyframe representa un estado del elemento (posiciones, colores, valores de variables), con interpolación y easing entre estados.

## Diagrama de Relaciones

```mermaid
graph TB
    StateAnimationPlugin[StateAnimationPlugin]
    
    StateAnimationPlugin --> Gum[Gum Tool]
    StateAnimationPlugin --> FlatRedBall.InterpolationCore
    StateAnimationPlugin --> Newtonsoft.Json
    StateAnimationPlugin --> SkiaSharp.Views.WPF
    
    subgraph PluginBase
        PluginBase[PluginBase]
        PriorityPlugin[PriorityPlugin]
    end
    
    StateAnimationPlugin -.->|Extends| PluginBase
    
    subgraph Events
        ElementSelected[ElementSelected]
        InstanceSelected[InstanceSelected]
        StateAdd[StateAdd]
        StateDelete[StateDelete]
        StateRename[StateRename]
        VariableSet[VariableSet]
    end
    
    StateAnimationPlugin --> Events
    
    subgraph ViewModels
        ElementAnimationsViewModel[ElementAnimationsViewModel]
        AnimationViewModel[AnimationViewModel]
        AnimatedKeyframeViewModel[AnimatedKeyframeViewModel]
    end
    
    StateAnimationPlugin --> ViewModels
    
    subgraph Views
        Timeline[Timeline.xaml]
        AnimationTreeView[AnimationTreeView]
    end
    
    ViewModels -.->|Data Binds| Views
    
    subgraph SaveClasses
        AnimationSave[AnimationSave]
        ElementAnimationsSave[ElementAnimationsSave]
    end
    
    StateAnimationPlugin --> SaveClasses
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | WPF |
| **.NET** | net8.0-windows |
| **Lenguaje** | C# 12.0 |
| **MVVM** | CommunityToolkit.Mvvm |
| **Serialización** | Newtonsoft.Json |
| **Animación** | FlatRedBall.InterpolationCore |
| **Dependencias** | Gum.csproj, WpfDataUi |

## Punto de Entrada

| Archivo | Clase | Ubicación |
|---------|-------|-----------|
| `MainStateAnimationPlugin.cs` | `MainStateAnimationPlugin` | `Gum/StateAnimationPlugin/` |

```csharp
[Export(typeof(PluginBase))]
internal class MainStateAnimationPlugin : PriorityPlugin
{
    public override void StartUp()
    {
        // Suscribirse a eventos
        this.ElementSelected += HandleElementSelected;
        this.InstanceSelected += (_, _) => RefreshViewModel();
        // ... más suscripciones
    }
}
```

## Archivos Principales

| Archivo | Propósito |
|---------|-----------|
| `MainStateAnimationPlugin.cs` | Entry point del plugin |
| `ElementAnimationsViewModel.cs` | ViewModel principal |
| `AnimationViewModel.cs` | ViewModel de una animación |
| `AnimatedKeyframeViewModel.cs` | ViewModel de un keyframe |
| `AnimationCollectionViewModelManager.cs` | Cache de ViewModels |
| `AnimationSave.cs` | Modelo de serialización |
| `Timeline.xaml.cs` | Vista del timeline |
| `AnimationTreeView.xaml.cs` | Árbol de animaciones |

## Funcionalidades

### Keyframes Basados en Estados
- Cada keyframe referencia un `StateSave` existente
- Soporte para states default y categorized states
- Interpolación entre estados

### Easing Functions
- Lineal
- QuadInOut
- SineInOut
- Exponential
- Custom easing

### Eventos de Timeline
- Eventos en keyframes específicos
- Callbacks al alcanzar un frame

### Loops
- Loop infinito
- Loop N veces
- Ping-pong (reverse loop)

## Cómo Ampliar

### Suscribirse a Eventos

```csharp
public class MyPlugin : PriorityPlugin
{
    public override void StartUp()
    {
        // Suscribirse a eventos de animación
        this.StateAdd += HandleStateAdd;
        this.StateDelete += HandleStateDelete;
        this.StateRename += HandleStateRename;
    }
    
    private void HandleStateAdd(StateSave state)
    {
        // Actualizar animaciones que referencian este estado
    }
}
```

### Crear Animación Programáticamente

```csharp
// Crear animación
var animation = new AnimationViewModel(pluginViewModel);
animation.Name = "FadeInOut";
animation.Length = 2.0f; // 2 segundos

// Añadir keyframes
var keyframe1 = new AnimatedKeyframeViewModel(pluginViewModel);
keyframe1.Time = 0f;
keyframe1.StateName = "Visible";

var keyframe2 = new AnimatedKeyframeViewModel(pluginViewModel);
keyframe2.Time = 1.0f;
keyframe2.StateName = "Hidden";

var keyframe3 = new AnimatedKeyframeViewModel(pluginViewModel);
keyframe3.Time = 2.0f;
keyframe3.StateName = "Visible";

// Guardar animación
animation.Keyframes.Add(keyframe1);
animation.Keyframes.Add(keyframe2);
animation.Keyframes.Add(keyframe3);

// Serializar
elementAnimationsSave.Animations.Add(animation.Save());
FileManager.XmlSerialize(elementAnimationsSave, "Animations.ganx");
```

### Añadir Nuevo Tipo de Easing

```csharp
// Extension: añadir custom easing
public static class AnimationExtensions
{
    public static float CustomEasing(float ratio)
    {
        // Custom easing function
        return (float)Math.Sin(ratio * Math.PI / 2);
    }
}

// Registrar en FlatRedBall.InterpolationCore (si es posible)
// o usar en AnimationRuntime.ProcessRatio()
```

## Retos al Ampliar

### Serialización de Estados
- Estados referenciados por nombre (string)
- Cambio de nombre de estado rompe animación
- **Recomendación**: Usar IDs únicos o manejar rename events

### Performance en Timeline Grande
- Timeline con muchos keyframes puede ser lento
- WPF data binding overhead
- **Recomendación**: Virtualización de keyframes

### Preview de Animaciones
- Preview en tiempo real en el editor
- Sincronización conreloj de aplicación
- **Recomendación**: Usar framerates fijos para preview

### Memoria de ViewModels
- Cada animación tiene ViewModels
- Cache en AnimationCollectionViewModelManager
- **Recomendación**: Liberar ViewModels no usados

## Formato de Archivo

Las animaciones se guardan en archivos `.ganx`:

```xml
<ElementAnimationsSave>
  <Animations>
    <AnimationSave>
      <Name>FadeInOut</Name>
      <Length>2</Length>
      <Keyframes>
        <KeyframeSave>
          <Time>0</Time>
          <StateName>Visible</StateName>
          <InterpolationType>Linear</InterpolationType>
        </KeyframeSave>
        <KeyframeSave>
          <Time>1</Time>
          <StateName>Hidden</StateName>
          <InterpolationType>Linear</InterpolationType>
        </KeyframeSave>
        <KeyframeSave>
          <Time>2</Time>
          <StateName>Visible</StateName>
          <InterpolationType>Linear</InterpolationType>
        </KeyframeSave>
      </Keyframes>
    </AnimationSave>
  </Animations>
</ElementAnimationsSave>
```