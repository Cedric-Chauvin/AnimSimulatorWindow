using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class OneDBlend : CustomBlendTree
{
    public float minValue = float.PositiveInfinity;
    public float maxValue = float.NegativeInfinity;
    
    public OneDBlend(BlendTree blendTree, Animator animator)
        : base(blendTree, animator)
    {

    }

    public override AnimationClipPlayable[] SetupClip(BlendTree blendTree)
    {
        clips = new AnimationClipPlayable[blendTree.children.Length];
        for (int i = 0; i < blendTree.children.Length; i++)
        {
            //Create all child animation
            ChildMotion childMotion = blendTree.children[i];
            AnimationClip castClip = childMotion.motion as AnimationClip;
            if (castClip)
            {
                clips[i] = AnimationClipPlayable.Create(_graph, castClip);
            }
            else
                Debug.LogError("Blend :" + blendTree.name + " motion :" + i + " is not a clip");

            //Setup min & max value for slider 
            if (childMotion.threshold < minValue)
                minValue = childMotion.threshold;
            if (childMotion.threshold > maxValue)
                maxValue = childMotion.threshold;
        }
        return clips;
    }

    public void UpdateMixer(float parameter)
    {
        //child are already sort gradually

        int i = 0;
        for (; i < tree.children.Length; ++i)
        {
            if (tree.children[i].threshold > parameter)
                break;
            mixer.SetInputWeight(i, 0);
        }

        if (i == tree.children.Length)
        {
            mixer.SetInputWeight(i-1, 1);
            clips[i - 1].SetSpeed(1);
            return;
        }

        //calculate animations threshold
        float a = tree.children[i - 1].threshold;
        float b = tree.children[i].threshold;
        float dif = b - a;
        float prevIWeight = 1 - (parameter - a) / dif;
        float currentIWeight = 1 - (b - parameter) / dif;
        mixer.SetInputWeight(i - 1, prevIWeight);
        mixer.SetInputWeight(i, currentIWeight);

        //calculate animations animSpeed
        a = (float)clips[i - 1].GetAnimationClip().length;
        b = (float)clips[i].GetAnimationClip().length;
        dif = b - a;
        float time = a + dif * currentIWeight;
        clips[i - 1].SetSpeed(a/time);
        clips[i].SetSpeed(b/time);

        i++;

        for (; i < tree.children.Length; i++)
        {
            mixer.SetInputWeight(i, 0);
        }
    }
}
