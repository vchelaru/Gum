using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GameUiSamples.Screens.FrbClicker;
internal class BuildingDefs
{
    public static readonly BuildingDef _3DRendering =
        CreateBuilding("3D Rendering Software", 10, 1.2m, .1m,
        "Render a red sphere in a 3D application. It's slow, but can be automated.",
        "redball.png");
    public static readonly BuildingDef WindowsApplication =
        CreateBuilding("Windows Application", 100, 1.2m, 1m,
        "Create a windows application which displays red balls",
        "WinformsApp.png");
    public static readonly BuildingDef DirectXGame = 
        CreateBuilding("DirectX/C++ Game", 500, 1.2m, 8m,
            "Games can run at 60 fps, rendering lots of red balls",
            "DirectX.png");
    public static readonly BuildingDef DllEngine = 
        CreateBuilding("Game Engine .dll", 3000, 1.2m, 20m,
            "Your game engine can be used by others who create their own games to render red balls",
            "dllFile.png");
    public static readonly BuildingDef EditorEngine = 
        CreateBuilding("Game Engine with Editor", 10000, 1.2m, 50m,
            "Even users who don't know how to code can now create games with red balls",
            "Editor.png");
    public static readonly BuildingDef ChatRoom =
        CreateBuilding("Chat Room", 75000, 1.2m, 100m,
            "Chat rooms help grow your community, so more people make apps and games to render red balls",
            "Discord.png");

    public static List<BuildingDef> AllBuildings = new List<BuildingDef>
    {
        _3DRendering,
        WindowsApplication,
        DirectXGame,
        DllEngine,
        EditorEngine,
        ChatRoom
    };

    static BuildingDef CreateBuilding(string name, BigInteger initialCost, 
        decimal costScale, decimal earnings, string description,
        string icon) =>
        new BuildingDef
        {
            Name = name,
            InitialCost = initialCost,
            CostScaleMultiplier = costScale,
            EarningsPerSecond = earnings,
            Description = description,
            Icon = icon
        };
}

public class BuildingDef
{
    public string Name { get; set; }
    public BigInteger InitialCost { get; set; }
    public decimal CostScaleMultiplier { get; set; }

    public string Icon { get; set; }

    public decimal EarningsPerSecond { get; set; }

    public string Description { get; set; }
}