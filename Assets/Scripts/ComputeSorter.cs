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
        sortShader.SetInt("numValues", _values.count);

        int numPairs = NextPowerOfTwo(_values.count) / 2;
        int numStages = (int)Mathf.Log(numPairs * 2, 2);

        for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
        {
            for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
            {
                int groupWidth = 1 << (stageIndex - stepIndex);
                int groupHeight = 2 * groupWidth - 1;
                sortShader.SetInt("groupWidth", groupWidth);
                sortShader.SetInt("groupHeight", groupHeight);
                sortShader.SetInt("stepIndex", stepIndex);

                var threadGroupsX = Mathf.CeilToInt((float)numPairs / xThreadGroup);
                sortShader.Dispatch(bitonicKernel, threadGroupsX, 1, 1);
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
