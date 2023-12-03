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
		public string Sound = "completion";
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

    public string ActivatedAbilityName = "Ping";
    public string ActivatedAbilityCommandNamePrefix = "ActivateFindingFork";
    [FieldSaveVersion(236)]
    public string ActivatedAbilityClass;
    [FieldSaveVersion(236)]
    public string ActivatedAbilityIcon = "û";
    [FieldSaveVersion(236)]
    public Guid ActivatedAbilityID;

    public int LastDist = -1;

    public int Coldest = 25000;
    public int Colder = 5000;
    public int Cold = 1000;
    public int Mild = 250;
    public int Hot = 50;
    public int Hotter = 10;
    public int Hottest = 5;

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
        this.TargetCell = GenerateTargetCell();
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(CommandEvent E)
    {
      if (
        E.Command == this.GetActivatedAbilityCommandName()
        && E.Actor == this.GetActivePartFirstSubject()
        && AttemptPing( E )
      )
      {
        E.Actor.UseEnergy(1000, "Item Finding Fork");
				if (!string.IsNullOrEmpty(this.Sound))
          E.Actor.PlayWorldSound(this.Sound);
      }
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(EquippedEvent E)
    {
      E.Actor.RegisterPartEvent((IPart) this, this.GetActivatedAbilityCommandName());
      this.SetUpActivatedAbility(E.Actor);
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(UnequippedEvent E)
    {
      E.Actor.UnregisterPartEvent((IPart) this, this.GetActivatedAbilityCommandName());
      E.Actor.RemoveActivatedAbility(ref this.ActivatedAbilityID);
      return base.HandleEvent(E);
    }

		public override bool HandleEvent(GetInventoryActionsEvent E)
		{
			if ( this.IsObjectActivePartSubject(IComponent<GameObject>.ThePlayer) )
        E.AddAction("Activate", "ping", "LabyrinthineTrail_ActivateFindingFork", Key: 'p', Default: 100);
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(InventoryActionEvent E)
		{
			if (E.Command == "LabyrinthineTrail_ActivateFindingFork" && AttemptPing( E ) )
      {
        E.RequestInterfaceExit();
				E.Actor.UseEnergy(1000, "Item Finding Fork");
				if (!string.IsNullOrEmpty(this.Sound))
          E.Actor.PlayWorldSound(this.Sound);
      }
			return base.HandleEvent(E);
		}

    public string GetDistReportString( int CurrentDist )
    {
      string returnString = this.ParentObject.The + this.ParentObject.ShortDisplayName + this.ParentObject.GetVerb("hum") + ". It is";
      if( CurrentDist >= Coldest )
        returnString += " almost silent.";
      else if( CurrentDist < Coldest && CurrentDist >= Colder )
        returnString += " a whisper.";
      else if( CurrentDist < Colder && CurrentDist >= Cold )
        returnString += " quiet.";
      else if( CurrentDist < Cold && CurrentDist >= Mild )
        returnString += " a steady tone.";
      else if( CurrentDist < Mild && CurrentDist >= Hot )
        returnString += " loud.";
      else if( CurrentDist < Hot && CurrentDist >= Hotter )
        returnString += " piercing.";
      else if( CurrentDist < Hotter && CurrentDist >= Hottest )
        returnString += " booming.";
      else if( CurrentDist < Hottest )
        returnString += " a triumphant blast.";
      else
        returnString += " wavering in volume.";

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
      XRL.Messages.MessageQueue.AddPlayerMessage( this.TargetZone );

			int num = this.ParentObject.QueryCharge();
			ActivePartStatus activePartStatus = this.GetActivePartStatus(true);
			if (activePartStatus == ActivePartStatus.Operational)
			{
				XRL.Messages.MessageQueue.AddPlayerMessage( "Ping with finding fork" );
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
        XRL.Messages.MessageQueue.AddPlayerMessage( currentDist.ToString() );

        string SuccessMessage = "The fork pings";
        if( this.LastDist < 0 )
          SuccessMessage =  this.ParentObject.The + this.ParentObject.ShortDisplayName + this.ParentObject.GetVerb("quiver") + " and attunes to " + Grammar.MakePossessive( this.ParentObject.It ) + " partner's signal.";
        else
          SuccessMessage = this.GetDistReportString( currentDist );
        this.LastDist = currentDist;
        // if (!string.IsNullOrEmpty(SuccessMessage) && this.IsPlayer())
        Popup.Show(SuccessMessage);
        this.PingSoundwave( currentDist );

        if (currentDist < Hottest)
        {
          if( GoesToHideaway )
            this.CheckTeleport();
          else
          {
            if( !this.GaveReward )
            {
              Popup.Show("The fork's tines probe the air and plunge into an unseen firmness. An object is excised.");
              List<Cell> emptyAdjacentCells = currentCell.GetEmptyAdjacentCells(1, 1);
              emptyAdjacentCells.RemoveRandomElement<Cell>()?.AddObject( this.RewardBlueprint );
              this.GaveReward = true;
              // Make all rival hunters in the area hostile to the player
              Predicate<GameObject> pred = item => item.HasTag("LABYRINTHINETRAIL_AttacksForksUsers");
              foreach ( GameObject rival in currentCell.ParentZone.FindObjects( pred ) )
              {
                rival.pBrain.Hostile = true;
                rival.pBrain.Hibernating = false;
                rival.pBrain.SetFeeling(The.Player, -100);
                rival.pBrain.PushGoal((GoalHandler) new Kill(The.Player));
              }
            }
            else
              Popup.Show("The fork's tines probe the air and touch an unseen firmness, but its home is now vacant.");
          }
        }

				return true;
			}
			XRL.Messages.MessageQueue.AddPlayerMessage( GetStatusSummary( activePartStatus ) );
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
        if (this.GetActivePartFirstSubject().CurrentZone.ZoneID.StartsWith("JoppaWorld.") && Object.IsPlayer())
          this.TeleportToHideaway(Object);
      }
      return true;
    }

    public void TeleportToHideaway(GameObject Object)
    {
      // bool flag = The.ZoneManager.IsZoneBuilt(this.ClamSystem.ClamWorldId);
      bool zoneBuilt  = The.ZoneManager.IsZoneBuilt("LABYRINTHINETRAIL_Hideaway.40.12.1.1.10");
      // Zone clamZone = this.ClamSystem.GetClamZone();
      Zone hideawayZone = The.ZoneManager.GetZone("LABYRINTHINETRAIL_Hideaway.40.12.1.1.10");
      // Cell currentCell = this.GetLinkedClam(clamZone)?.CurrentCell;
      Cell targetCell = hideawayZone.GetEmptyReachableCells().RemoveRandomElement<Cell>();
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
        this.TeleportToHideaway(Object);
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
      if ( !this.TargetZoneState.IsNullOrEmpty() )
        return XRLCore.Core.Game.GetStringGameState( this.TargetZoneState );
      else if (TargetZoneX >= 0 && TargetZoneY >= 0)
        return Zone.XYToID(this.World, TargetZoneX, TargetZoneY, 10);
      else {
        // TODO: Make this configurable and find a better way to get the dimensions
        // of the world
        int RandX = Stat.Random( 0, 80 * 3 );
        int RandY = Stat.Random( 0, 25 * 3 );

        XRL.Messages.MessageQueue.AddPlayerMessage( RandX.ToString() );
        XRL.Messages.MessageQueue.AddPlayerMessage( RandY.ToString() );

        return Zone.XYToID(
          this.World,
          RandX,
          RandY,
          10
        );
      }
    }

    public Cell GenerateTargetCell()
    {
      if ( !this.TargetZoneState.IsNullOrEmpty() )
        return Cell.FromAddress( XRLCore.Core.Game.GetStringGameState( this.TargetCellState ) );
      else if (TargetCellX >= 0 && TargetCellY >= 0)
        return The.ZoneManager.GetZone(this.TargetZone).GetCell(TargetCellX, TargetCellY);
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

    public void SetUpActivatedAbility(GameObject Who = null)
    {
      if (Who == null)
        Who = this.GetActivePartFirstSubject();
      if (Who == null)
        return;
      if (this.ActivatedAbilityID == Guid.Empty)
        this.ActivatedAbilityID = Who.AddActivatedAbility(
          this.ActivatedAbilityName,
          this.GetActivatedAbilityCommandName(),
          this.ActivatedAbilityClass ?? (Who == this.ParentObject ? "Maneuvers" : "Items"),
          Icon: this.ActivatedAbilityIcon
        );
      else
        this.SyncActivatedAbilityName(Who);
    }

    public void SyncActivatedAbilityName(GameObject Who = null)
    {
      if (this.ActivatedAbilityID == Guid.Empty)
        return;
      if (Who == null)
        Who = this.GetActivePartFirstSubject();
      if (Who == null)
        return;
      Who.SetActivatedAbilityDisplayName(this.ActivatedAbilityID, this.ActivatedAbilityName);
    }

    public string GetActivatedAbilityCommandName() => this.ActivatedAbilityCommandNamePrefix + this.ParentObject.id;
  }
}
