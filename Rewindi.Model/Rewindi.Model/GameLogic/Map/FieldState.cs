using System;

namespace Rewindi.Model.GameLogic.Map
{
	public class FieldState
	{
		/*  null = nothing
		 *  0 = emtpy
		 *  1 = player1
		 *  ...
		 *  8 = player8
		 * 	9 = expansion
		 * -1 = choice 
		 * -2 = inversion
		 * -3 = bonus
		 *  
		 *  => all free fields: 	< 0
		 *  => all player stones:	> 0
		 */
	    public int Type { get; set; }

		public void SetValues(int type)
		{
            this.Type = type;
		}

		public void SetType(char value)
		{
			int result;
			if (int.TryParse (value.ToString (), out result)) 
			{
                this.Type = result;
			}
			else
			{
				switch(value)
				{
				case('x'):
                    this.Type = 9;
					break;
				case('c'):
                    this.Type = -1;
					break;
				case('i'):
                    this.Type = -2;
					break;
				case('b'):
                    this.Type = -3;
					break;
				default:
					throw(new Exception());	
				}
			}
		}

    }
}

