namespace Rewindi.Model.GameLogic.Map
{
	public class Field
	{
		public FieldState state;

		/* referenzen auf nachbaren, reihenfolge wie in spezifikation
		 * wenn sie nicht initialisiert sind, ist in diese Richtung kein weiteres Feld
		 * !!Immer prüfen mit NULL 
		 * linien sind über rekursive aufrufe erreichbar!
		 * zb: 
		 * c = current Field
		 * while(c != NULL && c.Type != Field.Type.EMPTY)
		 * 		c = c.Neighbours[1];
		 * if(c != NULL)
		 * 		c = leeres feld
		 */
		public Field[] Neighbours;

		//needed because a straight line can change its direction when going through a transition
		public int[] dirOffset = { 0, 0, 0, 0, 0, 0, 0, 0 };

		public int xPos;
		public int yPos;

		//returns the type the field had before
		public int changeTo(int type)
		{
			int before = state.Type;
			state.Type = type;

			return before;
		}
		
		/// <summary>
        /// gets only called once, after the map was initialized
		/// </summary>
		public void initFieldValues()
		{
			//initialize base value
			int turnableOverDirections = 0;
			for (int i = 0; i < 4; i++) 
			{
				if (Neighbours [i] != null && Neighbours [(i + 4) % 8] != null)
					turnableOverDirections++;
			}

			switch(turnableOverDirections)
			{
			    case(4):
				    break;
			}
		}
			
		//returns if this field is an enemy to the current player
		public bool isEnemy(int currentPlayer)
		{
			return (state.Type > 0 && state.Type != currentPlayer);
		}

		public Field (FieldState state, int x, int y)
		{
			this.state = state;
			xPos = x;
			yPos = y;
			Neighbours = new Field[8];
		}

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
	    public override string ToString()
	    {
            if (state.Type < 0)
                return " " + state.Type;
            return "  " + state.Type;
	    }
	}
}

