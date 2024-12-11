using Occult.Engine.CodeGeneration;
using System.CodeDom.Compiler;

namespace XRL.World.AI
{
  [GenerateSerializationPartial]
  public class LABYRINTHINETRAIL_OpinionForkHunt : IOpinionSubject
  {
    [GeneratedCode("SerializationPartialsGenerator", "1.0.0.0")]
    public override bool WantFieldReflection => false;

    [GeneratedCode("SerializationPartialsGenerator", "1.0.0.0")]
    public override void Write(SerializationWriter Writer)
    {
      Writer.Write(this.Magnitude);
      Writer.WriteOptimized(this.Time);
    }

    [GeneratedCode("SerializationPartialsGenerator", "1.0.0.0")]
    public override void Read(SerializationReader Reader)
    {
      this.Magnitude = Reader.ReadSingle();
      this.Time = Reader.ReadOptimizedInt64();
    }

    public override int BaseValue => -100;

    public override string GetText(GameObject Actor) => "Has finding fork I want.";
  }
}
