using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public abstract class CustomBlendTree
{
    public PlayableGraph _graph;
    public AnimationMixerPlayable mixer;
    public BlendTree tree;
    protected AnimationClipPlayable[] clips;

    public CustomBlendTree(BlendTree blendTree, Animator animator)
    {
        tree = blendTree;
        //Create Graph
        _graph = PlayableGraph.Create();

        //Create Graph Output
        var playbleOutput = AnimationPlayableOutput.Create(_graph, "Animation", animator);

        //Create all Clips
        AnimationClipPlayable[] clips = SetupClip(blendTree);

        //Create Mixer
        mixer = AnimationMixerPlayable.Create(_graph, blendTree.children.Length);

        playbleOutput.SetSourcePlayable(mixer);

        //Link Clip to Mixer
        for (int i = 0; i < clips.Length; i++)
        {
            _graph.Connect(clips[i], 0, mixer, i);
        }
    }

    ~CustomBlendTree()
    {
        _graph.Destroy();
    }

    public abstract AnimationClipPlayable[] SetupClip(BlendTree blendTree);
}
