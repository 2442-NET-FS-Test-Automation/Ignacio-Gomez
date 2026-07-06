using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace DsaThreading; 

public static class Searches
{
    public static int LinearSearch(int[] data, int target)
    {
        for(int i = 0; i < data.Length; i++)
        {
            if(data[i] == target) return i;
        }
        return -1;
    }

    public static int[] Insertation(int[] input)
    {
        int length = input.Length;
        for(int i = 1; i < length; i++)
        {
            int key = input[i];
            int j = i - 1;
            while(j >= 0 && input[j] > key)
            {
                input[j + 1] = input[j];
                j--;
            }
            input[j + 1] = key;
        }
        return input;
    }
    public static int[] Selection(int[] input)
    {
        int length = input.Length;
        for (int i = 0; i < length - 1; i++)
        {
            int min_index = i;
            for (int j = i + 1;j < length; j++)
            {
                if(input[j] < input[min_index])
                {
                    min_index = j;
                }
            }
            // move the minimum element to its correcto position
            int temp = input[i];
            input[i] = input[min_index];
            input[min_index] = temp;
        }
        return input;
    }

    public static int[] Merge(int[] input)
    {
        //Base case, 
        if(input.Length <= 1) return input;
        int mid = input.Length / 2;

        //We split the array in two halfs
        int[] left = Merge(input[..mid]);
        int[] right = Merge(input[mid..]);
        return MergeTwo(left, right);
    }
    public static int[] MergeTwo(int[] left, int[] right)
    {
        int[] sorted = new int[left.Length + right.Length];
        int i = 0, j = 0, k = 0;
        while (i < left.Length && j < right.Length)
        {
            sorted[k++] = left[i] <= right[j] ? left[i++] : right[j++];
        }
        while(i < left.Length) sorted[k++] = left[i++];
        while(j < right.Length) sorted[k++] = right[j++];
        return sorted;
    }
    
}