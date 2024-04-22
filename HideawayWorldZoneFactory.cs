using System;
using System.Collections.Generic;

namespace XRL.World.ZoneFactories
{
  public class LABYRINTHINETRAIL_HideawayWorldZoneFactory : IZoneFactory
  {
    public override Zone BuildZone(ZoneRequest Request)
    {
			MetricsManager.LogInfo("LABYRINTHINETRAIL_HideawayWorldZoneFactory building hideaway");
      if (Request.IsWorldZone)
      {
        Zone zone = new Zone(80, 25);
        zone.GetCells().ForEach((Action<Cell>) (c => c.AddObject("LABYRINTHINETRAIL_TerrainHideaway")));
        return zone;
      }
      Zone zone1 = new Zone(80, 25);
      zone1.ZoneID = Request.ZoneID;
      zone1.loadMap("HereticsHideaway.rpm");
      zone1.DisplayName = "Hideaway";
      zone1.GetCell(0, 0).RequireObject("ZoneMusic").SetStringProperty("Track", "Clam Dimension");
      zone1.Built = true;
      The.Game.ZoneManager.SetZoneProperty(Request.ZoneID, "SpecialUpMessage", (object) "You’re in a pocket dimension with no worldmap.");
      return zone1;
    }

    public override void AfterBuildZone(Zone zone, ZoneManager zoneManager)
    {
      zone.SetZoneProperty("inside", "1");
      ZoneManager.PaintWalls(zone);
      ZoneManager.PaintWater(zone);
    }
  }
}
