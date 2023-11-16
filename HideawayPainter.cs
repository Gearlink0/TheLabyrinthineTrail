using System;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts
{
  [Serializable]
  public class LABYRINTHINETRAIL_HideawayPainter : IPart
  {
    public override bool SameAs(IPart p) => false;

    public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade) || ID == EnteredCellEvent.ID;

    public override bool HandleEvent(EnteredCellEvent E)
    {
      try
      {
        if (!Options.DisableFloorTextureObjects)
        {
          Zone parentZone = E.Cell.ParentZone;
          for (int y = 0; y < parentZone.Height; ++y)
          {
            for (int x = 0; x < parentZone.Width; ++x)
              LABYRINTHINETRAIL_HideawayPainter.PaintCell(parentZone.GetCell(x, y));
          }
        }
      }
      catch
      {
      }
      return base.HandleEvent(E);
    }

    public static void PaintCell(Cell C)
    {
      if (!string.IsNullOrEmpty(C.PaintTile))
        return;
      C.PaintTile = "Tiles/tile-dirt1.png";
      C.PaintDetailColor = "k";
      C.PaintRenderString = "ú";
      // Make the dirt dark by default
      C.PaintColorString = "&k";
      // Fourth of the time, use one of the otherworldly colors
      switch (Stat.RandomCosmetic(0, 19))
      {
        case 0:
          C.PaintColorString = "&m";
          break;
        case 1:
          C.PaintColorString = "&M";
          break;
        case 2:
          C.PaintColorString = "&y";
          break;
        case 3:
          C.PaintColorString = "&Y";
          break;
        case 4:
          C.PaintColorString = "&O";
          break;
      }
    }
  }
}
