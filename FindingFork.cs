using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.AI;
using XRL.World.AI.GoalHandlers;

namespace XRL.World.Parts
{
  public class LABYRINTHINETRAIL_FindingFork : IPoweredPart
  {
    private string World = "JoppaWorld";
    private List<string> MusicNoteTextStrings = new List<string>{ "&R!", "&r!", "&R\r", "&r\u000E" };

    public bool GaveReward = false;
    public string ColderSound = "completion";
    public string WarmerSound = "startup";
    public string TargetZoneState = "";
    public string TargetCellState = "";
    public int TargetZoneX = -1;
    public int TargetZoneY = -1;
    public int TargetCellX = -1;
    public int TargetCellY = -1;
    public string TargetZone = "";
    public Cell TargetCell = null;
    public string RewardBlueprint = "LABYRINTHINETRAIL_SubdimensionalCask";
    public bool GoesToHideaway = false;

    public string ActivatedAbilityName = "ping";
    public string ActivatedAbilityCommandName = "LABYRINTHINETRAIL_ActivateFindingFork";
    [FieldSaveVersion(236)]
    public Guid ActivatedAbilityID;

    public int LastDist = -1;

    private const int UNKNOWNID = 0, COLDESTID = 1, COLDERID = 2, COLDID = 3,
      MILDID = 4, HOTID = 5, HOTTERID = 6, HOTTESTID = 7, FOUNDITID = 8 ;

    public int Coldest = 25000, Colder = 5000, Cold = 1000, Mild = 250, Hot = 50,
      Hotter = 10, Hottest = 5;

    public Dictionary<int, string> DistIDToReportMap = new Dictionary<int, string>{
      { UNKNOWNID, " wavering in volume." },
      { COLDESTID, " almost silent." },
      { COLDERID, " a whisper." },
      { COLDID, " quiet." },
      { MILDID, " a steady tone." },
      { HOTID, " loud." },
      { HOTTERID, " piercing." },
      { HOTTESTID,  " booming." },
      { FOUNDITID, " a triumphant blast." },
    };

    public Dictionary<int, float> DistIDToVolumeMap = new Dictionary<int, float>{
      { UNKNOWNID, 0.0f },
      { COLDESTID, 0.2f },
      { COLDERID, 0.3f },
      { COLDID, 0.4f },
      { MILDID, 0.5f },
      { HOTID, 0.6f },
      { HOTTERID, 0.7f },
      { HOTTESTID, 0.8f },
      { FOUNDITID, 1.0f },
    };

		public LABYRINTHINETRAIL_FindingFork()
		{
			this.ChargeUse = 0;
			this.MustBeUnderstood = true;
      this.WorksOnCarrier = true;
      this.WorksOnHolder = true;
		}

    public override bool WantEvent(int ID, int cascade)
    {
      return base.WantEvent(ID, cascade)
      || ID == ObjectCreatedEvent.ID
      || ID == EquippedEvent.ID
      || ID == UnequippedEvent.ID
      || ID == CommandEvent.ID
      || ID == GetInventoryActionsEvent.ID
      || ID == InventoryActionEvent.ID;
    }

    public override bool HandleEvent(ObjectCreatedEvent  E)
    {
      if( this.TargetZone.IsNullOrEmpty() )
        this.TargetZone = this.GenerateTargetZone();

      if( this.TargetCell == null )
        this.TargetCell = this.GenerateTargetCell();
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(CommandEvent E)
    {
      if (
        E.Command == this.ActivatedAbilityCommandName
        && E.Actor == this.GetActivePartFirstSubject()
        && AttemptPing( E )
      )
      {
        E.Actor.UseEnergy(1000, "Item Finding Fork");
      }
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(EquippedEvent E)
    {
      E.Actor.RegisterPartEvent((IPart) this, this.ActivatedAbilityCommandName);
      this.ActivatedAbilityID = E.Actor.AddActivatedAbility(
        Name: this.ActivatedAbilityName,
        Command: this.ActivatedAbilityCommandName,
        Class: "Items",
        IsWorldMapUsable: true
      );
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(UnequippedEvent E)
    {
      E.Actor.UnregisterPartEvent((IPart) this, this.ActivatedAbilityCommandName);
      E.Actor.RemoveActivatedAbility(ref this.ActivatedAbilityID);
      return base.HandleEvent(E);
    }

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if ( this.IsObjectActivePartSubject(IComponent<GameObject>.ThePlayer) )
        E.AddAction("Activate", "ping", "LABYRINTHINETRAIL_ActivateFindingFork", Key: 'p', Default: 100);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == "LABYRINTHINETRAIL_ActivateFindingFork" && AttemptPing( E ) )
      {
        E.RequestInterfaceExit();
				E.Actor.UseEnergy(1000, "Item Finding Fork");
      }
			return base.HandleEvent(E);
		}

    public int GetDistID( int CurrentDist )
    {
      if( CurrentDist >= Coldest )
        return COLDESTID;
      else if( CurrentDist < Coldest && CurrentDist >= Colder )
        return COLDERID;
      else if( CurrentDist < Colder && CurrentDist >= Cold )
        return COLDID;
      else if( CurrentDist < Cold && CurrentDist >= Mild )
        return MILDID;
      else if( CurrentDist < Mild && CurrentDist >= Hot )
        return HOTID;
      else if( CurrentDist < Hot && CurrentDist >= Hotter )
        return HOTTERID;
      else if( CurrentDist < Hotter && CurrentDist >= Hottest )
        return HOTTESTID;
      else if( CurrentDist < Hottest )
        return FOUNDITID;
      else
        return UNKNOWNID;
    }

    public string GetDistReportString( int CurrentDist )
    {
      string returnString = this.ParentObject.The + this.ParentObject.ShortDisplayName + this.ParentObject.GetVerb("hum") + ". It is";

      returnString += DistIDToReportMap[ GetDistID( CurrentDist ) ];

      if( this.LastDist < CurrentDist )
        returnString += " It is quieter than last time.";
      else if( this.LastDist > CurrentDist )
        returnString += " It is louder than last time.";
      else
        returnString += " It is the same volume as last time.";
      return returnString;
    }

		public bool AttemptPing(IEvent FromEvent = null)
		{

			int num = this.ParentObject.QueryCharge();
			ActivePartStatus activePartStatus = this.GetActivePartStatus(true);
			if (activePartStatus == ActivePartStatus.Operational)
			{
        GameObject Subject = this.GetActivePartFirstSubject();
        Cell currentCell = Subject.CurrentCell;
        if ( currentCell.ParentZone.IsWorldMap() )
        {
          // If the player is on the world map, get their last location on the surface
          string stringGameState = XRLCore.Core.Game.GetStringGameState("LastLocationOnSurface");
          if (!stringGameState.IsNullOrEmpty())
            currentCell = Cell.FromAddress(stringGameState);
          // If that isn't available, calculate the cell the player would be sent
          // to if they went down
          else
          {
            int x = currentCell.X;
            int y = currentCell.Y;
            int X = 1;
            int Y = 1;

            string ZoneID = this.World + "." + x.ToString() + "." + y.ToString();
            foreach (CellBlueprint cellBlueprint in The.ZoneManager.GetCellBlueprints(ZoneID))
            {
              if (!cellBlueprint.LandingZone.IsNullOrEmpty())
              {
                try
                {
                  string[] strArray = cellBlueprint.LandingZone.Split(',', StringSplitOptions.None);
                  X = Convert.ToInt32(strArray[0]);
                  Y = Convert.ToInt32(strArray[1]);
                  break;
                }
                catch {}
              }
            }
            Zone PullDownZone = The.ZoneManager.GetZone(this.World, x, y, X, Y, 10);
            currentCell = PullDownZone.GetPullDownLocation( Subject );
          }
        }

        int currentDist = currentCell.PathDistanceTo( this.TargetCell );

        string SuccessMessage = "The fork pings";
        if( this.LastDist < 0 )
          SuccessMessage =  this.ParentObject.The + this.ParentObject.ShortDisplayName + this.ParentObject.GetVerb("quiver") + " and attunes to " + Grammar.MakePossessive( this.ParentObject.it ) + " home's signal.";
        else
          SuccessMessage = this.GetDistReportString( currentDist );
        Popup.Show(SuccessMessage);
        this.PingSoundwave( currentDist );

        string SoundToUse = null;
        if( this.LastDist > currentDist )
          SoundToUse = this.WarmerSound;
        else
          SoundToUse = this.ColderSound;
        this.ParentObject.PlayWorldSound(SoundToUse, Volume:DistIDToVolumeMap[ GetDistID( currentDist ) ] );

        if (currentDist < Hottest)
        {
          if( GoesToHideaway )
            this.CheckTeleport();
          else
          {
            if( !this.GaveReward )
            {
              // Complete quests
              Popup.Show("The fork's tines probe the air and plunge into an unseen firmness. An object is excised.");
              List<Cell> emptyAdjacentCells = currentCell.GetEmptyAdjacentCells(1, 1);
              emptyAdjacentCells.RemoveRandomElement<Cell>()?.AddObject( this.RewardBlueprint );
              this.GaveReward = true;
              // Make all rival hunters in the area hostile to the player
              Predicate<GameObject> pred = item => item.HasTag("LABYRINTHINETRAIL_AttacksForksUsers");
              foreach ( GameObject rival in currentCell.ParentZone.FindObjects( pred ) )
              {
                rival.Brain.Allegiance.Hostile = true;
                rival.Brain.Hibernating = false;
                rival.Brain.AddOpinion<LABYRINTHINETRAIL_OpinionForkHunt>(The.Player);
                rival.Brain.PushGoal((GoalHandler) new Kill(The.Player));
              }
            }
            else
              Popup.Show("The fork's tines probe the air and touch an unseen firmness, but its home is now vacant.");
          }
        }

        this.LastDist = currentDist;

				return true;
			}
			switch (activePartStatus)
      {
				case ActivePartStatus.Broken:
	        Popup.ShowFail(this.ParentObject.Itis + " broken...");
	        break;
				case ActivePartStatus.Rusted:
          Popup.ShowFail(Grammar.MakePossessive(this.ParentObject.The + this.ParentObject.ShortDisplayName) + " activation button is rusted in place.");
          break;
        case ActivePartStatus.Booting:
          Popup.ShowFail(this.ParentObject.The + this.ParentObject.ShortDisplayName + this.ParentObject.Is + " still starting up.");
					break;
				case ActivePartStatus.Unpowered:
					if( num > 0 && this.ParentObject.QueryCharge() < num )
					{
						Popup.ShowFail(this.ParentObject.The + this.ParentObject.ShortDisplayName + this.ParentObject.GetVerb("hum") + " for a moment, then powers down. " + this.ParentObject.It + this.ParentObject.GetVerb("don't") + " have enough charge to function.");
            break;
          }
          Popup.ShowFail(this.ParentObject.The + this.ParentObject.ShortDisplayName + this.ParentObject.GetVerb("don't") + " have enough charge to function.");
          break;
				default:
          Popup.ShowFail("Nothing happens.");
          break;
			}
			return false;
		}

    public bool CheckTeleport(GameObject Object = null, Cell cell = null)
    {
      if (Object == null)
        Object = this.GetActivePartFirstSubject();
      if (cell == null)
        cell = Object.GetCurrentCell();
      if (cell.ParentZone.Built)
      {
        if (XRLCore.Core.Game.GetBooleanGameState("LABYRINTHINETRAIL_HideawayCollapsed"))
        {
          Popup.Show("The fork's tines prod an unseen firmness but it does not budge. The fork's home is no more.");
          return true;
        }
        if (this.GetActivePartFirstSubject().CurrentZone.ZoneID.StartsWith("JoppaWorld.") && Object.IsPlayer())
          LABYRINTHINETRAIL_FindingFork.TeleportToHideaway(Object);
      }
      return true;
    }

    public static void TeleportToHideaway(GameObject Object)
    {
      bool zoneBuilt  = The.ZoneManager.IsZoneBuilt("LABYRINTHINETRAIL_Hideaway.40.12.1.1.10");
      Zone hideawayZone = The.ZoneManager.GetZone("LABYRINTHINETRAIL_Hideaway.40.12.1.1.10");
      Predicate<Cell> pred = cell => cell.X < 20 || cell.X > 60;
      Cell targetCell = hideawayZone.GetEmptyReachableCells( pred ).RemoveRandomElement<Cell>();
      if (targetCell == null)
      {
        IComponent<GameObject>.AddPlayerMessage("You hear a shloop and then a hitch. Nothing happens.");
        if (hideawayZone.CountObjects((Predicate<GameObject>) (x => x.IsReal)) != 0)
          return;
        MetricsManager.LogError("Hideaway empty, zone " + (zoneBuilt ? "was" : "was not") + " built previously.");
        if (Popup.ShowYesNo("This zone didn't build properly, do you wish to rebuild it?") != DialogResult.Yes)
          return;
        The.ZoneManager.SuspendZone(hideawayZone);
        The.ZoneManager.DeleteZone(hideawayZone);
        LABYRINTHINETRAIL_FindingFork.TeleportToHideaway(Object);
      }
      else
      {
        XRLCore.Core.Game.SetStringGameState("LABYRINTHINETRAIL_EnteredHideaway_CellAddress", Object.CurrentCell.GetAddress());
        Object.pPhysics.PlayWorldSound("teleport_long", 1f);
        SoundManager.PlayMusic("Clam Dimension", CrossfadeDuration: 20f);
        Popup.Show("The fork's tines probe the air and catch on an unseen firmness. A seam in the world tears open and you fall through!");
        GiantClamProperties.Teleport(Object, targetCell, 'O');
      }
    }

    public string GenerateTargetZone()
    {
      string zoneID = null;
      if ( !this.TargetZoneState.IsNullOrEmpty() ) {
        zoneID = XRLCore.Core.Game.GetStringGameState( this.TargetZoneState );
        if(The.ZoneManager.GetZone(zoneID) == null) {
          MetricsManager.LogError( "LABYRINTHINETRAIL_FindingFork.GenerateTargetZone: Provided TargetZoneState did not contain a valid Zone ID." );
          zoneID = this.GenerateRandomZone();
        }
      }
      else if (TargetZoneX >= 0 && TargetZoneY >= 0) {
        zoneID = Zone.XYToID(this.World, TargetZoneX, TargetZoneY, 10);
        if(The.ZoneManager.GetZone(zoneID) == null) {
          MetricsManager.LogError( "LABYRINTHINETRAIL_FindingFork.GenerateTargetZone: Provided TargetZoneX and TargetCellY did not result in a valid Zone ID." );
          zoneID = this.GenerateRandomZone();
        }
      }
      else {
        zoneID = this.GenerateRandomZone();
      }
      return zoneID;
    }

    public string GenerateRandomZone()
    {
      // TODO: Make this configurable and find a better way to get the dimensions
      // of the world
      int RandX = Stat.Random( 0, 80 * 3 );
      int RandY = Stat.Random( 0, 25 * 3 );

      return Zone.XYToID(
        this.World,
        RandX,
        RandY,
        10
      );
    }

    public Cell GenerateTargetCell()
    {
      Cell cell = null;
      if ( !this.TargetCellState.IsNullOrEmpty() ) {
        cell = Cell.FromAddress( XRLCore.Core.Game.GetStringGameState( this.TargetCellState ) );
        if(cell == null) {
          MetricsManager.LogError( "LABYRINTHINETRAIL_FindingFork.GenerateTargetCell: Provided TargetCellState did not contain a valid Cell address." );
          cell = this.GenerateRandomCell();
        }
      }
      else if (TargetCellX >= 0 && TargetCellY >= 0 && !(The.ZoneManager.GetZone(this.TargetZone) == null)) {
        cell = The.ZoneManager.GetZone(this.TargetZone).GetCell(TargetCellX, TargetCellY);
        if(cell == null) {
          MetricsManager.LogError( "LABYRINTHINETRAIL_FindingFork.GenerateTargetCell: Provided TargetZoneX and TargetCellY did not result in a valid Cell." );
          cell = this.GenerateRandomCell();
        }
      }
      else
        cell = this.GenerateRandomCell();
      return cell;
    }

    public Cell GenerateRandomCell()
    {
      if( The.ZoneManager.GetZone(this.TargetZone) == null ){
        MetricsManager.LogError( "LABYRINTHINETRAIL_FindingFork.GenerateTargetCell: No zone prior to generating cell." );
        this.TargetZone = this.GenerateTargetZone();
      }
      return The.ZoneManager.GetZone(this.TargetZone).GetEmptyReachableCells().RemoveRandomElement<Cell>();
    }

    public void PingSoundwave( int CurrentDist )
    {
      Cell currentCell = this.GetActivePartFirstSubject().CurrentCell;
      if (currentCell == null || currentCell.ParentZone != XRLCore.Core.Game.ZoneManager.ActiveZone)
        return;

      // Determine number and variety of notes based on volume
      int maxNumIndex = 3;
      int maxVarIndex = 4;

      if( CurrentDist >= Coldest ) {
        maxNumIndex = 1;
        maxVarIndex = 1;
      }
      else if( CurrentDist < Coldest && CurrentDist >= Colder ) {
        maxNumIndex = 1;
        maxVarIndex = 2;
      }
      else if( CurrentDist < Colder && CurrentDist >= Cold ) {
        maxNumIndex = 2;
        maxVarIndex = 1;
      }
      else if( CurrentDist < Cold && CurrentDist >= Mild ) {
        maxNumIndex = 2;
        maxVarIndex = 2;
      }
      else if( CurrentDist < Mild && CurrentDist >= Hot ) {
        maxNumIndex = 2;
        maxVarIndex = 3;
      }
      else if( CurrentDist < Hot && CurrentDist >= Hotter ) {
        maxNumIndex = 3;
        maxVarIndex = 2;
      }
      else if( CurrentDist < Hotter && CurrentDist >= Hottest ) {
        maxNumIndex = 3;
        maxVarIndex = 3;
      }
      else if( CurrentDist < Hottest ) {
        maxNumIndex = 3;
        maxVarIndex = 4;
      }

      if( maxVarIndex > this.MusicNoteTextStrings.Count)
        maxVarIndex = this.MusicNoteTextStrings.Count;

      for (int numIndex = 0; numIndex < maxNumIndex; ++numIndex)
      {
        for (int varIndex = 0; varIndex < maxVarIndex; ++varIndex)
        {
          XRLCore.ParticleManager.AddRadial(
            Text: this.MusicNoteTextStrings[ varIndex ],
            x: (float) currentCell.X,
            y: (float) currentCell.Y,
            r: (float) XRL.Rules.Stat.RandomCosmetic(0, 7),
            d: (float) XRL.Rules.Stat.RandomCosmetic(1, 1),
            rdel: 0.015f * (float) XRL.Rules.Stat.RandomCosmetic(8, 12),
            ddel: (float) (0.3 + 0.05 * (double) XRL.Rules.Stat.RandomCosmetic(1, 3)),
            Life: 40
          );
        }
      }
    }

  }
}
