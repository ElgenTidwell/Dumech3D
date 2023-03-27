public class pRandom
{
	private static int[] randomints = {
		23, 8, 87, 18, 32, 36, 82, 12, 63, 11, 55, 74, 84, 95, 72, 52, 15, 92, 93, 69, 64, 88, 71, 58, 98, 99, 6, 48, 20, 46, 86, 28, 45, 14, 79, 22, 42, 19, 56, 24, 67, 31, 66, 4, 78, 27, 10, 59, 9, 75, 76, 34, 77, 21, 62, 3, 2, 30, 68, 49, 43, 44, 7, 100, 39, 54, 70, 5, 26, 35, 65, 16, 13, 97, 91, 90, 47, 25, 85, 17, 73, 96, 53, 38, 80, 57, 61, 41, 33, 1, 51, 29, 89, 60, 94, 37, 83, 40, 50, 81};
	private static int pointer;
	public static int GetRandom()
	{
		if(pointer >= 100)
		{
			pointer = 0;
		}
		var final = randomints[pointer];
		pointer++;
		return final;
	}
}
