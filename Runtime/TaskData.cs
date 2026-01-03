using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YanickSenn.TaskView
{
    [Serializable]
    public class TaskData
    {
        public string id = Guid.NewGuid().ToString();
        public string title = "New Task";
        public string description = "";
        public bool isCompleted = false;
        public string createdAt = DateTime.Now.ToString("O"); // ISO 8601
        public string updatedAt = DateTime.Now.ToString("O");
        public List<Object> links = new List<Object>();

        public void UpdateTimestamp()
        {
            updatedAt = DateTime.Now.ToString("O");
        }
    }
}
