using ConsoleLib.Console;
using Genkit;
using System;
using System.Collections.Generic;
using XRL.Core;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_SubdimensionalVortex : SpaceTimeVortex
  {
		public Cell targetCell;

    public String targetWorld = "JoppaWorld";

    public String destinationZoneIDState = "LABYRINTHINETRAIL_VortexDest_ZoneID";

    public String repelledByTag = "LABYRINTHINETRAIL_RepelsSubdimensionalVortices";

		public override bool SpaceTimeAnomalyStationary() => true;

		public override bool HandleEvent(EndTurnEvent E)
    {
      this.SpaceTimeAnomalyPeriodicEvents();
      return base.HandleEvent(E);
    }

    public void SpaceTimeAnomalyPeriodicEvents()
		{
			// Move the vortex
			if ( this.targetCell != null )
				this.ParentObject.SystemMoveTo( this.targetCell );

			Cell currentCell = this.ParentObject.CurrentCell;
      // Filter adjacentCells so it doesn't contain cells with or objects that
      // repel them
      Predicate<Cell> pred = cell => cell.GetFirstObjectWithTag( repelledByTag ) == null;
      this.targetCell = currentCell.GetRandomLocalAdjacentCell(pred);
		}

    public override bool FireEvent(Event E)
    {
      if (E.ID == "DefendMeleeHit")
        this.RequireDestinationZone();
      return base.FireEvent(E);
    }

    public override bool HandleEvent(BeingConsumedEvent E)
    {
      this.RequireDestinationZone();
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(EnteredCellEvent E)
    {
      this.RequireDestinationZone();
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(ObjectEnteredCellEvent E)
    {
      this.RequireDestinationZone();
      return base.HandleEvent(E);
    }

    public void RequireDestinationZone()
    {
      // Choose a destination zone in JoppaWorld isntead of the current world
      if (!this.DestinationZoneID.IsNullOrEmpty())
        return;
      if (!this.destinationZoneIDState.IsNullOrEmpty())
      {
        string zoneIDFromState = XRLCore.Core.Game.GetStringGameState( this.destinationZoneIDState );
        if (!zoneIDFromState.IsNullOrEmpty())
          this.DestinationZoneID = zoneIDFromState;
        else
        {
          string newDestZoneID = SpaceTimeVortex.GetRandomDestinationZoneID( this.targetWorld );
          XRLCore.Core.Game.SetStringGameState( this.destinationZoneIDState, newDestZoneID);
          this.DestinationZoneID = newDestZoneID;
        }
      }
      else
        this.DestinationZoneID = SpaceTimeVortex.GetRandomDestinationZoneID( this.targetWorld );
    }

		public override bool FinalRender(RenderEvent E, bool bAlt)
    {
      if (this.targetCell != null)
        E.WantsToPaint = true;
      return true;
    }

    public override void OnPaint(ScreenBuffer buffer)
    {
      if (this.targetCell != null && this.targetCell.IsVisible())
      {
        buffer.Goto(this.targetCell.X, this.targetCell.Y);
        if (XRLCore.CurrentFrame >= 20)
        {
          buffer.Buffer[this.targetCell.X, this.targetCell.Y].TileForeground = The.Color.DarkRed;
          buffer.Buffer[this.targetCell.X, this.targetCell.Y].Detail = The.Color.DarkRed;
        }
        else
        {
          buffer.Buffer[this.targetCell.X, this.targetCell.Y].TileForeground = The.Color.Red;
          buffer.Buffer[this.targetCell.X, this.targetCell.Y].Detail = The.Color.Red;
        }
        if (XRLCore.CurrentFrame % 20 >= 10)
        {
          buffer.Buffer[this.targetCell.X, this.targetCell.Y].TileBackground = The.Color.Red;
          buffer.Buffer[this.targetCell.X, this.targetCell.Y].SetBackground('R');
        }
        buffer.Buffer[this.targetCell.X, this.targetCell.Y].SetForeground('r');
      }
      base.OnPaint(buffer);
    }
	}
}
