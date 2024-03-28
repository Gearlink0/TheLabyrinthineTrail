using XRL;
using XRL.World;

[PlayerMutator]
public class LABYRINTHINETRAIL_AddRivalHunterSystem : IPlayerMutator
{
	/* Add the LABYRINTHINETRAIL_RivalHunterSystem to the game. Doing it here because
	this IPlayerMutator will run when the game starts regardless of which type of
	game it is. */
	public void mutate(GameObject player)
	{
		The.Game.AddSystem( new LABYRINTHINETRAIL_RivalHunterSystem() );
	}
}
