using System.Collections.Generic;
using XRL.Core;
using XRL.Language;
using XRL.UI;
using XRL.World;

namespace XRL.World.Parts
{
  public class LABYRINTHINETRAIL_FindingFork : IPoweredPart
  {
    private string World = "JoppaWorld";
    private List<string> MusicNoteTextStrings = new List<string>{ "&R!", "&r!", "&R\r", "&r\u000E" };

		public string Sound = "completion";
    public string TargetZone = "";
    public Cell TargetCell = null;
    public string RewardBlueprint = "LABYRINTHINETRAIL_SubdimensionalCask";

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
      return base.WantEvent(ID, cascade) || ID == GetInventoryActionsEvent.ID || ID == InventoryActionEvent.ID;
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
				E.Actor.UseEnergy(1000, "Item Finding Fork");
				E.RequestInterfaceExit();
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
      if( this.TargetZone.IsNullOrEmpty() )
        this.TargetZone = this.GenerateTargetZone();

      if( this.TargetCell == null && !this.TargetZone.IsNullOrEmpty() ) {
        this.TargetCell = this.GenerateTargetCell();
      }

			int num = this.ParentObject.QueryCharge();
			ActivePartStatus activePartStatus = this.GetActivePartStatus(true);
			if (activePartStatus == ActivePartStatus.Operational)
			{
				XRL.Messages.MessageQueue.AddPlayerMessage( "Ping with finding fork" );
        Cell currentCell = this.GetActivePartFirstSubject().CurrentCell;
        // TODO: Check if player is on world map. If they are, need to compare
        // zones not cells
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
          Popup.Show("The fork's tines probe the air and plunge into an unseen firmness. An object is excised.");
          List<Cell> emptyAdjacentCells = currentCell.GetEmptyAdjacentCells(1, 1);
          emptyAdjacentCells.RemoveRandomElement<Cell>()?.AddObject( this.RewardBlueprint );
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

    public string GenerateTargetZone()
    {
      // TODO: Randomly generate target zone
      return Zone.XYToID(this.World, 32, 32, 10);
    }

    public Cell GenerateTargetCell()
    {
      if( this.TargetZone.IsNullOrEmpty() )
        this.TargetZone = this.GenerateTargetZone();
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
