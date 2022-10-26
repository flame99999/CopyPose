using System;
using UnityEditor;
using UnityEngine;

namespace FlameArthur.Editor
{
    public class CopyPoseWindow : EditorWindow
    {
        [SerializeField] private Transform sourceModel;
        [SerializeField] private Transform[] targetModels;
        [SerializeField] private bool humanoidMapping;

        private Transform _sourceRootBoneTran;
        private Transform _targetRootBoneTran;
        private SerializedObject _serializedObject;
        private SerializedProperty _sourceModelProperty;
        private SerializedProperty _targetModelsProperty;
        private SerializedProperty _humanoidMappingProperty;


        [MenuItem("Window/Copy Pose")]
        public static void ShowWindow()
        {
            GetWindow<CopyPoseWindow>(true, "CopyPose");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _sourceModelProperty = _serializedObject.FindProperty("sourceModel");
            _targetModelsProperty = _serializedObject.FindProperty("targetModels");
            _humanoidMappingProperty = _serializedObject.FindProperty("humanoidMapping");
        }

        private void OnGUI()
        {
            EditorGUILayout.PropertyField(_sourceModelProperty);
            EditorGUILayout.PropertyField(_targetModelsProperty);
            EditorGUILayout.PropertyField(_humanoidMappingProperty);
            if (GUILayout.Button("Copy pose"))
            {
                if (sourceModel == null)
                {
                    EditorUtility.DisplayDialog("No Source Model",
                        $"Source model has not been set", "OK");
                    return;
                }

                if (humanoidMapping)
                {
                    CopyPoseHumanoid();
                }
                else
                {
                    CopyPoseGeneral();
                }
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void CopyPoseGeneral()
        {
            var sourceSkinnedMeshRenderer = sourceModel.GetComponentInChildren<SkinnedMeshRenderer>();
            _sourceRootBoneTran = sourceSkinnedMeshRenderer.rootBone;
            foreach (var targetModelRoot in targetModels)
            {
                if (targetModelRoot == null) continue;
                var targetSkinnedMeshRenderer = targetModelRoot.GetComponentInChildren<SkinnedMeshRenderer>();
                _targetRootBoneTran = targetSkinnedMeshRenderer.rootBone;
                foreach (Transform boneTran in _sourceRootBoneTran)
                {
                    CopyTransform("", boneTran);
                }

                EditorUtility.SetDirty(targetModelRoot.gameObject);
            }
        }

        private void CopyPoseHumanoid()
        {
            var sourceAnimator = sourceModel.GetComponentInChildren<Animator>();
            if (sourceAnimator == null)
            {
                EditorUtility.DisplayDialog("No Animator",
                    $"sourceModel {sourceModel.name} doesn't have an animator with avatar assigned. Can't use humanoid mapping.",
                    "OK");
                return;
            }

            if (!sourceAnimator.isHuman)
            {
                EditorUtility.DisplayDialog("Not humanoid",
                    $"sourceModel {sourceModel.name} is not set as humanoid", "OK");
                return;
            }

            foreach (var targetModelRoot in targetModels)
            {
                if (targetModelRoot == null) continue;
                var targetAnimator = targetModelRoot.GetComponentInChildren<Animator>();
                var humanBoneTypes = Enum.GetValues(typeof(HumanBodyBones)) as HumanBodyBones[];
                for (var i = 0; i < humanBoneTypes.Length - 1; i++)
                {
                    var sourceBone = sourceAnimator.GetBoneTransform(humanBoneTypes[i]);
                    var targetBone = targetAnimator.GetBoneTransform(humanBoneTypes[i]);
                    if (sourceBone == null || targetBone == null) continue;
                    targetBone.localRotation = sourceBone.localRotation;
                    targetBone.localPosition = sourceBone.localPosition;
                }
            }
        }


        private void CopyTransform(string parentPath, Transform curTran)
        {
            var targetTran = _targetRootBoneTran.Find($"{parentPath}{curTran.name}");
            if (targetTran != null)
            {
                targetTran.localRotation = curTran.localRotation;
                targetTran.localPosition = curTran.localPosition;
            }

            foreach (Transform childBoneTran in curTran)
            {
                CopyTransform($"{parentPath}{curTran.name}/", childBoneTran);
            }
        }
    }
}
