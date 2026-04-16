# EventOutputPlugin (Plugin de Exportación de Eventos)

## Descripción

EventOutputPlugin exporta eventos de modificación del proyecto a archivos JSON para herramientas externas o sistemas de control de versiones. Permite rastrear cambios como adiciones, eliminaciones y renombrados de elementos en tiempo real.

## Diagrama de Relaciones

```mermaid
graph TB
    EventOutputPlugin[EventOutputPlugin]
    
    EventOutputPlugin --> Gum[Gum Tool]
    EventOutputPlugin --> Newtonsoft.Json
    EventOutputPlugin --> System.ComponentModel.Composition
    
    subgraph PluginBase
        PluginBase[PluginBase]
    end
    
    EventOutputPlugin -.->|Extends| PluginBase
    
    subgraph Events
        ProjectLoad[ProjectLoad]
        ElementAdd[ElementAdd]
        ElementDelete[ElementDelete]
        ElementRename[ElementRename]
        InstanceAdd[InstanceAdd]
        InstanceDelete[InstanceDelete]
        InstanceRename[InstanceRename]
        StateRename[StateRename]
        CategoryRename[CategoryRename]
    end
    
    EventOutputPlugin --> Events
    
    subgraph Managers
        ExportEventFileManager[ExportEventFileManager]
    end
    
    EventOutputPlugin --> ExportEventFileManager
    
    subgraph Models
        ExportedEvent[ExportedEvent]
        ExportedEventCollection[ExportedEventCollection]
    end
    
    ExportEventFileManager --> Models
    
    subgraph Output
        JSONFiles[JSON Files]
        VersionControl[Version Control Integration]
        ExternalTools[External Tool Integration]
    end
    
    ExportEventFileManager --> JSONFiles
    JSONFiles --> VersionControl
    JSONFiles --> ExternalTools
```

## Tecnología

| Aspecto | Valor |
|---------|-------|
| **Framework** | .NET Standard 2.0 |
| **Lenguaje** | C# 7.3+ |
| **Serialización** | Newtonsoft.Json |
| **Dependencias** | Gum.csproj |

## Punto de Entrada

| Archivo | Clase | Ubicación |
|---------|-------|-----------|
| `MainEventOutputPlugin.cs` | `MainEventOutputPlugin` | `Gum/EventOutputPlugin/` |

```csharp
[Export(typeof(PluginBase))]
public class MainEventOutputPlugin : PriorityPlugin
{
    public override void StartUp()
    {
        // Suscribirse a todos los eventos de modificación
        this.ProjectLoad += (project) => 
            ExportEventFileManager.DeleteOldEventFiles();
            
        this.ElementAdd += (element) => 
            ExportEventFileManager.ExportEvent("ElementAdd", element.Name);
            
        this.ElementDelete += (element) => 
            ExportEventFileManager.ExportEvent("ElementDelete", element.Name);
            
        this.ElementRename += (element, oldName) => 
            ExportEventFileManager.ExportEvent("ElementRename", element.Name, oldName);
            
        // ... más suscripciones
    }
}
```

## Modelos de Datos

### ExportedEvent

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `EventType` | string | Tipo de evento ("ElementAdd", "ElementDelete", etc.) |
| `ObjectName` | string | Nombre del objeto |
| `OldValue` | string | Valor anterior (para renombrados) |
| `Timestamp` | DateTime | Cuándo ocurrió |
| `Details` | Dictionary | Detalles adicionales |

### ExportedEventCollection

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Events` | List<ExportedEvent> | Lista de eventos |
| `ProjectName` | string | Nombre del proyecto |

## Formato de Salida

```json
{
  "projectName": "MyGame",
  "events": [
    {
      "eventType": "ElementAdd",
      "objectName": "Button",
      "objectType": "Component",
      "timestamp": "2024-01-15T10:30:00Z",
      "details": {
        "baseType": "Container",
        "fileName": "Components/Button.gucx"
      }
    },
    {
      "eventType": "InstanceAdd",
      "objectName": "Button1",
      "objectType": "Instance",
      "timestamp": "2024-01-15T10:31:00Z",
      "details": {
        "parentElement": "MainMenu",
        "instanceType": "Button"
      }
    },
    {
      "eventType": "ElementRename",
      "objectName": "NewButtonName",
      "oldValue": "OldButtonName",
      "timestamp": "2024-01-15T10:32:00Z"
    }
  ]
}
```

## Cómo Ampliar

### Añadir Nuevo Tipo de Evento

```csharp
// 1. Definir el nuevo tipo de evento
public static class GumEventTypes
{
    public const string ElementAdd = "ElementAdd";
    public const string ElementDelete = "ElementDelete";
    public const string ElementRename = "ElementRename";
    public const string MyCustomEvent = "MyCustomEvent"; // Nuevo
}

// 2. Suscribirse y exportar
this.VariableSet += (element, instance, variable, oldValue, newValue) =>
{
    if (variable.Name == "MyCustomVariable")
    {
        ExportEventFileManager.ExportEvent(
            GumEventTypes.MyCustomEvent,
            element.Name,
            details: new Dictionary<string, object>
            {
                ["variable"] = variable.Name,
                ["oldValue"] = oldValue,
                ["newValue"] = newValue
            }
        );
    }
};
```

### Consumir Eventos Externamente

```python
# Python script para consumir eventos
import json
import os
from pathlib import Path

class GumEventWatcher:
    def __init__(self, project_path):
        self.events_path = Path(project_path) / ".events"
        
    def get_events(self):
        events = []
        for event_file in self.events_path.glob("*.json"):
            with open(event_file) as f:
                events.append(json.load(f))
        return events
    
    def sync_to_database(self):
        events = self.get_events()
        for event in events:
            # Sincronizar con base de datos externa
            self.db.insert_event(event)

# Uso
watcher = GumEventWatcher("path/to/MyProject.gumx")
watcher.sync_to_database()
```

### Integración con CI/CD

```yaml
# .github/workflows/gum-events.yml
name: Track Gum Changes

on:
  push:
    paths:
      - '**/.events/*.json'

jobs:
  process-events:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Process Gum Events
        run: |
          python3 scripts/process_gum_events.py \
            --project MyProject.gumx \
            --output report.md
      
      - name: Upload Report
        uses: actions/upload-artifact@v3
        with:
          name: gum-event-report
          path: report.md
```

## Retos al Ampliar

### Volumen de Eventos
- Muchos eventos puede generar archivos grandes
- Falta de limpieza automática
- **Recomendación**: Implementar rotación de archivos

### Atomicidad
- Eventos múltiples durante una operación
- Orden importante pero no garantizado
- **Recomendación**: Usar timestamps y batching

### Archivos Huérfanos
- Eventos de proyectos eliminados
- Archivos temporales no limpiados
- **Recomendación**: ProjectLoad limpia archivos antiguos

### Conflicto de Nombres
- Múltiples plugins pueden querer archivos
-命名 collisions
- **Recomendación**: Usar subdirectorios por plugin