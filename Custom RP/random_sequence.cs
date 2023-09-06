using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class RandomSequence
{
    static int[] primeNumberArray = new int[8] { 2, 3, 5, 7, 11, 13, 17, 19 };
    
    public static int NthPrimeNumber(int index) {

        if (index > 7 || index < 0 ) {
            Debug.Log("RandomSequence::NthPrimeNumber index out of range");
        }

        return primeNumberArray[index];
    }

    public static float IntegerRadicalInverse(int Base, int i) {
        int numPoints, inverse;
        numPoints = 1;
        
        for (inverse = 0; i > 0; i /= Base) {
            inverse = inverse * Base + (i % Base);
            numPoints = numPoints * Base;
        }

        return inverse / (float)numPoints;
    }

    public static float Halton(int Dimension, int Index) {
        return IntegerRadicalInverse(NthPrimeNumber(Dimension), Index);
    }

    // NumSamples是样本总数
    public static float Hammersley(int Dimension, int Index, int NumSamples) {
        
        if (Dimension == 0)
            return Index / (float)NumSamples;
        else
            return IntegerRadicalInverse(NthPrimeNumber(Dimension - 1), Index);
    }
}
