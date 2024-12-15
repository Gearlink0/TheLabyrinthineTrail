using System;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_TreasureHunterCorpseLoot : IPart
  {
    public bool created;

    public override bool SameAs(IPart p) => false;

    public override void Register(GameObject Object, IEventRegistrar Registrar)
    {
      Registrar.Register("EnteredCell");
      base.Register(Object, Registrar);
    }

    public override bool FireEvent(Event E)
    {
      if (E.ID == "EnteredCell")
      {
        if (this.created)
          return true;
        this.created = true;
        Cell currentCell = this.ParentObject.CurrentCell;
        currentCell.AddObject("LABYRINTHINETRAIL_FirstQuestFork");
        currentCell.AddObject("LABYRINTHINETRAIL_DissolvedJournal");
        this.ParentObject.UnregisterPartEvent((IPart) this, "EnteredCell");
      }
      return base.FireEvent(E);
    }
  }
}
