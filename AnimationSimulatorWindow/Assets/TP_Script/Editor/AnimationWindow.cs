using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class AnimationWindow : EditorWindow
{
    private static readonly string[] stateStr = { "Play", "Pause", "Stop" };

    //GUI Data
    private int selectedAnimator =0;
    private int selectedAnimation =0;
    private int readerState = 3;
    private bool loopAnimation = false;
    private bool inPlace = false;
    private float playRate = 1;
    private float param1Slider = 0;
    private GUIContent paramContent = new GUIContent();
    private GUIStyle style = new GUIStyle();

    private AnimatorData[] animatorsData;
    private string[] animatorDropdownStr;
    private float animTimer = 0;
    private float lastTime = 0;


    [MenuItem("Window/AnimationViewer")]
    public static void OpenWindow()
    {
        AnimationWindow myWindow = GetWindow<AnimationWindow>("AnimationViewer");
        myWindow.autoRepaintOnSceneChange = true;

    }


    private void OnEnable()
    {
        Refresh();
        if(animatorsData.Length>0)
            Selection.activeObject = animatorsData[selectedAnimator]._animator.gameObject;
        EditorApplication.playModeStateChanged += OnPlayModeChange;
        EditorApplication.hierarchyChanged += Refresh;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        readerState = 2;
        style.normal.textColor = new Color(0.8f,0.8f,0.8f);
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChange;
        EditorApplication.hierarchyChanged -= Refresh;
        Stop();
    }


    private void OnGUI()
    {
        if(Application.isPlaying)
        {
            EditorGUILayout.LabelField("Exit play mode");
            return;
        }

        if (GUILayout.Button("Refresh"))
        {
            Refresh();
        }

        EditorGUILayout.Space();

        if(animatorDropdownStr.Length<=0)
        {
            EditorGUILayout.LabelField("No Animator found");
            return;
        }

        int temp = EditorGUILayout.Popup(selectedAnimator, animatorDropdownStr);
        if (temp != selectedAnimator)
        {
            Selection.activeObject = animatorsData[temp]._animator.gameObject;
            selectedAnimator = temp;
            selectedAnimation = 0;
            Stop();
        }

        if (animatorsData[selectedAnimator]._animationsData.Count <= 0)
        {
            EditorGUILayout.LabelField("Empty Animator");
            return;
        }
        EditorGUILayout.Space();

        temp = EditorGUILayout.Popup(selectedAnimation, animatorsData[selectedAnimator]._animationStr);

        AnimationData newAnimData = animatorsData[selectedAnimator]._animationsData[temp];

        if (temp != selectedAnimation)
        {
            animTimer = 0;
            selectedAnimation = temp;
            lastTime = Time.realtimeSinceStartup;
        }

        EditorGUILayout.Space();

        Animator animator = animatorsData[selectedAnimator]._animator;

        if (!newAnimData._isClip)
        {
            BlendTree tree = newAnimData._blendTree.tree;
            if (newAnimData._blendTree.tree.blendType == BlendTreeType.Direct)
            {
                GUILayout.Label("Direct BlendTree not implemented");
                #region PreviousBlendMethode
                //foreach (var directParam in DirectBlendTreeChild)
                //{
                //    paramContent.text = directParam;
                //    float blendSlider = EditorGUILayout.Slider(paramContent, animator.GetFloat(directParam), 0, 1);
                //    animator.SetFloat(directParam, blendSlider);
                //}
                #endregion
            }
            else
            {
                if (tree.blendType == BlendTreeType.Simple1D)
                {
                    OneDBlend Blend1D = newAnimData._blendTree as OneDBlend;
                    paramContent.text = tree.blendParameter;
                    param1Slider = EditorGUILayout.Slider(paramContent, param1Slider, Blend1D.minValue, Blend1D.maxValue);
                    Blend1D.UpdateMixer(param1Slider);
                }
                else
                {
                    GUILayout.Label("2D BlendTree not implemented");
                    #region PreviousBlendMethode
                    //paramContent.text = tree.blendParameterY;
                    //blendSlider = EditorGUILayout.Slider(paramContent, animator.GetFloat(tree.blendParameterY), 0, 1);
                    //animator.SetFloat(tree.blendParameterY, blendSlider);
                    #endregion
                }
            }
        }

        temp = GUILayout.SelectionGrid(readerState, stateStr, 3);
        if (temp != readerState)
        {
            readerState = temp;
            switch (readerState)
            {
                case 0:
                    Play();
                    break;
                case 2:
                    Stop();
                    break;
            }
        }

        //Animation Options
        GUILayout.BeginHorizontal();
        GUILayout.Space(position.width*0.1f);
        loopAnimation = GUILayout.Toggle(loopAnimation, "Looping");
        inPlace = GUILayout.Toggle(inPlace, "In Place");
        style.alignment = TextAnchor.MiddleRight;
        style.fontSize = 13;
        GUILayout.Label("Play rate", style,GUILayout.Height(20));
        playRate = EditorGUILayout.FloatField(playRate,GUILayout.Width(50));
        GUILayout.Space(10);
        GUILayout.EndHorizontal();

        //Animation time Slider
        float max = animatorsData[selectedAnimator]._animationsData[selectedAnimation].Duration;
        float tempf = GUILayout.HorizontalSlider(animTimer, 0, max);
        if (tempf != animTimer)
        {
            animTimer = tempf;
            readerState = 1;
            if (!AnimationMode.InAnimationMode())
                Play();
        }

        //Animation Slier info
        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        style.alignment = TextAnchor.MiddleLeft;
        GUILayout.Label("0",style,GUILayout.Width(position.width/3));
        style.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label((animTimer%max).ToString(),style, GUILayout.Width(position.width / 3));
        style.alignment = TextAnchor.MiddleRight;
        GUILayout.Label(max.ToString(),style, GUILayout.Width(position.width / 3));
        GUILayout.EndHorizontal();
    }

    private void Update()
    {
        if (Application.isPlaying)
            return;

        if (animatorsData.Length <= selectedAnimator || animatorsData[selectedAnimator]._animationsData.Count <= selectedAnimation)
            return;

        float deltaTime = Time.realtimeSinceStartup - lastTime;
        Animator animator = animatorsData[selectedAnimator]._animator;
        AnimationData animationData = animatorsData[selectedAnimator]._animationsData[selectedAnimation];

        switch (readerState)
        {
            //Play
            case 0:
                if (!loopAnimation && animTimer >= animationData.Duration)
                {
                    Stop();
                }
                if (AnimationMode.InAnimationMode())
                {
                    animTimer += deltaTime * playRate;
                    if (animationData._isClip)
                    {
                        if (loopAnimation)
                            animTimer %= animationData.Duration;
                        AnimationMode.SampleAnimationClip(animator.gameObject, animationData._clip, animTimer);
                    }
                    else
                    {
                        AnimationMode.SamplePlayableGraph(animationData._blendTree._graph, 0, animTimer);
                    }
                    if (inPlace)
                        animator.transform.localPosition = Vector3.zero;
                }
                break;
            //Pause
            case 1:
                if (AnimationMode.InAnimationMode())
                {
                    if (animationData._isClip)
                        AnimationMode.SampleAnimationClip(animator.gameObject, animationData._clip, animTimer);
                    else
                    {
                        AnimationMode.SamplePlayableGraph(animationData._blendTree._graph, 0, animTimer);
                    }
                    if (inPlace)
                        animator.transform.localPosition = Vector3.zero;
                }
                break;
            //Stop
            case 2:
                break;
        }
        lastTime = Time.realtimeSinceStartup;
    }

    private void Stop()
    {
        if (AnimationMode.InAnimationMode())
        {
            AnimationMode.EndSampling();
            AnimationMode.StopAnimationMode();
        }
        readerState = 2;
        animTimer = 0;
    }

    private void Play()
    {
        if (!AnimationMode.InAnimationMode())
        {
            AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            readerState = 0;
        }
    }

    private void Refresh()
    {
        Stop();
        Animator[] animators = SceneAsset.FindObjectsOfType<Animator>();
        int animatorCount = animators.Length;
        animatorsData = new AnimatorData[animatorCount];
        animatorDropdownStr = new string[animatorCount];

        if (selectedAnimator >= animatorCount)
            selectedAnimator = animatorCount==0? 0 : animators.Length - 1;

        for (int i = 0; i < animators.Length; i++)
        {
            animatorsData[i] = new AnimatorData();
            animatorsData[i].SetAnimator(animators[i]);
            animatorDropdownStr[i] = animators[i].name;
        }
    }

    private void Clear()
    {
        animatorsData = new AnimatorData[0];
        animatorDropdownStr = new string[0];
        Stop();
    }
    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        Clear();
        Refresh();
        if (animatorsData.Length > 0)
            Selection.activeObject = animatorsData[selectedAnimator]._animator.gameObject;
    }

    private void OnPlayModeChange(PlayModeStateChange obj)
    {
        switch (obj)
        {
            case PlayModeStateChange.EnteredEditMode:
                break;
            case PlayModeStateChange.ExitingEditMode:
                Clear();
                break;
            case PlayModeStateChange.EnteredPlayMode:
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
            default:
                break;
        }
    }

}


class AnimatorData
{
    public Animator _animator;
    public string[] _animationStr;
    public List<AnimationData> _animationsData = new List<AnimationData>();

    public void SetAnimator(Animator animator)
    {
        _animator = animator;

        var controller = (_animator.runtimeAnimatorController as AnimatorController);
        if (controller == null)
        {
            Debug.LogWarning("Animator Controller must not be null.");
            return;
        }

        foreach (var clip in controller.animationClips)
        {
            _animationsData.Add(new AnimationData(clip));
        }

        List<BlendTree> trees = new List<BlendTree>();
        GetBlendTrees(trees, controller.layers[0].stateMachine);

        foreach (var blendTree in trees)
        {
            _animationsData.Add(new AnimationData(blendTree,animator));
        }

        _animationStr = new string[_animationsData.Count];
        for (int i = 0; i < _animationsData.Count; i++)
        {
            _animationStr[i] = _animationsData[i]._name;
        }
    }

    private void GetBlendTrees(List<BlendTree> trees, AnimatorStateMachine stateMachine)
    {
        foreach (var item in stateMachine.states)
        {
            BlendTree tree = item.state.motion as BlendTree;
            if(tree)
                trees.Add(tree);
        }
        foreach (var item in stateMachine.stateMachines)
        {
            GetBlendTrees(trees, item.stateMachine);
        }
    }
}

class AnimationData
{
    public string _name = "";
    public bool _isClip = true;
    //Clip
    public AnimationClip _clip = null;
    //Behaviour
    public CustomBlendTree _blendTree;

    public bool IsLooping
    {
        get
        {
            if (_isClip)
                return _clip.isLooping;
            else
                return true;
        }
    }
    public float Duration
    {
        get
        {
            if (_isClip)
                return _clip.averageDuration;
            else
                return _blendTree.tree.averageDuration;
        }
    }

    public AnimationData(AnimationClip clip)
    {
        _name = clip.name;
        _clip = clip;
    }
    public AnimationData(BlendTree blendTree ,Animator animator)
    {
        _name = blendTree.name;
        _isClip = false;
        if (blendTree.blendType == BlendTreeType.Simple1D)
            _blendTree = new OneDBlend(blendTree, animator);
    }
}
