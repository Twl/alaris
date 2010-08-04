using System;

namespace Alaris.Irc
{
	/// <summary>
	/// Generates random, made-up names. The names appear to be language neutral (sort of).
	/// </summary>
	/// <remarks>This is a port of the orginal Javascript written by John Ahlschwede, ahlschwede@hotmail.com</remarks>
	public sealed class NameGenerator
	{
		private int[] numNames = new int[]{1,2,3,4};
		private int[] numNamesChance = new int[]{200,180,1,1};
		private int[] numSyllables = new int[]{1,2,3,4,5};
		private int[] numSyllablesChance = new int[]{150,500,80,10,1};
		private int[] numConsonants = new int[]{0,1,2,3,4};
		private int[] numConsonantsChance = new int[]{80,350,25,5,1};
		private int[] numVowels = new int[]{1,2,3};
		private int[] numVowelsChance = new int[]{180,25,1};
		private char[] vowel = new char[]{'a','e','i','o','u','y'};
		private int[] vowelChance = new int[]{10,12,10,10,8,2};
		private char[] consonant = new 	char[]{'b','c','d','f','g','h','j','k','l','m','n','p','q','r','s','t','v','w','x','y','z'};
		private int[] consonantChance = new 	int[]{10,10,10,10,10,10,10,10,12,12,12,10,5,12,12,12,8,8,3,4,3};
		private Random random;

		/// <summary>
		/// Create an instance.
		/// </summary>
		public NameGenerator()
		{
			random = new Random();
		}

		private int IndexSelect( int[] intArray )
		{
			int totalPossible = 0;
			for(int i=0; i < intArray.Length; i++)
			{
				totalPossible = totalPossible + intArray[i];
			}
			int chosen = random.Next( totalPossible );
			int chancesSoFar = 0;
			for(int j=0; j < intArray.Length; j++)
			{
				chancesSoFar = chancesSoFar + intArray[j];
				if( chancesSoFar > chosen )
				{
					return j;
				}
			}
			return 0;
		}	
		private string MakeSyllable()
		{
			return MakeConsonantBlock() + MakeVowelBlock() + MakeConsonantBlock();
		}
		private string MakeConsonantBlock()
		{
			string	newName = "";
			int numberConsonants = numConsonants[ IndexSelect(numConsonantsChance)];
			for(int i=0; i<numberConsonants; i++)
			{
				newName += consonant[IndexSelect(consonantChance)];
			}
			return newName;
		}
		private string MakeVowelBlock()
		{
			string	newName = "";
			int numberVowels = numVowels[ IndexSelect(numVowelsChance) ];
			for(int i=0;i<numberVowels;i++)
			{
				newName += vowel[ IndexSelect(vowelChance) ];
			}
			return newName;
		}

		/// <summary>
		/// Generates a name randomly using certain construction rules. The name
		/// will be different each time it is called.
		/// </summary>
		/// <returns>A name string.</returns>
		public string MakeName()
		{
			int numberSyllables = numSyllables[ IndexSelect( numSyllablesChance ) ];
			string newName = "";
			for(int i=0; i < numberSyllables; i++)
			{
				newName = newName + MakeSyllable();
			}
			return char.ToUpper(newName[0] ) + newName.Substring(1);
		}

	}
}

