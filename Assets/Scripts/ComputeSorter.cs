using UnityEngine;

public class ComputeSorter
{
    private static ComputeShader sortShader;
    private static int bitonicKernel;
    private static uint xThreadGroup;
    
    static ComputeSorter()
    {
        sortShader = Resources.Load<ComputeShader>("BitonicMergeSorter");
        bitonicKernel = sortShader.FindKernel("BitonicMergeSort");
        sortShader.GetKernelThreadGroupSizes(bitonicKernel, out xThreadGroup, out _, out _);
    }

    public static void Sort(ComputeBuffer _values)
    {
        sortShader.SetBuffer(bitonicKernel, "values", _values);

        int valuesCount = _values.count;

        float[] floatArray = new float[valuesCount];
        
        _values.GetData(floatArray);
        string debug = "";
        for (int i = 0; i < valuesCount; i++)
        {
            debug += floatArray[i] + " ";
        }
        Debug.Log($"0 0");
        Debug.Log(debug);

        for (int k = 2; k <= valuesCount; k *= 2)
        {
            for (int j = k/2; j > 0; j /= 2)
            {
                sortShader.SetInt("k", k);
                sortShader.SetInt("j", j);

                var threadGroupsX = Mathf.CeilToInt((float)valuesCount / xThreadGroup);
                sortShader.Dispatch(bitonicKernel, threadGroupsX, 1, 1);
                _values.GetData(floatArray);
                debug = "";
                for (int i = 0; i < valuesCount; i++)
                {
                    debug += floatArray[i] + " ";
                }
                Debug.Log($"{k}  {j}");
                Debug.Log(debug);
            }
        }
    }
    
    private static int NextPowerOfTwo(int n)
    {
        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        n++;
        return n;
    }
}
