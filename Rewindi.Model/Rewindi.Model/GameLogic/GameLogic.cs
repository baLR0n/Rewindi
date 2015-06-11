using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rewindi.Model.GameLogic.Map;

namespace Rewindi.Model.GameLogic
{
	
    public class GameLogic
    {
	//MAP DATA
		public int PlayerCount;
		public short MyPlayerNr;

        /// <summary>
        /// Gets or sets the game states.
        /// </summary>
        /// <value>
        /// The game states.
        /// </value>
        public List<FieldState[]> GameStates { get; private set; }

        /// <summary>
        /// Get the amount with OverwriteStonesPerPlayer[playerNumber].
        /// </summary>
		public int[] OverwriteStonesPerPlayer;

        /// <summary>
        /// /// Get the amount with BombsPerPlayer[playerNumber].
        /// </summary>
		public int[] BombsPerPlayer;

        /// <summary>
        /// The bomb strength
        /// </summary>
		public int BombStrength;

        /// <summary>
        /// The game phase.
        /// true = game phase, false = bombing phase.
        /// </summary>
        public bool GamePhase = true;

		//Careful! Width and Height are opposite to the specification!
		//So that width matches x and height matches y
		public int MapWidth;
		public int MapHeight;

		public Field[] Map;
        /// <summary>
        /// Saves the Mapdata in a binary format:
        /// Bit 0-7: Is there a valid field in the given direction?
        /// Bit 8-10: Is this field occupied by an enemy?
        /// Bit 11-23: Not used!
        /// Bit 24-31: Is this field an transformation field?
        /// All sections follow the game specification -> Starts with 0 at the top, and goes on clockwise
        /// 0 top, 1 top right, 2 right, 3 down right, ...
        /// </summary> 
        public UInt64[] Map_binary;

		public List<Transition> Transitions = new List<Transition>();

	//GAMETREE DATA
		private Stack<FieldState[]> fieldStateStack = new Stack<FieldState[]>();

		public FieldState[] CurrentMapState;

        public List<Field> OwnStones = new List<Field>();
        public List<UInt64> OwnStones_binary = new List<UInt64>();

		public List<Field> legitMoves = new List<Field>();
		public List<Field> legitOverwriteMoves = new List<Field>();

		//holds all free fields, that are possible legit moves
		private List<Field> possibleLegitMoves = new List<Field>();

		//the player that is currently in first position (updated in evaluateMap)
		private int currentFirstPlayer;

	//MAP EVALUATION
		private float[] playerscores;
		private int[] stoneCount;

	//SET STONE
		private List<List<Field>> stonesToTurn = new List<List<Field>>();
		private List<Field> visited = new List<Field>();

		public class NextInLine
        {
            public int dir; 
            public Field nextField; 

            public NextInLine(int dir, Field nextField)
            {
                this.dir = dir;
                this.nextField = nextField;
            }
        };

        //TODO save evaluated maps in hashmap,  
        public GameLogic()
        {
            this.GameStates = new List<FieldState[]>();
        }

        /// <summary>
        /// Sets the stone from the move we got from the server.
        /// Here does the recoloring of the map happen.
        /// </summary>
        /// <param name="posX">The position x.</param>
        /// <param name="posY">The position y.</param>
        /// <param name="extraField">The extra field byte value.</param>
        /// <param name="playerNr">The player nr.</param>
        /// <returns></returns>
        public void SetStone(int posX, int posY, byte extraField, int playerNr)
        {
            Field startField = Map[posX*MapHeight + posY];

            if (startField.state.Type > 0)
                OverwriteStonesPerPlayer[playerNr]--;

            // Get value of target field.
            int item = startField.changeTo(playerNr);

			stonesToTurn.Clear();
			visited.Clear();

            // For each direction, check if stones need o be flipped
            for (int i = 0; i < 8; i++)
            {
                if (startField.Neighbours[i] == null || !startField.Neighbours[i].isEnemy(playerNr))
                    continue;
                //first Neighbours
                NextInLine current = new NextInLine(i, startField);

                this.goInLine(current);

                //all other neighbours in that direction
                while (current.nextField != null && current.nextField.isEnemy(playerNr))
                {
                    visited.Add(current.nextField);
                    this.goInLine(current);
                }
                //if the pattern is right, add the current visited list to the stonesToTurn List
                if (current.nextField != null && current.nextField != startField &&
                    current.nextField.state.Type == playerNr)
                {
                    stonesToTurn.Add(visited);
                    visited = new List<Field>();
                }
                else
                {
                    visited.Clear();
                }

            }

            //finally flip all stones
            foreach (List<Field> l in stonesToTurn)
            {
                foreach (Field f in l)
                {
                    f.changeTo(playerNr);
                }
            }

            //check if the player gained an item
            // -1 = choice, -2 = inversion, -3 = bonus
            if (item < 0)
            {
                if (item == -1) // Choice
                {
                    if (extraField > 0 && extraField < 20)
                        this.SwapPlayerStones(playerNr, extraField);
                }
                else if (item == -2)
                {
                    this.ExecuteInversion();
                }
                else if (item == -3)
                {
                    if (extraField == 20)
                        BombsPerPlayer[playerNr]++;
                    else if (extraField == 21)
                        OverwriteStonesPerPlayer[playerNr]++;
                }
            }

            this.SaveState();
        }

        /// <summary>
        /// Swaps colors of two specific players.
        /// </summary>
        /// <param name="playerSwapOne">The player swap one.</param>
        /// <param name="playerSwapTwo">The player swap two.</param>
        public void SwapPlayerStones(int playerSwapOne, int playerSwapTwo)
        {
            foreach (Field f in Map)
            {
                if (f != null && f.state.Type == playerSwapOne)
                {
                    f.state.Type = playerSwapTwo;
                }
                else if (f != null && f.state.Type == playerSwapTwo)
                {
                    f.state.Type = playerSwapOne;
                }
            }
        }

        /// <summary>
        /// Executes the inversion.
        /// Player 1 gets colors of player 2, player 2 gets player 3 and so on.
        /// </summary>
        public void ExecuteInversion()
        {
            foreach (Field f in Map)
            {
                if (f != null && f.state.Type > 0 && f.state.Type <= this.PlayerCount)
                {
                    if (f.state.Type == this.PlayerCount)
                    {
                        f.state.Type = 1;
                    }
                    else
                    {
                        f.state.Type++;
                    }
                }
            }
        }

        /// <summary>
        /// Decides what to take when landing on a bonus field.
        /// </summary>
        /// <param name="playerNumber">The player number that got the bonus field.</param>
        public void ExecuteBonus(int playerNumber, bool choseBomb)
        {
            if (choseBomb)
				BombsPerPlayer [playerNumber]++;
			else
				OverwriteStonesPerPlayer [playerNumber]++;
		}

        /// <summary>
        /// Saves the current mapstate in the list of moves.
        /// </summary>
		public void SaveState()
		{           		
			FieldState[] newMapState = new FieldState[this.CurrentMapState.Length];
            for (int i = 0; i < newMapState.Length; i++)
            {
                newMapState[i] = new FieldState();
            }

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    if (Map[x*MapHeight + y] == null)
                    {
                        newMapState[x + y * MapWidth].Type = -5;
                    }
                    else if (CurrentMapState[x*MapHeight + y] != null)
                    {
                        newMapState[x + y * MapWidth].SetValues(CurrentMapState[x * MapHeight + y].Type);
                    }
                }
            }

			this.GameStates.Add(newMapState);
		}

        /// <summary>
        /// Resets the map references.
        /// </summary>
		public void ResetMapReferences()
		{
			//reset references. better method?
			for (int y = 0; y < this.MapHeight; y++)
			{
				for (int x = 0; x < this.MapWidth; x++)
				{
					if (Map [x * MapHeight + y] != null)
						Map [x * MapHeight + y].state = CurrentMapState [x * MapHeight + y];
				}
			}
        }

        /// <summary>
        /// Deletes all neighbours of a field to a specific depth (using a recursive algorithm)
        /// </summary>
        /// <param name="field">game field</param>
        /// <param name="depth">depth to destroy</param>
        private void DeleteAllNeighbours(Field field, int depth)
        {
            if (depth > 0)
                for (int i = 0; i < 8; i++)
                {
                    if (field.Neighbours[i] != null)
						this.DeleteAllNeighbours(field.Neighbours[i], depth - 1);
                }
            Map[field.xPos * MapHeight + field.yPos] = null;
        }

        /// <summary>
        /// Throws a bomb on the map
        /// </summary>
        /// <param name="x">x-Coordinate</param>
        /// <param name="y">y-Coordinate</param>
        public void ExecuteBomb(int x, int y)
        {
            //Delete all neighbours
            this.DeleteAllNeighbours(Map[x * MapHeight + y], BombStrength);
            //Delete centre of the explosion
            Map[x * MapHeight + y] = null;
        }

        public void UpdateLegitMoves_binary(int currPlayer)
        {
            this.UpdateOwnStones_binary(currPlayer);
            legitMoves.Clear();
            legitOverwriteMoves.Clear();

            foreach (UInt64 field in OwnStones_binary)
            {
                //todo: flatten loop faster/possible?
                for (int dir = 0; dir < 8; dir++)
                {
                    //is neighbour field null (=invalid field) or own field?
                    if ((field == 0) || //todo: falsch -> nur auf entsprechenden Teil schauen
                        ((field & (15UL << (dir * 4 + 8) | 1UL << dir)) //get mask for field in the desired direction
                        == (((ulong)(currPlayer + 7) << (dir * 4 + 8)) | (1UL << dir)))) //calculate own field, //playerNr - 1 + 8(for occupied stone)
                        continue;
                    Console.WriteLine(field);
                }

            }
        }

		//caluculates which moves are legal in the current map state 
		//and saves them in the legitMoves and legitOverwriteMoves lists
		public void UpdateLegitMoves(int currPlayer)
		{
			//TODO implement second method
			// its even faster, because legit moves are not found double

			//this method finds legit moves and legit overwrite moves, by taking all own 
			//stones as starting points
			this.UpdateOwnStones(currPlayer);
			legitMoves.Clear ();
			legitOverwriteMoves.Clear ();

			foreach (Field ownField in OwnStones) 
			{
				//look at each neighbour
				for (int directionIndex = 0; directionIndex < 8; directionIndex++) {
					if (ownField.Neighbours [directionIndex] == null || ownField.Neighbours [directionIndex].isEnemy(currPlayer) == false)
						continue;
					
					NextInLine current = new NextInLine(directionIndex, ownField);

					//the first neighbour is skipped, because we already tested him to be an enemy;
					this.goInLine(current);
                    this.goInLine(current);

                    //check until a non defined field, or a non enemy field is found
                    while (current.nextField != null && current.nextField.isEnemy(currPlayer)) 
					{
                        //if the current field isnt in the legit overwrite moves, add it
                        //todo: Unnötig? Da legitOverwriteMoves ja oben sowieso immer gecleart wird?
                        if (!legitOverwriteMoves.Contains(current.nextField))
                        {
                            legitOverwriteMoves.Add(current.nextField);
                        }

						this.goInLine (current);
					}

                    //if the found field is defined and an field you can set on ( <=0)
                    if (current.nextField != null && current.nextField.state.Type <= 0)
					{
                        //todo: Unnötig? Da legitMoves ja oben sowieso immer gecleart wird?
						if(!legitMoves.Contains(current.nextField))
							legitMoves.Add (current.nextField);
					}
					
				}

			}

            //Add all expansion fields to possible overwrite Moves
            foreach(Field f in Map)
                if (f != null && f.state.Type == 9)
                    legitOverwriteMoves.Add(f);

			//Console.WriteLine ("Possible moves: " + legitMoves.Count);
			//Console.WriteLine ("Possible overwrite moves: " + legitOverwriteMoves.Count);


			if(/*trigger for enabling overwrite stones || no other possible moves*/ false)
			{
				//TODO update possibleOverwriteMoves
			}
				
		}

        /// <summary>
        /// Updates the list of owned stones.
        /// </summary>
        /// <param name="playerNr">The player nr.</param>
		public void UpdateOwnStones (int playerNr)
		{
			OwnStones.Clear();

			foreach (Field f in Map) 
			{
				if (f != null && f.state.Type == playerNr)
					OwnStones.Add (f);
			}
		}

        /// <summary>
        /// Updates the list OwnStones, where all stones from the player committed are storred
        /// </summary>
        /// <param name="playerNr">number of the player to update</param>
        public void UpdateOwnStones_binary(int playerNr)
        {
            OwnStones.Clear();

            for (int x = 0; x < this.MapWidth; x++)
            {
                for (int y = 0; y < this.MapHeight; y++)
                {
                    if ((Map_binary[x * MapHeight + y] & (((ulong)(15)) << 48)) == (((ulong)(playerNr + 7)) << 48)) //playerNr - 1 + 8(for occupied stone)
                       OwnStones_binary.Add(Map_binary[x * MapHeight + y]);
                }
            }
        }


        /// <summary>
        /// Sets the given reference and the direction to the neighbour in the given direction.
        /// </summary>
        /// <param name="line">The line.</param>
        public void goInLine(NextInLine line)
        {
            int i = line.dir;
            //HACK: In initializiation of the dirOffset in ReadMapData, every offset with data was incremented by 1
            //thus to get the actual direction, now we have to decrement the direction by 1
            if (line.nextField.dirOffset[i] != 0)
            {
                line.dir = line.nextField.dirOffset[i] - 1;
                line.nextField = line.nextField.Neighbours[i];
                return;
            }
            line.nextField = line.nextField.Neighbours[i];
            return;
        }

        /// <summary>
        /// Keeps the possibleLegitMoves list updated
        /// </summary>
		public void UpdatePossibleLegitMoves()
		{
			//TODO
		}

        /// <summary>
        /// Initializes the arrays.
        /// </summary>
		public void InitializeArrays()
		{
			stoneCount = new int[PlayerCount + 1];
			playerscores = new float[PlayerCount + 1];
			playerscores [0] = float.MinValue;

			CurrentMapState = new FieldState[this.MapHeight * this.MapWidth];
		    for (int i = 0; i < CurrentMapState.Length; i++)
		    {
		        CurrentMapState[i] = new FieldState();
		    }
		}

        /// <summary>
        /// Reads map data and stores it in the class
        /// </summary>
        /// <param name="mapData">map data</param>
        public void ReadMapData(string[] mapData)
        {
            try
            {
                //Konfiguration einlesen
                this.PlayerCount = Convert.ToInt32(mapData[0].Trim());

                this.OverwriteStonesPerPlayer = new int[PlayerCount + 1];
                for (int i = 0; i < OverwriteStonesPerPlayer.Length; i++)
                {
                    // 0 Auslassen, damit man mit der SpielerNr auf die Liste zugreifen kann.
                    // An Stelle 0 bleibt der Ursprungswert stehen.
                    OverwriteStonesPerPlayer[i] = Convert.ToInt32(mapData[1].Trim());
                }
                    
                string[] bomben = mapData[2].Split(' ');

                this.BombsPerPlayer = new int[PlayerCount + 1];

                for (int i = 0; i < BombsPerPlayer.Length; i++)
                {
                    // 0 Auslassen, damit man mit der SpielerNr auf die Liste zugreifen kann.
                    // An Stelle 0 bleibt der Ursprungswert stehen.
                    BombsPerPlayer[i] = Convert.ToInt32(bomben[0]);
                }

                this.BombStrength = Convert.ToInt32(bomben[1]);

                string[] groeße = mapData[3].Split(' ');
                this.MapWidth = Convert.ToInt32(groeße[1]);
                this.MapHeight = Convert.ToInt32(groeße[0]);


				//initialize needed Arrays
				this.InitializeArrays();

                //Karte einlesen
                this.Map = new Field[this.MapWidth * this.MapHeight];
                for (int y = 0; y < this.MapHeight; y++)
                {
                    string[] currentLine = mapData[y + 4].Split(' ');
					for (int x = 0; x < this.MapWidth; x++)
                    {
                        char c = Convert.ToChar(currentLine[x].Trim());
                        if (c != '-')
						{
							CurrentMapState[x * MapHeight + y].SetType(c);
							this.Map[x * MapHeight + y] = new Field(CurrentMapState[x * MapHeight + y], x, y);
						}
                    }
                }

                // Startzustand als erste Map speichern.
                this.SaveState();

                //Transitionen einlesen
                int anzahlTransitionen = mapData.Length - (this.MapHeight + 4);

                //Lese alle Transitionen ein
                for (int t = anzahlTransitionen; t > 0; t--)
                {
                    if (String.IsNullOrEmpty(mapData[mapData.Length - t]))
                        continue;
                    //Hole die aktuelle Zeile (Transition)
                    string[] currentLine = mapData[mapData.Length - t].Split(' ');

					Transition.positionWithDir source;
					source.x = Convert.ToInt32(currentLine[0].Trim());
					source.y = Convert.ToInt32(currentLine[1].Trim());
					source.dir = Convert.ToSByte(currentLine[2].Trim());
                    
					Transition.positionWithDir target;
					target.x = Convert.ToInt32(currentLine[4].Trim());
					target.y = Convert.ToInt32(currentLine[5].Trim());
					target.dir = Convert.ToSByte(currentLine[6].Trim());

                    this.Transitions.Add(new Transition(source, target));
                }

                this.InitializeNeighbours();

                //calculate baseValues (initialize) and tmpValues (first update) of each field.
                for (int x = 0; x < this.MapWidth; x++)
                {
                    for (int y = 0; y < this.MapHeight; y++)
                    {
                        if (Map[x * MapHeight + y] == null)
                            continue;
                        Map[x * MapHeight + y].initFieldValues();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Fehler beim Einlesen der Karte: " + e.Message);
            }
        }

        //Todo: Methode zum Einlesen auf Bitebene
        //wenn das hier funktioniert, sollte es in readMapData()
        //integriert werden
        public void readMapData_binary()
        {
            Map_binary = new UInt64[MapWidth * MapHeight];
            //go thru all fields of the map
            for (int x = 0; x < this.MapWidth; x++)
            {
                for (int y = 0; y < this.MapHeight; y++)
                {
                    if (Map[x * MapHeight + y] == null)
                        continue;

                    //Sets the type of the stone itself
                    switch (Map[x * MapHeight + y].state.Type)
                    {
                        //expansion stone (0100)
                        case (9):
                            {
                                Map_binary[x * MapHeight + y] |= 5UL << 48;
                                break;
                            }
                        //player 1 - 8
                        //player 8 (1111)
                        case (8):
                            {
                                Map_binary[x * MapHeight + y] |= 15UL << 48;
                                break;
                            }
                        //player 7 (1110)
                        case (7):
                            {
                                Map_binary[x * MapHeight + y] |= 14UL << 48;
                                break;
                            }
                        //player 6 (1101)
                        case (6):
                            {
                                Map_binary[x * MapHeight + y] |= 13UL << 48;
                                break;
                            }
                        //player 5 (1100)
                        case (5):
                            {
                                Map_binary[x * MapHeight + y] |= 12UL << 48;
                                break;
                            }
                        //player 4 (1011)
                        case (4):
                            {
                                Map_binary[x * MapHeight + y] |= 11UL << 48;
                                break;
                            }
                        //player 3 (1010)
                        case (3):
                            {
                                Map_binary[x * MapHeight + y] |= 10UL << 48;
                                break;
                            }
                        //player 2 (1001)
                        case (2):
                            {
                                Map_binary[x * MapHeight + y] |= 9UL << 48;
                                break;
                            }
                        //player 1 (1000)
                        case (1):
                            {
                                Map_binary[x * MapHeight + y] |= 8UL << 48;
                                break;
                            }
                        case (0):
                            {
                                //case(0) ignored, because all bits have to be set to 0, which they already are
                                break;
                            }
                        //choice stone (0001)
                        case (-1):
                            {
                                Map_binary[x * MapHeight + y] |= 1UL << 48;
                                break;
                            }
                        //inversion stone (0010)
                        case (-2):
                            {
                                Map_binary[x * MapHeight + y] |= 2UL << 48;
                                break;
                            }
                        //bonus stone (0011)
                        case (-3):
                            {
                                Map_binary[x * MapHeight + y] |= 3UL << 48;
                                break;
                            }
                        default:
                            {
                                throw new InvalidDataException("unknown field type.#readMapData_binary");
                            }

                    }

                    //check all directions
                    for (int i = 0; i < 8; i++)
                    {
                        if (Map[x * MapHeight + y].Neighbours[i] == null)
                        {
                            continue;
                        }
                        else
                        {
                            //Sets if neighbour field exists
                            Map_binary[x * MapHeight + y] |= 1UL << i;
                        }

                        //Sets the type of the neighbour
                        switch (Map[x * MapHeight + y].Neighbours[i].state.Type)
                        {
                            //expansion stone (0100)
                            case (9):
                                {
                                    Map_binary[x * MapHeight + y] |= 5UL << (i * 4 + 8);
                                    break;
                                }
                            //player 1 - 8
                            //player 8 (1111)
                            case (8):
                                {
                                    Map_binary[x * MapHeight + y] |= 15UL << (i * 4 + 8);
                                    break;
                                }
                            //player 7 (1110)
                            case (7):
                                {
                                    Map_binary[x * MapHeight + y] |= 14UL << (i * 4 + 8);
                                    break;
                                }
                            //player 6 (1101)
                            case (6):
                                {
                                    Map_binary[x * MapHeight + y] |= 13UL << (i * 4 + 8);
                                    break;
                                }
                            //player 5 (1100)
                            case (5):
                                {
                                    Map_binary[x * MapHeight + y] |= 12UL << (i * 4 + 8);
                                    break;
                                }
                            //player 4 (1011)
                            case (4):
                                {
                                    Map_binary[x * MapHeight + y] |= 11UL << (i * 4 + 8);
                                    break;
                                }
                            //player 3 (1010)
                            case (3):
                                {
                                    Map_binary[x * MapHeight + y] |= 10UL << (i * 4 + 8);
                                    break;
                                }
                            //player 2 (1001)
                            case (2):
                                {
                                    Map_binary[x * MapHeight + y] |= 9UL << (i * 4 + 8);
                                    break;
                                }
                            //player 1 (1000)
                            case (1):
                                {
                                    Map_binary[x * MapHeight + y] |= 8UL << (i * 4 + 8);
                                    break;
                                }
                            case (0):
                                {
                                    //case(0) ignored, because all bits have to be set to 0, which they already are
                                    break;
                                }
                            //choice stone (0001)
                            case (-1):
                                {
                                    Map_binary[x * MapHeight + y] |= 1UL << (i * 4 + 8);
                                    break;
                                }
                            //inversion stone (0010)
                            case (-2):
                                {
                                    Map_binary[x * MapHeight + y] |= 2UL << (i * 4 + 8);
                                    break;
                                }
                            //bonus stone (0011)
                            case (-3):
                                {
                                    Map_binary[x * MapHeight + y] |= 3UL << (i * 4 + 8);
                                    break;
                                }
                            default:
                                {
                                    throw new InvalidDataException("unknown neighbour field type.#readMapData_binary");
                                }

                        }
                    }
                }
            }
            //Sets if neighbour is a transition
            foreach (Transition transition in Transitions)
            {
               Map_binary[transition.source.x * MapHeight + transition.source.y] |= 1UL << (transition.source.dir + 40);
               Map_binary[transition.target.x * MapHeight + transition.target.y] |= 1UL << (transition.target.dir + 40);
            }
        }

        /// <summary>
        /// Initializes the neighbour references of each field.
        /// </summary>
		private void InitializeNeighbours()
		{
			for (int x = 0; x < this.MapWidth; x++) 
			{
				for (int y = 0; y < this.MapHeight; y++) 
				{
					if (this.Map [x * MapHeight + y] == null)
						continue;
					//for each field, a reference to all neighbours is saved
					Field current = this.Map [x * MapHeight + y];

					//8 mögliche richtungen
					if(this.IsValidNeighbour(x + 1, y))
						current.Neighbours[2] = this.Map [(x + 1) * MapHeight + y];
					if(this.IsValidNeighbour(x + 1, y + 1))
						current.Neighbours[3] = this.Map [(x + 1) * MapHeight + (y + 1)];
					if(this.IsValidNeighbour(x, y + 1))
						current.Neighbours[4] = this.Map [x * MapHeight + (y + 1)];
					if(this.IsValidNeighbour(x + 1 , y -1))
						current.Neighbours[1] = this.Map [(x + 1) * MapHeight + (y - 1)];
					if(this.IsValidNeighbour(x, y - 1))
						current.Neighbours[0] = this.Map [x * MapHeight + (y - 1)];
					if(this.IsValidNeighbour(x - 1, y - 1))
						current.Neighbours[7] = this.Map [(x - 1) * MapHeight + (y - 1)];
					if(this.IsValidNeighbour(x - 1, y))
						current.Neighbours[6] = this.Map [(x - 1) * MapHeight + y];
					if(this.IsValidNeighbour(x - 1, y + 1))
						current.Neighbours[5] = this.Map [(x - 1) * MapHeight + (y + 1)];
					
					foreach (Transition t in Transitions) 
					{
						if (t.source.x == x && t.source.y == y) 
						{
							current.Neighbours [t.source.dir] = this.Map [t.target.x * MapHeight + t.target.y];
                            current.dirOffset[t.source.dir] = (t.target.dir + 4) % 8 + 1;
						}
						else if (t.target.x == x && t.target.y == y)
						{
							current.Neighbours [t.target.dir] = this.Map [t.source.x * MapHeight + t.source.y];
							current.dirOffset [t.target.dir] = (t.source.dir + 4) % 8 + 1;
						}
					}
				}
			}
		}

        /// <summary>
        /// Determines whether a field at a specific position is a valid neighbour.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns></returns>
		private bool IsValidNeighbour(int x, int y)
        {
            return (x >= 0 && x < this.MapWidth && y >= 0 && y < this.MapHeight && this.Map[x*MapHeight + y] != null);
        }


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="outputMode">The output mode.
        /// 0 = output of real current state of the map
        /// 1 = output of base map values
        /// 2 = output of possible moves
        /// 3 = output of real map values
        /// </param>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append("Players: " + this.PlayerCount);
            result.Append("\nUeberschreibsteine: " + this.OverwriteStonesPerPlayer[0]);
            result.Append("\n#Bomben: " + this.BombsPerPlayer[0] + " Staerke: " + this.BombStrength);
            result.Append("\n   ");
            //Column numbers
            for (int i = 0; i < MapWidth; i++)
            {
                result.AppendFormat(" {0:0#} ", i);
            }

            result.AppendLine();

            for (int y = 0; y < this.MapHeight; y++)
            {
                //Row numbers
                StringBuilder line = new StringBuilder(String.Format("{0:0#})", y));
                for (int x = 0; x < this.MapWidth; x++)
                {
                    //Führt zu einem leerzeichen am Ende jeder Zeile, sollte aber bei der ausgabe irrelevant sein
                    if (this.Map[x * MapHeight + y] != null)
                        line.Append(this.Map[x * MapHeight + y] + " ");
                    else
                        line.Append("  - ");
                }
                result.Append("\n" + line);
            }

            result.Append("\n\nTransitions:");

			if (Transitions != null) 
			{
				foreach (Transition t in this.Transitions) {
					result.Append("\n" + t.source.x + " " + t.source.y + " " + t.source.dir + " <-> "
					+ t.target.x + " " + t.target.y + " " + t.target.dir);
				}
			}

            return result.ToString();
        }
    }
}
