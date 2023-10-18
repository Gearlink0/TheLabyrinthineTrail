using Genkit;
using Qud.API;
using XRL.Core;
using XRL.World;
using XRL.World.Parts;
using XRL.World;

// namespace YourMod.YourNamespace
// namespace AERONAUTS
namespace XRL.World.WorldBuilders
{
  [JoppaWorldBuilderExtension]
  public class LABYRINTHINETRAIL_QuestBuilderExtension : IJoppaWorldBuilderExtension
  {
    private string World = "JoppaWorld";

    public override void OnAfterBuild(JoppaWorldBuilder builder)
    {
      // The game calls this method before JoppaWorld generation takes place.
      // JoppaWorld generation includes the creation of lairs, historic ruins, villages, and more.
      MetricsManager.LogInfo("LABYRINTHINETRAIL_QuestBuilderExtension running");

      // Pick a random ruin zone to be the target of the first quest
      Location2D location = builder.popMutableLocationOfTerrain("Ruins");
			string zoneID = Zone.XYToID(this.World, location.x, location.y, 10);
      XRLCore.Core.Game.SetStringGameState("LABYRINTHINETRAIL_FirstQuest_ZoneID", zoneID);
      MetricsManager.LogInfo( zoneID );

      // Pick a random rainbow wood zone to be the target of the second quest
      location = builder.popMutableLocationOfTerrain("Fungal");
			zoneID = Zone.XYToID(this.World, location.x, location.y, 10);
      XRLCore.Core.Game.SetStringGameState("LABYRINTHINETRAIL_SecondQuest_ZoneID", zoneID);
      MetricsManager.LogInfo( zoneID );

      // Pick a random baroque ruin zone to be the target of the final quest
      location = builder.popMutableLocationOfTerrain("BaroqueRuins");
			zoneID = Zone.XYToID(this.World, location.x, location.y, 10);
      XRLCore.Core.Game.SetStringGameState("LABYRINTHINETRAIL_FinalQuest_ZoneID", zoneID);
			MetricsManager.LogInfo( zoneID );
    }
	}
}
