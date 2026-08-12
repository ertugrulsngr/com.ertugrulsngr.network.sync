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

        private void OnEnable()
        {
            _syncPositionX = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.SyncPositionX);
            _syncPositionY = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.SyncPositionY);
            _syncPositionZ = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.SyncPositionZ);
            _syncRotation = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.SyncRotation);
            _compressRotation = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.CompressRotation);
            _syncScaleX = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.SyncScaleX);
            _syncScaleY = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.SyncScaleY);
            _syncScaleZ = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.SyncScaleZ);
            _positionThreshold = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.PositionThreshold);
            _rotationAngleThreshold = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.RotationAngleThreshold);
            _scaleThreshold = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.ScaleThreshold);
            _relativePosition = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.RelativePosition);
            _relativeRotation = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.RelativeRotation);
            _relativeScale = serializedObject.FindProperty(NetworkTransformSync.PropertyNames.RelativeScale);
            _ticksPerSend = serializedObject.FindProperty("_ticksPerSend");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            EditorGUILayout.LabelField("Sync Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_ticksPerSend);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Axis to Synchronize", EditorStyles.boldLabel);
            DrawAxisRow("Position", _syncPositionX, _syncPositionY, _syncPositionZ);
            DrawAxisRow("Scale", _syncScaleX, _syncScaleY, _syncScaleZ);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_syncRotation);
            EditorGUI.BeginDisabledGroup(!_syncRotation.boolValue);
            EditorGUILayout.PropertyField(_compressRotation);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Relative to Anchor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_relativePosition);
            EditorGUILayout.PropertyField(_relativeRotation);
            EditorGUILayout.PropertyField(_relativeScale);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Thresholds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_positionThreshold);
            EditorGUILayout.PropertyField(_rotationAngleThreshold);
            EditorGUILayout.PropertyField(_scaleThreshold);

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
