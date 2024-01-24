using UnityEngine;

public class ComputeSorter
{
    private static ComputeShader sortShader;
    private static int bitonicFlipKernel;
    private static int bitonicDisperseKernel;
    private static uint xThreadGroup;
    
    static ComputeSorter()
    {
        sortShader = Resources.Load<ComputeShader>("BitonicMergeSorter");
        bitonicFlipKernel = sortShader.FindKernel("BitonicMergeFlip");
        bitonicDisperseKernel = sortShader.FindKernel("BitonicMergeDisperse");
        sortShader.GetKernelThreadGroupSizes(bitonicFlipKernel, out xThreadGroup, out _, out _);
    }

    public static void Sort(ComputeBuffer _values)
    {
        sortShader.SetBuffer(bitonicFlipKernel, "values", _values);
        sortShader.SetBuffer(bitonicDisperseKernel, "values", _values);

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

        // for (int k = 2; k <= valuesCount; k *= 2)
        // {
        //     for (int j = k/2; j > 0; j /= 2)
        //     {
        //         sortShader.SetInt("k", k);
        //         sortShader.SetInt("j", j);
        //
        //         var threadGroupsX = Mathf.CeilToInt((float)valuesCount / xThreadGroup);
        //         sortShader.Dispatch(bitonicKernel, threadGroupsX, 1, 1);
        //         _values.GetData(floatArray);
        //         debug = "";
        //         for (int i = 0; i < valuesCount; i++)
        //         {
        //             debug += floatArray[i] + " ";
        //         }
        //         Debug.Log($"{k}  {j}");
        //         Debug.Log(debug);
        //     }
        // }
        
        // Launch each step of the sorting algorithm (once the previous step is complete)
        // Number of steps = [log2(n) * (log2(n) + 1)] / 2
        // where n = nearest power of 2 that is greater or equal to the number of inputs
        int numStages = (int)Mathf.Log(NextPowerOfTwo(valuesCount), 2);

        for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
        {
            for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
            {
                int groupWidth = 1 << (stageIndex - stepIndex);
                int groupHeight = 2 * groupWidth;
                sortShader.SetInt("h", groupHeight);
                
                var threadGroupsX = Mathf.CeilToInt((float)valuesCount / xThreadGroup / 2);
                if (stepIndex == 0)
                {
                    sortShader.Dispatch(bitonicFlipKernel, threadGroupsX, 1, 1);
                }
                else
                {
                    sortShader.Dispatch(bitonicDisperseKernel, threadGroupsX, 1, 1);
                }
                
                
                _values.GetData(floatArray);
                debug = "";
                for (int i = 0; i < valuesCount; i++)
                {
                    debug += floatArray[i] + " ";
                }
                Debug.Log($"{stageIndex}  {stepIndex}");
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
