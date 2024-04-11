using System;
using XRL.Core;
using XRL.UI;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_CollapseHideaway : IPart
  {
		public String targetWorld = "JoppaWorld";

    public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade);

    public override void Register(GameObject Object)
    {
      Object.RegisterPartEvent((IPart) this, "BeginBeingTaken");
      Object.RegisterPartEvent((IPart) this, "Taken");
      Object.RegisterPartEvent((IPart) this, "Equipped");
    	base.Register(Object);
    }

		public override bool FireEvent(Event E)
    {
      if (E.ID == "BeginBeingTaken")
      {
        GameObject TakingObject = E.GetGameObjectParameter("TakingObject");
        if (
					TakingObject != null
					&& TakingObject.IsPlayer()
					&& The.PlayerCell?.ParentZone?.ZoneWorld == "LABYRINTHINETRAIL_Hideaway"
				)
        {
          if (Popup.ShowYesNoCancel("You feel a weight around this mask. It has become a spine of this bubble and moving it risks a collapse. Are you sure you want to take it?") != DialogResult.Yes)
            return false;
        }
      }
      else if (E.ID == "Taken" || E.ID == "Equipped")
      {
				GameObject TakingObject = E.GetGameObjectParameter("TakingObject") ?? E.GetGameObjectParameter("EquippingObject");
        if (
					TakingObject != null
					&& TakingObject.IsPlayer()
					&& The.PlayerCell?.ParentZone?.ZoneWorld == "LABYRINTHINETRAIL_Hideaway"
				)
				{
					string DestZoneID = SpaceTimeVortex.GetRandomDestinationZoneID( this.targetWorld );
					Cell destinationCellFor = SpaceTimeVortex.GetDestinationCellFor(
	          DestZoneID,
	          TakingObject,
	          this.ParentObject.CurrentCell
	        );
          XRLCore.Core.Game.SetBooleanGameState("LABYRINTHINETRAIL_HideawayCollapsed", true);
          Popup.Show("The subdimensional bubble ruptures and you are pulled back into the world!");
					SpaceTimeVortex.Teleport(TakingObject, destinationCellFor, this.ParentObject);
				}
      }
      return base.FireEvent(E);
    }
  }
}
