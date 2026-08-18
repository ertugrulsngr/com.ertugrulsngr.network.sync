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
        private SerializedProperty _autoAnchorFromParent;
        private SerializedProperty _ticksPerSend;
        private SerializedProperty _bufferCapacity;
        private SerializedProperty _sendStage;
        private SerializedProperty _interpolationStage;
        private SerializedProperty _smoothPosition;
        private SerializedProperty _positionSmoothTime;
        private SerializedProperty _smoothRotation;
        private SerializedProperty _rotationSmoothTime;
        private SerializedProperty _smoothScale;
        private SerializedProperty _scaleSmoothTime;

        private bool _generalFoldout = true;
        private bool _axisFoldout = true;
        private bool _relativeFoldout = true;
        private bool _thresholdsFoldout = true;
        private bool _smoothingFoldout = true;

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
            _autoAnchorFromParent = serializedObject.FindProperty(nameof(NetworkTransformSync.AutoAnchorFromParent));
            _ticksPerSend = serializedObject.FindProperty(nameof(NetworkTransformSync.TicksPerSend));
            _bufferCapacity = serializedObject.FindProperty(nameof(NetworkTransformSync.BufferCapacity));
            _sendStage = serializedObject.FindProperty(nameof(NetworkTransformSync.SendStage));
            _interpolationStage = serializedObject.FindProperty(nameof(NetworkTransformSync.InterpolationStage));
            _smoothPosition = serializedObject.FindProperty(nameof(NetworkTransformSync.SmoothPosition));
            _positionSmoothTime = serializedObject.FindProperty(nameof(NetworkTransformSync.PositionSmoothTime));
            _smoothRotation = serializedObject.FindProperty(nameof(NetworkTransformSync.SmoothRotation));
            _rotationSmoothTime = serializedObject.FindProperty(nameof(NetworkTransformSync.RotationSmoothTime));
            _smoothScale = serializedObject.FindProperty(nameof(NetworkTransformSync.SmoothScale));
            _scaleSmoothTime = serializedObject.FindProperty(nameof(NetworkTransformSync.ScaleSmoothTime));
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
                EditorGUILayout.PropertyField(_sendStage);
                EditorGUILayout.PropertyField(_interpolationStage);
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
                EditorGUILayout.PropertyField(_autoAnchorFromParent);
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

            _smoothingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_smoothingFoldout, "Smoothing");
            if (_smoothingFoldout)
            {
                EditorGUILayout.PropertyField(_smoothPosition);
                EditorGUI.BeginDisabledGroup(!_smoothPosition.boolValue);
                EditorGUILayout.PropertyField(_positionSmoothTime);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(_smoothRotation);
                EditorGUI.BeginDisabledGroup(!_smoothRotation.boolValue);
                EditorGUILayout.PropertyField(_rotationSmoothTime);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.PropertyField(_smoothScale);
                EditorGUI.BeginDisabledGroup(!_smoothScale.boolValue);
                EditorGUILayout.PropertyField(_scaleSmoothTime);
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
