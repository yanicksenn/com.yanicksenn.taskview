using System.Collections.Generic;
using UnityEngine;

namespace YanickSenn.TaskView
{
    [CreateAssetMenu(fileName = "New Task List", menuName = "Task View/Task List")]
    public class TaskListAsset : ScriptableObject
    {
        public bool isDefault = false;
        public List<TaskData> tasks = new List<TaskData>();
    }
}
