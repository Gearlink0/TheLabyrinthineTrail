using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.AI;
using XRL.World.AI.GoalHandlers;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using LabyrinthineTrailExtensions;

namespace XRL
{
  [Serializable]
  public class LABYRINTHINETRAIL_RivalHunterSystem : IGameSystem
  {
    public int chance = 5;

    [NonSerialized]
    public Dictionary<string, bool> Visited = new Dictionary<string, bool>();

    public override void Register(XRLGame Game, IEventRegistrar Registrar) => Registrar.Register(ZoneActivatedEvent.ID);

    public override void Read(SerializationReader Reader) => this.Visited = Reader.ReadDictionary<string, bool>();

    public override void Write(SerializationWriter Writer) => Writer.Write<string, bool>(this.Visited);

    public override bool HandleEvent(ZoneActivatedEvent E)
    {
      this.CheckHunters(E.Zone);
      return base.HandleEvent(E);
    }

    public void CheckHunters(Zone zone)
    {
      if (zone.ZoneWorld != "JoppaWorld")
        return;
      if (zone.IsWorldMap())
        return;
      GameObject player = The.Player;
      // Make sure the player actually has a finding fork for the rivals to track
      if ( !player.HasCarriedItemWithTag("LABYRINTHINETRAIL_AttractsRivalHunters") )
        return;
      if (this.Visited.ContainsKey(zone.ZoneID))
        return;
      this.Visited.Add(zone.ZoneID, true);
      int ambushChance;
      /* Ambush chance is a property currently only the interiors of the mechs
      seem to use so this will basically always not skip the river hunter spawn. */
      if (The.ZoneManager.TryGetZoneProperty<int>(zone.ZoneID, "AmbushChance", out ambushChance) && !ambushChance.in100())
        return;
      int numHunters = LABYRINTHINETRAIL_RivalHunterSystem.GetNumHunters(player.Statistics["Level"].Value);
      if (numHunters <= 0)
        return;
      if (chance.in100())
        LABYRINTHINETRAIL_RivalHunterSystem.CreateHunters(numHunters, zone);
    }

    public static void CreateHunters(int numHunters, Zone zone)
    {
      bool placedHunters = false;
      if (The.Game.PlayerReputation.Get("Prey") >= 250)
        return;
      GameObject player = The.Player;
      for (int index = 1; index <= numHunters; ++index)
      {
        GameObject gameObject = GameObject.Create("LABYRINTHINETRAIL_BaseRivalHunter");
        gameObject.Brain.Allegiance.Hostile = true;
        gameObject.Brain.Hibernating = false;
        gameObject.Brain.AddOpinion<LABYRINTHINETRAIL_OpinionForkHunt>(The.Player);
        gameObject.Brain.PushGoal((GoalHandler) new Kill(player));

        gameObject.AwardXP(player.Stat("XP"));

        LABYRINTHINETRAIL_RivalHunterSystem.InvestHunterAPMP( gameObject );

        placedHunters |= LABYRINTHINETRAIL_RivalHunterSystem.PlaceHunter(zone, gameObject);
      }
      // Unsure if I want to alert players to the arival of the hunters.
      // if( placedHunters )
      //   Popup.Show("{{c|You hear the sharp ping of another fork. Someone else is here.}}");
    }

    public static bool PlaceHunter( Zone Zone, XRL.World.GameObject Hunter )
    {
      Cell cellToPlace = null;
      for (int index = 0; index < 100 && cellToPlace == null; ++index)
      {
        int x = Stat.Random(0, Zone.Width - 1);
        int y = Stat.Random(0, Zone.Height - 1);
        Cell cell = Zone.GetCell(x, y);
        if (cell.IsSpawnable() && cell.GetNavigationWeightFor(Hunter) < 30)
          cellToPlace = cell;
      }
      if (cellToPlace == null)
      {
        List<Cell> passableCells = Zone.GetPassableCells(Hunter);
        if (passableCells.IsNullOrEmpty<Cell>())
          return false;
        cellToPlace = passableCells.GetRandomElement<Cell>((System.Random) null);
      }
      cellToPlace.AddObject(Hunter);
      Hunter.MakeActive();
      return true;
    }

    public static int GetNumHunters(int level)
    {
      // Have one hunter for every five levels above level 10 the player is.
      return ( level - 10 ) / 5;
    }

    public static void InvestHunterAPMP( GameObject rivalHunter, int numMutations=2 )
    {
      // Have the rival invest all their AP into Agility
      if (rivalHunter.Statistics.ContainsKey("AP"))
        rivalHunter.Statistics["Agility"].BaseValue += rivalHunter.Statistics["AP"].BaseValue;
        rivalHunter.Statistics["AP"].BaseValue = 0;
      // Have the rival start with two random mutations that cost more than 1 point
      int levelPerMutation = 1;
      // Evenly distribute the rival's MP points
      if (rivalHunter.Statistics.ContainsKey("MP"))
        levelPerMutation = rivalHunter.Statistics["MP"].BaseValue / numMutations;
        rivalHunter.Statistics["MP"].BaseValue = 0;
      for (int addMutIndex = 0; addMutIndex < numMutations; ++addMutIndex)
      {
        List<MutationEntry> mutationList = new List<MutationEntry>((IEnumerable<MutationEntry>) rivalHunter.GetPart<Mutations>().GetMutatePool());
        mutationList.ShuffleInPlace<MutationEntry>();
        MutationEntry mutationToAdd = (MutationEntry) null;
        foreach (MutationEntry mutation in mutationList)
        {
          if (mutation.Category != null && !rivalHunter.HasPart(mutation.Class) && mutation.Cost > 1)
          {
            mutationToAdd = mutation;
            break;
          }
        }
        if (mutationToAdd != null)
          (rivalHunter.GetPart("Mutations") as Mutations).AddMutation(mutationToAdd.Class, levelPerMutation);
      }
    }
	}
}
