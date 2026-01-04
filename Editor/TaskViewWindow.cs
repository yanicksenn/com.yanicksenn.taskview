using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using YanickSenn.TaskView;
using Object = UnityEngine.Object;

namespace YanickSenn.TaskView.Editor
{
    public class TaskViewWindow : EditorWindow
    {
        private const string DefaultTaskListPath = "Assets/DefaultTaskList.asset";

        private TaskListAsset _currentTaskList;
        private SerializedObject _serializedTaskList;
        private ReorderableList _taskReorderableList;
        private Vector2 _scrollPosition;
        
        // Task Detail Scroll
        private Vector2 _detailScrollPosition;
        
        // Splitter
        private float _splitNormalizedPosition = 0.5f;
        private bool _isResizing;

        private bool _showCompletedTasks = false;

        [MenuItem("Window/Task View")]
        public static void ShowWindow()
        {
            GetWindow<TaskViewWindow>("Task View");
        }

        private void OnEnable()
        {
            RefreshTaskListSelection();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_currentTaskList == null)
            {
                DrawNoTaskListState();
                return;
            }

            if (_serializedTaskList == null || _serializedTaskList.targetObject == null)
            {
                // Handle case where asset was deleted externally
                RefreshTaskListSelection();
                return;
            }

            _serializedTaskList.Update();

            // Calculate heights
            float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            float availableHeight = position.height - toolbarHeight;
            float listHeight = availableHeight * _splitNormalizedPosition;
            
            // Left Panel: Task List
            // We force the height here
            EditorGUILayout.BeginVertical(GUILayout.Height(listHeight));
            DrawTaskListPanel();
            EditorGUILayout.EndVertical();

            // Splitter
            DrawSplitter(availableHeight, toolbarHeight);

            // Right Panel: Task Details
            // Takes remaining space automatically
            DrawTaskDetailPanel();

            _serializedTaskList.ApplyModifiedProperties();
        }

        private void DrawSplitter(float availableHeight, float toolbarHeight)
        {
            // Draw a thin line
            GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
            
            Rect splitterRect = GUILayoutUtility.GetLastRect();
            // Create a larger invisible hit area for easier grabbing
            Rect hitRect = new Rect(splitterRect.x, splitterRect.y - 2, splitterRect.width, 6);
            
            EditorGUIUtility.AddCursorRect(hitRect, MouseCursor.ResizeVertical);

            if (Event.current.type == EventType.MouseDown && hitRect.Contains(Event.current.mousePosition))
            {
                _isResizing = true;
            }
            
            if (_isResizing)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    float mouseY = Event.current.mousePosition.y;
                    // Calculate new normalized position
                    _splitNormalizedPosition = Mathf.Clamp((mouseY - toolbarHeight) / availableHeight, 0.1f, 0.9f);
                    Repaint();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _isResizing = false;
                }
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Task List Dropdown
            var taskLists = FindAllTaskLists();
            var options = taskLists.Select(t => t.name).ToArray();
            var currentIndex = taskLists.IndexOf(_currentTaskList);

            var newIndex = EditorGUILayout.Popup(currentIndex, options, EditorStyles.toolbarPopup, GUILayout.Width(200));
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < taskLists.Count)
            {
                SelectTaskList(taskLists[newIndex]);
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshTaskListSelection();
            }

            if (GUILayout.Button("New List", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                CreateNewTaskList();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNoTaskListState()
        {
            EditorGUILayout.HelpBox("No Task List selected or found.", MessageType.Info);
            if (GUILayout.Button("Create Default Task List"))
            {
                CreateDefaultTaskList();
            }
        }

        private void DrawTaskListPanel()
        {
            EditorGUILayout.BeginVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_taskReorderableList != null)
            {
                _taskReorderableList.DoLayoutList();
            }

            EditorGUILayout.Space();

            _showCompletedTasks = EditorGUILayout.Foldout(_showCompletedTasks, "Completed Tasks", true);
            if (_showCompletedTasks && _serializedTaskList != null)
            {
                SerializedProperty tasksProp = _serializedTaskList.FindProperty("tasks");
                for (int i = 0; i < tasksProp.arraySize; i++)
                {
                    SerializedProperty taskProp = tasksProp.GetArrayElementAtIndex(i);
                    SerializedProperty isCompletedProp = taskProp.FindPropertyRelative("isCompleted");

                    if (isCompletedProp.boolValue)
                    {
                        DrawCompletedTaskRow(taskProp, i);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCompletedTaskRow(SerializedProperty taskProp, int index)
        {
            SerializedProperty titleProp = taskProp.FindPropertyRelative("title");
            SerializedProperty priorityProp = taskProp.FindPropertyRelative("priority");
            SerializedProperty isCompletedProp = taskProp.FindPropertyRelative("isCompleted");
            SerializedProperty updatedAtProp = taskProp.FindPropertyRelative("updatedAt");

            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Space(10); 

            // Done (Checkbox) - Keep enabled so it can be unchecked
            EditorGUI.BeginChangeCheck();
            bool newCompleted = EditorGUILayout.Toggle(isCompletedProp.boolValue, GUILayout.Width(25));
            if (EditorGUI.EndChangeCheck())
            {
                isCompletedProp.boolValue = newCompleted;
                updatedAtProp.stringValue = DateTime.Now.ToString("O");
            }

            // Priority and Title - Make Read-Only
            GUI.enabled = false;
            EditorGUILayout.PropertyField(priorityProp, GUIContent.none, GUILayout.Width(60));

            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = Color.gray;
            EditorGUILayout.LabelField(titleProp.stringValue, style);
            GUI.enabled = true;

            // Select button - Keep enabled
            if (GUILayout.Button(">", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                _taskReorderableList.index = index;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTaskDetailPanel()
        {
            EditorGUILayout.BeginVertical();
            _detailScrollPosition = EditorGUILayout.BeginScrollView(_detailScrollPosition);

            if (_taskReorderableList != null && _taskReorderableList.index >= 0 && _taskReorderableList.index < _currentTaskList.tasks.Count)
            {
                DrawSelectedTask(_taskReorderableList.index);
            }
            else
            {
                EditorGUILayout.LabelField("Select a task to view details", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedTask(int index)
        {
            SerializedProperty tasksProp = _serializedTaskList.FindProperty("tasks");
            SerializedProperty taskProp = tasksProp.GetArrayElementAtIndex(index);

            SerializedProperty titleProp = taskProp.FindPropertyRelative("title");
            SerializedProperty descProp = taskProp.FindPropertyRelative("description");
            SerializedProperty isCompletedProp = taskProp.FindPropertyRelative("isCompleted");
            SerializedProperty priorityProp = taskProp.FindPropertyRelative("priority");
            SerializedProperty createdAtProp = taskProp.FindPropertyRelative("createdAt");
            SerializedProperty updatedAtProp = taskProp.FindPropertyRelative("updatedAt");
            SerializedProperty linksProp = taskProp.FindPropertyRelative("links");

            EditorGUILayout.LabelField("Task Details", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Header Editing
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(titleProp);
            
            EditorGUILayout.LabelField("Description");
            descProp.stringValue = EditorGUILayout.TextArea(descProp.stringValue, GUILayout.Height(100));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(priorityProp);
            EditorGUILayout.PropertyField(isCompletedProp);

            // Dates (Read Only)
            GUI.enabled = false;
            EditorGUILayout.TextField("Created", createdAtProp.stringValue);
            EditorGUILayout.TextField("Updated", updatedAtProp.stringValue);
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(linksProp, true);

            if (EditorGUI.EndChangeCheck())
            {
                // Update timestamp
                updatedAtProp.stringValue = DateTime.Now.ToString("O");
            }
        }

        private void SelectTaskList(TaskListAsset taskList)
        {
            _currentTaskList = taskList;
            if (_currentTaskList != null)
            {
                _serializedTaskList = new SerializedObject(_currentTaskList);
                InitializeReorderableList();
            }
            else
            {
                _serializedTaskList = null;
                _taskReorderableList = null;
            }
        }

        private void InitializeReorderableList()
        {
            SerializedProperty tasksProp = _serializedTaskList.FindProperty("tasks");
            _taskReorderableList = new ReorderableList(_serializedTaskList, tasksProp, true, true, true, true);

            _taskReorderableList.elementHeightCallback = (int index) =>
            {
                var element = tasksProp.GetArrayElementAtIndex(index);
                var isCompletedProp = element.FindPropertyRelative("isCompleted");
                if (isCompletedProp.boolValue)
                {
                    return 0;
                }
                return EditorGUIUtility.singleLineHeight + 6; // Standard height + padding
            };

            _taskReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                float doneWidth = 25f;
                float priorityWidth = 60f;
                float titleWidth = rect.width - doneWidth - priorityWidth - 10f; // -10 for spacing

                float x = rect.x;
                
                EditorGUI.LabelField(new Rect(x, rect.y, doneWidth, rect.height), "Done");
                x += doneWidth + 5f;
                
                EditorGUI.LabelField(new Rect(x, rect.y, priorityWidth, rect.height), "Priority");
                x += priorityWidth + 5f;
                
                EditorGUI.LabelField(new Rect(x, rect.y, titleWidth, rect.height), "Title");
            };

            _taskReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = tasksProp.GetArrayElementAtIndex(index);
                var isCompletedProp = element.FindPropertyRelative("isCompleted");

                // Filter check
                if (isCompletedProp.boolValue)
                {
                    return;
                }

                var titleProp = element.FindPropertyRelative("title");
                var priorityProp = element.FindPropertyRelative("priority");

                // Draw background based on priority
                // P0 = 0 (Red), P1 = 1 (Yellow), P2 = 2 (None)
                if (priorityProp.enumValueIndex == 0) 
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.3f, 0.3f, 0.2f));
                }
                else if (priorityProp.enumValueIndex == 1)
                {
                    EditorGUI.DrawRect(rect, new Color(1f, 0.9f, 0.2f, 0.2f));
                }

                rect.y += 2;
                float height = EditorGUIUtility.singleLineHeight;
                
                float doneWidth = 25f;
                float priorityWidth = 60f;
                float titleWidth = rect.width - doneWidth - priorityWidth - 10f;

                float x = rect.x;

                // Checkbox
                EditorGUI.BeginChangeCheck();
                bool newCompleted = EditorGUI.Toggle(new Rect(x, rect.y, doneWidth, height), isCompletedProp.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    isCompletedProp.boolValue = newCompleted;
                    element.FindPropertyRelative("updatedAt").stringValue = DateTime.Now.ToString("O");
                }
                x += doneWidth + 5f;

                // Priority
                EditorGUI.PropertyField(new Rect(x, rect.y, priorityWidth, height), priorityProp, GUIContent.none);
                x += priorityWidth + 5f;

                // Title
                // Strikethrough style if completed
                var style = new GUIStyle(EditorStyles.label);
                if (newCompleted)
                {
                    style.normal.textColor = Color.gray;
                }
                
                EditorGUI.LabelField(new Rect(x, rect.y, titleWidth, height), titleProp.stringValue, style);
            };

            _taskReorderableList.onAddCallback = (ReorderableList list) =>
            {
                var index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;
                
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("title").stringValue = "New Task";
                element.FindPropertyRelative("description").stringValue = "";
                element.FindPropertyRelative("isCompleted").boolValue = false;
                element.FindPropertyRelative("priority").enumValueIndex = (int)TaskPriority.P2;
                element.FindPropertyRelative("createdAt").stringValue = DateTime.Now.ToString("O");
                element.FindPropertyRelative("updatedAt").stringValue = DateTime.Now.ToString("O");
                element.FindPropertyRelative("id").stringValue = Guid.NewGuid().ToString();
                element.FindPropertyRelative("links").ClearArray();
            };
        }

        private void RefreshTaskListSelection()
        {
            var lists = FindAllTaskLists();
            
            // Auto-create default if nothing exists
            if (lists.Count == 0)
            {
                CreateDefaultTaskList();
                lists = FindAllTaskLists(); // Refresh list
            }

            if (lists.Count > 0)
            {
                // Try to keep current selection
                if (_currentTaskList != null && lists.Contains(_currentTaskList))
                {
                    SelectTaskList(_currentTaskList); 
                }
                else
                {
                    // Prefer one marked as default
                    var defaultList = lists.FirstOrDefault(t => t.isDefault);
                    SelectTaskList(defaultList != null ? defaultList : lists[0]);
                }
            }
            else
            {
                // Should not happen due to auto-create, but safe fallback
                SelectTaskList(null);
            }
        }

        private List<TaskListAsset> FindAllTaskLists()
        {
            var guids = AssetDatabase.FindAssets("t:TaskListAsset");
            var lists = new List<TaskListAsset>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TaskListAsset>(path);
                if (asset != null)
                {
                    lists.Add(asset);
                }
            }
            return lists;
        }

        private void CreateDefaultTaskList()
        {
            var asset = CreateInstance<TaskListAsset>();
            asset.isDefault = true;
            // Ensure Assets directory exists (it always does in Unity projects, but good practice)
            AssetDatabase.CreateAsset(asset, DefaultTaskListPath);
            AssetDatabase.SaveAssets();
            // No recursive call to RefreshTaskListSelection here to avoid potential loops, 
            // logic handles selection after this returns.
        }

        private void CreateNewTaskList()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create New Task List", "New Task List", "asset", "Create a new task list");
            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<TaskListAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            RefreshTaskListSelection();
            SelectTaskList(asset);
        }
    }
}
