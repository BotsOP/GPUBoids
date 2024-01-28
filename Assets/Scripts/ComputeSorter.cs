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
        int count = _values.count;
        sortShader.SetInt("count", count);
        sortShader.SetBuffer(bitonicFlipKernel, "values", _values);
        sortShader.SetBuffer(bitonicDisperseKernel, "values", _values);

        int numStages = (int)Mathf.Log(NextPowerOfTwo(count), 2);

        for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
        {
            for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
            {
                int groupWidth = 1 << (stageIndex - stepIndex);
                int groupHeight = 2 * groupWidth;
                sortShader.SetInt("h", groupHeight);
                
                var threadGroupsX = Mathf.CeilToInt((float)count / xThreadGroup / 2);
                if (stepIndex == 0)
                {
                    sortShader.Dispatch(bitonicFlipKernel, threadGroupsX, 1, 1);
                }
                else
                {
                    sortShader.Dispatch(bitonicDisperseKernel, threadGroupsX, 1, 1);
                }
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
