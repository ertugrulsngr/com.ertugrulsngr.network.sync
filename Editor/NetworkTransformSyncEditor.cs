using NetworkSync.Transform;
using UnityEditor;
using UnityEngine;

namespace NetworkSync.Editor
{
    [CustomEditor(typeof(NetworkTransformSync), true)]
    public class NetworkTransformSyncEditor : UnityEditor.Editor
    {
        private const float ToggleOffset = 45f;

        private SerializedProperty _syncPositionX;
        private SerializedProperty _syncPositionY;
        private SerializedProperty _syncPositionZ;
        private SerializedProperty _syncRotation;
        private SerializedProperty _compressRotation;
        private SerializedProperty _syncScaleX;
        private SerializedProperty _syncScaleY;
        private SerializedProperty _syncScaleZ;
        private SerializedProperty _positionThreshold;
        private SerializedProperty _rotationAngleThreshold;
        private SerializedProperty _scaleThreshold;
        private SerializedProperty _relativePosition;
        private SerializedProperty _relativeRotation;
        private SerializedProperty _relativeScale;
        private SerializedProperty _ticksPerSend;
        private SerializedProperty _bufferCapacity;
        private SerializedProperty _positionLerpSmoothing;
        private SerializedProperty _positionMaxInterpolationTime;
        private SerializedProperty _rotationLerpSmoothing;
        private SerializedProperty _rotationMaxInterpolationTime;
        private SerializedProperty _scaleLerpSmoothing;
        private SerializedProperty _scaleMaxInterpolationTime;

        private bool _generalFoldout = true;
        private bool _axisFoldout = true;
        private bool _relativeFoldout = true;
        private bool _thresholdsFoldout = true;
        private bool _lerpSmoothingFoldout = true;

        private void OnEnable()
        {
            _syncPositionX = serializedObject.FindProperty(nameof(NetworkTransformSync.SyncPositionX));
            _syncPositionY = serializedObject.FindProperty(nameof(NetworkTransformSync.SyncPositionY));
            _syncPositionZ = serializedObject.FindProperty(nameof(NetworkTransformSync.SyncPositionZ));
            _syncRotation = serializedObject.FindProperty(nameof(NetworkTransformSync.SyncRotation));
            _compressRotation = serializedObject.FindProperty(nameof(NetworkTransformSync.CompressRotation));
            _syncScaleX = serializedObject.FindProperty(nameof(NetworkTransformSync.SyncScaleX));
            _syncScaleY = serializedObject.FindProperty(nameof(NetworkTransformSync.SyncScaleY));
            _syncScaleZ = serializedObject.FindProperty(nameof(NetworkTransformSync.SyncScaleZ));
            _positionThreshold = serializedObject.FindProperty(nameof(NetworkTransformSync.PositionThreshold));
            _rotationAngleThreshold = serializedObject.FindProperty(nameof(NetworkTransformSync.RotationAngleThreshold));
            _scaleThreshold = serializedObject.FindProperty(nameof(NetworkTransformSync.ScaleThreshold));
            _relativePosition = serializedObject.FindProperty(nameof(NetworkTransformSync.RelativePosition));
            _relativeRotation = serializedObject.FindProperty(nameof(NetworkTransformSync.RelativeRotation));
            _relativeScale = serializedObject.FindProperty(nameof(NetworkTransformSync.RelativeScale));
            _ticksPerSend = serializedObject.FindProperty(nameof(NetworkTransformSync.TicksPerSend));
            _bufferCapacity = serializedObject.FindProperty(nameof(NetworkTransformSync.BufferCapacity));
            _positionLerpSmoothing = serializedObject.FindProperty(nameof(NetworkTransformSync.PositionLerpSmoothing));
            _positionMaxInterpolationTime = serializedObject.FindProperty(nameof(NetworkTransformSync.PositionMaxInterpolationTime));
            _rotationLerpSmoothing = serializedObject.FindProperty(nameof(NetworkTransformSync.RotationLerpSmoothing));
            _rotationMaxInterpolationTime = serializedObject.FindProperty(nameof(NetworkTransformSync.RotationMaxInterpolationTime));
            _scaleLerpSmoothing = serializedObject.FindProperty(nameof(NetworkTransformSync.ScaleLerpSmoothing));
            _scaleMaxInterpolationTime = serializedObject.FindProperty(nameof(NetworkTransformSync.ScaleMaxInterpolationTime));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            _generalFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_generalFoldout, "General");
            if (_generalFoldout)
            {
                EditorGUILayout.PropertyField(_ticksPerSend);
                EditorGUILayout.PropertyField(_bufferCapacity);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _axisFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_axisFoldout, "Axis to Synchronize");
            if (_axisFoldout)
            {
                DrawAxisRow("Position", _syncPositionX, _syncPositionY, _syncPositionZ);
                DrawAxisRow("Scale", _syncScaleX, _syncScaleY, _syncScaleZ);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_syncRotation);
                EditorGUI.BeginDisabledGroup(!_syncRotation.boolValue);
                EditorGUILayout.PropertyField(_compressRotation);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _relativeFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_relativeFoldout, "Relative to Anchor");
            if (_relativeFoldout)
            {
                EditorGUILayout.PropertyField(_relativePosition);
                EditorGUILayout.PropertyField(_relativeRotation);
                EditorGUILayout.PropertyField(_relativeScale);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _thresholdsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_thresholdsFoldout, "Thresholds");
            if (_thresholdsFoldout)
            {
                EditorGUILayout.PropertyField(_positionThreshold);
                EditorGUILayout.PropertyField(_rotationAngleThreshold);
                EditorGUILayout.PropertyField(_scaleThreshold);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            _lerpSmoothingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_lerpSmoothingFoldout, "Lerp Smoothing");
            if (_lerpSmoothingFoldout)
            {
                EditorGUILayout.PropertyField(_positionLerpSmoothing);
                EditorGUI.BeginDisabledGroup(!_positionLerpSmoothing.boolValue);
                EditorGUILayout.PropertyField(_positionMaxInterpolationTime);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(_rotationLerpSmoothing);
                EditorGUI.BeginDisabledGroup(!_rotationLerpSmoothing.boolValue);
                EditorGUILayout.PropertyField(_rotationMaxInterpolationTime);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(_scaleLerpSmoothing);
                EditorGUI.BeginDisabledGroup(!_scaleLerpSmoothing.boolValue);
                EditorGUILayout.PropertyField(_scaleMaxInterpolationTime);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            }
        }

        private static void DrawAxisRow(string label, SerializedProperty x, SerializedProperty y, SerializedProperty z)
        {
            Rect row = EditorGUILayout.GetControlRect();
            row = EditorGUI.PrefixLabel(row, GUIUtility.GetControlID(FocusType.Keyboard, row), new GUIContent(label));
            row.width = ToggleOffset;

            x.boolValue = EditorGUI.ToggleLeft(row, "X", x.boolValue);
            row.x += ToggleOffset;
            y.boolValue = EditorGUI.ToggleLeft(row, "Y", y.boolValue);
            row.x += ToggleOffset;
            z.boolValue = EditorGUI.ToggleLeft(row, "Z", z.boolValue);
        }
    }
}
