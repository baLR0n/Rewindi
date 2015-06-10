namespace Rewindi.Model.GameLogic.Map
{
	//Repräsentiert eine Transition
	public class Transition
	{
		public struct positionWithDir
		{
			public int x;
			public int y;
			public short dir;
		};

		public positionWithDir source;
		public positionWithDir target;

		private Transition ()
		{
		}

		/// <summary>
		/// Erzeugt eine neue Transition.
		/// </summary>
		/// <param name="source">Source of the Transition</param>
		/// <param name="target">Target of the Transition</param>
		public Transition(positionWithDir source, positionWithDir target)
		{
			this.source = source;
			this.target = target;
		}
	}
}

