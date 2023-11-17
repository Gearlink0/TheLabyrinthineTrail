using ConsoleLib.Console;
using Genkit;
using System;
using System.Collections.Generic;
using XRL.Core;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_SubdimensionalVortex : IPart
  {
    public string DestinationZoneID;
    public String destinationZoneIDState = "LABYRINTHINETRAIL_VortexDest_ZoneID";
    public String repelledByTag = "LABYRINTHINETRAIL_RepelsSubdimensionalVortices";
    public Cell targetCell;
    // Choose a destination zone in JoppaWorld instead of the current world
    public String targetWorld = "JoppaWorld";

    [NonSerialized]
    private List<GameObject> Queue;
    [NonSerialized]
    private Dictionary<GameObject, Location2D> Locations;

    public override bool AllowStaticRegistration() => true;

    public override bool FireEvent(Event E)
    {
      if (E.ID == "DefendMeleeHit")
        this.ApplyVortex(E.GetGameObjectParameter("Attacker"));
      return base.FireEvent(E);
    }

    public override bool WantEvent(int ID, int cascade)
    {
      return base.WantEvent(ID, cascade)
      || ID == BeingConsumedEvent.ID
      || ID == BlocksRadarEvent.ID
      || ID == CanBeInvoluntarilyMovedEvent.ID
      || ID == EndTurnEvent.ID
      || ID == EnteredCellEvent.ID
      || ID == GetMaximumLiquidExposureEvent.ID
      || ID == InterruptAutowalkEvent.ID
      || ID == ObjectEnteredCellEvent.ID
      || ID == RealityStabilizeEvent.ID;
    }

    public override bool HandleEvent(BeingConsumedEvent E)
    {
      this.ApplyVortex(E.Actor);
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(BlocksRadarEvent E) => false;

    public override bool HandleEvent(CanBeInvoluntarilyMovedEvent E) => E.Object != this.ParentObject && base.HandleEvent(E);

		public override bool HandleEvent(EndTurnEvent E)
    {
      // Move the vortex
			if ( this.targetCell != null )
				this.ParentObject.SystemMoveTo( this.targetCell );

			Cell currentCell = this.ParentObject.CurrentCell;
      // Filter adjacentCells so it doesn't contain cells with or objects that
      // repel them
      Predicate<Cell> pred = cell => cell.GetFirstObjectWithTag( repelledByTag ) == null;
      this.targetCell = currentCell.GetRandomLocalAdjacentCell(pred);
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(EnteredCellEvent E)
    {
      if (!E.Cell.IsGraveyard())
      {
        foreach (GameObject GO in E.Cell.GetObjectsWithPartReadonly("Render"))
        {
          if (
            this.ParentObject.IsValid()
            && !this.ParentObject.IsInGraveyard()
            && this.ParentObject.CurrentCell == E.Cell
          )
            this.ApplyVortex(GO);
        }
      }
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(GetMaximumLiquidExposureEvent E)
    {
      E.PercentageReduction = 100;
      return false;
    }

    public override bool HandleEvent(InterruptAutowalkEvent E) => false;

    public override bool HandleEvent(ObjectEnteredCellEvent E)
    {
      if (!E.Cell.IsGraveyard())
        this.ApplyVortex(E.Object);
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(RealityStabilizeEvent E)
    {
      if (!E.Check(true))
        return base.HandleEvent(E);
      this.ParentObject.ParticleBlip("&m-");
      this.DidX("collapse", "under the pressure of normality", ColorAsBadFor: this.ParentObject);
      this.ParentObject.Destroy();
      return false;
    }

    public bool ApplyVortex( GameObject GO )
    {
      if (this.ParentObject == GO || !SpaceTimeVortex.IsValidTarget(GO))
        return false;

      if (this.DestinationZoneID == null && !SpaceTimeVortex.ObjectCallsForExplicitTracking(GO))
      {
        IComponent<GameObject>.XDidYToZ(
          GO,
          "are",
          "sucked into",
          this.ParentObject,
          EndMark: "!",
          ColorAsBadFor: GO,
          IndefiniteObject: true,
          DescribeSubjectDirection: true
        );
        Location2D location = GO.CurrentCell?.location;
        if (location != (Location2D) null)
        {
          if (this.Queue == null)
            this.Queue = new List<GameObject>();
          if (this.Locations == null)
            this.Locations = new Dictionary<GameObject, Location2D>();
          this.Queue.Add(GO);
          this.Locations[GO] = location;
          GO.RemoveFromContext();
          GO.MakeInactive();
        }
        else
          GO.Obliterate();
      }
      else
      {
        this.RequireDestinationZone();
        Cell destinationCellFor = SpaceTimeVortex.GetDestinationCellFor(
          this.DestinationZoneID,
          GO,
          this.ParentObject.CurrentCell
        );
        if (destinationCellFor == null || !GO.FireEvent(Event.New("SpaceTimeVortexContact", "Object", (object) this.ParentObject, "DestinationCell", (object) destinationCellFor)))
          return false;
        if (GO.IsPlayerLed() && !GO.IsTrifling)
          Popup.Show("Your companion, " + GO.GetDisplayName(Short: true, WithIndefiniteArticle: true) + "," + GO.GetVerb("have") + " been sucked into " + this.ParentObject.t() + " " + The.Player.DescribeDirectionToward(this.ParentObject) + "!");
        else
          IComponent<GameObject>.XDidYToZ(GO, "are", "sucked into", this.ParentObject, EndMark: "!", ColorAsBadFor: GO, IndefiniteObject: true, DescribeSubjectDirection: true);
        SpaceTimeVortex.Teleport(GO, destinationCellFor, this.ParentObject);
      }
      return true;
    }

    public void RequireDestinationZone()
    {
      // if (!this.DestinationZoneID.IsNullOrEmpty())
      //   return;
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

      if (this.Queue == null || this.Locations == null)
        return;

      if (!this.DestinationZoneID.IsNullOrEmpty())
      {
        Zone targetZone = The.ZoneManager.GetZone( this.DestinationZoneID );
        foreach (GameObject gameObject in this.Queue)
        {
          try
          {
            if (GameObject.Validate(gameObject) && !gameObject.HasContext())
            {
              Location2D location = this.Locations[gameObject];
              Cell destinationCellFor = SpaceTimeVortex.GetDestinationCellFor(this.DestinationZoneID, gameObject, location);
              if (destinationCellFor != null)
              {

                SpaceTimeVortex.Teleport(gameObject, destinationCellFor, this.ParentObject, targetZone?.GetCell(location), false);
                gameObject.MakeActive();
              }
              else
                gameObject.Obliterate();
            }
          }
          catch (Exception ex)
          {
            MetricsManager.LogException("Space-time vortex queue unload", ex);
          }
        }
      }
      else
      {
        foreach (GameObject Object in this.Queue)
        {
          if (GameObject.Validate(Object) && !Object.HasContext())
            Object.Obliterate();
        }
      }
      this.Queue = (List<GameObject>) null;
      this.Locations = (Dictionary<GameObject, Location2D>) null;
    }

    public override bool Render(RenderEvent E)
    {
      if (Stat.RandomCosmetic(1, 60) < 3)
      {
        string particleColor = "&m";
        switch (Stat.RandomCosmetic(0, 4))
        {
          case 0:
            particleColor = "&m";
            break;
          case 1:
            particleColor = "&M";
            break;
          case 2:
            particleColor = "&y";
            break;
          case 3:
            particleColor = "&Y";
            break;
          case 4:
            particleColor = "&O";
            break;
        }
        Cell currentCell = this.ParentObject.CurrentCell;
        The.ParticleManager.AddRadial(
          particleColor + "ù",
          (float) currentCell.X,
          (float) currentCell.Y,
          (float) Stat.RandomCosmetic(0, 7),
          (float) Stat.RandomCosmetic(5, 10),
          0.01f * (float) Stat.RandomCosmetic(4, 6),
          -0.01f * (float) Stat.RandomCosmetic(3, 7)
        );
      }
      switch (Stat.RandomCosmetic(0, 4))
      {
        case 0:
          E.ColorString = "&m^k";
          break;
        case 1:
          E.ColorString = "&M^k";
          break;
        case 2:
          E.ColorString = "&y^k";
          break;
        case 3:
          E.ColorString = "&Y^k";
          break;
        case 4:
          E.ColorString = "&O^k";
          break;
      }
      switch (Stat.RandomCosmetic(0, 3))
      {
        case 0:
          E.RenderString = "\t";
          break;
        case 1:
          E.RenderString = "é";
          break;
        case 2:
          E.RenderString = "\u0015";
          break;
        case 3:
          E.RenderString = "\u000F";
          break;
      }
      return true;
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
